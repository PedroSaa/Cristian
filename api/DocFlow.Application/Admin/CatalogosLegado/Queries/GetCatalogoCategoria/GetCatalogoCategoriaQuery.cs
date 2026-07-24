using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Domain.Interfaces;
using MediatR;

namespace DocFlow.Application.Admin.CatalogosLegado.Queries.GetCatalogoCategoria;

public record GetCatalogoCategoriaQuery(int CatCod) : IRequest<CatalogoCategoriaDto>;

public class GetCatalogoCategoriaQueryHandler : IRequestHandler<GetCatalogoCategoriaQuery, CatalogoCategoriaDto>
{
    private readonly ICatalogoCategoriaRepository _repo;

    public GetCatalogoCategoriaQueryHandler(ICatalogoCategoriaRepository repo) => _repo = repo;

    public async Task<CatalogoCategoriaDto> Handle(GetCatalogoCategoriaQuery request, CancellationToken ct)
    {
        var categoria = await _repo.GetByIdAsync(request.CatCod)
            ?? throw new KeyNotFoundException($"Categoría {request.CatCod} no encontrada.");

        return new CatalogoCategoriaDto(categoria.CatCod, categoria.CatDesc, categoria.Subcategorias.Count);
    }
}
