using System.Text.Json;
using DocFlow.Application.Common.Interfaces;
using DocFlow.Application.OrdenesCompra.Interfaces;
using Microsoft.Extensions.Logging;

namespace DocFlow.Infrastructure.Services.OrdenesCompra;

/// <summary>
/// HTTP client for the Mercado Público (ChileCompra) public purchase-orders API.
/// The access ticket is read at call time from <see cref="IIntegracionConfigService"/>
/// (Admin → Integraciones), so ticket changes apply without restarting.
///
/// Portal quirks handled here (verified against the live API):
/// - Errors come wrapped as {"Codigo":n,"Mensaje":"..."} — sometimes with HTTP 203 (success range!).
/// - Codigo 10300 ("parámetros no válidos") is what a malformed order code returns → treated as not found.
/// - A well-formed but unknown code returns 200 with Cantidad=0 and an empty Listado.
/// </summary>
public class MercadoPublicoService : IMercadoPublicoService
{
    // Fallback si la card de Integraciones no tiene "Dirección base" configurada.
    private const string DefaultBaseUrl = "https://api.mercadopublico.cl";
    private const string OrdenesPath = "/servicios/v1/publico/ordenesdecompra.json";
    private const int CodigoErrorParametrosInvalidos = 10300;

    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    private readonly HttpClient _httpClient;
    private readonly IIntegracionConfigService _configService;
    private readonly ILogger<MercadoPublicoService> _logger;

    public MercadoPublicoService(
        HttpClient httpClient,
        IIntegracionConfigService configService,
        ILogger<MercadoPublicoService> logger)
    {
        _httpClient = httpClient;
        _configService = configService;
        _logger = logger;

        if (_httpClient.Timeout == TimeSpan.FromSeconds(100)) // default → tighten it
            _httpClient.Timeout = RequestTimeout;
    }

    public async Task<MercadoPublicoOrdenDto?> BuscarPorCodigoAsync(string codigo, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código de Mercado Público es obligatorio.", nameof(codigo));

        var ticket = _configService.GetMercadoPublicoTicket();
        if (string.IsNullOrWhiteSpace(ticket))
            throw new InvalidOperationException(
                "El ticket de acceso de Mercado Público no está configurado. Configúrelo en Administración → Integraciones.");

        // URL base editable desde Admin → Integraciones (card MercadoPublico), con fallback al dominio oficial.
        var baseUrl = _configService.GetMercadoPublicoBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = DefaultBaseUrl;

        var url = $"{baseUrl.TrimEnd('/')}{OrdenesPath}?codigo={Uri.EscapeDataString(codigo.Trim())}&ticket={Uri.EscapeDataString(ticket)}";

        HttpResponseMessage response;
        string body;
        try
        {
            _logger.LogInformation("Consultando orden de compra {Codigo} en Mercado Público", codigo.Trim());
            response = await _httpClient.GetAsync(url, ct);
            body = await response.Content.ReadAsStringAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Mercado Público inaccesible al consultar {Codigo}", codigo.Trim());
            throw new InvalidOperationException("No se pudo conectar con Mercado Público.", ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Timeout consultando {Codigo} en Mercado Público", codigo.Trim());
            throw new InvalidOperationException("Mercado Público no respondió a tiempo.", ex);
        }

        using var doc = ParseJson(body, response.StatusCode);
        var root = doc.RootElement;

        // Success shape: { Cantidad, Listado: [...] }
        if (root.ValueKind == JsonValueKind.Object
            && TryGetPropertyInsensitive(root, "Listado", out var listado)
            && listado.ValueKind == JsonValueKind.Array)
        {
            if (listado.GetArrayLength() == 0)
                return null;

            return MapOrden(listado[0], codigo.Trim());
        }

        // Error envelope shape: { Codigo: n, Mensaje: "..." } (may arrive with HTTP 203 or 500).
        if (root.ValueKind == JsonValueKind.Object
            && TryGetPropertyInsensitive(root, "Mensaje", out var mensajeEl))
        {
            var mensaje = mensajeEl.GetString() ?? "Error desconocido del portal.";
            var codigoError = TryGetPropertyInsensitive(root, "Codigo", out var codigoEl)
                && codigoEl.ValueKind == JsonValueKind.Number
                    ? codigoEl.GetInt32()
                    : (int?)null;

            // A malformed order code is rejected by the portal with "parámetros no válidos":
            // for the caller that simply means the order does not exist.
            if (codigoError == CodigoErrorParametrosInvalidos)
            {
                _logger.LogInformation(
                    "Mercado Público rechazó el código {Codigo} como inválido: {Mensaje}", codigo.Trim(), mensaje);
                return null;
            }

            _logger.LogWarning(
                "Mercado Público respondió error {CodigoError} para {Codigo}: {Mensaje}",
                codigoError, codigo.Trim(), mensaje);
            throw new InvalidOperationException($"Mercado Público respondió un error: {mensaje}");
        }

        throw new InvalidOperationException(
            $"Mercado Público devolvió una respuesta inesperada (HTTP {(int)response.StatusCode}).");
    }

    // ── Parsing helpers (defensive: portal fields are optional and names may vary in casing) ──

    private static JsonDocument ParseJson(string body, System.Net.HttpStatusCode status)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Mercado Público devolvió una respuesta no válida (HTTP {(int)status}).", ex);
        }
    }

    private static MercadoPublicoOrdenDto MapOrden(JsonElement orden, string codigoConsultado)
    {
        var comprador = GetObject(orden, "Comprador");
        var proveedor = GetObject(orden, "Proveedor");
        var fechas = GetObject(orden, "Fechas");

        var estado = GetString(orden, "Estado");
        if (string.IsNullOrWhiteSpace(estado) && GetDecimal(orden, "CodigoEstado") is { } codigoEstado)
            estado = codigoEstado.ToString("0");

        return new MercadoPublicoOrdenDto(
            Codigo: GetString(orden, "Codigo") ?? codigoConsultado,
            Nombre: GetString(orden, "Nombre"),
            Estado: estado,
            FechaCreacion: fechas is { } f ? GetString(f, "FechaCreacion") : null,
            CompradorNombre: comprador is { } c ? GetString(c, "NombreOrganismo") : null,
            CompradorRut: comprador is { } c2 ? GetString(c2, "RutUnidad") : null,
            ProveedorNombre: proveedor is { } p ? GetString(p, "Nombre") : null,
            ProveedorRut: proveedor is { } p2 ? GetString(p2, "RutSucursal") : null,
            MontoTotal: GetDecimal(orden, "Total"),
            Items: MapItems(orden));
    }

    private static IReadOnlyList<MercadoPublicoOrdenItemDto> MapItems(JsonElement orden)
    {
        if (GetObject(orden, "Items") is not { } items
            || !TryGetPropertyInsensitive(items, "Listado", out var listado)
            || listado.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<MercadoPublicoOrdenItemDto>();
        foreach (var item in listado.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var descripcion = GetString(item, "Producto") ?? GetString(item, "EspecificacionComprador");
            result.Add(new MercadoPublicoOrdenItemDto(
                descripcion,
                GetDecimal(item, "Cantidad"),
                GetDecimal(item, "PrecioNeto")));
        }

        return result;
    }

    private static bool TryGetPropertyInsensitive(JsonElement element, string name, out JsonElement value)
    {
        if (element.TryGetProperty(name, out value))
            return true;

        foreach (var prop in element.EnumerateObject())
        {
            if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = prop.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    private static JsonElement? GetObject(JsonElement element, string name)
        => TryGetPropertyInsensitive(element, name, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : null;

    private static string? GetString(JsonElement element, string name)
    {
        if (!TryGetPropertyInsensitive(element, name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }

    private static decimal? GetDecimal(JsonElement element, string name)
    {
        if (!TryGetPropertyInsensitive(element, name, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetDecimal(out var d) => d,
            JsonValueKind.String when decimal.TryParse(
                value.GetString(),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out var d) => d,
            _ => null,
        };
    }
}
