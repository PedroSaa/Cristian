using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetSeClaseg;

public record GetSeClasegQuery(short DFClasif) : IRequest<SeClasegDto>;

public class GetSeClasegQueryHandler : IRequestHandler<GetSeClasegQuery, SeClasegDto>
{
    private readonly ISeClasegRepository _repo;

    public GetSeClasegQueryHandler(ISeClasegRepository repo) => _repo = repo;

    public async Task<SeClasegDto> Handle(GetSeClasegQuery request, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(request.DFClasif)
            ?? throw new KeyNotFoundException($"Clasificación {request.DFClasif} no encontrada.");

        return new SeClasegDto(entity.DFClasif, entity.DFNCLASIF, entity.DFDClasif);
    }
}
