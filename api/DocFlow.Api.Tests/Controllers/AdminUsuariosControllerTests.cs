using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Api.Helpers;
using DocFlow.Application.Common.Authorization;
using DocFlow.Application.Admin.Usuarios.Commands.ActivarUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.BloquearUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.CrearUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.DesactivarUsuario;
using DocFlow.Application.Admin.Usuarios.Commands.DesbloquearUsuario;
using DocFlow.Application.Admin.Usuarios.DTOs;
using DocFlow.Application.Common;
using DocFlow.Application.Admin.Usuarios.Queries.ListUsuarios;
using DocFlow.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminUsuariosControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AdminUsuariosController _controller;

    public AdminUsuariosControllerTests()
    {
        _controller = new AdminUsuariosController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Activate_Should_Return_200_WhenMediatorCompletes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ActivarUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Activate(userId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task Deactivate_Should_Return_200_WhenMediatorCompletes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DesactivarUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Deactivate(userId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task List_Should_Return_PagedResult_WithCanonicalPagingFields()
    {
        // Arrange
        var pagedResult = new PagedResult<UsuarioAdminDto>(
            new List<UsuarioAdminDto>
            {
                new(Guid.NewGuid(), "Ada Lovelace", "ada@docflow.cl", "Administrador", Guid.NewGuid(), "TI", true, DateTime.UtcNow)
            },
            Total: 1,
            Page: 1,
            TotalPaginas: 1);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.List(page: 1, pageSize: 20, ct: CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<PagedResult<UsuarioAdminDto>>().Subject;
        value.Items.Should().HaveCount(1);
        value.Items[0].NombreCompleto.Should().Be("Ada Lovelace");
        value.Total.Should().Be(1);
        value.Page.Should().Be(1);
        value.TotalPaginas.Should().Be(1);
    }

    [Fact]
    public async Task List_Should_Forward_Search_Query_To_Handler()
    {
        var pagedResult = new PagedResult<UsuarioAdminDto>(new List<UsuarioAdminDto>(), Total: 0, Page: 1, TotalPaginas: 1);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        await _controller.List(page: 1, pageSize: 20, search: "ada", ct: CancellationToken.None);

        _mediatorMock.Verify(x => x.Send(It.Is<ListUsuariosQuery>(q => q.Search == "ada"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(2, 0, 2, PaginationQuery.DefaultPageSize)]
    [InlineData(3, 500, 3, PaginationQuery.MaxPageSize)]
    [InlineData(4, 50, 4, 50)]
    public async Task List_Should_Normalize_Pagination(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var pagedResult = new PagedResult<UsuarioAdminDto>(new List<UsuarioAdminDto>(), Total: 0, Page: expectedPage, TotalPaginas: 1);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        await _controller.List(page: page, pageSize: pageSize, ct: CancellationToken.None);

        _mediatorMock.Verify(x => x.Send(
            It.Is<ListUsuariosQuery>(q => q.Page == expectedPage && q.PageSize == expectedPageSize),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task List_Returns_Rut_Populated()
    {
        // Arrange
        var pagedResult = new PagedResult<UsuarioAdminDto>(
            new List<UsuarioAdminDto>
            {
                new(Guid.NewGuid(), "Test User", "test@docflow.cl", "Operador", null, null, true, DateTime.UtcNow, "12.345.678-9")
            },
            Total: 1,
            Page: 1,
            TotalPaginas: 1);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.List(page: 1, pageSize: 20, ct: CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<PagedResult<UsuarioAdminDto>>().Subject;
        value.Items[0].Rut.Should().Be("12.345.678-9");
    }

    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        // Act
        var attr = typeof(AdminUsuariosController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Where(a => a.GetType() == typeof(AuthorizeAttribute))
            .ToList();

        // Assert
        attr.Should().ContainSingle();
        attr[0].Roles.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Activate_Should_Return_404_WhenUsuarioNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ActivarUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"No se encontró el usuario con id {userId}."));

        // Act
        var result = await _controller.Activate(userId, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"No se encontró el usuario con id {userId}." });
    }

    [Fact]
    public async Task Deactivate_Should_Return_404_WhenUsuarioNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DesactivarUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"No se encontró el usuario con id {userId}."));

        // Act
        var result = await _controller.Deactivate(userId, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"No se encontró el usuario con id {userId}." });
    }

    [Fact]
    public async Task Deactivate_Should_Return_409_WhenBusinessRuleFails()
    {
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DesactivarUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No puedes desactivar tu propia cuenta."));

        var result = await _controller.Deactivate(userId, CancellationToken.None);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new { mensaje = "No puedes desactivar tu propia cuenta." });
    }

    [Fact]
    public async Task Bloquear_Should_Return_204_WhenMediatorCompletes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BloquearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Bloquear(userId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Bloquear_Should_Return_404_WhenUsuarioNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BloquearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Usuario {userId} no encontrado."));

        // Act
        var result = await _controller.Bloquear(userId);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"Usuario {userId} no encontrado." });
    }

    [Fact]
    public async Task Bloquear_Should_Return_409_WhenBusinessRuleFails()
    {
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<BloquearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("No puedes bloquear tu propia cuenta."));

        var result = await _controller.Bloquear(userId);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflict.Value.Should().BeEquivalentTo(new { mensaje = "No puedes bloquear tu propia cuenta." });
    }

    [Fact]
    public async Task Desbloquear_Should_Return_204_WhenMediatorCompletes()
    {
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DesbloquearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _controller.Desbloquear(userId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Desbloquear_Should_Return_404_WhenUsuarioNotFound()
    {
        var userId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DesbloquearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Usuario {userId} no encontrado."));

        var result = await _controller.Desbloquear(userId);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"Usuario {userId} no encontrado." });
    }

    [Fact]
    public async Task Create_WithRut_SendsCrearUsuarioCommandWithRut()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var departamentoId = Guid.NewGuid();
        var request = new CrearUsuarioRequest("Test", "Paterno", "Materno", null, null, "test@docflow.cl", "Operador", departamentoId, "Secure@123", "12.345.678-9");
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioAdminDto(userId, "Test Paterno Materno", "test@docflow.cl", "Operador", departamentoId, "IT", true, DateTime.UtcNow, "12.345.678-9"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(x => x.Send(
            It.Is<CrearUsuarioCommand>(c =>
                c.Rut == "12.345.678-9" &&
                c.Email == "test@docflow.cl" &&
                c.Nombres == "Test" &&
                c.ApellidoPaterno == "Paterno" &&
                c.ApellidoMaterno == "Materno"),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public void Controller_Should_Have_RequireMfaAttribute()
    {
        // Act
        var attr = typeof(AdminUsuariosController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        // Assert
        attr.Should().NotBeEmpty("Admin endpoints require MFA enforcement");
    }

    [Theory]
    [InlineData(nameof(AdminUsuariosController.Create), "admin.usuarios.crear")]
    [InlineData(nameof(AdminUsuariosController.Update), "admin.usuarios.editar")]
    [InlineData(nameof(AdminUsuariosController.Activate), "admin.usuarios.activar")]
    [InlineData(nameof(AdminUsuariosController.Deactivate), "admin.usuarios.desactivar")]
    [InlineData(nameof(AdminUsuariosController.ResetPassword), "admin.usuarios.reset-password")]
    [InlineData(nameof(AdminUsuariosController.Bloquear), "admin.usuarios.bloquear")]
    [InlineData(nameof(AdminUsuariosController.Desbloquear), "admin.usuarios.bloquear")]
    public void SensitiveActions_Should_Use_LeastPrivilegePermissions(string methodName, string expectedPermission)
    {
        var method = typeof(AdminUsuariosController).GetMethod(methodName)!;
        var attr = method.GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .Single();

        attr.Policy.Should().Be($"Permission:{expectedPermission}");
    }

    [Fact]
    public async Task Create_WithoutRut_SendsCrearUsuarioCommandWithNullRut()
    {
        // Arrange
        var departamentoId = Guid.NewGuid();
        var request = new CrearUsuarioRequest("Test", "Paterno", "Materno", null, null, "test@docflow.cl", "Operador", departamentoId, "Secure@123", null);
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CrearUsuarioCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsuarioAdminDto(Guid.NewGuid(), "Test Paterno Materno", "test@docflow.cl", "Operador", departamentoId, "IT", true, DateTime.UtcNow));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(x => x.Send(
            It.Is<CrearUsuarioCommand>(c => c.Rut == null && c.Nombres == "Test"),
            It.IsAny<CancellationToken>()), Times.Once);
        result.Should().BeOfType<CreatedAtActionResult>();
    }
}
