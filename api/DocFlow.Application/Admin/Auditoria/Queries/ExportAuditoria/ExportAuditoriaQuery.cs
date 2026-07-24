using DocFlow.Application.Admin.Auditoria.DTOs;
using DocFlow.Application.Admin.Auditoria.Interfaces;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Auditoria.Queries.ExportAuditoria;

public record ExportAuditoriaQuery(
    Guid? UsuarioId = null,
    string? Entidad = null,
    string? Accion = null,
    DateTime? Desde = null,
    DateTime? Hasta = null
) : IRequest<byte[]>;

public class ExportAuditoriaQueryHandler : IRequestHandler<ExportAuditoriaQuery, byte[]>
{
    private readonly IAuditoriaRepository _repo;
    private readonly IAuditoriaCsvService _csv;
    internal const int MaxExportRows = 10_000;

    public ExportAuditoriaQueryHandler(IAuditoriaRepository repo, IAuditoriaCsvService csv)
    {
        _repo = repo;
        _csv = csv;
    }

    public async Task<byte[]> Handle(ExportAuditoriaQuery query, CancellationToken ct)
    {
        // First, get the total count (cheap — only WHERE + COUNT, no SKIP/TAKE)
        var (_, total) = await _repo.GetPaginatedAsync(
            page: 1,
            pageSize: 1,
            query.UsuarioId,
            query.Entidad,
            query.Accion,
            query.Desde,
            query.Hasta,
            null);

        if (total > MaxExportRows)
            throw new InvalidOperationException(
                $"La exportación está limitada a {MaxExportRows} registros. " +
                $"Los filtros actuales devuelven {total} registros. " +
                "Ajusta los filtros para reducir el resultado.");

        var (items, _) = await _repo.GetPaginatedAsync(
            page: 1,
            pageSize: MaxExportRows,
            query.UsuarioId,
            query.Entidad,
            query.Accion,
            query.Desde,
            query.Hasta,
            null);

        var dtos = items
            .Select(r => new RegistroAuditoriaDto(
                r.Registro.Id,
                r.Registro.UsuarioId,
                r.UsuarioNombre,
                r.Registro.Accion,
                r.Registro.Entidad,
                r.Registro.EntidadId,
                r.Registro.Detalle,
                r.Registro.DireccionIp,
                r.Registro.UserAgent,
                r.Registro.CreadoEn))
            .ToList();

        return _csv.GenerateCsv(dtos);
    }
}
