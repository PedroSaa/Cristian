using DocFlow.Application.Admin.Auditoria.DTOs;
using FluentAssertions;
using Xunit;

namespace DocFlow.Application.Tests.Admin.Auditoria.DTOs;

public class DetalleAuditoriaTests
{
    [Fact]
    public void FromString_Should_Parse_Json_With_All_Fields()
    {
        // Arrange
        var json = """{"valorAnterior": "old@test.com", "valorNuevo": "new@test.com", "metadata": "actualización manual"}""";

        // Act
        var result = DetalleAuditoria.FromString(json);

        // Assert
        result.Should().NotBeNull();
        result!.ValorAnterior.Should().Be("old@test.com");
        result.ValorNuevo.Should().Be("new@test.com");
        result.Metadata.Should().Be("actualización manual");
    }

    [Fact]
    public void FromString_Should_Parse_Json_With_Partial_Fields()
    {
        // Arrange
        var json = """{"valorNuevo": "nuevo valor"}""";

        // Act
        var result = DetalleAuditoria.FromString(json);

        // Assert
        result.Should().NotBeNull();
        result!.ValorAnterior.Should().BeNull();
        result.ValorNuevo.Should().Be("nuevo valor");
        result.Metadata.Should().BeNull();
    }

    [Fact]
    public void FromString_Should_Return_Null_For_Plain_Text()
    {
        // Arrange
        var plainText = "Usuario modificado manualmente";

        // Act
        var result = DetalleAuditoria.FromString(plainText);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromString_Should_Return_Null_For_Null_Input()
    {
        // Arrange
        string? input = null;

        // Act
        var result = DetalleAuditoria.FromString(input);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromString_Should_Return_Null_For_Empty_String()
    {
        // Arrange
        var input = "";

        // Act
        var result = DetalleAuditoria.FromString(input);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromString_Should_Return_Null_For_Whitespace()
    {
        // Arrange
        var input = "   ";

        // Act
        var result = DetalleAuditoria.FromString(input);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromString_Should_Return_Null_When_Json_Has_Null_Fields()
    {
        // Arrange
        var json = """{"valorAnterior": null, "valorNuevo": null, "metadata": null}""";

        // Act
        var result = DetalleAuditoria.FromString(json);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ToJsonString_Should_Produce_CamelCase_Json()
    {
        // Arrange
        var detalle = new DetalleAuditoria
        {
            ValorAnterior = "old",
            ValorNuevo = "new",
            Metadata = "test"
        };

        // Act
        var json = detalle.ToJsonString();

        // Assert
        json.Should().Contain("\"valorAnterior\":");
        json.Should().Contain("\"valorNuevo\":");
        json.Should().Contain("\"metadata\":");
        json.Should().NotContain("\"ValorAnterior\":");
    }

    [Fact]
    public void Render_Should_Return_Formatted_String_For_Structured_Json()
    {
        // Arrange
        var json = """{"valorAnterior": "old@test.com", "valorNuevo": "new@test.com", "metadata": "cambio de email"}""";

        // Act
        var rendered = DetalleAuditoria.Render(json);

        // Assert
        rendered.Should().Contain("Valor anterior: old@test.com");
        rendered.Should().Contain("Valor nuevo: new@test.com");
        rendered.Should().Contain("Metadata: cambio de email");
    }

    [Fact]
    public void Render_Should_Return_Raw_Text_For_Legacy_Plain_Text()
    {
        // Arrange
        var plainText = "Usuario modificado manualmente";

        // Act
        var rendered = DetalleAuditoria.Render(plainText);

        // Assert
        rendered.Should().Be(plainText);
    }

    [Fact]
    public void Render_Should_Return_Empty_String_For_Null()
    {
        // Act
        var rendered = DetalleAuditoria.Render(null);

        // Assert
        rendered.Should().BeEmpty();
    }

    [Fact]
    public void Render_Should_Return_Only_Present_Fields()
    {
        // Arrange
        var json = """{"valorNuevo": "new value"}""";

        // Act
        var rendered = DetalleAuditoria.Render(json);

        // Assert
        rendered.Should().Be("Valor nuevo: new value");
        rendered.Should().NotContain("Valor anterior");
        rendered.Should().NotContain("Metadata");
    }
}
