namespace DocFlow.Application.OrdenesCompra.DTOs;

// ── Item input (create/update payloads) ──
public record OrdenCompraItemInput(
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario);

// ── Item detail ──
public record OrdenCompraItemDto(
    Guid Id,
    int NumeroLinea,
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal TotalLinea);

// ── Attachment metadata (no binary content) ──
public record OrdenCompraAdjuntoDto(
    Guid Id,
    string NombreArchivo,
    string ContentType,
    long Tamano,
    Guid SubidoPor,
    string CreadoEn);

// ── Attachment content (download) ──
public record OrdenCompraAdjuntoContenidoDto(
    string NombreArchivo,
    string ContentType,
    byte[] Contenido);

// ── Full detail DTO ──
public record OrdenCompraDto(
    Guid Id,
    string? Numero,
    Guid ProveedorId,
    string ProveedorNombre,
    string ProveedorRut,
    string Fecha,
    string Moneda,
    string? FormaPago,
    string? PlazoEntrega,
    string? LugarEntrega,
    string? Observaciones,
    decimal Neto,
    decimal Iva,
    decimal Total,
    string Estado,
    Guid CreadoPor,
    string CreadoEn,
    string ActualizadoEn,
    Guid? AprobadoPor,
    string? AprobadoEn,
    string? ComentarioAprobacion,
    string? MotivoAnulacion,
    IReadOnlyList<OrdenCompraItemDto> Items,
    IReadOnlyList<OrdenCompraAdjuntoDto> Adjuntos,
    string? CodigoMercadoPublico = null);

// ── Summary for list ──
public record OrdenCompraListItemDto(
    Guid Id,
    string? Numero,
    Guid ProveedorId,
    string ProveedorNombre,
    string Fecha,
    string Moneda,
    decimal Neto,
    decimal Iva,
    decimal Total,
    string Estado,
    string CreadoEn,
    string? CodigoMercadoPublico = null);

// ── Paginated response (same shape as Proveedores) ──
public record PaginatedOrdenesCompraResponse(
    IReadOnlyList<OrdenCompraListItemDto> Items,
    int TotalItems,
    int Pagina,
    int TotalPaginas);

// ── PDF result ──
public record OrdenCompraPdfDto(
    string NombreArchivo,
    byte[] Contenido);
