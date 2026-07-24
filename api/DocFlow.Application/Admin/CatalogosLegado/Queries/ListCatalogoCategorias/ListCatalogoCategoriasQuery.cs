using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListCatalogoCategorias;

public record ListCatalogoCategoriasQuery : IRequest<IReadOnlyList<CatalogoCategoriaDto>>;

public class ListCatalogoCategoriasQueryHandler : IRequestHandler<ListCatalogoCategoriasQuery, IReadOnlyList<CatalogoCategoriaDto>>
{
    private readonly ICatalogoCategoriaRepository _repo;

    public ListCatalogoCategoriasQueryHandler(ICatalogoCategoriaRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CatalogoCategoriaDto>> Handle(ListCatalogoCategoriasQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync();
        return items
            .Select(x => new CatalogoCategoriaDto(x.CatCod, x.CatDesc, x.Subcategorias.Count))
            .ToList();
    }
}
