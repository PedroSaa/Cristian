using System.Text.Json;
using DocFlow.Application.Auth.DTOs;
using FluentAssertions;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

/// <summary>
/// Validates that PermisosDtoConverter is symmetric (Read and Write are inverses) for the 6 new flags.
/// These tests are RED until Task 3 (PermisosDto flags) and Task 5 (converter) are implemented.
/// </summary>
public class PermisosDtoConverterRoundTripTests
{
    // The converter ignores JsonNamingPolicy — it writes literal property names.
    // Options are still needed to invoke the converter.
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void RoundTrip_AllSixNewFlagsTrue_SurviveSerializeDeserialize()
    {
        // S4: construct with all 6 new flags = true; pre-existing flags in a known state.
        var original = new PermisosDto(
            Bandeja: true,
            CrearDocumento: false,
            Derivar: false,
            Firmar: false,
            Admin: false,
            Reportes: "none",
            AdminRespaldosDescargar: true,
            AdminRespaldosRestaurar: true,
            AdminNumeracionVer: true,
            AdminNumeracionEditar: true,
            AdminPlantillasNumeracionVer: true,
            AdminPlantillasNumeracionEditar: true);

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<PermisosDto>(json, Options);

        deserialized.Should().NotBeNull();
        deserialized!.AdminRespaldosDescargar.Should().BeTrue();
        deserialized.AdminRespaldosRestaurar.Should().BeTrue();
        deserialized.AdminNumeracionVer.Should().BeTrue();
        deserialized.AdminNumeracionEditar.Should().BeTrue();
        deserialized.AdminPlantillasNumeracionVer.Should().BeTrue();
        deserialized.AdminPlantillasNumeracionEditar.Should().BeTrue();

        // Pre-existing flags must be unaffected.
        deserialized.Bandeja.Should().BeTrue();
        deserialized.CrearDocumento.Should().BeFalse();
    }

    [Fact]
    public void RoundTrip_AllSixNewFlagsFalse_SurviveSerializeDeserialize()
    {
        // S5: default (false) round-trips correctly; JSON contains the 6 camelCase keys.
        var original = new PermisosDto(
            Bandeja: false,
            CrearDocumento: false,
            Derivar: false,
            Firmar: false,
            Admin: false,
            Reportes: "none");

        var json = JsonSerializer.Serialize(original, Options);
        var deserialized = JsonSerializer.Deserialize<PermisosDto>(json, Options);

        deserialized.Should().NotBeNull();
        deserialized!.AdminRespaldosDescargar.Should().BeFalse();
        deserialized.AdminRespaldosRestaurar.Should().BeFalse();
        deserialized.AdminNumeracionVer.Should().BeFalse();
        deserialized.AdminNumeracionEditar.Should().BeFalse();
        deserialized.AdminPlantillasNumeracionVer.Should().BeFalse();
        deserialized.AdminPlantillasNumeracionEditar.Should().BeFalse();

        // JSON must contain the 6 camelCase keys with false.
        json.Should().Contain("\"adminRespaldosDescargar\":false");
        json.Should().Contain("\"adminRespaldosRestaurar\":false");
        json.Should().Contain("\"adminNumeracionVer\":false");
        json.Should().Contain("\"adminNumeracionEditar\":false");
        json.Should().Contain("\"adminPlantillasNumeracionVer\":false");
        json.Should().Contain("\"adminPlantillasNumeracionEditar\":false");
    }

    [Fact]
    public void Write_NewFlags_EmitCamelCaseKeys()
    {
        // S6: serialized JSON uses camelCase keys, not PascalCase.
        var dto = new PermisosDto(
            Bandeja: false,
            CrearDocumento: false,
            Derivar: false,
            Firmar: false,
            Admin: false,
            Reportes: "none",
            AdminRespaldosDescargar: true,
            AdminRespaldosRestaurar: true,
            AdminNumeracionVer: true,
            AdminNumeracionEditar: true,
            AdminPlantillasNumeracionVer: true,
            AdminPlantillasNumeracionEditar: true);

        var json = JsonSerializer.Serialize(dto, Options);

        // camelCase keys expected
        json.Should().Contain("\"adminRespaldosDescargar\":true");
        json.Should().Contain("\"adminRespaldosRestaurar\":true");
        json.Should().Contain("\"adminNumeracionVer\":true");
        json.Should().Contain("\"adminNumeracionEditar\":true");
        json.Should().Contain("\"adminPlantillasNumeracionVer\":true");
        json.Should().Contain("\"adminPlantillasNumeracionEditar\":true");

        // PascalCase must NOT appear as a key
        json.Should().NotContain("\"AdminRespaldosDescargar\"");
        json.Should().NotContain("\"AdminRespaldosRestaurar\"");
        json.Should().NotContain("\"AdminNumeracionVer\"");
        json.Should().NotContain("\"AdminNumeracionEditar\"");
        json.Should().NotContain("\"AdminPlantillasNumeracionVer\"");
        json.Should().NotContain("\"AdminPlantillasNumeracionEditar\"");
    }
}
