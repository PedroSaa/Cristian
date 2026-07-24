using DocFlow.Application.Admin.Permisos.DTOs;
using DocFlow.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace DocFlow.Application.Admin.Permisos.Queries;

public record ListPermisosQuery : IRequest<IReadOnlyList<PermisoDto>>;

public class ListPermisosQueryHandler : IRequestHandler<ListPermisosQuery, IReadOnlyList<PermisoDto>>
{
    private readonly IPermisoRepository _repo;

    public ListPermisosQueryHandler(IPermisoRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<PermisoDto>> Handle(ListPermisosQuery q, CancellationToken ct)
    {
        var permisos = await _repo.GetAllAsync();
        return permisos
            .Select(p => new PermisoDto(p.Id, p.Nombre, p.Descripcion, p.Grupo))
            .ToList();
    }
}

public class ListPermisosQueryValidator : AbstractValidator<ListPermisosQuery>
{
    // No fields to validate — query has no parameters
}
