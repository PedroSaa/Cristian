using System.Text.Json.Serialization;

namespace DocFlow.Application.Admin.CatalogosLegado.DTOs;

public record SeClasegDto(
    short DFClasif,
    [property: JsonPropertyName("dfnClasif")] string DFNCLASIF,
    string DFDClasif);
