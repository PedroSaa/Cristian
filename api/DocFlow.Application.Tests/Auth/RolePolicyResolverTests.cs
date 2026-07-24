using DocFlow.Application.Common.Authorization;
using DocFlow.Domain.Authorization;
using DocFlow.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class RolePolicyResolverTests
{
    [Fact]
    public void ResolveGroup_WithCodigoSistemaAdministrador_ReturnsAdministrador()
    {
        var rol = new Rol(Guid.NewGuid(), "Gerencia", "Display name changed", false, codigoSistema: RoleCodes.Administrador);

        var group = RolePolicyResolver.ResolveGroup(rol);

        group.Should().Be(RoleGroup.Administrador);
    }

    [Fact]
    public void ResolveGroup_WithNullCodigoSistema_FallsBackToRolNombre()
    {
        var rol = new Rol(Guid.NewGuid(), "Administrador", "Legacy role", false);

        var group = RolePolicyResolver.ResolveGroup(rol);

        group.Should().Be(RoleGroup.Administrador);
    }

    [Fact]
    public void ResolveGroup_WithNonAdminRole_ReturnsOtherUsers()
    {
        var rol = new Rol(Guid.NewGuid(), "Operador", "Operator role", false, codigoSistema: RoleCodes.Operador);

        var group = RolePolicyResolver.ResolveGroup(rol);

        group.Should().Be(RoleGroup.OtrosUsuarios);
    }
}
