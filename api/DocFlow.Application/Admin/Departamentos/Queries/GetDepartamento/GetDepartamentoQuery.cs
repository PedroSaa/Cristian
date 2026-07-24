using DocFlow.Application.Admin.Departamentos.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Departamentos.Queries.GetDepartamento;

public record GetDepartamentoQuery(Guid Id) : IRequest<DepartamentoAdminDto>;

public class GetDepartamentoQueryHandler : IRequestHandler<GetDepartamentoQuery, DepartamentoAdminDto>
{
    private readonly IDepartamentoRepository _repo;

    public GetDepartamentoQueryHandler(IDepartamentoRepository repo) => _repo = repo;

    public async Task<DepartamentoAdminDto> Handle(GetDepartamentoQuery query, CancellationToken ct)
    {
        var dep = await _repo.GetByIdAsync(query.Id)
            ?? throw new KeyNotFoundException($"Departamento {query.Id} no encontrado.");

        return new DepartamentoAdminDto(
            dep.Id,
            dep.Nombre,
            dep.Codigo,
            dep.Activo,
            dep.CreadoEn,
            dep.Usuarios.Count);
    }
}
