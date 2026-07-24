using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeClaseg;

public record ListSeClasegQuery : IRequest<IReadOnlyList<SeClasegDto>>;

public class ListSeClasegQueryHandler : IRequestHandler<ListSeClasegQuery, IReadOnlyList<SeClasegDto>>
{
    private readonly ISeClasegRepository _repo;

    public ListSeClasegQueryHandler(ISeClasegRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SeClasegDto>> Handle(ListSeClasegQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items.Select(x => new SeClasegDto(x.DFClasif, x.DFNCLASIF, x.DFDClasif)).ToList();
    }
}
