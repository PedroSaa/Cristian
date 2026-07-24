using System.Text.Json.Serialization;

namespace DocFlow.Application.Auth.DTOs;

public record LoginResultDto(
    [property: JsonIgnore]
    string AccessToken,
    [property: JsonIgnore]
    string RefreshToken,
    int ExpiresIn,
    UsuarioDto User,
    [property: JsonConverter(typeof(AuthStateJsonConverter))]
    AuthState AuthState = AuthState.Normal,
    string? SetupToken = null,
    bool CanLogout = false);
