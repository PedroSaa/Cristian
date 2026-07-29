using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Enums;
using DocFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DocFlow.Infrastructure.Repositories;

/// <summary>
/// Resolves workflow-step responsible names with a single batched query per type.
/// Best-effort: unresolved ids are simply absent from the returned map.
/// </summary>
public class ResponsableFlujoNombreResolver : IResponsableFlujoNombreResolver
{
    private readonly DocFlowDbContext _db;

    public ResponsableFlujoNombreResolver(DocFlowDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<Guid, string>> ResolverNombresAsync(
        ResponsableFlujoTipo tipo, IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return tipo switch
        {
            ResponsableFlujoTipo.Usuario => await ResolverUsuariosAsync(ids, ct),
            ResponsableFlujoTipo.Rol => await ResolverRolesAsync(ids, ct),
            ResponsableFlujoTipo.Departamento => await ResolverDepartamentosAsync(ids, ct),
            _ => new Dictionary<Guid, string>(),
        };
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolverUsuariosAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var filas = await _db.SeUsuaris
            .AsNoTracking()
            .Where(u => ids.Contains(u.UsuarioId))
            .Select(u => new
            {
                u.UsuarioId,
                u.Usucod,
                u.Personal!.Nombres,
                u.Personal!.ApellidoPaterno,
                u.Personal!.ApellidoMaterno,
            })
            .ToListAsync(ct);

        var mapa = new Dictionary<Guid, string>();
        foreach (var f in filas)
        {
            var nombre = string.Join(' ',
                new[] { f.Nombres, f.ApellidoPaterno, f.ApellidoMaterno }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
            mapa[f.UsuarioId] = string.IsNullOrWhiteSpace(nombre) ? f.Usucod : nombre;
        }

        return mapa;
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolverRolesAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        return await _db.Roles
            .AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Nombre, ct);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> ResolverDepartamentosAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        return await _db.Departamentos
            .AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Nombre, ct);
    }
}
