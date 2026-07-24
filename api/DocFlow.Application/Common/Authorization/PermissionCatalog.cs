namespace DocFlow.Application.Common.Authorization;

public sealed record PermissionDefinition(
    Guid Id,
    string Name,
    string Group,
    string Label,
    string Description,
    bool IsLegacyAlias,
    string? CanonicalName,
    string[] Aliases);

public static class PermissionCatalog
{
    private static readonly PermissionDefinition[] SeedDefinitions =
    [
        // Bandeja (7)
        Canonical("a1000000-0000-0000-0000-000000000001", "bandeja.ver", "bandeja", "Ver bandeja de entrada"),
        Canonical("a1000000-0000-0000-0000-000000000002", "bandeja.recibir", "bandeja", "Recibir documentos"),
        Canonical("a1000000-0000-0000-0000-000000000003", "bandeja.derivar", "bandeja", "Derivar documentos"),
        Canonical("a1000000-0000-0000-0000-000000000004", "bandeja.reenviar", "bandeja", "Reenviar documentos"),
        Canonical("a1000000-0000-0000-0000-000000000005", "bandeja.devolver", "bandeja", "Devolver documentos"),
        Canonical("a1000000-0000-0000-0000-000000000006", "bandeja.archivar", "bandeja", "Archivar documentos"),
        Canonical("a1000000-0000-0000-0000-000000000007", "bandeja.anular", "bandeja", "Anular documentos"),

        // Documentos (5)
        Canonical("a2000000-0000-0000-0000-000000000001", "documentos.crear", "documentos", "Crear documentos"),
        Canonical("a2000000-0000-0000-0000-000000000002", "documentos.editar", "documentos", "Editar documentos"),
        Canonical("a2000000-0000-0000-0000-000000000003", "documentos.eliminar", "documentos", "Eliminar documentos"),
        Canonical("a2000000-0000-0000-0000-000000000004", "documentos.firmar", "documentos", "Firmar documentos"),
        Canonical("a2000000-0000-0000-0000-000000000005", "documentos.ver", "documentos", "Ver documentos"),

        // Modules (view)
        Canonical("a2100000-0000-0000-0000-000000000001", "despacho.ver", "despacho", "Ver despacho"),
        Canonical("a2100000-0000-0000-0000-000000000002", "expedientes.ver", "expedientes", "Ver expedientes"),
        Canonical("a2100000-0000-0000-0000-000000000003", "archivadores.ver", "archivadores", "Ver archivadores"),
        Canonical("a2100000-0000-0000-0000-000000000004", "proveedores.ver", "proveedores", "Ver proveedores"),
        Canonical("a2100000-0000-0000-0000-000000000005", "facturas.ver", "facturas", "Ver facturas"),
        Canonical("a2100000-0000-0000-0000-000000000006", "firmas.ver", "firmas", "Ver firma electrónica"),
        Canonical("a2100000-0000-0000-0000-000000000007", "oirs.ver", "oirs", "Ver OIRS"),

        // Modules (actions)
        Canonical("a2200000-0000-0000-0000-000000000001", "expedientes.gestionar", "expedientes", "Gestionar expedientes"),
        Canonical("a2200000-0000-0000-0000-000000000002", "archivadores.gestionar", "archivadores", "Gestionar archivadores"),
        Canonical("a2200000-0000-0000-0000-000000000003", "proveedores.gestionar", "proveedores", "Gestionar proveedores"),
        Canonical("a2200000-0000-0000-0000-000000000004", "facturas.gestionar", "facturas", "Gestionar facturas"),
        Canonical("a2200000-0000-0000-0000-000000000005", "oirs.gestionar", "oirs", "Gestionar OIRS"),
        Canonical("a2200000-0000-0000-0000-000000000006", "firmas.gestionar", "firmas", "Gestionar firmas electrónicas"),

        // Admin canonical (31)
        Canonical("a3000000-0000-0000-0000-000000000001", "admin.usuarios.ver", "admin", "Ver lista de usuarios"),
        Canonical("a3000000-0000-0000-0000-000000000002", "admin.usuarios.crear", "admin", "Crear usuarios"),
        Canonical("a3000000-0000-0000-0000-000000000003", "admin.usuarios.editar", "admin", "Editar usuarios"),
        Canonical("a3000000-0000-0000-0000-000000000004", "admin.usuarios.desactivar", "admin", "Desactivar usuarios"),
        Canonical("a3000000-0000-0000-0000-000000000005", "admin.usuarios.reset-password", "admin", "Restablecer contraseñas de usuarios"),
        Canonical("a3000000-0000-0000-0000-000000000010", "admin.usuarios.activar", "admin", "Activar usuarios"),
        Canonical("a3000000-0000-0000-0000-000000000023", "admin.usuarios.bloquear", "admin", "Bloquear usuarios"),
        Canonical("a3000000-0000-0000-0000-000000000006", "admin.roles.ver", "admin", "Ver roles"),
        Canonical("a3000000-0000-0000-0000-000000000007", "admin.roles.crear", "admin", "Crear roles"),
        Canonical("a3000000-0000-0000-0000-000000000008", "admin.roles.editar", "admin", "Editar roles"),
        Canonical("a3000000-0000-0000-0000-000000000009", "admin.roles.eliminar", "admin", "Eliminar roles"),
        Canonical("a3000000-0000-0000-0000-00000000000a", "admin.roles.permisos", "admin", "Gestionar permisos de roles"),
        Canonical("a3000000-0000-0000-0000-000000000011", "admin.departamentos.ver", "admin", "Ver departamentos", "admin.departamentos"),
        Canonical("a3000000-0000-0000-0000-000000000012", "admin.departamentos.editar", "admin", "Editar departamentos"),
        Canonical("a3000000-0000-0000-0000-000000000021", "admin.catalogos.ver", "admin", "Ver catálogos", "admin.catalogos"),
        Canonical("a3000000-0000-0000-0000-000000000022", "admin.catalogos.editar", "admin", "Editar catálogos"),
        Canonical("a3000000-0000-0000-0000-000000000024", "admin.numeracion.ver", "admin", "Ver numeración"),
        Canonical("a3000000-0000-0000-0000-000000000025", "admin.numeracion.editar", "admin", "Editar numeración"),
        Canonical("a3000000-0000-0000-0000-000000000026", "admin.plantillasNumeracion.ver", "admin", "Ver plantillas de numeración"),
        Canonical("a3000000-0000-0000-0000-000000000027", "admin.plantillasNumeracion.editar", "admin", "Editar plantillas de numeración"),
        Canonical("a3000000-0000-0000-0000-000000000013", "admin.config.ver", "admin", "Ver configuración", "admin.configuracion"),
        Canonical("a3000000-0000-0000-0000-000000000014", "admin.config.editar", "admin", "Editar configuración"),
        Canonical("a3000000-0000-0000-0000-000000000015", "admin.integraciones.ver", "admin", "Ver integraciones", "admin.integraciones"),
        Canonical("a3000000-0000-0000-0000-000000000016", "admin.integraciones.editar", "admin", "Editar integraciones"),
        Canonical("a3000000-0000-0000-0000-000000000017", "admin.respaldos.crear", "admin", "Crear respaldos"),
        Canonical("a3000000-0000-0000-0000-000000000018", "admin.respaldos.ver", "admin", "Ver respaldos", "admin.respaldos"),
        Canonical("a3000000-0000-0000-0000-000000000019", "admin.respaldos.editar", "admin", "Editar respaldos"),
        Canonical("a3000000-0000-0000-0000-000000000028", "admin.respaldos.descargar", "admin", "Descargar respaldos"),
        Canonical("a3000000-0000-0000-0000-000000000029", "admin.respaldos.restaurar", "admin", "Restaurar respaldos"),
        Canonical("a3000000-0000-0000-0000-000000000030", "admin.respaldos.configurar", "admin", "Configurar respaldos"),
        Canonical("a3000000-0000-0000-0000-00000000000e", "admin.auditoria.ver", "admin", "Ver auditoría"),

        // Legacy alias rows kept for backwards compatibility (5)
        Legacy("a3000000-0000-0000-0000-00000000000b", "admin.departamentos", "admin", "Gestionar departamentos (legacy)", "admin.departamentos.ver"),
        Legacy("a3000000-0000-0000-0000-00000000000c", "admin.configuracion", "admin", "Configuración del sistema (legacy)", "admin.config.ver"),
        Legacy("a3000000-0000-0000-0000-00000000000d", "admin.integraciones", "admin", "Gestionar integraciones (legacy)", "admin.integraciones.ver"),
        Legacy("a3000000-0000-0000-0000-00000000000f", "admin.respaldos", "admin", "Gestionar respaldos (legacy)", "admin.respaldos.ver"),
        Legacy("a3000000-0000-0000-0000-000000000020", "admin.catalogos", "admin", "Gestionar catálogos legacy", "admin.catalogos.ver"),

        // Reportes (2)
        Canonical("a4000000-0000-0000-0000-000000000001", "reportes.generar", "reportes", "Generar reportes"),
        Canonical("a4000000-0000-0000-0000-000000000002", "reportes.programar", "reportes", "Programar reportes"),

        // Órdenes de Compra (4)
        Canonical("a6000000-0000-0000-0000-000000000001", "ordenescompra.ver", "ordenescompra", "Ver órdenes de compra"),
        Canonical("a6000000-0000-0000-0000-000000000002", "ordenescompra.crear", "ordenescompra", "Crear órdenes de compra"),
        Canonical("a6000000-0000-0000-0000-000000000003", "ordenescompra.aprobar", "ordenescompra", "Aprobar órdenes de compra"),
        Canonical("a6000000-0000-0000-0000-000000000004", "ordenescompra.anular", "ordenescompra", "Anular órdenes de compra"),

        // RRHH / Marcajes (2)
        Canonical("a5000000-0000-0000-0000-000000000001", "rrhh.gestionar", "rrhh", "Acceso a módulo RRHH"),
        Canonical("a5000000-0000-0000-0000-000000000002", "marcajes.revisar-equipo", "marcajes", "Revisar marcajes de equipo"),
    ];

    private static readonly IReadOnlyList<PermissionDefinition> CanonicalDefinitions =
        SeedDefinitions.Where(definition => !definition.IsLegacyAlias).ToArray();

    private static readonly IReadOnlyDictionary<string, PermissionDefinition> SeedDefinitionsByName =
        SeedDefinitions.ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, PermissionDefinition> CanonicalDefinitionsByName =
        CanonicalDefinitions.ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyDictionary<string, string> AliasToCanonicalName =
        BuildAliasMap();

    public static IReadOnlyList<PermissionDefinition> Export() => CanonicalDefinitions;

    public static IReadOnlyList<PermissionDefinition> Seed() => SeedDefinitions;

    public static bool TryNormalize(string permissionName, out string canonicalName)
    {
        canonicalName = string.Empty;

        if (string.IsNullOrWhiteSpace(permissionName))
            return false;

        var trimmed = permissionName.Trim();

        if (CanonicalDefinitionsByName.ContainsKey(trimmed))
        {
            canonicalName = trimmed;
            return true;
        }

        if (AliasToCanonicalName.TryGetValue(trimmed, out var aliasCanonicalName))
        {
            canonicalName = aliasCanonicalName;
            return true;
        }

        canonicalName = trimmed;
        return false;
    }

    public static string Normalize(string permissionName)
        => string.IsNullOrWhiteSpace(permissionName)
            ? string.Empty
            : TryNormalize(permissionName, out var canonicalName)
                ? canonicalName
                : permissionName.Trim();

    public static Guid GetSeedId(string permissionName)
        => SeedDefinitionsByName[(permissionName?.Trim() ?? string.Empty)].Id;

    public static Guid GetCanonicalId(string permissionName)
        => CanonicalDefinitionsByName[Normalize(permissionName)].Id;

    private static IReadOnlyDictionary<string, string> BuildAliasMap()
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in CanonicalDefinitions)
        {
            foreach (var alias in definition.Aliases)
                aliases[alias] = definition.Name;
        }

        foreach (var definition in SeedDefinitions.Where(definition => definition.IsLegacyAlias))
        {
            if (!string.IsNullOrWhiteSpace(definition.CanonicalName))
                aliases[definition.Name] = definition.CanonicalName!;
        }

        return aliases;
    }

    private static PermissionDefinition Canonical(string id, string name, string group, string label, params string[] aliases)
        => new(Guid.Parse(id), name, group, label, label, false, null, aliases);

    private static PermissionDefinition Legacy(string id, string name, string group, string label, string canonicalName)
        => new(Guid.Parse(id), name, group, label, label, true, canonicalName, []);
}
