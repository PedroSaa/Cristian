using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetPlantillaFlujo;

/// <summary>Returns the workflow (ordered steps) configured for a document template.</summary>
public record GetPlantillaFlujoQuery(string CodForm) : IRequest<IReadOnlyList<PlantillaFlujoPasoDto>>;

public class GetPlantillaFlujoQueryHandler
    : IRequestHandler<GetPlantillaFlujoQuery, IReadOnlyList<PlantillaFlujoPasoDto>>
{
    private readonly IPlantillaFlujoRepository _flujo;
    private readonly IResponsableFlujoNombreResolver _nombres;

    public GetPlantillaFlujoQueryHandler(
        IPlantillaFlujoRepository flujo,
        IResponsableFlujoNombreResolver nombres)
    {
        _flujo = flujo;
        _nombres = nombres;
    }

    public async Task<IReadOnlyList<PlantillaFlujoPasoDto>> Handle(
        GetPlantillaFlujoQuery request, CancellationToken ct)
    {
        var pasos = await _flujo.GetByCodFormAsync(request.CodForm, ct);
        if (pasos.Count == 0)
            return [];

        // Batch-fetch names once per responsible type to avoid N+1.
        var nombresPorTipo = new Dictionary<ResponsableFlujoTipo, IReadOnlyDictionary<Guid, string>>();
        foreach (var grupo in pasos.GroupBy(p => p.ResponsableTipo))
        {
            var ids = grupo.Select(p => p.ResponsableId).Distinct().ToList();
            nombresPorTipo[grupo.Key] = await _nombres.ResolverNombresAsync(grupo.Key, ids, ct);
        }

        return pasos
            .OrderBy(p => p.Orden)
            .Select(p => Map(p, nombresPorTipo))
            .ToList();
    }

    private static PlantillaFlujoPasoDto Map(
        PlantillaFlujoPaso paso,
        IReadOnlyDictionary<ResponsableFlujoTipo, IReadOnlyDictionary<Guid, string>> nombresPorTipo)
    {
        string? nombre = null;
        if (nombresPorTipo.TryGetValue(paso.ResponsableTipo, out var mapa)
            && mapa.TryGetValue(paso.ResponsableId, out var resuelto))
        {
            nombre = resuelto;
        }

        return new PlantillaFlujoPasoDto(
            paso.Id,
            paso.Orden,
            paso.TipoAccion.ToString(),
            paso.ResponsableTipo.ToString(),
            paso.ResponsableId,
            nombre,
            paso.Obligatorio);
    }
}
