using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeForplaMedidas;

public record GetSeForplaMedidasQuery(string CodForm) : IRequest<IReadOnlyList<SeForplaMedidaDto>>;

public class GetSeForplaMedidasQueryHandler : IRequestHandler<GetSeForplaMedidasQuery, IReadOnlyList<SeForplaMedidaDto>>
{
    private readonly ISeForplaRepository _plantillas;
    private readonly ISeForplaMedidaRepository _medidas;

    public GetSeForplaMedidasQueryHandler(ISeForplaRepository plantillas, ISeForplaMedidaRepository medidas)
    {
        _plantillas = plantillas;
        _medidas = medidas;
    }

    public async Task<IReadOnlyList<SeForplaMedidaDto>> Handle(GetSeForplaMedidasQuery query, CancellationToken ct)
    {
        if (!await _plantillas.ExistsAsync(query.CodForm))
            throw new KeyNotFoundException($"Plantilla {query.CodForm} no encontrada.");

        var medidas = await _medidas.GetByCodFormAsync(query.CodForm);

        // Side effect deliberado: las plantillas creadas antes de este subsistema no tienen
        // medidas, así que la consulta siembra los 7 defaults del legacy (pingresamedida)
        // la primera vez que se piden y los devuelve recién persistidos.
        if (medidas.Count == 0)
        {
            var defaults = SeForplaMedida.CrearDefaults(query.CodForm);
            await _medidas.CreateRangeAsync(defaults);
            medidas = defaults;
        }

        return medidas
            .Select(m => new SeForplaMedidaDto(m.IdForplaMed, m.Objeto, m.X, m.Y, m.Ancho, m.Alto))
            .ToList();
    }
}
