using System.Reflection;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Domain.Entities;
using Moq;

namespace DocFlow.Application.Tests.Auth;

internal static class AuthUserFactory
{
    /// <summary>
    /// A no-op MFA protector for handler tests that don't care about encryption:
    /// Protect/Unprotect are the identity function, so existing assertions on the raw
    /// secret keep working. Tests that exercise encryption use their own configured mock.
    /// </summary>
    public static IMfaSecretProtector PassthroughMfaProtector()
    {
        var mock = new Mock<IMfaSecretProtector>();
        mock.Setup(p => p.Protect(It.IsAny<string>())).Returns((string s) => s);
        mock.Setup(p => p.Unprotect(It.IsAny<string>())).Returns((string s) => s);
        return mock.Object;
    }

    public static string[] AdminPermissions() =>
    [
        "bandeja.ver",
        "bandeja.recibir",
        "bandeja.derivar",
        "bandeja.reenviar",
        "bandeja.devolver",
        "bandeja.archivar",
        "bandeja.anular",
        "documentos.crear",
        "documentos.firmar",
        "documentos.ver",
        "admin.usuarios.ver",
        "admin.roles.ver",
        "admin.usuarios.bloquear",
        "admin.respaldos.crear",
        "admin.respaldos.configurar",
        "reportes.generar",
    ];

    public static string[] UsuarioPermissions() =>
    [
        "bandeja.ver",
        "bandeja.recibir",
        "bandeja.derivar",
        "bandeja.reenviar",
        "bandeja.devolver",
        "bandeja.archivar",
        "bandeja.anular",
        "documentos.crear",
        "documentos.ver",
    ];

    public static string[] FirmantePermissions() =>
    [
        "bandeja.ver",
        "documentos.firmar",
        "documentos.ver",
    ];

    public static string[] RRHHPermissions() =>
    [
        "bandeja.ver",
        "reportes.generar",
        "rrhh.gestionar",
        "marcajes.revisar-equipo",
        "documentos.ver",
    ];

    public static SeUsuari CreateUser(
        string fullName,
        string email,
        string roleName = "Usuario",
        IEnumerable<string>? permissions = null,
        Guid? departamentoId = null,
        bool estadoCuenta = true,
        int failedLoginAttempts = 0,
        bool mfaEnabled = false,
        string passwordHash = "$2b$hash",
        string? mfaSecretKey = null)
    {
        var usucod = BuildUsucod(email, fullName);
        var personal = SePersonal.Crear(usucod, fullName, correo: email, estado: estadoCuenta);
        var user = SeUsuari.Crear(Guid.NewGuid(), usucod, passwordHash, departamentoId: departamentoId, estadoCuenta: estadoCuenta);
        var role = CreateRole(roleName, permissions);

        user.VincularPersonal(personal);
        user.ActualizarAcceso(rolId: role.Id, departamentoId: departamentoId, estadoCuenta: estadoCuenta);
        SetRole(user, role);

        for (var i = 0; i < failedLoginAttempts; i++)
            user.RegistrarIntentoFallido();

        if (mfaEnabled)
            user.EstablecerMfa(mfaSecretKey ?? "SECRET");

        return user;
    }

    public static List<Permiso> CreatePermisos(params string[] permissionNames)
        => permissionNames.Select(CreatePermiso).ToList();

    public static Rol CreateRole(string roleName, IEnumerable<string>? permissions = null)
    {
        var role = new Rol(Guid.NewGuid(), roleName, $"Rol {roleName}");

        foreach (var permiso in (permissions ?? []).Select(CreatePermiso))
        {
            role.RolPermisos.Add(new RolPermiso(role.Id, permiso.Id)
            {
                Rol = role,
                Permiso = permiso,
            });
        }

        return role;
    }

    private static Permiso CreatePermiso(string permissionName)
        => new(Guid.NewGuid(), permissionName, permissionName, permissionName.Split('.')[0]);

    private static void SetRole(SeUsuari usuario, Rol role)
        => typeof(SeUsuari).GetProperty(nameof(SeUsuari.Rol), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(usuario, role);

    private static string BuildUsucod(string email, string fullName)
    {
        var source = !string.IsNullOrWhiteSpace(email)
            ? email.Split('@', 2)[0]
            : fullName;

        var normalized = new string(source.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        return normalized.Length <= 25 ? normalized : normalized[..25];
    }
}
