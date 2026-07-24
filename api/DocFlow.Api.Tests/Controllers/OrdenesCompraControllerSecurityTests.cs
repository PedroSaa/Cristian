using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Application.Common.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class OrdenesCompraControllerSecurityTests
{
    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        var attr = typeof(OrdenesCompraController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Where(a => a.GetType() == typeof(AuthorizeAttribute))
            .ToList();

        attr.Should().ContainSingle();
        attr[0].Roles.Should().BeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(nameof(OrdenesCompraController.List), "ordenescompra.ver")]
    [InlineData(nameof(OrdenesCompraController.GetById), "ordenescompra.ver")]
    [InlineData(nameof(OrdenesCompraController.GetPdf), "ordenescompra.ver")]
    [InlineData(nameof(OrdenesCompraController.DownloadAdjunto), "ordenescompra.ver")]
    [InlineData(nameof(OrdenesCompraController.BuscarMercadoPublico), "ordenescompra.ver")]
    [InlineData(nameof(OrdenesCompraController.VincularMercadoPublico), "ordenescompra.crear")]
    [InlineData(nameof(OrdenesCompraController.DesvincularMercadoPublico), "ordenescompra.crear")]
    [InlineData(nameof(OrdenesCompraController.Create), "ordenescompra.crear")]
    [InlineData(nameof(OrdenesCompraController.Update), "ordenescompra.crear")]
    [InlineData(nameof(OrdenesCompraController.EnviarAprobacion), "ordenescompra.crear")]
    [InlineData(nameof(OrdenesCompraController.MarcarEnviada), "ordenescompra.crear")]
    [InlineData(nameof(OrdenesCompraController.AgregarAdjunto), "ordenescompra.crear")]
    [InlineData(nameof(OrdenesCompraController.EliminarAdjunto), "ordenescompra.crear")]
    [InlineData(nameof(OrdenesCompraController.Aprobar), "ordenescompra.aprobar")]
    [InlineData(nameof(OrdenesCompraController.Rechazar), "ordenescompra.aprobar")]
    [InlineData(nameof(OrdenesCompraController.Anular), "ordenescompra.anular")]
    public void Actions_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        var method = typeof(OrdenesCompraController).GetMethod(actionName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var attr = method!
            .GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Policy.Should().Be($"Permission:{expectedPermission}");
    }

    [Fact]
    public void Every_Action_Should_Be_Guarded_By_A_Permission()
    {
        var actions = typeof(OrdenesCompraController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName);

        foreach (var action in actions)
        {
            action.GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
                .Should().NotBeEmpty($"the action {action.Name} must declare a HasPermission attribute");
        }
    }
}
