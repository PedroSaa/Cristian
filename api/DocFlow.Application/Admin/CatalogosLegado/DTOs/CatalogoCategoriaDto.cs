namespace DocFlow.Application.Admin.CatalogosLegado.DTOs;

public record CatalogoCategoriaDto(
    int CatCod,
    string CatDesc,
    int TotalSubcategorias);
