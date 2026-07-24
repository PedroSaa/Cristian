using System.Data;
using DocFlow.Domain.Entities.NumeracionesDocumento;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.ValueObjects;
using DocFlow.Infrastructure.Data.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocFlow.Infrastructure.Services;

/// <summary>
/// Implements <see cref="ICounterService"/> using raw SQL with <c>SELECT ... FOR UPDATE</c>
/// inside an explicit RepeatableRead transaction for atomic increments.
/// </summary>
public class CounterService : ICounterService
{
    private const string Schema = "docflow";

    private static readonly string[] ValidPeriodicidades = ["CONTINUO", "ANUAL", "MENSUAL"];

    private readonly Data.DocFlowDbContext _db;
    private readonly ILogger<CounterService> _logger;

    public CounterService(Data.DocFlowDbContext db, ILogger<CounterService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<long> NextValueAsync(CounterKey key, string periodicidad = "CONTINUO", CancellationToken ct = default)
    {
        var key2 = key.Canonicalize(); // ensure sentinel defaults
        ArgumentException.ThrowIfNullOrWhiteSpace(key2.CodigoContador);
        ArgumentException.ThrowIfNullOrWhiteSpace(key2.OrgDepCod);
        if (!ValidPeriodicidades.Contains(periodicidad))
            throw new ArgumentException($"Periodicidad debe ser CONTINUO, ANUAL o MENSUAL. Recibido: {periodicidad}", nameof(periodicidad));

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var txn = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);

            try
            {
                // 1. Lock the row by its key dimensions (excluding PeriodoRef)
                // Alias every column to the exact CounterRow property name (quoted, case-sensitive).
                // EF composes this raw query inside a subquery `d` for FirstOrDefaultAsync and references
                // columns by property name (d."Activo", d."CodigoContador"); without these aliases the
                // snake_case columns don't match and PostgreSQL throws 42703 (no existe la columna d.Activo).
                var sql = $"""
                    SELECT id AS "Id", codigo_contador AS "CodigoContador", org_dep_cod AS "OrgDepCod",
                           nivel_cod AS "NivelCod", tipo_cod AS "TipoCod", df_tipo AS "DfTipo",
                           periodicidad AS "Periodicidad", periodo_ref AS "PeriodoRef", ultimo_valor AS "UltimoValor",
                           activo AS "Activo", created_at AS "CreatedAt", updated_at AS "UpdatedAt",
                           created_by AS "CreatedBy", updated_by AS "UpdatedBy"
                    FROM {Schema}.contadores_numeracion
                    WHERE codigo_contador = @p0
                      AND org_dep_cod = @p1
                      AND nivel_cod = @p2
                      AND tipo_cod = @p3
                      AND df_tipo = @p4
                    FOR UPDATE
                    """;

                var row = await _db.Database.SqlQueryRaw<CounterRow>(sql,
                    key2.CodigoContador, key2.OrgDepCod, key2.NivelCod, key2.TipoCod, key2.DfTipo)
                    .FirstOrDefaultAsync(ct);

                if (row is null)
                    throw new KeyNotFoundException(
                        $"No se encontró un contador activo para la clave {key2}.");

                if (!row.Activo)
                    throw new InvalidOperationException(
                        $"El contador {key2} está desactivado.");

                // 2. Compute expected period reference
                var expectedPeriodoRef = ComputePeriodoRef(periodicidad);

                // 3. Check if period reset is needed
                long nextValue;
                string newPeriodoRef;

                if (periodicidad != "CONTINUO" && row.PeriodoRef != expectedPeriodoRef)
                {
                    // Reset for new period
                    nextValue = 1;
                    newPeriodoRef = expectedPeriodoRef;
                    _logger.LogInformation(
                        "Contador {Key} reseteado: PeriodoRef cambió de '{Old}' a '{New}'",
                        key2, row.PeriodoRef, newPeriodoRef);
                }
                else
                {
                    nextValue = row.UltimoValor + 1;
                    newPeriodoRef = row.PeriodoRef;
                }

                // 4. Update row
                var now = DateTime.UtcNow;
                var updateSql = $"""
                    UPDATE {Schema}.contadores_numeracion
                    SET ultimo_valor = @v, periodo_ref = @pr, updated_at = @ua
                    WHERE id = @id
                    """;

                var affected = await _db.Database.ExecuteSqlRawAsync(updateSql,
                    new Npgsql.NpgsqlParameter("@v", nextValue),
                    new Npgsql.NpgsqlParameter("@pr", newPeriodoRef),
                    new Npgsql.NpgsqlParameter("@ua", now),
                    new Npgsql.NpgsqlParameter("@id", row.Id));

                if (affected == 0)
                    throw new InvalidOperationException(
                        $"No se pudo actualizar el contador {key2} (id={row.Id}). Concurrencia inesperada.");

                await txn.CommitAsync(ct);

                _logger.LogDebug(
                    "Contador {Key} incrementado: {OldVal} → {NewVal} (periodo={Periodo})",
                    key2, row.UltimoValor, nextValue, newPeriodoRef);

                return nextValue;
            }
            catch
            {
                await txn.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        });
    }

    public async Task<ContadorNumeracion> CreateCounterAsync(CounterKey key, long valorInicial = 0, string periodicidad = "CONTINUO", CancellationToken ct = default)
    {
        var key2 = key.Canonicalize();
        ArgumentException.ThrowIfNullOrWhiteSpace(key2.CodigoContador);
        ArgumentException.ThrowIfNullOrWhiteSpace(key2.OrgDepCod);

        var periodoRef = ComputePeriodoRef(periodicidad);

        var entity = new ContadorNumeracion(
            Guid.NewGuid(),
            key2.CodigoContador,
            key2.OrgDepCod,
            key2.NivelCod,
            key2.TipoCod,
            key2.DfTipo,
            periodicidad,
            periodoRef,
            valorInicial);

        _db.ContadoresNumeracion.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException pgEx
                                            && pgEx.SqlState == "23505")
        {
            throw new InvalidOperationException(
                $"Ya existe un contador con la clave {key2} y periodo {periodoRef}.", ex);
        }

        _logger.LogInformation("Contador {Key} creado con UltimoValor={Valor} (periodo={Periodo})",
            key2, valorInicial, periodoRef);

        return entity;
    }

    public async Task<ContadorNumeracion> SetCounterValueAsync(Guid id, long valor, CancellationToken ct = default)
    {
        var entity = await _db.ContadoresNumeracion.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"No se encontró el contador con id {id}.");

        entity.EstablecerValor(valor);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Contador {Id} valor establecido a {Valor}", id, valor);
        return entity;
    }

    public async Task DeactivateCounterAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.ContadoresNumeracion.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"No se encontró el contador con id {id}.");

        entity.Desactivar();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Contador {Id} desactivado", id);
    }

    public async Task<ContadorNumeracion> ReactivateCounterAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.ContadoresNumeracion.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"No se encontró el contador con id {id}.");

        entity.Activar();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Contador {Id} reactivado", id);
        return entity;
    }

    public async Task<ContadorNumeracion> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.ContadoresNumeracion.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"No se encontró el contador con id {id}.");
    }

    public async Task<int> GetCountAsync(bool? activo = null, string? codigoContador = null, string? orgDepCod = null, CancellationToken ct = default)
    {
        var query = _db.ContadoresNumeracion.AsNoTracking();

        if (activo.HasValue)
            query = query.Where(c => c.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(codigoContador))
            query = query.Where(c => c.CodigoContador == codigoContador);

        if (!string.IsNullOrWhiteSpace(orgDepCod))
            query = query.Where(c => c.OrgDepCod == orgDepCod);

        return await query.CountAsync(ct);
    }

    public async Task<IReadOnlyList<ContadorNumeracion>> GetPaginatedAsync(int page, int pageSize, bool? activo = null, string? codigoContador = null, string? orgDepCod = null, CancellationToken ct = default)
    {
        var query = _db.ContadoresNumeracion.AsNoTracking();

        if (activo.HasValue)
            query = query.Where(c => c.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(codigoContador))
            query = query.Where(c => c.CodigoContador == codigoContador);

        if (!string.IsNullOrWhiteSpace(orgDepCod))
            query = query.Where(c => c.OrgDepCod == orgDepCod);

        return await query
            .OrderBy(c => c.CodigoContador)
            .ThenBy(c => c.OrgDepCod)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    // ── Helpers ──

    private static string ComputePeriodoRef(string periodicidad)
    {
        var now = DateTime.UtcNow;
        return periodicidad switch
        {
            "ANUAL" => now.Year.ToString(),
            "MENSUAL" => $"{now.Year}-{now.Month:D2}",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Internal DTO for raw SQL row mapping.
    /// </summary>
    private sealed record CounterRow
    {
        public Guid Id { get; init; }
        public string CodigoContador { get; init; } = string.Empty;
        public string OrgDepCod { get; init; } = string.Empty;
        public string NivelCod { get; init; } = string.Empty;
        public int TipoCod { get; init; }
        public string DfTipo { get; init; } = string.Empty;
        public string Periodicidad { get; init; } = string.Empty;
        public string PeriodoRef { get; init; } = string.Empty;
        public long UltimoValor { get; init; }
        public bool Activo { get; init; }
        public DateTime CreatedAt { get; init; }
        public DateTime UpdatedAt { get; init; }
        public string CreatedBy { get; init; } = string.Empty;
        public string UpdatedBy { get; init; } = string.Empty;
    }
}
