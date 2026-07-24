using DocFlow.Application.Admin.Departamentos.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Departamentos.Queries.ListDepartamentos;

public record ListDepartamentosQuery(bool? Activo = null) : IRequest<IReadOnlyList<DepartamentoAdminDto>>;

public class ListDepartamentosQueryHandler : IRequestHandler<ListDepartamentosQuery, IReadOnlyList<DepartamentoAdminDto>>
{
    private readonly IDepartamentoRepository _repo;

    public ListDepartamentosQueryHandler(IDepartamentoRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<DepartamentoAdminDto>> Handle(ListDepartamentosQuery query, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(query.Activo);
        return items
            .Select(d => new DepartamentoAdminDto(
                d.Id,
                d.Nombre,
                d.Codigo,
                d.Activo,
                d.CreadoEn,
                d.Usuarios.Count))
            .ToList();
    }
}
