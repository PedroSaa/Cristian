using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.ListCatalogoSubcategorias;

public record ListCatalogoSubcategoriasQuery(int? CatCod = null) : IRequest<IReadOnlyList<CatalogoSubcategoriaDto>>;

public class ListCatalogoSubcategoriasQueryHandler : IRequestHandler<ListCatalogoSubcategoriasQuery, IReadOnlyList<CatalogoSubcategoriaDto>>
{
    private readonly ICatalogoSubcategoriaRepository _repo;

    public ListCatalogoSubcategoriasQueryHandler(ICatalogoSubcategoriaRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<CatalogoSubcategoriaDto>> Handle(ListCatalogoSubcategoriasQuery request, CancellationToken ct)
    {
        var items = await _repo.GetAllAsync(request.CatCod);
        return items
            .Select(x => new CatalogoSubcategoriaDto(
                x.CatCod,
                x.Categoria.CatDesc,
                x.IdSubcategoria,
                x.SubcatNombre,
                x.SubcatDescripcion))
            .ToList();
    }
}
