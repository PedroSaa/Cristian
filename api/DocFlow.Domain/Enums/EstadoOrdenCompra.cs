namespace DocFlow.Domain.Enums;

/// <summary>
/// Lifecycle states of a purchase order.
/// Flow: Borrador → PendienteAprobacion → (Aprobada → Enviada | Rechazada → PendienteAprobacion).
/// Anulada is terminal and reachable from any other state.
/// </summary>
public enum EstadoOrdenCompra
{
    Borrador,
    PendienteAprobacion,
    Aprobada,
    Rechazada,
    Enviada,
    Anulada,
}
