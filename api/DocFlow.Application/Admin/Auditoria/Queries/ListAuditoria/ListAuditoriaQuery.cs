using DocFlow.Application.Admin.Auditoria.DTOs;
using DocFlow.Application.Common;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Auditoria.Queries.ListAuditoria;

public record ListAuditoriaQuery(
    int Page = 1,
    int PageSize = 20,
    Guid? UsuarioId = null,
    string? Entidad = null,
    string? Accion = null,
    DateTime? Desde = null,
    DateTime? Hasta = null,
    string? UsuarioNombre = null
) : IRequest<PagedResult<RegistroAuditoriaDto>>;

public class ListAuditoriaQueryHandler : IRequestHandler<ListAuditoriaQuery, PagedResult<RegistroAuditoriaDto>>
{
    private readonly IAuditoriaRepository _repo;

    public ListAuditoriaQueryHandler(IAuditoriaRepository repo) => _repo = repo;

    public async Task<PagedResult<RegistroAuditoriaDto>> Handle(ListAuditoriaQuery query, CancellationToken ct)
    {
        var (items, total) = await _repo.GetPaginatedAsync(
            query.Page,
            query.PageSize,
            query.UsuarioId,
            query.Entidad,
            query.Accion,
            query.Desde,
            query.Hasta,
            query.UsuarioNombre);

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

        var totalPaginas = (int)Math.Ceiling(total / (double)query.PageSize);
        return new PagedResult<RegistroAuditoriaDto>(dtos, total, query.Page, totalPaginas);
    }
}
