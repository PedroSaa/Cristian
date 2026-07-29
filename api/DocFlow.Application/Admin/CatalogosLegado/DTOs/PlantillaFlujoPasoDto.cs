namespace DocFlow.Application.Admin.CatalogosLegado.DTOs;

/// <summary>
/// A single workflow step of a document template, as exposed by the API.
/// Consumed by the execution team through <c>GET {codForm}/flujo</c>.
/// </summary>
public record PlantillaFlujoPasoDto(
    Guid Id,
    int Orden,
    string TipoAccion,
    string ResponsableTipo,
    Guid ResponsableId,
    string? ResponsableNombre,
    bool Obligatorio);
