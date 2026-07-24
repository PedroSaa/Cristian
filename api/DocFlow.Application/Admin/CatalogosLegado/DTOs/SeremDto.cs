namespace DocFlow.Application.Admin.CatalogosLegado.DTOs;

public record SeremDto(
    string RemCod,
    string RemTipo,
    string RemTipoDesc,
    short? RemRutValid,
    string? RemSector,
    string RemNomb,
    string? RemComuna,
    int? RemNro,
    string? RemEmail,
    string? RemFax,
    string? RemRut,
    string? RemDirec,
    string? RemTelef,
    string? RemZip,
    string? RemRegion,
    string? RemBlock,
    string? RemCalle,
    decimal? RemCodDocDigital);
