using DocFlow.Application.Admin.Permisos.DTOs;
using DocFlow.Application.Admin.Roles.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Roles.Queries.ListRoles;

public record ListRolesQuery : IRequest<IReadOnlyList<RolDto>>;

public class ListRolesQueryHandler : IRequestHandler<ListRolesQuery, IReadOnlyList<RolDto>>
{
    private readonly IRolRepository _repo;

    public ListRolesQueryHandler(IRolRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<RolDto>> Handle(ListRolesQuery q, CancellationToken ct)
    {
        var roles = await _repo.GetAllWithPermisosAsync();
        return roles
            .Select(r =>
            {
                IReadOnlyList<PermisoDto>? permisos = null;
                if (r.RolPermisos.Count > 0)
                {
                    permisos = r.RolPermisos
                        .Select(rp => new PermisoDto(
                            rp.Permiso.Id,
                            rp.Permiso.Nombre,
                            rp.Permiso.Descripcion,
                            rp.Permiso.Grupo))
                        .ToList();
                }

                return new RolDto(r.Id, r.Nombre, r.Descripcion, r.EsSistema, permisos);
            })
            .ToList();
    }
}
