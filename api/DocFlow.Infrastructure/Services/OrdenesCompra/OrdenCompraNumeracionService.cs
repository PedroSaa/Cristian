using System.Globalization;
using System.Text.RegularExpressions;
using DocFlow.Domain.Interfaces;
using DocFlow.Domain.Interfaces.OrdenesCompra;
using DocFlow.Domain.ValueObjects;

namespace DocFlow.Infrastructure.Services.OrdenesCompra;

/// <summary>
/// Issues purchase order numbers by consuming the existing numbering engine as-is:
/// <see cref="ICounterService"/> for the atomic counter (code "ORDEN_COMPRA", created on demand,
/// periodic reset handled by the engine) and <see cref="IPlantillaService"/> for the active
/// template pattern. Formatting is resolved inside this module so the engine stays untouched.
/// </summary>
public class OrdenCompraNumeracionService : IOrdenCompraNumeracionService
{
    public const string CodigoContador = "ORDEN_COMPRA";
    public const string OrgDepCodGlobal = "GLOBAL";
    public const string PatronPorDefecto = "OC-{ano}-{correlativo}";
    public const int RellenoCerosPorDefecto = 4;
    public const string PeriodicidadPorDefecto = "ANUAL";

    private static readonly Regex TokenRegex = new(@"\{([^}]*)\}", RegexOptions.Compiled);
    private static readonly Regex EspaciosRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly ICounterService _counterService;
    private readonly IPlantillaService _plantillaService;

    public OrdenCompraNumeracionService(ICounterService counterService, IPlantillaService plantillaService)
    {
        _counterService = counterService;
        _plantillaService = plantillaService;
    }

    public async Task<string> ObtenerSiguienteNumeroAsync(CancellationToken ct = default)
    {
        // Las plantillas activas del motor numeran DOCUMENTOS; para las OC solo aplica una
        // plantilla designada por convención: la descripción debe contener la frase completa
        // "orden de compra" (p.ej. "Orden de Compra"). Un match más laxo (solo "orden") dejaría
        // que plantillas ajenas ("Orden de Pago", "Ordenanza Municipal") secuestren la numeración.
        // Con varias candidatas se ordena por descripción para que la elección sea determinista.
        // Sin plantilla designada rige el patrón por defecto del módulo.
        var plantilla = (await _plantillaService.ListarAsync(soloActivos: true, ct))
            .Where(p => EspaciosRegex.Replace(p.Descripcion ?? string.Empty, " ")
                .Contains("orden de compra", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Descripcion, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var patron = !string.IsNullOrWhiteSpace(plantilla?.Patron) ? plantilla!.Patron! : PatronPorDefecto;
        var rellenoCeros = plantilla?.RellenoCeros ?? RellenoCerosPorDefecto;
        var periodicidad = plantilla?.Periodicidad ?? PeriodicidadPorDefecto;

        var key = new CounterKey(CodigoContador, OrgDepCodGlobal);

        long correlativo;
        try
        {
            correlativo = await _counterService.NextValueAsync(key, periodicidad, ct);
        }
        catch (KeyNotFoundException)
        {
            // The counter does not exist yet — create it with default values and retry.
            try
            {
                await _counterService.CreateCounterAsync(key, plantilla?.ValorInicial ?? 0, periodicidad, ct);
            }
            catch (InvalidOperationException)
            {
                // Created concurrently by another request — the retry below will succeed.
            }

            correlativo = await _counterService.NextValueAsync(key, periodicidad, ct);
        }

        return FormatearNumero(patron, correlativo, rellenoCeros, DateTime.UtcNow);
    }

    /// <summary>
    /// Renders a numbering pattern for a purchase order. Supports the engine tokens
    /// {correlativo}, {ano}, {ano2}, {mes}, {dia} and {fecha:FORMAT}; document-centric tokens
    /// ({tipo}, {formato}, {organismo}) do not apply to purchase orders and render empty.
    /// </summary>
    internal static string FormatearNumero(string patron, long correlativo, int rellenoCeros, DateTime fecha)
    {
        return TokenRegex.Replace(patron, match =>
        {
            var inner = match.Groups[1].Value;
            var partes = inner.Split(':', 2);
            var nombre = partes[0].Trim().ToLowerInvariant();

            return nombre switch
            {
                "correlativo" => rellenoCeros > 0
                    ? correlativo.ToString(CultureInfo.InvariantCulture).PadLeft(rellenoCeros, '0')
                    : correlativo.ToString(CultureInfo.InvariantCulture),
                "ano" => fecha.Year.ToString("D4", CultureInfo.InvariantCulture),
                "ano2" => fecha.ToString("yy", CultureInfo.InvariantCulture),
                "mes" => fecha.Month.ToString("D2", CultureInfo.InvariantCulture),
                "dia" => fecha.Day.ToString("D2", CultureInfo.InvariantCulture),
                "fecha" => partes.Length > 1
                    ? fecha.ToString(partes[1], CultureInfo.InvariantCulture)
                    : fecha.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture),
                _ => string.Empty,
            };
        });
    }
}
