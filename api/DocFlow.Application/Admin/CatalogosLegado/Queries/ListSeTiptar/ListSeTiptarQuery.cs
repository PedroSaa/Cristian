using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeTiptar;

public record ListSeTiptarQuery : IRequest<IReadOnlyList<SeTiptarDto>>;

public class ListSeTiptarQueryHandler : IRequestHandler<ListSeTiptarQuery, IReadOnlyList<SeTiptarDto>>
{
    private readonly ISeTiptarRepository _repo;

    public ListSeTiptarQueryHandler(ISeTiptarRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SeTiptarDto>> Handle(ListSeTiptarQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items.Select(x => new SeTiptarDto(x.DFTACCION, x.DFTACOBSV, x.DFTACDESC)).ToList();
    }
}
