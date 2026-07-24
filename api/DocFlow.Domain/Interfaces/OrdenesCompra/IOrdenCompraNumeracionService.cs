namespace DocFlow.Domain.Interfaces.OrdenesCompra;

/// <summary>
/// Issues the next business number for a purchase order using the existing numbering engine
/// (counter code "ORDEN_COMPRA"). The counter is created on demand and the number is formatted
/// with the active numbering template pattern, or the module default "OC-{ano}-{correlativo}"
/// (zero-padded to 4) when no template is active.
/// </summary>
public interface IOrdenCompraNumeracionService
{
    Task<string> ObtenerSiguienteNumeroAsync(CancellationToken ct = default);
}
