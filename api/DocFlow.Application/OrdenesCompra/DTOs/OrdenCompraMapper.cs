using DocFlow.Domain.Entities;
using DocFlow.Domain.Entities.OrdenesCompra;
using DocFlow.Domain.Interfaces.OrdenesCompra;

namespace DocFlow.Application.OrdenesCompra.DTOs;

internal static class OrdenCompraMapper
{
    public static OrdenCompraDto ToDto(
        OrdenCompra oc,
        Proveedor? proveedor,
        IReadOnlyList<OrdenCompraAdjuntoMetadata>? adjuntos = null)
    {
        return new OrdenCompraDto(
            oc.Id,
            oc.Numero,
            oc.ProveedorId,
            proveedor?.Nombre ?? string.Empty,
            proveedor?.Rut.Formatted ?? string.Empty,
            oc.Fecha.ToString("o"),
            oc.Moneda,
            oc.FormaPago,
            oc.PlazoEntrega,
            oc.LugarEntrega,
            oc.Observaciones,
            oc.Neto,
            oc.Iva,
            oc.Total,
            oc.Estado.ToString(),
            oc.CreadoPor,
            oc.CreadoEn.ToString("o"),
            oc.ActualizadoEn.ToString("o"),
            oc.AprobadoPor,
            oc.AprobadoEn?.ToString("o"),
            oc.ComentarioAprobacion,
            oc.MotivoAnulacion,
            oc.Items
                .OrderBy(i => i.NumeroLinea)
                .Select(ToItemDto)
                .ToList(),
            (adjuntos ?? []).Select(ToAdjuntoDto).ToList(),
            oc.CodigoMercadoPublico);
    }

    public static OrdenCompraItemDto ToItemDto(OrdenCompraItem item) => new(
        item.Id,
        item.NumeroLinea,
        item.Descripcion,
        item.Cantidad,
        item.PrecioUnitario,
        item.TotalLinea);

    public static OrdenCompraAdjuntoDto ToAdjuntoDto(OrdenCompraAdjuntoMetadata adjunto) => new(
        adjunto.Id,
        adjunto.NombreArchivo,
        adjunto.ContentType,
        adjunto.Tamano,
        adjunto.SubidoPor,
        adjunto.CreadoEn.ToString("o"));
}
