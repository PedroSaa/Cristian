using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Application.Admin.CatalogosLegado.DTOs;
using DocFlow.Application.Admin.CatalogosLegado.Queries.ListSeremTipos;
using DocFlow.Application.Common.Authorization;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminRemitentesLegadoControllerTests
{
    private readonly Mock<ISender> _senderMock = new();
    private readonly AdminRemitentesLegadoController _controller;

    public AdminRemitentesLegadoControllerTests()
    {
        _controller = new AdminRemitentesLegadoController(_senderMock.Object);
    }

    [Fact]
    public async Task ListTipos_ReturnsOkWithData()
    {
        var data = new List<SeremTipoDto> { new("A01", "Municipales", 1) };
        _senderMock.Setup(x => x.Send(It.IsAny<ListSeremTiposQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(data);

        var result = await _controller.ListTipos(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        var attr = typeof(AdminRemitentesLegadoController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Where(a => a.GetType() == typeof(AuthorizeAttribute))
            .ToList();

        attr.Should().ContainSingle();
        attr[0].Roles.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public void Controller_Should_Have_RequireMfaAttribute()
    {
        var attr = typeof(AdminRemitentesLegadoController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        attr.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(nameof(AdminRemitentesLegadoController.ListTipos), "admin.catalogos.ver")]
    [InlineData(nameof(AdminRemitentesLegadoController.GetTipo), "admin.catalogos.ver")]
    [InlineData(nameof(AdminRemitentesLegadoController.CreateTipo), "admin.catalogos.editar")]
    [InlineData(nameof(AdminRemitentesLegadoController.UpdateTipo), "admin.catalogos.editar")]
    [InlineData(nameof(AdminRemitentesLegadoController.DeleteTipo), "admin.catalogos.editar")]
    [InlineData(nameof(AdminRemitentesLegadoController.ListRemitentes), "admin.catalogos.ver")]
    [InlineData(nameof(AdminRemitentesLegadoController.GetRemitente), "admin.catalogos.ver")]
    [InlineData(nameof(AdminRemitentesLegadoController.CreateRemitente), "admin.catalogos.editar")]
    [InlineData(nameof(AdminRemitentesLegadoController.UpdateRemitente), "admin.catalogos.editar")]
    [InlineData(nameof(AdminRemitentesLegadoController.DeleteRemitente), "admin.catalogos.editar")]
    public void Actions_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        var method = typeof(AdminRemitentesLegadoController).GetMethod(actionName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var attr = method!
            .GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Policy.Should().Be($"Permission:{expectedPermission}");
    }
}
