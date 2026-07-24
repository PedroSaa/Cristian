using DocFlow.Application.Admin.Auditoria.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Auditoria.Queries.GetAuditoria;

public record GetAuditoriaQuery(Guid Id) : IRequest<RegistroAuditoriaDto>;

public class GetAuditoriaQueryHandler : IRequestHandler<GetAuditoriaQuery, RegistroAuditoriaDto>
{
    private readonly IAuditoriaRepository _repo;

    public GetAuditoriaQueryHandler(IAuditoriaRepository repo) => _repo = repo;

    public async Task<RegistroAuditoriaDto> Handle(GetAuditoriaQuery query, CancellationToken ct)
    {
        var result = await _repo.GetByIdWithUserAsync(query.Id)
            ?? throw new KeyNotFoundException($"Registro de auditoría {query.Id} no encontrado.");

        var r = result.Registro;
        return new RegistroAuditoriaDto(
            r.Id,
            r.UsuarioId,
            result.UsuarioNombre,
            r.Accion,
            r.Entidad,
            r.EntidadId,
            r.Detalle,
            r.DireccionIp,
            r.UserAgent,
            r.CreadoEn);
    }
}
