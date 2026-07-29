namespace DocFlow.Application.Admin.Usuarios.Firma.DTOs;

/// <summary>
/// Signature configuration metadata (no image bytes, no decrypted PIN).
/// Lets the UI know whether a signature exists and its descriptive attributes.
/// </summary>
public record FirmaUsuarioMetadataDto(
    Guid UsuarioId,
    bool TieneFirma,
    bool TieneClave,
    string? Sigla,
    string? ContentType,
    long Tamano,
    DateTime? CreadoEn,
    DateTime? ActualizadoEn);
