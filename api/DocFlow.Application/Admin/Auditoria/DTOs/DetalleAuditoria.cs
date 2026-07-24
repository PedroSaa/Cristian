using System.Text.Json;

namespace DocFlow.Application.Admin.Auditoria.DTOs;

public record DetalleAuditoria
{
    public string? ValorAnterior { get; init; }
    public string? ValorNuevo { get; init; }
    public string? Metadata { get; init; }

    public static DetalleAuditoria? FromString(string? detalle)
    {
        if (string.IsNullOrWhiteSpace(detalle)) return null;

        // Try to parse as JSON
        try
        {
            var parsed = JsonSerializer.Deserialize<DetalleAuditoria>(detalle, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (parsed != null && (parsed.ValorAnterior != null || parsed.ValorNuevo != null || parsed.Metadata != null))
                return parsed;
        }
        catch (JsonException) { }

        // Legacy plain text fallback
        return null;
    }

    public string ToJsonString()
    {
        return JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Returns the structured detail or falls back to the raw text for legacy entries.
    /// </summary>
    public static string Render(string? detalle)
    {
        var parsed = FromString(detalle);
        if (parsed == null) return detalle ?? string.Empty;

        var parts = new List<string>();
        if (parsed.ValorAnterior != null)
            parts.Add($"Valor anterior: {parsed.ValorAnterior}");
        if (parsed.ValorNuevo != null)
            parts.Add($"Valor nuevo: {parsed.ValorNuevo}");
        if (parsed.Metadata != null)
            parts.Add($"Metadata: {parsed.Metadata}");

        return string.Join("\n", parts);
    }
}
