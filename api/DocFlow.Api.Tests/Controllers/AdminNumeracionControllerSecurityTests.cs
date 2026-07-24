using System.Reflection;
using DocFlow.Api.Controllers;
using DocFlow.Api.Filters;
using DocFlow.Application.Common.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace DocFlow.Api.Tests.Controllers;

public class AdminNumeracionControllerSecurityTests
{
    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        var attr = typeof(AdminNumeracionController)
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
        var attr = typeof(AdminNumeracionController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        attr.Should().NotBeEmpty("Admin endpoints require MFA enforcement");
    }

    [Theory]
    [InlineData(nameof(AdminNumeracionController.List), "admin.numeracion.ver")]
    [InlineData(nameof(AdminNumeracionController.GetById), "admin.numeracion.ver")]
    [InlineData(nameof(AdminNumeracionController.Create), "admin.numeracion.editar")]
    [InlineData(nameof(AdminNumeracionController.SetValue), "admin.numeracion.editar")]
    [InlineData(nameof(AdminNumeracionController.Increment), "admin.numeracion.editar")]
    [InlineData(nameof(AdminNumeracionController.Deactivate), "admin.numeracion.editar")]
    [InlineData(nameof(AdminNumeracionController.Reactivate), "admin.numeracion.editar")]
    public void Actions_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        var method = typeof(AdminNumeracionController).GetMethod(actionName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var attr = method!
            .GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Policy.Should().Be($"Permission:{expectedPermission}");
    }
}

public class AdminPlantillasNumeracionControllerSecurityTests
{
    [Fact]
    public void Controller_Should_Have_AuthorizeAttribute_Without_RoleRestriction()
    {
        var attr = typeof(AdminPlantillasNumeracionController)
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
        var attr = typeof(AdminPlantillasNumeracionController)
            .GetCustomAttributes(typeof(RequireMfaAttribute), inherit: true)
            .Cast<RequireMfaAttribute>()
            .ToList();

        attr.Should().NotBeEmpty("Admin endpoints require MFA enforcement");
    }

    [Theory]
    [InlineData(nameof(AdminPlantillasNumeracionController.List), "admin.plantillasNumeracion.ver")]
    [InlineData(nameof(AdminPlantillasNumeracionController.Create), "admin.plantillasNumeracion.editar")]
    [InlineData(nameof(AdminPlantillasNumeracionController.Update), "admin.plantillasNumeracion.editar")]
    [InlineData(nameof(AdminPlantillasNumeracionController.Toggle), "admin.plantillasNumeracion.editar")]
    public void Actions_Should_Have_HasPermissionAttribute(string actionName, string expectedPermission)
    {
        var method = typeof(AdminPlantillasNumeracionController).GetMethod(actionName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        var attr = method!
            .GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
            .Cast<HasPermissionAttribute>()
            .FirstOrDefault();

        attr.Should().NotBeNull();
        attr!.Policy.Should().Be($"Permission:{expectedPermission}");
    }
}
