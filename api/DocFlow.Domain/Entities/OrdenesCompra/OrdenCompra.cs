using DocFlow.Domain.Enums;

namespace DocFlow.Domain.Entities.OrdenesCompra;

/// <summary>
/// Purchase order line input. Line numbers are assigned sequentially by <see cref="OrdenCompra.ReemplazarItems"/>.
/// </summary>
public record OrdenCompraItemData(string Descripcion, decimal Cantidad, decimal PrecioUnitario);

/// <summary>
/// Purchase order aggregate root. Totals are always derived from the items:
/// Neto = sum of line totals, Iva = 19% (Chile) rounded to 0 decimals, Total = Neto + Iva.
/// </summary>
public class OrdenCompra
{
    public const decimal TasaIva = 0.19m;

    private readonly List<OrdenCompraItem> _items = [];
    private readonly List<OrdenCompraAdjunto> _adjuntos = [];

    public Guid Id { get; private set; }

    /// <summary>Business number. Null while the order is a draft; assigned on first submission for approval.</summary>
    public string? Numero { get; private set; }

    public Guid ProveedorId { get; private set; }
    public DateTime Fecha { get; private set; }
    public string Moneda { get; private set; } = "CLP";
    public string? FormaPago { get; private set; }
    public string? PlazoEntrega { get; private set; }
    public string? LugarEntrega { get; private set; }
    public string? Observaciones { get; private set; }
    public decimal Neto { get; private set; }
    public decimal Iva { get; private set; }
    public decimal Total { get; private set; }
    public EstadoOrdenCompra Estado { get; private set; }
    public Guid CreadoPor { get; private set; }
    public DateTime CreadoEn { get; private set; }
    public DateTime ActualizadoEn { get; private set; }

    /// <summary>User that made the last approval decision (approve or reject). Cleared on resubmission.</summary>
    public Guid? AprobadoPor { get; private set; }
    public DateTime? AprobadoEn { get; private set; }
    public string? ComentarioAprobacion { get; private set; }
    public string? MotivoAnulacion { get; private set; }

    /// <summary>Mercado Público (ChileCompra) purchase order code this order is linked to, if any.</summary>
    public string? CodigoMercadoPublico { get; private set; }

    public IReadOnlyCollection<OrdenCompraItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<OrdenCompraAdjunto> Adjuntos => _adjuntos.AsReadOnly();

    private OrdenCompra() { }

    public static OrdenCompra Crear(
        Guid id,
        Guid proveedorId,
        DateTime fecha,
        Guid creadoPor,
        string? moneda = null,
        string? formaPago = null,
        string? plazoEntrega = null,
        string? lugarEntrega = null,
        string? observaciones = null)
    {
        if (proveedorId == Guid.Empty)
            throw new ArgumentException("El proveedor es obligatorio.", nameof(proveedorId));
        if (creadoPor == Guid.Empty)
            throw new ArgumentException("El usuario creador es obligatorio.", nameof(creadoPor));

        var now = DateTime.UtcNow;
        return new OrdenCompra
        {
            Id = id,
            ProveedorId = proveedorId,
            Fecha = NormalizarFechaUtc(fecha),
            Moneda = NormalizarMoneda(moneda),
            FormaPago = Limpiar(formaPago),
            PlazoEntrega = Limpiar(plazoEntrega),
            LugarEntrega = Limpiar(lugarEntrega),
            Observaciones = Limpiar(observaciones),
            Estado = EstadoOrdenCompra.Borrador,
            CreadoPor = creadoPor,
            CreadoEn = now,
            ActualizadoEn = now,
        };
    }

    /// <summary>Updates header data. Only allowed while the order is editable (Borrador or Rechazada).</summary>
    public void ActualizarDatos(
        Guid proveedorId,
        DateTime fecha,
        string? moneda,
        string? formaPago,
        string? plazoEntrega,
        string? lugarEntrega,
        string? observaciones)
    {
        ExigirEditable();

        if (proveedorId == Guid.Empty)
            throw new ArgumentException("El proveedor es obligatorio.", nameof(proveedorId));

        ProveedorId = proveedorId;
        Fecha = NormalizarFechaUtc(fecha);
        Moneda = NormalizarMoneda(moneda);
        FormaPago = Limpiar(formaPago);
        PlazoEntrega = Limpiar(plazoEntrega);
        LugarEntrega = Limpiar(lugarEntrega);
        Observaciones = Limpiar(observaciones);
        Tocar();
    }

    /// <summary>Replaces all items and recalculates totals. Only allowed while editable.</summary>
    public void ReemplazarItems(IEnumerable<OrdenCompraItemData> items)
    {
        ExigirEditable();

        var datos = items?.ToList() ?? [];

        _items.Clear();
        var linea = 1;
        foreach (var dato in datos)
        {
            _items.Add(OrdenCompraItem.Crear(
                Guid.NewGuid(), Id, linea++, dato.Descripcion, dato.Cantidad, dato.PrecioUnitario));
        }

        RecalcularTotales();
        Tocar();
    }

    /// <summary>
    /// Submits the order for approval (Borrador|Rechazada → PendienteAprobacion). Requires at least one item.
    /// Assigns <paramref name="numero"/> only when the order has no number yet — a resubmission
    /// after a rejection keeps the originally assigned number.
    /// </summary>
    public void EnviarAAprobacion(string? numero)
    {
        if (Estado is not (EstadoOrdenCompra.Borrador or EstadoOrdenCompra.Rechazada))
            throw new InvalidOperationException(
                $"Solo se puede enviar a aprobación una orden en estado Borrador o Rechazada (estado actual: {Estado}).");

        if (_items.Count == 0)
            throw new InvalidOperationException("La orden de compra debe tener al menos un ítem para enviarse a aprobación.");

        if (Numero is null)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("El número de la orden de compra es obligatorio al emitirla.", nameof(numero));
            Numero = numero.Trim();
        }

        AprobadoPor = null;
        AprobadoEn = null;
        ComentarioAprobacion = null;
        Estado = EstadoOrdenCompra.PendienteAprobacion;
        Tocar();
    }

    /// <summary>
    /// Approves the order (PendienteAprobacion → Aprobada). The approver cannot be the creator
    /// (standard internal-control segregation of duties).
    /// </summary>
    public void Aprobar(Guid userId, string? comentario = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("El usuario aprobador es obligatorio.", nameof(userId));

        if (Estado != EstadoOrdenCompra.PendienteAprobacion)
            throw new InvalidOperationException(
                $"Solo se puede aprobar una orden pendiente de aprobación (estado actual: {Estado}).");

        if (userId == CreadoPor)
            throw new InvalidOperationException("Un usuario no puede aprobar su propia orden de compra.");

        Estado = EstadoOrdenCompra.Aprobada;
        AprobadoPor = userId;
        AprobadoEn = DateTime.UtcNow;
        ComentarioAprobacion = Limpiar(comentario);
        Tocar();
    }

    /// <summary>Rejects the order (PendienteAprobacion → Rechazada). A comment is mandatory.</summary>
    public void Rechazar(Guid userId, string comentario)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("El usuario que rechaza es obligatorio.", nameof(userId));
        if (string.IsNullOrWhiteSpace(comentario))
            throw new ArgumentException("El comentario de rechazo es obligatorio.", nameof(comentario));

        if (Estado != EstadoOrdenCompra.PendienteAprobacion)
            throw new InvalidOperationException(
                $"Solo se puede rechazar una orden pendiente de aprobación (estado actual: {Estado}).");

        Estado = EstadoOrdenCompra.Rechazada;
        AprobadoPor = userId;
        AprobadoEn = DateTime.UtcNow;
        ComentarioAprobacion = comentario.Trim();
        Tocar();
    }

    /// <summary>Marks the order as sent to the provider (Aprobada → Enviada).</summary>
    public void MarcarEnviada()
    {
        if (Estado != EstadoOrdenCompra.Aprobada)
            throw new InvalidOperationException(
                $"Solo se puede marcar como enviada una orden aprobada (estado actual: {Estado}).");

        Estado = EstadoOrdenCompra.Enviada;
        Tocar();
    }

    /// <summary>Cancels the order from any state except Anulada. A reason is mandatory.</summary>
    public void Anular(string motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de anulación es obligatorio.", nameof(motivo));

        if (Estado == EstadoOrdenCompra.Anulada)
            throw new InvalidOperationException("La orden de compra ya está anulada.");

        Estado = EstadoOrdenCompra.Anulada;
        MotivoAnulacion = motivo.Trim();
        Tocar();
    }

    /// <summary>
    /// Links the order to a Mercado Público purchase order code. Allowed in any state except Anulada.
    /// The code existence in the portal is validated at the Application layer.
    /// </summary>
    public void VincularMercadoPublico(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            throw new ArgumentException("El código de Mercado Público es obligatorio.", nameof(codigo));

        var limpio = codigo.Trim();
        if (limpio.Length > 40)
            throw new ArgumentException("El código de Mercado Público no puede superar los 40 caracteres.", nameof(codigo));

        if (Estado == EstadoOrdenCompra.Anulada)
            throw new InvalidOperationException("No se puede vincular a Mercado Público una orden anulada.");

        CodigoMercadoPublico = limpio;
        Tocar();
    }

    /// <summary>
    /// Guards attachment addition. Allowed in any state except Anulada: post-approval
    /// supporting documents (delivery notes, goods receptions, invoices) are legitimate
    /// additions to the order's file.
    /// </summary>
    public void ExigirPuedeAgregarAdjuntos()
    {
        if (Estado == EstadoOrdenCompra.Anulada)
            throw new InvalidOperationException("No se pueden agregar adjuntos a una orden de compra anulada.");
    }

    /// <summary>
    /// Guards attachment removal. Allowed only while there is no approval decision yet
    /// (Borrador, Rechazada, PendienteAprobacion). Internal control: management approved the
    /// order based on its supporting documents, so deleting (or swapping) them post-approval
    /// would break the audit trail that justified the purchase.
    /// </summary>
    public void ExigirPuedeEliminarAdjuntos()
    {
        if (Estado == EstadoOrdenCompra.Anulada)
            throw new InvalidOperationException("No se pueden eliminar adjuntos de una orden de compra anulada.");

        if (Estado is EstadoOrdenCompra.Aprobada or EstadoOrdenCompra.Enviada)
            throw new InvalidOperationException(
                $"No se pueden eliminar respaldos de una orden aprobada o enviada (estado actual: {Estado}).");
    }

    /// <summary>Removes the Mercado Público link. Idempotent.</summary>
    public void DesvincularMercadoPublico()
    {
        if (CodigoMercadoPublico is null)
            return;

        CodigoMercadoPublico = null;
        Tocar();
    }

    // ── Helpers ──

    private void ExigirEditable()
    {
        if (Estado is not (EstadoOrdenCompra.Borrador or EstadoOrdenCompra.Rechazada))
            throw new InvalidOperationException(
                $"Solo se puede modificar una orden en estado Borrador o Rechazada (estado actual: {Estado}).");
    }

    private void RecalcularTotales()
    {
        Neto = _items.Sum(i => i.TotalLinea);
        // Half-away-from-zero: el default de Math.Round es banker's rounding (ToEven),
        // que en el midpoint (p. ej. neto 150 → 28.5) daría 28; el SII espera 29.
        Iva = Math.Round(Neto * TasaIva, 0, MidpointRounding.AwayFromZero);
        Total = Neto + Iva;
    }

    private void Tocar() => ActualizadoEn = DateTime.UtcNow;

    private static string NormalizarMoneda(string? moneda)
        => string.IsNullOrWhiteSpace(moneda) ? "CLP" : moneda.Trim().ToUpperInvariant();

    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    // La columna es timestamptz: Npgsql solo acepta DateTime con Kind=Utc.
    private static DateTime NormalizarFechaUtc(DateTime fecha) => fecha.Kind switch
    {
        DateTimeKind.Utc => fecha,
        DateTimeKind.Local => fecha.ToUniversalTime(),
        _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc),
    };
}
