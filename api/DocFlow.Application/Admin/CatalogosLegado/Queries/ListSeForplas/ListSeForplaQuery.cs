using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeForplas;

public record ListSeForplaQuery : IRequest<IReadOnlyList<SeForplaDto>>;

public class ListSeForplaQueryHandler : IRequestHandler<ListSeForplaQuery, IReadOnlyList<SeForplaDto>>
{
    private readonly ISeForplaRepository _repo;

    public ListSeForplaQueryHandler(ISeForplaRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SeForplaDto>> Handle(ListSeForplaQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items.Select(x => new SeForplaDto(x.CodForm, x.Usucod, x.TipoCod, x.NomForm, x.BlobForm, x.SisForm, x.ObsForm, x.ExtForm, x.Alto, x.Ancho)).ToList();
    }
}
