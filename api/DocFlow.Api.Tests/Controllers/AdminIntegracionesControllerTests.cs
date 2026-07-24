using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Integraciones.Commands.ActualizarIntegracion;
using DocFlow.Application.Admin.Integraciones.Commands.ProbarConexion;
using DocFlow.Application.Admin.Integraciones.DTOs;
using DocFlow.Application.Admin.Integraciones.Queries.GetIntegracion;
using DocFlow.Application.Admin.Integraciones.Queries.GetIntegracionIdByNombre;
using DocFlow.Application.Admin.Integraciones.Queries.ListIntegraciones;
using DocFlow.Application.Common.Authorization;
using DocFlow.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminIntegracionesControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AdminIntegracionesController _controller;

    public AdminIntegracionesControllerTests()
    {
        _controller = new AdminIntegracionesController(_mediatorMock.Object);
    }

    [Fact]
    public async Task List_Should_Return_200_WithListOfIntegraciones()
    {
        // Arrange
        var integraciones = new List<IntegracionDto>
        {
            new(Guid.NewGuid(), "DocDigital", "DocDigital", "https://api.docdigital.cl", "****abcd", true, new Dictionary<string, string>()),
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListIntegracionesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(integraciones);

        // Act
        var result = await _controller.List(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<List<IntegracionDto>>().Subject;
        value.Should().HaveCount(1);
        value[0].Nombre.Should().Be("DocDigital");
    }

    [Fact]
    public async Task GetById_Should_Return_200_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new IntegracionDto(id, "DocDigital", "DocDigital", "https://api.docdigital.cl", "****abcd", true, new Dictionary<string, string>());
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetIntegracionQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task GetById_Should_Return_404_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetIntegracionQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Integración {id} no encontrada."));

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"Integración {id} no encontrada." });
    }

    [Fact]
    public async Task Update_ByGuid_Should_Return_200_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        var req = new ActualizarIntegracionRequest("https://new.url", "new-key", false);
        var dto = new IntegracionDto(id, "DocDigital", "DocDigital", "https://new.url", "****ew-key", false, new Dictionary<string, string>());
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ActualizarIntegracionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.Update(id.ToString(), req, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Update_ByServiceName_Should_Return_200_WhenSuccessful()
    {
        // Arrange
        var id = Guid.NewGuid();
        var req = new ActualizarIntegracionRequest("https://new.url", "new-key", false);
        var dto = new IntegracionDto(id, "DocDigital", "DocDigital", "https://new.url", "****ew-key", false, new Dictionary<string, string>());
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetIntegracionIdByNombreQuery>(q => q.Nombre == "docdigital"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(id);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ActualizarIntegracionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.Update("docdigital", req, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Update_ByServiceName_Should_Return_404_WhenNotFound()
    {
        // Arrange
        var req = new ActualizarIntegracionRequest("https://url.com", "key", true);
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetIntegracionIdByNombreQuery>(q => q.Nombre == "nonexistent"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("Integración 'nonexistent' no encontrada."));

        // Act
        var result = await _controller.Update("nonexistent", req, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Update_Should_Return_400_WhenValidationFails()
    {
        // Arrange
        var id = Guid.NewGuid();
        var req = new ActualizarIntegracionRequest("", null, true);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ActualizarIntegracionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new FluentValidation.ValidationException("La URL base es obligatoria."));

        // Act
        var result = await _controller.Update(id.ToString(), req, CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeEquivalentTo(new { mensaje = "La URL base es obligatoria." });
    }

    [Fact]
    public async Task Update_Should_Return_404_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var req = new ActualizarIntegracionRequest("https://url.com", "key", true);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ActualizarIntegracionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Integración {id} no encontrada."));

        // Act
        var result = await _controller.Update(id.ToString(), req, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"Integración {id} no encontrada." });
    }

    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        // Act
        var attr = typeof(AdminIntegracionesController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Where(a => a.GetType() == typeof(AuthorizeAttribute))
            .ToList();

        // Assert
        attr.Should().ContainSingle();
        attr[0].Roles.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public void Controller_Should_Have_RequireMfaAttribute()
    {
        // Act
        var attr = typeof(AdminIntegracionesController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        // Assert
        attr.Should().NotBeEmpty("Admin endpoints require MFA enforcement");
    }

    [Theory]
    [InlineData("List", "admin.integraciones.ver")]
    [InlineData("GetById", "admin.integraciones.ver")]
    [InlineData("Update", "admin.integraciones.editar")]
    public void Action_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        // Arrange
        var method = typeof(AdminIntegracionesController).GetMethod(actionName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Act
        var attr = method!
            .GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull($"Action {actionName} should have [HasPermission(\"{expectedPermission}\")]");
        attr!.Policy.Should().Be($"Permission:{expectedPermission}");
    }

    // ── ProbarConexion action tests ──

    [Fact]
    public async Task ProbarConexion_ByGuid_Should_Return_200_WithDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new IntegracionTestResultDto(true, "Servidor alcanzable (HTTP 200).", 123);
        _mediatorMock
            .Setup(x => x.Send(It.Is<ProbarConexionIntegracionCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.ProbarConexion(id.ToString(), CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task ProbarConexion_ByServiceName_Should_Resolve_Id_Then_Return_200()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new IntegracionTestResultDto(true, "OK", 50);
        _mediatorMock
            .Setup(x => x.Send(It.Is<GetIntegracionIdByNombreQuery>(q => q.Nombre == "docdigital"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(id);
        _mediatorMock
            .Setup(x => x.Send(It.Is<ProbarConexionIntegracionCommand>(c => c.Id == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.ProbarConexion("docdigital", CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(dto);
    }

    [Fact]
    public async Task ProbarConexion_Should_Return_404_When_KeyNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ProbarConexionIntegracionCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Integración {id} no encontrada."));

        // Act
        var result = await _controller.ProbarConexion(id.ToString(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ProbarConexion_Should_Return_200_When_Success_False()
    {
        // A failed connection is DATA — must be HTTP 200 with Success=false
        var id = Guid.NewGuid();
        var failDto = new IntegracionTestResultDto(false, "No se pudo conectar al servidor: Connection refused.", null);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ProbarConexionIntegracionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failDto);

        var result = await _controller.ProbarConexion(id.ToString(), CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<IntegracionTestResultDto>().Subject;
        value.Success.Should().BeFalse();
    }

    [Fact]
    public void ProbarConexion_Should_Have_HasPermission_Editar_Attribute()
    {
        // Arrange
        var method = typeof(AdminIntegracionesController).GetMethod("ProbarConexion",
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        // Act
        var attr = method!
            .GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .FirstOrDefault();

        // Assert
        attr.Should().NotBeNull("ProbarConexion should have [HasPermission(\"admin.integraciones.editar\")]");
        attr!.Policy.Should().Be("Permission:admin.integraciones.editar");
    }
}
