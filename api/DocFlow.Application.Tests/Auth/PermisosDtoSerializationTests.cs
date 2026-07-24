using System.Text.Json;
using DocFlow.Application.Auth.DTOs;
using FluentAssertions;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class PermisosDtoSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public void Serialize_WithReportesVer_EmitsString()
    {
        var dto = new PermisosDto(true, true, true, true, true, "ver");

        var json = JsonSerializer.Serialize(dto, Options);

        json.Should().Contain("\"reportes\":\"ver\"");
    }

    [Fact]
    public void Serialize_WithReportesNone_EmitsString()
    {
        var dto = new PermisosDto(true, true, true, true, true, "none");

        var json = JsonSerializer.Serialize(dto, Options);

        json.Should().Contain("\"reportes\":\"none\"");
    }

    [Fact]
    public void Serialize_WithBandejaActions_EmitsGranularFlags()
    {
        var dto = new PermisosDto(
            true,
            true,
            true,
            true,
            true,
            "none",
            Recibir: true,
            Reenviar: true,
            Devolver: true,
            Archivar: true,
            Anular: true,
            AdminUsuariosVer: true,
            AdminRolesEditar: true,
            AdminRespaldosCrear: true,
            AdminRespaldosConfigurar: true);

        var json = JsonSerializer.Serialize(dto, Options);

        json.Should().Contain("\"recibir\":true");
        json.Should().Contain("\"reenviar\":true");
        json.Should().Contain("\"devolver\":true");
        json.Should().Contain("\"archivar\":true");
        json.Should().Contain("\"anular\":true");
        json.Should().Contain("\"adminUsuariosVer\":true");
        json.Should().Contain("\"adminRolesEditar\":true");
        json.Should().Contain("\"adminRespaldosCrear\":true");
        json.Should().Contain("\"adminRespaldosConfigurar\":true");
    }

    [Fact]
    public void Deserialize_WithBoolTrue_ReturnsVer()
    {
        var json = """{"bandeja":true,"crearDocumento":false,"derivar":false,"firmar":false,"admin":false,"reportes":true}""";

        var dto = JsonSerializer.Deserialize<PermisosDto>(json, Options);

        dto.Should().NotBeNull();
        dto!.Reportes.Should().Be("ver");
    }

    [Fact]
    public void Deserialize_WithBoolFalse_ReturnsNone()
    {
        var json = """{"bandeja":true,"crearDocumento":false,"derivar":false,"firmar":false,"admin":false,"reportes":false}""";

        var dto = JsonSerializer.Deserialize<PermisosDto>(json, Options);

        dto.Should().NotBeNull();
        dto!.Reportes.Should().Be("none");
    }

    [Fact]
    public void Deserialize_WithStringVer_Passthrough()
    {
        var json = """{"bandeja":true,"crearDocumento":false,"derivar":false,"firmar":false,"admin":false,"reportes":"ver"}""";

        var dto = JsonSerializer.Deserialize<PermisosDto>(json, Options);

        dto.Should().NotBeNull();
        dto!.Reportes.Should().Be("ver");
    }

    [Fact]
    public void Deserialize_WithStringNone_Passthrough()
    {
        var json = """{"bandeja":true,"crearDocumento":false,"derivar":false,"firmar":false,"admin":false,"reportes":"none"}""";

        var dto = JsonSerializer.Deserialize<PermisosDto>(json, Options);

        dto.Should().NotBeNull();
        dto!.Reportes.Should().Be("none");
    }

    [Fact]
    public void Deserialize_WithExplicitAdminPermissionFlags_RoundTripsThem()
    {
        var json = """{"bandeja":true,"crearDocumento":false,"derivar":false,"firmar":false,"admin":false,"reportes":"none","adminUsuariosVer":true,"adminRolesEditar":true,"adminRespaldosCrear":true}""";

        var dto = JsonSerializer.Deserialize<PermisosDto>(json, Options);

        dto.Should().NotBeNull();
        dto!.AdminUsuariosVer.Should().BeTrue();
    }
}
