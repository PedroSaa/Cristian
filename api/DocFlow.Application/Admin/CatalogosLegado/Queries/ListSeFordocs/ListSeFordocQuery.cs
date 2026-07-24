using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeFordocs;

public record ListSeFordocQuery : IRequest<IReadOnlyList<SeFordocDto>>;

public class ListSeFordocQueryHandler : IRequestHandler<ListSeFordocQuery, IReadOnlyList<SeFordocDto>>
{
    private readonly ISeFordocRepository _repo;

    public ListSeFordocQueryHandler(ISeFordocRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<SeFordocDto>> Handle(ListSeFordocQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items.Select(x => new SeFordocDto(x.TipoCod, x.TipoRec, x.TipoInt, x.TipoDesc, x.CorrN, x.CorrFecha, x.TipoEnv, x.SeFordocVistaI, x.SeFordocVistaE, x.SeFordocVistaR, x.SeFordocFormatoNum)).ToList();
    }
}
