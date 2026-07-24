namespace DocFlow.Application.Admin.Integraciones.DTOs;

public record IntegracionDto(
    Guid Id,
    string Nombre,
    string Tipo,
    string BaseUrl,
    string ApiKeyMasked,
    bool Activo,
    IReadOnlyDictionary<string, string> Settings);
