using System.Text.Json;
using DocFlow.Application.Auth.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class UsuarioDtoCanonicalPermissionsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AuthUserMapper_WithDbPermisos_PopulatesCanonicalPermissionsFromDatabase()
    {
        var usuario = AuthUserFactory.CreateUser("Admin", "admin@docflow.cl", nameof(RolUsuario.Administrador));
        var dbPermisos = new List<Permiso>
        {
            new(Guid.NewGuid(), "bandeja.ver", "Ver bandeja", "bandeja"),
            new(Guid.NewGuid(), "documentos.crear", "Crear documentos", "documentos"),
            new(Guid.NewGuid(), "admin.usuarios.ver", "Ver usuarios", "admin"),
            new(Guid.NewGuid(), "admin.usuarios.bloquear", "Bloquear usuarios", "admin"),
        };

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos);

        dto.Permissions.Should().Equal("bandeja.ver", "documentos.crear", "admin.usuarios.ver", "admin.usuarios.bloquear");
        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.CrearDocumento.Should().BeTrue();
        dto.Permisos.Admin.Should().BeTrue();
    }

    [Fact]
    public void AuthUserMapper_WithLegacyDbPermisos_CanonicalizesPermissionNames()
    {
        var usuario = AuthUserFactory.CreateUser("Admin", "admin@docflow.cl", nameof(RolUsuario.Administrador));
        var dbPermisos = new List<Permiso>
        {
            new(Guid.NewGuid(), "admin.configuracion", "Configuración legacy", "admin"),
            new(Guid.NewGuid(), "admin.catalogos", "Catálogos legacy", "admin"),
        };

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos);

        dto.Permissions.Should().Equal("admin.config.ver", "admin.catalogos.ver");
        dto.Permisos.AdminConfiguracionVer.Should().BeTrue();
        dto.Permisos.AdminCatalogosVer.Should().BeTrue();
    }

    [Fact]
    public void AuthUserMapper_WhenDbPermisosIsMissing_UsesLegacyFallbackForCanonicalPermissions()
    {
        var usuario = AuthUserFactory.CreateUser("Firmante", "firmante@docflow.cl", nameof(RolUsuario.Firmante), AuthUserFactory.FirmantePermissions());

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos: null);

        dto.Permissions.Should().ContainInOrder("bandeja.ver", "documentos.firmar", "documentos.ver");
        dto.Permissions.Should().HaveCount(3);
        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.Firmar.Should().BeTrue();
        dto.Permisos.Admin.Should().BeFalse();
    }

    [Fact]
    public void UsuarioDto_SerializesCanonicalPermissionsAsPermissionsProperty()
    {
        var dto = new UsuarioDto(
            Guid.NewGuid(),
            "Test",
            "test@docflow.cl",
            null,
            "Usuario",
            null,
            null,
            true,
            5,
            new PermisosDto(true, true, true, false, false, "none"),
            false,
            ["bandeja.ver", "documentos.crear"]);

        var json = JsonSerializer.Serialize(dto, JsonOptions);

        using var document = JsonDocument.Parse(json);

        document.RootElement.GetProperty("permissions").EnumerateArray()
            .Select(x => x.GetString())
            .Should().Equal("bandeja.ver", "documentos.crear");
        document.RootElement.GetProperty("permisos").Should().NotBeNull();
    }
}
