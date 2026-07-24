namespace DocFlow.Application.Admin.CatalogosLegado.DTOs;

public record CatalogoSubcategoriaDto(
    int CatCod,
    string CategoriaDesc,
    short IdSubcategoria,
    string SubcatNombre,
    string? SubcatDescripcion);
