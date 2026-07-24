using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Application.Admin.Departamentos.Commands.ActivarDepartamento;
using DocFlow.Application.Admin.Departamentos.Commands.DesactivarDepartamento;
using DocFlow.Application.Admin.Departamentos.Commands.EliminarDepartamento;
using DocFlow.Application.Admin.Departamentos.DTOs;
using DocFlow.Application.Admin.Departamentos.Queries.ListDepartamentos;
using DocFlow.Application.Common.Authorization;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminDepartamentosControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AdminDepartamentosController _controller;

    public AdminDepartamentosControllerTests()
    {
        _controller = new AdminDepartamentosController(_mediatorMock.Object);
    }

    [Fact]
    public async Task List_Should_Return_200_WithListOfDepartamentos()
    {
        // Arrange
        var departamentos = new List<DepartamentoAdminDto>
        {
            new(Guid.NewGuid(), "TI", "TI", true, DateTime.UtcNow, 5),
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListDepartamentosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(departamentos);

        // Act
        var result = await _controller.List(activo: null, ct: CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<List<DepartamentoAdminDto>>().Subject;
        value.Should().HaveCount(1);
        value[0].Nombre.Should().Be("TI");
    }

    [Fact]
    public async Task Activar_Should_Return_200_WhenMediatorCompletes()
    {
        // Arrange
        var depId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ActivarDepartamentoCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Activar(depId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Activar_Should_Return_404_WhenDepartamentoNotFound()
    {
        // Arrange
        var depId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ActivarDepartamentoCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Departamento {depId} no encontrado."));

        // Act
        var result = await _controller.Activar(depId, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"Departamento {depId} no encontrado." });
    }

    [Fact]
    public async Task Desactivar_Should_Return_200_WhenMediatorCompletes()
    {
        // Arrange
        var depId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DesactivarDepartamentoCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Desactivar(depId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Desactivar_Should_Return_404_WhenDepartamentoNotFound()
    {
        // Arrange
        var depId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DesactivarDepartamentoCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Departamento {depId} no encontrado."));

        // Act
        var result = await _controller.Desactivar(depId, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"Departamento {depId} no encontrado." });
    }

    [Fact]
    public async Task Delete_Should_Return_204_WhenMediatorCompletes()
    {
        // Arrange
        var depId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EliminarDepartamentoCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(depId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_Should_Return_404_WhenDepartamentoNotFound()
    {
        // Arrange
        var depId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EliminarDepartamentoCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Departamento {depId} no encontrado."));

        // Act
        var result = await _controller.Delete(depId, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"Departamento {depId} no encontrado." });
    }

    [Fact]
    public async Task Delete_Should_Return_409_WhenDepartamentoHasUsuarios()
    {
        // Arrange
        var depId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<EliminarDepartamentoCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No se puede eliminar un departamento con usuarios asignados"));

        // Act
        var result = await _controller.Delete(depId, CancellationToken.None);

        // Assert
        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new { mensaje = "No se puede eliminar un departamento con usuarios asignados" });
    }

    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        // Act
        var attr = typeof(AdminDepartamentosController)
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
        var attr = typeof(AdminDepartamentosController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        // Assert
        attr.Should().NotBeEmpty("Admin endpoints require MFA enforcement");
    }

    [Theory]
    [InlineData("List", "admin.departamentos.ver")]
    [InlineData("GetById", "admin.departamentos.ver")]
    [InlineData("Create", "admin.departamentos.editar")]
    [InlineData("Update", "admin.departamentos.editar")]
    [InlineData("Activar", "admin.departamentos.editar")]
    [InlineData("Desactivar", "admin.departamentos.editar")]
    [InlineData("Delete", "admin.departamentos.editar")]
    public void Action_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        // Arrange
        var method = typeof(AdminDepartamentosController).GetMethod(actionName,
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
}
