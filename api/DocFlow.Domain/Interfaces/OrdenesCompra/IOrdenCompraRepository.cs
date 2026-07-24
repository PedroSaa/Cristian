using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Enums;

namespace DocFlow.Domain.Interfaces.OrdenesCompra;

/// <summary>List projection row: purchase order header joined with the provider name (read-only).</summary>
public record OrdenCompraListRow(
    Guid Id,
    string? Numero,
    Guid ProveedorId,
    string ProveedorNombre,
    DateTime Fecha,
    string Moneda,
    decimal Neto,
    decimal Iva,
    decimal Total,
    EstadoOrdenCompra Estado,
    DateTime CreadoEn,
    string? CodigoMercadoPublico = null);

/// <summary>Attachment metadata projection — never loads the binary content.</summary>
public record OrdenCompraAdjuntoMetadata(
    Guid Id,
    string NombreArchivo,
    string ContentType,
    long Tamano,
    Guid SubidoPor,
    DateTime CreadoEn);

public interface IOrdenCompraRepository
{
    Task AddAsync(OrdenCompra ordenCompra, CancellationToken ct = default);

    /// <summary>Loads the order with its items. Attachments are NOT included (use the metadata/content methods).</summary>
    Task<OrdenCompra?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task UpdateAsync(OrdenCompra ordenCompra, CancellationToken ct = default);

    Task<(IReadOnlyList<OrdenCompraListRow> Items, int TotalCount)> GetListAsync(
        EstadoOrdenCompra? estado = null,
        Guid? proveedorId = null,
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<IReadOnlyList<OrdenCompraAdjuntoMetadata>> GetAdjuntosMetadataAsync(Guid ordenCompraId, CancellationToken ct = default);

    Task<OrdenCompraAdjunto?> GetAdjuntoAsync(Guid ordenCompraId, Guid adjuntoId, CancellationToken ct = default);

    Task AddAdjuntoAsync(OrdenCompraAdjunto adjunto, CancellationToken ct = default);

    Task RemoveAdjuntoAsync(OrdenCompraAdjunto adjunto, CancellationToken ct = default);
}
