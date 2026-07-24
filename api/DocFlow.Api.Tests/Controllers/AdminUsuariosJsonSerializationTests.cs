using System.Text.Json;
using System.Text.Json.Serialization;
using DocFlow.Api.Controllers;
using DocFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminUsuariosJsonSerializationTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public void CreateRequest_Deserializes_Rol_As_String()
    {
        // Arrange
        var json = """
            {
                "nombres": "Test",
                "apellidoPaterno": "User",
                "apellidoMaterno": "",
                "telefono": "",
                "direccion": "",
                "email": "test@docflow.cl",
                "rol": "Operador",
                "password": "Secure@123"
            }
            """;

        // Act
        var request = JsonSerializer.Deserialize<CrearUsuarioRequest>(json, Options);

        // Assert
        request.Should().NotBeNull();
        request!.Rol.Should().Be("Operador");
        request.Nombres.Should().Be("Test");
        request.ApellidoPaterno.Should().Be("User");
        request.Email.Should().Be("test@docflow.cl");
    }

    [Fact]
    public void UpdateRequest_Deserializes_Rol_As_String()
    {
        // Arrange
        var json = """
            {
                "nombres": "Updated",
                "apellidoPaterno": "User",
                "apellidoMaterno": "",
                "telefono": "",
                "direccion": "",
                "rol": "MinistroDeFe"
            }
            """;

        // Act
        var request = JsonSerializer.Deserialize<ActualizarUsuarioRequest>(json, Options);

        // Assert
        request.Should().NotBeNull();
        request!.Rol.Should().Be("MinistroDeFe");
        request.Nombres.Should().Be("Updated");
        request.ApellidoPaterno.Should().Be("User");
    }

    [Fact]
    public void CreateRequest_Deserializes_All_Rol_Values()
    {
        var rolNames = new[] { "Administrador", "Usuario", "Operador", "MinistroDeFe", "Receptor", "Firmante", "RRHH", "Jefatura" };

        var template = """{"nombres":"T","apellidoPaterno":"P","apellidoMaterno":"M","telefono":"","direccion":"","email":"t@t.cl","rol":"{0}","password":"Secure@123"}""";

        foreach (var name in rolNames)
        {
            var json = template.Replace("{0}", name);
            var request = JsonSerializer.Deserialize<CrearUsuarioRequest>(json, Options);
            request.Should().NotBeNull();
            request!.Rol.Should().Be(name, $"rol '{name}' should deserialize as string");
        }
    }

    [Fact]
    public void CrearUsuarioRequest_Uses_CaseInsensitive_PropertyNames()
    {
        // Arrange — frontend sends lowercase property names
        var json = """
            {
                "nombres": "Lower",
                "apellidoPaterno": "Case",
                "apellidoMaterno": "",
                "telefono": "",
                "direccion": "",
                "email": "lower@docflow.cl",
                "rol": "Operador",
                "password": "Secure@123"
            }
            """;

        // Act
        var request = JsonSerializer.Deserialize<CrearUsuarioRequest>(json, Options);

        // Assert
        request.Should().NotBeNull();
        request!.Nombres.Should().Be("Lower");
        request.ApellidoPaterno.Should().Be("Case");
        request.Email.Should().Be("lower@docflow.cl");
        request.Rol.Should().Be("Operador");
    }
}
