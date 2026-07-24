using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocFlow.Application.Auth.DTOs;

[JsonConverter(typeof(AuthStateJsonConverter))]
public enum AuthState
{
    Normal,
    MfaSetupRequired
}

internal sealed class AuthStateJsonConverter : JsonConverter<AuthState>
{
    public override AuthState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetString()?.Equals("mfa_setup_required", StringComparison.OrdinalIgnoreCase) == true
            ? AuthState.MfaSetupRequired
            : AuthState.Normal;

    public override void Write(Utf8JsonWriter writer, AuthState value, JsonSerializerOptions options)
        => writer.WriteStringValue(value == AuthState.MfaSetupRequired ? "mfa_setup_required" : "normal");
}
