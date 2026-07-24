using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Api.Helpers;
using DocFlow.Application.Admin.Auditoria.DTOs;
using DocFlow.Application.Admin.Auditoria.Queries.ExportAuditoria;
using DocFlow.Application.Admin.Auditoria.Queries.GetAuditoria;
using DocFlow.Application.Admin.Auditoria.Queries.GetValoresFiltro;
using DocFlow.Application.Admin.Auditoria.Queries.ListAuditoria;
using DocFlow.Application.Common;
using DocFlow.Application.Common.Authorization;
using DocFlow.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminAuditoriaControllerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AdminAuditoriaController _controller;

    public AdminAuditoriaControllerTests()
    {
        _controller = new AdminAuditoriaController(_mediatorMock.Object);
    }

    [Fact]
    public async Task List_Should_Return_200_WithPagedResult()
    {
        // Arrange
        var items = new List<RegistroAuditoriaDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Admin User", "Login", "Usuario", "usr-1", null, null, null, DateTime.UtcNow),
            new(Guid.NewGuid(), Guid.NewGuid(), "Admin User", "Logout", "Usuario", "usr-2", null, null, null, DateTime.UtcNow),
        };
        var pagedResult = new PagedResult<RegistroAuditoriaDto>(
            items, 2, 1, 1);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListAuditoriaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.List(page: 1, pageSize: 20, ct: CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<PagedResult<RegistroAuditoriaDto>>().Subject;
        value.Items.Should().HaveCount(2);
        value.Total.Should().Be(2);
        value.Page.Should().Be(1);
    }

    [Fact]
    public async Task List_FilteredByUsuarioId_Should_PassQueryParams()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var items = new List<RegistroAuditoriaDto>
        {
            new(Guid.NewGuid(), usuarioId, "Admin User", "Login", "Usuario", "usr-1", null, null, null, DateTime.UtcNow),
        };
        var pagedResult = new PagedResult<RegistroAuditoriaDto>(
            items, 1, 1, 1);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<ListAuditoriaQuery>(q => q.UsuarioId == usuarioId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.List(
            page: 1, pageSize: 20, usuarioId: usuarioId, ct: CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<PagedResult<RegistroAuditoriaDto>>().Subject;
        value.Items.Should().HaveCount(1);
        value.Items[0].UsuarioId.Should().Be(usuarioId);
    }

    [Theory]
    [InlineData(0, 20, 1, 20)]
    [InlineData(2, 0, 2, PaginationQuery.DefaultPageSize)]
    [InlineData(3, 500, 3, PaginationQuery.MaxPageSize)]
    [InlineData(4, 50, 4, 50)]
    public async Task List_Should_Normalize_Pagination(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var pagedResult = new PagedResult<RegistroAuditoriaDto>(new List<RegistroAuditoriaDto>(), Total: 0, Page: expectedPage, TotalPaginas: 0);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListAuditoriaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        await _controller.List(page: page, pageSize: pageSize, ct: CancellationToken.None);

        _mediatorMock.Verify(x => x.Send(
            It.Is<ListAuditoriaQuery>(q => q.Page == expectedPage && q.PageSize == expectedPageSize),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_Should_Return_200_WithRegistroAuditoriaDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new RegistroAuditoriaDto(
            id, Guid.NewGuid(), "Admin User", "UpsertConfiguracion", "ConfiguracionSistema",
            "cfg-1", "Configuración actualizada: MaxAdjuntosMB = 10", null, null, DateTime.UtcNow);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAuditoriaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dto);

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<RegistroAuditoriaDto>().Subject;
        value.Id.Should().Be(id);
        value.Accion.Should().Be("UpsertConfiguracion");
        value.Entidad.Should().Be("ConfiguracionSistema");
    }

    [Fact]
    public async Task GetById_Should_Return_404_WhenKeyNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAuditoriaQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Registro de auditoría {id} no encontrado."));

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.Value.Should().BeEquivalentTo(new { mensaje = $"Registro de auditoría {id} no encontrado." });
    }

    [Fact]
    public async Task List_FilteredByAccion_Should_PassAccionParam()
    {
        // Arrange
        var items = new List<RegistroAuditoriaDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Admin User", "CrearUsuario", "Usuario", "usr-1", null, null, null, DateTime.UtcNow),
        };
        var pagedResult = new PagedResult<RegistroAuditoriaDto>(items, 1, 1, 1);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<ListAuditoriaQuery>(q => q.Accion == "CrearUsuario"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.List(
            page: 1, pageSize: 20, accion: "CrearUsuario", ct: CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<PagedResult<RegistroAuditoriaDto>>().Subject;
        value.Items.Should().HaveCount(1);
        value.Items[0].Accion.Should().Be("CrearUsuario");
    }

    [Fact]
    public async Task List_FilteredByUsuarioNombre_Should_PassQueryParams()
    {
        // Arrange
        var usuarioNombre = "Juan Pérez";
        var items = new List<RegistroAuditoriaDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), usuarioNombre, "Login", "Usuario", "usr-1", null, null, null, DateTime.UtcNow),
        };
        var pagedResult = new PagedResult<RegistroAuditoriaDto>(items, 1, 1, 1);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<ListAuditoriaQuery>(q => q.UsuarioNombre == usuarioNombre),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.List(
            page: 1, pageSize: 20, usuarioNombre: usuarioNombre, ct: CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<PagedResult<RegistroAuditoriaDto>>().Subject;
        value.Items.Should().HaveCount(1);
        value.Items[0].UsuarioNombre.Should().Be(usuarioNombre);
    }

    [Fact]
    public async Task GetValoresFiltro_Should_Return_200_WithValoresFiltro()
    {
        // Arrange
        var expected = new ValoresFiltro(["Login", "CrearUsuario"], ["Usuario", "Documento"]);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetValoresFiltroQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetValoresFiltro(CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = ok.Value.Should().BeOfType<ValoresFiltro>().Subject;
        value.Acciones.Should().BeEquivalentTo(expected.Acciones);
        value.Entidades.Should().BeEquivalentTo(expected.Entidades);
    }

    [Fact]
    public async Task Exportar_Should_Return_200_WithCsvBytes()
    {
        // Arrange
        var expectedBytes = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ExportAuditoriaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBytes);

        // Act
        var result = await _controller.Exportar(ct: CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Should().BeEquivalentTo(expectedBytes);
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Match("auditoria-*.csv");
    }

    [Fact]
    public async Task Exportar_WithFilters_Should_PassQueryParams()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        _mediatorMock
            .Setup(x => x.Send(
                It.Is<ExportAuditoriaQuery>(q => q.UsuarioId == usuarioId && q.Accion == "Login"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<byte>());

        // Act
        var result = await _controller.Exportar(
            usuarioId: usuarioId, accion: "Login", ct: CancellationToken.None);

        // Assert
        result.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task Exportar_Should_Return_400_When_InvalidOperationException()
    {
        // Arrange
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ExportAuditoriaQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("La exportación está limitada a 10000 registros."));

        // Act
        var result = await _controller.Exportar(ct: CancellationToken.None);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeEquivalentTo(new { mensaje = "La exportación está limitada a 10000 registros." });
    }

    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        // Act
        var attr = typeof(AdminAuditoriaController)
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
        var attr = typeof(AdminAuditoriaController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        // Assert
        attr.Should().NotBeEmpty("Admin endpoints require MFA enforcement");
    }

    [Theory]
    [InlineData("List", "admin.auditoria.ver")]
    [InlineData("GetById", "admin.auditoria.ver")]
    [InlineData("Exportar", "admin.auditoria.ver")]
    [InlineData("GetValoresFiltro", "admin.auditoria.ver")]
    public void Action_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        // Arrange
        var method = typeof(AdminAuditoriaController).GetMethod(actionName,
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
