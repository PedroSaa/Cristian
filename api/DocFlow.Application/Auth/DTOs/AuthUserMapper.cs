using DocFlow.Application.Common.Authorization;
using DocFlow.Domain.Entities;

namespace DocFlow.Application.Auth.DTOs;

internal static class AuthUserMapper
{
    public static UsuarioDto ToDto(
        SeUsuari usuario,
        IReadOnlyList<Permiso>? dbPermisos = null,
        int? lockoutMaxAttempts = null,
        AuthState authState = AuthState.Normal,
        string? setupToken = null)
    {
        var personal = usuario.Personal;
        var permisos = ResolvePermissions(usuario, dbPermisos);

        return new UsuarioDto(
            usuario.UsuarioId,
            BuildFullName(personal),
            personal?.Correo ?? string.Empty,
            personal?.Rut,
            usuario.Rol?.Nombre ?? string.Empty,
            usuario.RolId?.ToString(),
            usuario.Departamento is not null
                ? new DepartamentoDto(usuario.Departamento.Id, usuario.Departamento.Nombre)
                : null,
            usuario.EstadoCuenta && (personal?.Estado ?? true),
            Math.Max(0, (lockoutMaxAttempts ?? 5) - usuario.IntentosFallidos),
            MapPermisos(permisos),
            usuario.MfaEnabled,
            MapCanonicalPermissions(permisos),
            authState,
            setupToken);
    }

    private static string BuildFullName(SePersonal? personal)
        => personal is null
            ? string.Empty
            : string.Join(" ", new[] { personal.Nombres, personal.ApellidoPaterno, personal.ApellidoMaterno }
                .Where(part => !string.IsNullOrWhiteSpace(part)));

    private static IReadOnlyList<Permiso> ResolvePermissions(SeUsuari usuario, IReadOnlyList<Permiso>? dbPermisos)
        => dbPermisos ?? usuario.Rol?.RolPermisos?.Select(rp => rp.Permiso).OfType<Permiso>().ToList() ?? [];

    private static PermisosDto MapPermisos(IReadOnlyList<Permiso> permisos)
    {
        var names = permisos.Select(p => PermissionCatalog.Normalize(p.Nombre)).ToHashSet(StringComparer.OrdinalIgnoreCase);

        static bool AnyStartsWith(HashSet<string> values, string prefix) => values.Any(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        return new PermisosDto(
            names.Contains("bandeja.ver"),
            names.Contains("documentos.crear"),
            names.Contains("bandeja.derivar"),
            names.Contains("documentos.firmar"),
            AnyStartsWith(names, "admin."),
            names.Contains("reportes.generar") ? "ver" : "none",
            names.Contains("documentos.ver"),
            AnyStartsWith(names, "expedientes."),
            AnyStartsWith(names, "archivadores."),
            AnyStartsWith(names, "proveedores."),
            AnyStartsWith(names, "facturas."),
            AnyStartsWith(names, "firmas."),
            AnyStartsWith(names, "oirs."),
            AnyStartsWith(names, "despacho."),
            names.Contains("bandeja.recibir"),
            names.Contains("bandeja.reenviar"),
            names.Contains("bandeja.devolver"),
            names.Contains("bandeja.archivar"),
            names.Contains("bandeja.anular"),
            names.Contains("admin.usuarios.ver"),
            names.Contains("admin.usuarios.crear"),
            names.Contains("admin.usuarios.editar"),
            names.Contains("admin.usuarios.activar"),
            names.Contains("admin.usuarios.desactivar"),
            names.Contains("admin.usuarios.reset-password"),
            names.Contains("admin.usuarios.bloquear"),
            names.Contains("admin.roles.ver"),
            names.Contains("admin.roles.crear"),
            names.Contains("admin.roles.editar"),
            names.Contains("admin.roles.eliminar"),
            names.Contains("admin.roles.permisos"),
            names.Contains("admin.departamentos.ver"),
            names.Contains("admin.departamentos.editar"),
            names.Contains("admin.catalogos.ver"),
            names.Contains("admin.catalogos.editar"),
            names.Contains("admin.config.ver"),
            names.Contains("admin.config.editar"),
            names.Contains("admin.integraciones.ver"),
            names.Contains("admin.integraciones.editar"),
            names.Contains("admin.auditoria.ver"),
            names.Contains("admin.respaldos.ver"),
            names.Contains("admin.respaldos.crear"),
            names.Contains("admin.respaldos.editar"),
            names.Contains("admin.respaldos.configurar"),
            names.Contains("admin.respaldos.descargar"),
            names.Contains("admin.respaldos.restaurar"),
            names.Contains("admin.numeracion.ver"),
            names.Contains("admin.numeracion.editar"),
            names.Contains("admin.plantillasNumeracion.ver"),
            names.Contains("admin.plantillasNumeracion.editar"),
            names.Contains("ordenescompra.ver"),
            names.Contains("ordenescompra.crear"),
            names.Contains("ordenescompra.aprobar"),
            names.Contains("ordenescompra.anular"));
    }

    private static string[] MapCanonicalPermissions(IReadOnlyList<Permiso> permisos)
        => permisos.Select(p => PermissionCatalog.Normalize(p.Nombre)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
}
