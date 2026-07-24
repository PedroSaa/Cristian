namespace DocFlow.Application.Admin.CatalogosLegado.DTOs;

public record SeForplaDto(
    string CodForm,
    string Usucod,
    short? TipoCod,
    string NomForm,
    byte[] BlobForm,
    string SisForm,
    string? ObsForm,
    string ExtForm,
    double? Alto,
    double? Ancho);
