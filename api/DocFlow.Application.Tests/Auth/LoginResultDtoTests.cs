using DocFlow.Application.Auth.Commands.Login;
using DocFlow.Application.Auth.DTOs;
using DocFlow.Domain.Entities;
using DocFlow.Domain.Enums;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace DocFlow.Application.Tests.Auth;

public class LoginResultDtoTests
{
    [Fact]
    public void LoginResultDto_WithExpiresIn_IncludesExpiresIn()
    {
        var dto = new LoginResultDto(
            "access-token",
            "refresh-token",
            28800,
            new UsuarioDto(
                Guid.NewGuid(), "Test", "test@docflow.cl", "123456789",
                nameof(RolUsuario.Administrador), null,
                new DepartamentoDto(Guid.NewGuid(), "Test Dept"),
                true, 5,
                new PermisosDto(true, true, true, true, true, "ver"),
                false));

        dto.ExpiresIn.Should().Be(28800);
    }

    [Fact]
    public void LoginResultDto_Serializes_WithoutTokensForBrowserResponses()
    {
        var dto = new LoginResultDto(
            "access-token",
            "refresh-token",
            28800,
            new UsuarioDto(
                Guid.NewGuid(), "Test", "test@docflow.cl", null,
                nameof(RolUsuario.Administrador), null,
                new DepartamentoDto(Guid.NewGuid(), "Test Dept"),
                true, 5,
                new PermisosDto(true, true, true, true, true, "ver"),
                false));

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().Contain("expiresIn");
        json.Should().Contain("user");
        json.Should().NotContain("accessToken");
        json.Should().NotContain("refreshToken");
    }

    [Fact]
    public void UsuarioDto_WithNestedDepartamento_HasDepartamentoWithIdAndNombre()
    {
        var deptId = Guid.NewGuid();
        var dto = new UsuarioDto(
            Guid.NewGuid(), "Test", "test@docflow.cl", null,
            nameof(RolUsuario.Administrador), null,
            new DepartamentoDto(deptId, "Finanzas"),
            true, 5,
            new PermisosDto(true, true, true, true, true, "ver"),
            false);

        dto.Departamento.Should().NotBeNull();
        dto.Departamento!.Id.Should().Be(deptId);
        dto.Departamento.Nombre.Should().Be("Finanzas");
    }

    [Fact]
    public void UsuarioDto_WithoutDepartamento_HasNullDepartamento()
    {
        var dto = new UsuarioDto(
            Guid.NewGuid(), "Test", "test@docflow.cl", null,
            nameof(RolUsuario.Administrador), null,
            null,
            true, 5,
            new PermisosDto(true, true, true, true, true, "ver"),
            false);

        dto.Departamento.Should().BeNull();
    }

    [Fact]
    public void UsuarioDto_IncludesIntentosRestantes()
    {
        var dto = new UsuarioDto(
            Guid.NewGuid(), "Test", "test@docflow.cl", null,
            nameof(RolUsuario.Administrador), null,
            null,
            true, 5,
            new PermisosDto(true, true, true, true, true, "ver"),
            false);

        dto.IntentosRestantes.Should().Be(5);
    }

    [Fact]
    public void AuthUserMapper_WithAdministrador_MapsCorrectPermissions()
    {
        var usuario = AuthUserFactory.CreateUser("Admin", "admin@docflow.cl", nameof(RolUsuario.Administrador), AuthUserFactory.AdminPermissions());

        var dto = AuthUserMapper.ToDto(usuario);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.CrearDocumento.Should().BeTrue();
        dto.Permisos.Derivar.Should().BeTrue();
        dto.Permisos.Firmar.Should().BeTrue();
        dto.Permisos.Admin.Should().BeTrue();
        dto.Permisos.Reportes.Should().Be("ver");
        dto.Permisos.AdminUsuariosVer.Should().BeTrue();
        dto.Permisos.AdminRolesVer.Should().BeTrue();
        dto.Permisos.AdminRespaldosCrear.Should().BeTrue();
        dto.Permisos.AdminRespaldosConfigurar.Should().BeTrue();
        dto.Permisos.AdminUsuariosBloquear.Should().BeTrue();
    }

    [Fact]
    public void AuthUserMapper_WithUsuarioRole_MapsCorrectPermissions()
    {
        var usuario = AuthUserFactory.CreateUser("User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions());

        var dto = AuthUserMapper.ToDto(usuario);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.CrearDocumento.Should().BeTrue();
        dto.Permisos.Derivar.Should().BeTrue();
        dto.Permisos.Firmar.Should().BeFalse();
        dto.Permisos.Admin.Should().BeFalse();
        dto.Permisos.Reportes.Should().Be("none");
    }

    [Fact]
    public void AuthUserMapper_WithFirmanteRole_MapsCorrectPermissions()
    {
        var usuario = AuthUserFactory.CreateUser("Firmante", "firmante@docflow.cl", nameof(RolUsuario.Firmante), AuthUserFactory.FirmantePermissions());

        var dto = AuthUserMapper.ToDto(usuario);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.CrearDocumento.Should().BeFalse();
        dto.Permisos.Derivar.Should().BeFalse();
        dto.Permisos.Firmar.Should().BeTrue();
        dto.Permisos.Admin.Should().BeFalse();
        dto.Permisos.Reportes.Should().Be("none");
        dto.Permisos.AdminUsuariosVer.Should().BeFalse();
    }

    [Fact]
    public void AuthUserMapper_WithDepartamento_MapsNestedDepartamento()
    {
        var deptId = Guid.NewGuid();
        var usuario = AuthUserFactory.CreateUser("User", "user@docflow.cl", nameof(RolUsuario.Usuario), AuthUserFactory.UsuarioPermissions(), deptId);
        // Navigation property not loaded — Departamento should be null

        var dto = AuthUserMapper.ToDto(usuario);

        dto.Departamento.Should().BeNull(); // Departamento navigation not loaded
    }

    [Fact]
    public void AuthUserMapper_SetsIntentosRestantesTo5()
    {
        var usuario = AuthUserFactory.CreateUser("User", "user@docflow.cl", nameof(RolUsuario.Usuario));

        var dto = AuthUserMapper.ToDto(usuario);

        dto.IntentosRestantes.Should().Be(5);
    }

    [Fact]
    public void AuthUserMapper_ComputesIntentosRestantesDynamically()
    {
        var usuario = AuthUserFactory.CreateUser("User", "user@docflow.cl", nameof(RolUsuario.Usuario));
        usuario.RegistrarIntentoFallido();
        usuario.RegistrarIntentoFallido();

        var dto = AuthUserMapper.ToDto(usuario);

        dto.IntentosRestantes.Should().Be(3);
    }

    // ── 3.1 MfaEnabled serialization ────────────────────────────────────────

    [Fact]
    public void UsuarioDto_IncludesMfaEnabled()
    {
        var usuario = AuthUserFactory.CreateUser("Test", "test@docflow.cl", nameof(RolUsuario.Usuario));

        var dto = AuthUserMapper.ToDto(usuario);

        dto.MfaEnabled.Should().BeFalse();
    }

    [Fact]
    public void LoginResultDto_EmbedsAuthStateAndSetupTokenInJson()
    {
        var dto = new LoginResultDto(
            string.Empty,
            string.Empty,
            0,
            new UsuarioDto(
                Guid.NewGuid(), "Test", "test@docflow.cl", null,
                nameof(RolUsuario.Usuario), null,
                null,
                true, 5,
                new PermisosDto(false, false, false, false, false, "none"),
                false,
                AuthState: AuthState.MfaSetupRequired,
                SetupToken: "setup-token"),
            AuthState: AuthState.MfaSetupRequired,
            SetupToken: "setup-token",
            CanLogout: true);

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().Contain("authState\":\"mfa_setup_required\"");
        json.Should().Contain("setupToken\":\"setup-token\"");
    }

    [Fact]
    public void UsuarioDto_EmbedsAuthStateAndSetupTokenInJson()
    {
        var dto = new UsuarioDto(
            Guid.NewGuid(), "Test", "test@docflow.cl", null,
            nameof(RolUsuario.Usuario), null,
            null,
            true, 5,
            new PermisosDto(false, false, false, false, false, "none"),
            false,
            AuthState: AuthState.MfaSetupRequired,
            SetupToken: "setup-token");

        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        json.Should().Contain("authState\":\"mfa_setup_required\"");
        json.Should().Contain("setupToken\":\"setup-token\"");
    }

    [Fact]
    public void UsuarioDto_WithMfaEnabled_ReturnsTrue()
    {
        var usuario = AuthUserFactory.CreateUser("Test", "test@docflow.cl", nameof(RolUsuario.Usuario), mfaEnabled: true);

        var dto = AuthUserMapper.ToDto(usuario);

        dto.MfaEnabled.Should().BeTrue();
    }

    // ── 2.1 AuthUserMapper derives PermisosDto from dbPermisos list ──────────

    [Fact]
    public void AuthUserMapper_WithDbPermisos_DerivesCorrectPermissions()
    {
        var usuario = AuthUserFactory.CreateUser("Admin", "admin@docflow.cl", nameof(RolUsuario.Administrador));
        var dbPermisos = new List<Permiso>
        {
            new(Guid.NewGuid(), "bandeja.ver", "Ver bandeja", "bandeja"),
            new(Guid.NewGuid(), "bandeja.derivar", "Derivar documentos", "bandeja"),
            new(Guid.NewGuid(), "bandeja.recibir", "Recibir documentos", "bandeja"),
            new(Guid.NewGuid(), "bandeja.reenviar", "Reenviar documentos", "bandeja"),
            new(Guid.NewGuid(), "bandeja.devolver", "Devolver documentos", "bandeja"),
            new(Guid.NewGuid(), "bandeja.archivar", "Archivar documentos", "bandeja"),
            new(Guid.NewGuid(), "bandeja.anular", "Anular documentos", "bandeja"),
            new(Guid.NewGuid(), "documentos.crear", "Crear documentos", "documentos"),
            new(Guid.NewGuid(), "documentos.firmar", "Firmar documentos", "documentos"),
            new(Guid.NewGuid(), "admin.usuarios.ver", "Ver usuarios", "admin"),
            new(Guid.NewGuid(), "reportes.generar", "Generar reportes", "reportes"),
        };

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.CrearDocumento.Should().BeTrue();
        dto.Permisos.Derivar.Should().BeTrue();
        dto.Permisos.Recibir.Should().BeTrue();
        dto.Permisos.Reenviar.Should().BeTrue();
        dto.Permisos.Devolver.Should().BeTrue();
        dto.Permisos.Archivar.Should().BeTrue();
        dto.Permisos.Anular.Should().BeTrue();
        dto.Permisos.Firmar.Should().BeTrue();
        dto.Permisos.Admin.Should().BeTrue();
        dto.Permisos.Reportes.Should().Be("ver");
        dto.Permisos.AdminUsuariosVer.Should().BeTrue();
        dto.Permisos.AdminRolesVer.Should().BeFalse();
        dto.Permisos.AdminRespaldosCrear.Should().BeFalse();
        dto.Permisos.AdminUsuariosBloquear.Should().BeFalse();
    }

    [Fact]
    public void AuthUserMapper_WithDbPermisos_PartialPermissions_RespectsDbData()
    {
        var usuario = AuthUserFactory.CreateUser("Firmante", "firmante@docflow.cl", nameof(RolUsuario.Firmante));
        // Only firmar permission — DB source of truth overrides enum fallback
        var dbPermisos = new List<Permiso>
        {
            new(Guid.NewGuid(), "bandeja.ver", "Ver bandeja", "bandeja"),
            new(Guid.NewGuid(), "documentos.firmar", "Firmar documentos", "documentos"),
        };

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.CrearDocumento.Should().BeFalse();
        dto.Permisos.Derivar.Should().BeFalse();
        dto.Permisos.Recibir.Should().BeFalse();
        dto.Permisos.Reenviar.Should().BeFalse();
        dto.Permisos.Devolver.Should().BeFalse();
        dto.Permisos.Archivar.Should().BeFalse();
        dto.Permisos.Anular.Should().BeFalse();
        dto.Permisos.Firmar.Should().BeTrue();
        dto.Permisos.Admin.Should().BeFalse();
        dto.Permisos.Reportes.Should().Be("none");
    }

    [Fact]
    public void AuthUserMapper_WithDbPermisos_GeneradorOnly_ReportsNone()
    {
        var usuario = AuthUserFactory.CreateUser("Admin", "admin@docflow.cl", nameof(RolUsuario.Administrador));
        // Only bandeja.ver — no reportes.generar
        var dbPermisos = new List<Permiso>
        {
            new(Guid.NewGuid(), "bandeja.ver", "Ver bandeja", "bandeja"),
            new(Guid.NewGuid(), "documentos.crear", "Crear documentos", "documentos"),
            new(Guid.NewGuid(), "bandeja.derivar", "Derivar", "bandeja"),
            new(Guid.NewGuid(), "admin.usuarios.ver", "Admin usuarios", "admin"),
        };

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos: dbPermisos);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.CrearDocumento.Should().BeTrue();
        dto.Permisos.Derivar.Should().BeTrue();
        dto.Permisos.Recibir.Should().BeFalse();
        dto.Permisos.Reenviar.Should().BeFalse();
        dto.Permisos.Firmar.Should().BeFalse();
        dto.Permisos.Admin.Should().BeTrue();
        dto.Permisos.Reportes.Should().Be("none");
        dto.Permisos.AdminUsuariosVer.Should().BeTrue();
        dto.Permisos.AdminRolesVer.Should().BeFalse();
        dto.Permisos.AdminRespaldosCrear.Should().BeFalse();
    }

    // ── 2.2 AuthUserMapper falls back to enum switch when dbPermisos null ───

    [Fact]
    public void AuthUserMapper_WhenDbPermisosNull_FallsBackToEnumForAdmin()
    {
        var usuario = AuthUserFactory.CreateUser("Admin", "admin@docflow.cl", nameof(RolUsuario.Administrador), AuthUserFactory.AdminPermissions());

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos: null);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.Admin.Should().BeTrue();
        dto.Permisos.Reportes.Should().Be("ver");
        dto.Permisos.AdminUsuariosVer.Should().BeTrue();
        dto.Permisos.AdminRolesVer.Should().BeTrue();
    }

    [Fact]
    public void AuthUserMapper_WhenDbPermisosNull_FallsBackToEnumForFirmante()
    {
        var usuario = AuthUserFactory.CreateUser("Firmante", "firmante@docflow.cl", nameof(RolUsuario.Firmante), AuthUserFactory.FirmantePermissions());

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos: null);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.CrearDocumento.Should().BeFalse();
        dto.Permisos.Derivar.Should().BeFalse();
        dto.Permisos.Firmar.Should().BeTrue();
        dto.Permisos.Admin.Should().BeFalse();
        dto.Permisos.Reportes.Should().Be("none");
    }

    [Fact]
    public void AuthUserMapper_WhenDbPermisosNull_FallsBackToEnumForRRHH()
    {
        var usuario = AuthUserFactory.CreateUser("RRHH", "rrhh@docflow.cl", nameof(RolUsuario.RRHH), AuthUserFactory.RRHHPermissions());

        var dto = AuthUserMapper.ToDto(usuario, dbPermisos: null);

        dto.Permisos.Bandeja.Should().BeTrue();
        dto.Permisos.Admin.Should().BeFalse();
        dto.Permisos.Reportes.Should().Be("ver");
    }
}
