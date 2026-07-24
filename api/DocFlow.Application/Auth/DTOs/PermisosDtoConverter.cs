using System.Text.Json;
using System.Text.Json.Serialization;

namespace DocFlow.Application.Auth.DTOs;

public class PermisosDtoConverter : JsonConverter<PermisosDto>
{
    public override PermisosDto Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        bool bandeja = GetBool(root, "bandeja");
        bool crearDocumento = GetBool(root, "crearDocumento");
        bool derivar = GetBool(root, "derivar");
        bool firmar = GetBool(root, "firmar");
        bool admin = GetBool(root, "admin");
        string reportes = GetReportes(root);
        bool documentosVer = GetBool(root, "documentosVer") || GetBool(root, "documentos");
        bool expedientes = GetBool(root, "expedientes");
        bool archivadores = GetBool(root, "archivadores");
        bool proveedores = GetBool(root, "proveedores");
        bool facturas = GetBool(root, "facturas");
        bool firmas = GetBool(root, "firmas");
        bool oirs = GetBool(root, "oirs");
        bool despacho = GetBool(root, "despacho");
        bool recibir = GetBool(root, "recibir") || GetBool(root, "bandejaRecibir");
        bool reenviar = GetBool(root, "reenviar") || GetBool(root, "bandejaReenviar");
        bool devolver = GetBool(root, "devolver") || GetBool(root, "bandejaDevolver");
        bool archivar = GetBool(root, "archivar") || GetBool(root, "bandejaArchivar");
        bool anular = GetBool(root, "anular") || GetBool(root, "bandejaAnular");
        bool adminUsuariosVer = GetBool(root, "adminUsuariosVer");
        bool adminUsuariosCrear = GetBool(root, "adminUsuariosCrear");
        bool adminUsuariosEditar = GetBool(root, "adminUsuariosEditar");
        bool adminUsuariosActivar = GetBool(root, "adminUsuariosActivar");
        bool adminUsuariosDesactivar = GetBool(root, "adminUsuariosDesactivar");
        bool adminUsuariosResetPassword = GetBool(root, "adminUsuariosResetPassword");
        bool adminUsuariosBloquear = GetBool(root, "adminUsuariosBloquear");
        bool adminRolesVer = GetBool(root, "adminRolesVer");
        bool adminRolesCrear = GetBool(root, "adminRolesCrear");
        bool adminRolesEditar = GetBool(root, "adminRolesEditar");
        bool adminRolesEliminar = GetBool(root, "adminRolesEliminar");
        bool adminRolesPermisos = GetBool(root, "adminRolesPermisos");
        bool adminDepartamentosVer = GetBool(root, "adminDepartamentosVer");
        bool adminDepartamentosEditar = GetBool(root, "adminDepartamentosEditar");
        bool adminCatalogosVer = GetBool(root, "adminCatalogosVer");
        bool adminCatalogosEditar = GetBool(root, "adminCatalogosEditar");
        bool adminConfiguracionVer = GetBool(root, "adminConfiguracionVer");
        bool adminConfiguracionEditar = GetBool(root, "adminConfiguracionEditar");
        bool adminIntegracionesVer = GetBool(root, "adminIntegracionesVer");
        bool adminIntegracionesEditar = GetBool(root, "adminIntegracionesEditar");
        bool adminAuditoriaVer = GetBool(root, "adminAuditoriaVer");
        bool adminRespaldosVer = GetBool(root, "adminRespaldosVer");
        bool adminRespaldosCrear = GetBool(root, "adminRespaldosCrear");
        bool adminRespaldosEditar = GetBool(root, "adminRespaldosEditar");
        bool adminRespaldosConfigurar = GetBool(root, "adminRespaldosConfigurar");
        bool adminRespaldosDescargar = GetBool(root, "adminRespaldosDescargar");
        bool adminRespaldosRestaurar = GetBool(root, "adminRespaldosRestaurar");
        bool adminNumeracionVer = GetBool(root, "adminNumeracionVer");
        bool adminNumeracionEditar = GetBool(root, "adminNumeracionEditar");
        bool adminPlantillasNumeracionVer = GetBool(root, "adminPlantillasNumeracionVer");
        bool adminPlantillasNumeracionEditar = GetBool(root, "adminPlantillasNumeracionEditar");
        bool ordenesCompraVer = GetBool(root, "ordenesCompraVer");
        bool ordenesCompraCrear = GetBool(root, "ordenesCompraCrear");
        bool ordenesCompraAprobar = GetBool(root, "ordenesCompraAprobar");
        bool ordenesCompraAnular = GetBool(root, "ordenesCompraAnular");

        // Also try camelCase and snake_case fallbacks
        if (root.TryGetProperty("crear_documento", out var cd))
            crearDocumento = cd.GetBoolean();
        if (root.TryGetProperty("CrearDocumento", out var cd2))
            crearDocumento = cd2.GetBoolean();

        return new PermisosDto(
            bandeja,
            crearDocumento,
            derivar,
            firmar,
            admin,
            reportes,
            documentosVer,
            expedientes,
            archivadores,
            proveedores,
            facturas,
            firmas,
            oirs,
            despacho,
            recibir,
            reenviar,
            devolver,
            archivar,
            anular,
            adminUsuariosVer,
            adminUsuariosCrear,
            adminUsuariosEditar,
            adminUsuariosActivar,
            adminUsuariosDesactivar,
            adminUsuariosResetPassword,
            adminUsuariosBloquear,
            adminRolesVer,
            adminRolesCrear,
            adminRolesEditar,
            adminRolesEliminar,
            adminRolesPermisos,
            adminDepartamentosVer,
            adminDepartamentosEditar,
            adminCatalogosVer,
            adminCatalogosEditar,
            adminConfiguracionVer,
            adminConfiguracionEditar,
            adminIntegracionesVer,
            adminIntegracionesEditar,
            adminAuditoriaVer,
            adminRespaldosVer,
            adminRespaldosCrear,
            adminRespaldosEditar,
            adminRespaldosConfigurar,
            adminRespaldosDescargar,
            adminRespaldosRestaurar,
            adminNumeracionVer,
            adminNumeracionEditar,
            adminPlantillasNumeracionVer,
            adminPlantillasNumeracionEditar,
            ordenesCompraVer,
            ordenesCompraCrear,
            ordenesCompraAprobar,
            ordenesCompraAnular);
    }

    public override void Write(Utf8JsonWriter writer, PermisosDto value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("bandeja", value.Bandeja);
        writer.WriteBoolean("crearDocumento", value.CrearDocumento);
        writer.WriteBoolean("derivar", value.Derivar);
        writer.WriteBoolean("firmar", value.Firmar);
        writer.WriteBoolean("admin", value.Admin);
        writer.WriteString("reportes", value.Reportes);
        writer.WriteBoolean("documentosVer", value.DocumentosVer);
        writer.WriteBoolean("expedientes", value.Expedientes);
        writer.WriteBoolean("archivadores", value.Archivadores);
        writer.WriteBoolean("proveedores", value.Proveedores);
        writer.WriteBoolean("facturas", value.Facturas);
        writer.WriteBoolean("firmas", value.Firmas);
        writer.WriteBoolean("oirs", value.Oirs);
        writer.WriteBoolean("despacho", value.Despacho);
        writer.WriteBoolean("recibir", value.Recibir);
        writer.WriteBoolean("reenviar", value.Reenviar);
        writer.WriteBoolean("devolver", value.Devolver);
        writer.WriteBoolean("archivar", value.Archivar);
        writer.WriteBoolean("anular", value.Anular);
        writer.WriteBoolean("adminUsuariosVer", value.AdminUsuariosVer);
        writer.WriteBoolean("adminUsuariosCrear", value.AdminUsuariosCrear);
        writer.WriteBoolean("adminUsuariosEditar", value.AdminUsuariosEditar);
        writer.WriteBoolean("adminUsuariosActivar", value.AdminUsuariosActivar);
        writer.WriteBoolean("adminUsuariosDesactivar", value.AdminUsuariosDesactivar);
        writer.WriteBoolean("adminUsuariosResetPassword", value.AdminUsuariosResetPassword);
        writer.WriteBoolean("adminUsuariosBloquear", value.AdminUsuariosBloquear);
        writer.WriteBoolean("adminRolesVer", value.AdminRolesVer);
        writer.WriteBoolean("adminRolesCrear", value.AdminRolesCrear);
        writer.WriteBoolean("adminRolesEditar", value.AdminRolesEditar);
        writer.WriteBoolean("adminRolesEliminar", value.AdminRolesEliminar);
        writer.WriteBoolean("adminRolesPermisos", value.AdminRolesPermisos);
        writer.WriteBoolean("adminDepartamentosVer", value.AdminDepartamentosVer);
        writer.WriteBoolean("adminDepartamentosEditar", value.AdminDepartamentosEditar);
        writer.WriteBoolean("adminCatalogosVer", value.AdminCatalogosVer);
        writer.WriteBoolean("adminCatalogosEditar", value.AdminCatalogosEditar);
        writer.WriteBoolean("adminConfiguracionVer", value.AdminConfiguracionVer);
        writer.WriteBoolean("adminConfiguracionEditar", value.AdminConfiguracionEditar);
        writer.WriteBoolean("adminIntegracionesVer", value.AdminIntegracionesVer);
        writer.WriteBoolean("adminIntegracionesEditar", value.AdminIntegracionesEditar);
        writer.WriteBoolean("adminAuditoriaVer", value.AdminAuditoriaVer);
        writer.WriteBoolean("adminRespaldosVer", value.AdminRespaldosVer);
        writer.WriteBoolean("adminRespaldosCrear", value.AdminRespaldosCrear);
        writer.WriteBoolean("adminRespaldosEditar", value.AdminRespaldosEditar);
        writer.WriteBoolean("adminRespaldosConfigurar", value.AdminRespaldosConfigurar);
        writer.WriteBoolean("adminRespaldosDescargar", value.AdminRespaldosDescargar);
        writer.WriteBoolean("adminRespaldosRestaurar", value.AdminRespaldosRestaurar);
        writer.WriteBoolean("adminNumeracionVer", value.AdminNumeracionVer);
        writer.WriteBoolean("adminNumeracionEditar", value.AdminNumeracionEditar);
        writer.WriteBoolean("adminPlantillasNumeracionVer", value.AdminPlantillasNumeracionVer);
        writer.WriteBoolean("adminPlantillasNumeracionEditar", value.AdminPlantillasNumeracionEditar);
        writer.WriteBoolean("ordenesCompraVer", value.OrdenesCompraVer);
        writer.WriteBoolean("ordenesCompraCrear", value.OrdenesCompraCrear);
        writer.WriteBoolean("ordenesCompraAprobar", value.OrdenesCompraAprobar);
        writer.WriteBoolean("ordenesCompraAnular", value.OrdenesCompraAnular);
        writer.WriteEndObject();
    }

    private static bool GetBool(JsonElement root, string propertyName)
    {
        if (root.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.True)
            return true;
        if (root.TryGetProperty(propertyName, out prop) && prop.ValueKind == JsonValueKind.False)
            return false;

        // camelCase fallback
        var camel = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        if (root.TryGetProperty(camel, out prop))
        {
            if (prop.ValueKind == JsonValueKind.True) return true;
            if (prop.ValueKind == JsonValueKind.False) return false;
        }

        return false;
    }

    private static string GetReportes(JsonElement root)
    {
        if (!root.TryGetProperty("reportes", out var prop))
        {
            // Try camelCase
            if (!root.TryGetProperty("Reportes", out prop))
                return "none";
        }

        return prop.ValueKind switch
        {
            JsonValueKind.True => "ver",
            JsonValueKind.False => "none",
            JsonValueKind.String => prop.GetString() ?? "none",
            _ => "none"
        };
    }
}
