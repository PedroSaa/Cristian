using DocFlow.Application.Auth.DTOs;
using DocFlow.Application.Common.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

// Assembly anchor: DocFlow.Api.Controllers lives here.
// DocFlow.Api.Tests already references DocFlow.Api — no csproj change needed.
using ApiAssemblyAnchor = DocFlow.Api.Controllers.AdminAuditoriaController;

namespace DocFlow.Api.Tests.Auth;

public class PermisosDtoDriftGuardTests
{
    // Permissions that have no direct bool flag on PermisosDto but are guarded in controllers.
    // Each entry must be documented with a justification.
    private static readonly HashSet<string> ExclusionList = new(StringComparer.OrdinalIgnoreCase)
    {
        // Has flag AdminRespaldosEditar on PermisosDto but no endpoint guard uses it — catalog entry without a guard (UUID a3000000-...-0019).
        "admin.respaldos.editar",
        // Covered by the string field PermisosDto.Reportes ("ver"/"none") rather than a bool flag.
        "reportes.generar",
    };

    // Explicit dot.case -> PascalCase map bridging the two naming conventions.
    // Non-bijective: e.g. "admin.plantillasNumeracion.*" has camelCase segment;
    // "admin.usuarios.reset-password" has a hyphen. A deterministic transform is not reliable.
    // Each map value is validated via reflection (GetProperty != null) so typos are caught.
    private static readonly Dictionary<string, string> FlagMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Bandeja
        { "bandeja.ver",                          "Bandeja" },
        { "bandeja.recibir",                      "Recibir" },
        { "bandeja.derivar",                      "Derivar" },
        { "bandeja.reenviar",                     "Reenviar" },
        { "bandeja.devolver",                     "Devolver" },
        { "bandeja.archivar",                     "Archivar" },
        { "bandeja.anular",                       "Anular" },

        // Documentos
        { "documentos.crear",                     "CrearDocumento" },
        { "documentos.firmar",                    "Firmar" },
        { "documentos.ver",                       "DocumentosVer" },

        // Module aggregate flags (both .ver and .gestionar guard → same bool flag)
        { "facturas.ver",                         "Facturas" },
        { "facturas.gestionar",                   "Facturas" },
        { "firmas.ver",                           "Firmas" },
        { "firmas.gestionar",                     "Firmas" },
        { "oirs.ver",                             "Oirs" },
        { "oirs.gestionar",                       "Oirs" },
        { "proveedores.ver",                      "Proveedores" },
        { "proveedores.gestionar",                "Proveedores" },

        // Órdenes de Compra
        { "ordenescompra.ver",                    "OrdenesCompraVer" },
        { "ordenescompra.crear",                  "OrdenesCompraCrear" },
        { "ordenescompra.aprobar",                "OrdenesCompraAprobar" },
        { "ordenescompra.anular",                 "OrdenesCompraAnular" },

        // Admin Usuarios
        { "admin.usuarios.ver",                   "AdminUsuariosVer" },
        { "admin.usuarios.crear",                 "AdminUsuariosCrear" },
        { "admin.usuarios.editar",                "AdminUsuariosEditar" },
        { "admin.usuarios.activar",               "AdminUsuariosActivar" },
        { "admin.usuarios.desactivar",            "AdminUsuariosDesactivar" },
        { "admin.usuarios.reset-password",        "AdminUsuariosResetPassword" },
        { "admin.usuarios.bloquear",              "AdminUsuariosBloquear" },

        // Admin Roles
        { "admin.roles.ver",                      "AdminRolesVer" },
        { "admin.roles.crear",                    "AdminRolesCrear" },
        { "admin.roles.editar",                   "AdminRolesEditar" },
        { "admin.roles.eliminar",                 "AdminRolesEliminar" },
        { "admin.roles.permisos",                 "AdminRolesPermisos" },

        // Admin Departamentos
        { "admin.departamentos.ver",              "AdminDepartamentosVer" },
        { "admin.departamentos.editar",           "AdminDepartamentosEditar" },

        // Admin Catálogos
        { "admin.catalogos.ver",                  "AdminCatalogosVer" },
        { "admin.catalogos.editar",               "AdminCatalogosEditar" },

        // Admin Configuración
        { "admin.config.ver",                     "AdminConfiguracionVer" },
        { "admin.config.editar",                  "AdminConfiguracionEditar" },

        // Admin Integraciones
        { "admin.integraciones.ver",              "AdminIntegracionesVer" },
        { "admin.integraciones.editar",           "AdminIntegracionesEditar" },

        // Admin Auditoría
        { "admin.auditoria.ver",                  "AdminAuditoriaVer" },

        // Admin Respaldos (existing)
        { "admin.respaldos.ver",                  "AdminRespaldosVer" },
        { "admin.respaldos.crear",                "AdminRespaldosCrear" },
        { "admin.respaldos.configurar",           "AdminRespaldosConfigurar" },

        // Admin Respaldos (NEW — Task 3 adds these to PermisosDto)
        { "admin.respaldos.descargar",            "AdminRespaldosDescargar" },
        { "admin.respaldos.restaurar",            "AdminRespaldosRestaurar" },

        // Admin Numeración (NEW)
        { "admin.numeracion.ver",                 "AdminNumeracionVer" },
        { "admin.numeracion.editar",              "AdminNumeracionEditar" },

        // Admin Plantillas Numeración (NEW — camelCase segment preserved)
        { "admin.plantillasNumeracion.ver",       "AdminPlantillasNumeracionVer" },
        { "admin.plantillasNumeracion.editar",    "AdminPlantillasNumeracionEditar" },
    };

    [Fact]
    public void AllGuardedPermissions_HaveAFlagOrAreExcluded()
    {
        // Validate the map itself: every mapped property name must resolve to a bool on PermisosDto.
        var dtoType = typeof(PermisosDto);
        var mapErrors = FlagMap
            .Where(kv => dtoType.GetProperty(kv.Value) == null
                         || dtoType.GetProperty(kv.Value)!.PropertyType != typeof(bool))
            .Select(kv => $"FlagMap entry '{kv.Key}' -> '{kv.Value}' does not resolve to a bool property on PermisosDto")
            .ToList();

        mapErrors.Should().BeEmpty(
            "every entry in FlagMap must reference a real bool property on PermisosDto (catches map typos)");

        // Reflect over all controller types in the Api assembly.
        var apiAssembly = typeof(ApiAssemblyAnchor).Assembly;
        var controllerTypes = apiAssembly.GetTypes()
            .Where(t => !t.IsAbstract
                        && typeof(ControllerBase).IsAssignableFrom(t));

        // Collect distinct guarded permission canonical names.
        var guarded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in controllerTypes)
        {
            // Controller-level attributes
            foreach (var attr in type.GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
                         .OfType<HasPermissionAttribute>())
            {
                var name = attr.Policy!.Replace(HasPermissionAttribute.PolicyPrefix, string.Empty);
                guarded.Add(PermissionCatalog.Normalize(name));
            }

            // Method-level attributes
            foreach (var method in type.GetMethods())
            {
                foreach (var attr in method.GetCustomAttributes(typeof(HasPermissionAttribute), inherit: true)
                             .OfType<HasPermissionAttribute>())
                {
                    var name = attr.Policy!.Replace(HasPermissionAttribute.PolicyPrefix, string.Empty);
                    guarded.Add(PermissionCatalog.Normalize(name));
                }
            }
        }

        // Every guarded permission must either be in FlagMap or be excluded.
        var offending = guarded
            .Where(perm => !FlagMap.ContainsKey(perm) && !ExclusionList.Contains(perm))
            .OrderBy(p => p)
            .ToList();

        offending.Should().BeEmpty(
            $"every guarded permission must have a PermisosDto flag or be in the exclusion list. " +
            $"Offending: [{string.Join(", ", offending)}]");
    }
}
