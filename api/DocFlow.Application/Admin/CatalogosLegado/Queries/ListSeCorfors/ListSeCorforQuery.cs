using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeCorfors;

public record ListSeCorforQuery : IRequest<IReadOnlyList<SeCorforDto>>;

public class ListSeCorforQueryHandler : IRequestHandler<ListSeCorforQuery, IReadOnlyList<SeCorforDto>>
{
    private readonly ISeCorforRepository _repo;

    public ListSeCorforQueryHandler(ISeCorforRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SeCorforDto>> Handle(ListSeCorforQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items.Select(x => new SeCorforDto(x.CorrTip, x.CorrNro, x.CorrDes, x.CorrFch)).ToList();
    }
}
