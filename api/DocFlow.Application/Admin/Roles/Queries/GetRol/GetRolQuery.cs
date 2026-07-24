using DocFlow.Application.Admin.Permisos.DTOs;
using DocFlow.Application.Admin.Roles.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Roles.Queries.GetRol;

public record GetRolQuery(Guid Id) : IRequest<RolDto>;

public class GetRolQueryHandler : IRequestHandler<GetRolQuery, RolDto>
{
    private readonly IRolRepository _repo;

    public GetRolQueryHandler(IRolRepository repo) => _repo = repo;

    public async Task<RolDto> Handle(GetRolQuery q, CancellationToken ct)
    {
        var rol = await _repo.GetByIdWithPermisosAsync(q.Id)
            ?? throw new KeyNotFoundException($"No se encontró el rol con id {q.Id}.");

        var permisos = rol.RolPermisos.Count > 0
            ? rol.RolPermisos
                .Select(rp => new PermisoDto(rp.Permiso.Id, rp.Permiso.Nombre, rp.Permiso.Descripcion, rp.Permiso.Grupo))
                .ToList()
            : null;

        return new RolDto(rol.Id, rol.Nombre, rol.Descripcion, rol.EsSistema, permisos);
    }
}
