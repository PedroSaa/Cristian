namespace DocFlow.Application.Admin.Usuarios.Firma.DTOs;

/// <summary>Binary signature image plus its content type, for download/display.</summary>
public record FirmaImagenDto(byte[] Contenido, string ContentType);
