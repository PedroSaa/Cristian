using System.Text.Json.Serialization;

namespace DocFlow.Application.Auth.DTOs;

public record UsuarioDto(
    Guid Id,
    string Nombre,
    string Email,
    string? Rut,
    string Rol,
    string? RolId,
    DepartamentoDto? Departamento,
    bool Activo,
    int IntentosRestantes,
    PermisosDto Permisos,
    bool MfaEnabled,
    string[]? Permissions = null,
    [property: JsonConverter(typeof(AuthStateJsonConverter))]
    AuthState AuthState = AuthState.Normal,
    string? SetupToken = null);
