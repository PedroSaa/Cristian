namespace DocFlow.Application.OrdenesCompra.Interfaces;

public record OrdenCompraPdfItem(
    int NumeroLinea,
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal TotalLinea);

public record OrdenCompraPdfData(
    string? Numero,
    DateTime Fecha,
    string Estado,
    string Moneda,
    string ProveedorNombre,
    string ProveedorRut,
    string ProveedorContacto,
    string ProveedorEmail,
    string ProveedorTelefono,
    string ProveedorDireccion,
    string? FormaPago,
    string? PlazoEntrega,
    string? LugarEntrega,
    string? Observaciones,
    decimal Neto,
    decimal Iva,
    decimal Total,
    IReadOnlyList<OrdenCompraPdfItem> Items,
    string? AprobadorNombre,
    DateTime? AprobadoEn,
    string? ComentarioAprobacion);

/// <summary>Renders the purchase order PDF with the module-owned layout (QuestPDF).</summary>
public interface IOrdenCompraPdfService
{
    byte[] Generar(OrdenCompraPdfData data);
}
