using DocFlow.Application.Numeracion.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Numeracion.Queries.ListPlantillas;

public record ListPlantillasQuery(bool? SoloActivos = null) : IRequest<List<PlantillaNumeracionDto>>;

public class ListPlantillasHandler : IRequestHandler<ListPlantillasQuery, List<PlantillaNumeracionDto>>
{
    private readonly IPlantillaService _service;

    public ListPlantillasHandler(IPlantillaService service) => _service = service;

    public async Task<List<PlantillaNumeracionDto>> Handle(ListPlantillasQuery q, CancellationToken ct)
    {
        var items = await _service.ListarAsync(q.SoloActivos, ct);
        return items.Select(PlantillaNumeracionDto.From).ToList();
    }
}
