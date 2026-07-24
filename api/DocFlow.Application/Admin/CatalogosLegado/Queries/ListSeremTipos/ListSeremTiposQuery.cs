using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeremTipos;

public record ListSeremTiposQuery : IRequest<IReadOnlyList<SeremTipoDto>>;

public class ListSeremTiposQueryHandler : IRequestHandler<ListSeremTiposQuery, IReadOnlyList<SeremTipoDto>>
{
    private readonly ISeremTipoRepository _repo;

    public ListSeremTiposQueryHandler(ISeremTipoRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SeremTipoDto>> Handle(ListSeremTiposQuery request, CancellationToken ct)
        => (await _repo.GetAllAsync())
            .Select(x => new SeremTipoDto(x.RemTipo, x.RemDesc, x.Serems.Count))
            .ToList();
}
