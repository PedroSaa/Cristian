using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.Integraciones.Queries.GetIntegracionIdByNombre;

public record GetIntegracionIdByNombreQuery(string Nombre) : IRequest<Guid>;

public class GetIntegracionIdByNombreQueryHandler
    : IRequestHandler<GetIntegracionIdByNombreQuery, Guid>
{
    private readonly IIntegracionRepository _repo;

    public GetIntegracionIdByNombreQueryHandler(IIntegracionRepository repo) => _repo = repo;

    public async Task<Guid> Handle(GetIntegracionIdByNombreQuery query, CancellationToken ct)
    {
        var integracion = await _repo.GetByNombreAsync(query.Nombre);
        if (integracion is null)
            throw new KeyNotFoundException($"Integración '{query.Nombre}' no encontrada.");

        return integracion.Id;
    }
}
