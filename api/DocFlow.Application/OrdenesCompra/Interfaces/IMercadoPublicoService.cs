namespace DocFlow.Application.OrdenesCompra.Interfaces;

/// <summary>Line item of a Mercado Público purchase order. All fields are best-effort (portal payloads vary).</summary>
public record MercadoPublicoOrdenItemDto(
    string? Descripcion,
    decimal? Cantidad,
    decimal? PrecioUnitario);

/// <summary>
/// Purchase order as published in the Mercado Público (ChileCompra) public API.
/// Mapped defensively: every field except the code may be missing in the portal payload.
/// </summary>
public record MercadoPublicoOrdenDto(
    string Codigo,
    string? Nombre,
    string? Estado,
    string? FechaCreacion,
    string? CompradorNombre,
    string? CompradorRut,
    string? ProveedorNombre,
    string? ProveedorRut,
    decimal? MontoTotal,
    IReadOnlyList<MercadoPublicoOrdenItemDto> Items);

/// <summary>
/// Read-only client for the Mercado Público public purchase-orders API.
/// </summary>
public interface IMercadoPublicoService
{
    /// <summary>
    /// Looks up a purchase order by its Mercado Público code.
    /// Returns null when the portal does not know the code.
    /// Throws <see cref="InvalidOperationException"/> when the access ticket is not configured
    /// or the portal is unreachable / returns an error (mapped to 503 at the API layer).
    /// </summary>
    Task<MercadoPublicoOrdenDto?> BuscarPorCodigoAsync(string codigo, CancellationToken ct = default);
}
