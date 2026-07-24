using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetCatalogoSubcategoria;

public record GetCatalogoSubcategoriaQuery(int CatCod, short IdSubcategoria) : IRequest<CatalogoSubcategoriaDto>;

public class GetCatalogoSubcategoriaQueryHandler : IRequestHandler<GetCatalogoSubcategoriaQuery, CatalogoSubcategoriaDto>
{
    private readonly ICatalogoSubcategoriaRepository _repo;

    public GetCatalogoSubcategoriaQueryHandler(ICatalogoSubcategoriaRepository repo) => _repo = repo;

    public async Task<CatalogoSubcategoriaDto> Handle(GetCatalogoSubcategoriaQuery request, CancellationToken ct)
    {
        var subcategoria = await _repo.GetByIdAsync(request.CatCod, request.IdSubcategoria)
            ?? throw new KeyNotFoundException($"Subcategoría {request.CatCod}-{request.IdSubcategoria} no encontrada.");

        return new CatalogoSubcategoriaDto(
            subcategoria.CatCod,
            subcategoria.Categoria.CatDesc,
            subcategoria.IdSubcategoria,
            subcategoria.SubcatNombre,
            subcategoria.SubcatDescripcion);
    }
}
