import type {
  CrearOrdenCompraRequest,
  OrdenCompraDto,
  OrdenCompraFormValues,
  OrdenCompraItemFormValues,
} from '../../types/ordenCompra';

// ─── Form defaults ───────────────────────────────────────────────────────────

export const EMPTY_ITEM: OrdenCompraItemFormValues = {
  descripcion: '',
  cantidad: '1',
  precioUnitario: '0',
};

export function emptyForm(): OrdenCompraFormValues {
  return {
    proveedorId: '',
    fecha: new Date().toISOString().slice(0, 10),
    formaPago: '',
    plazoEntrega: '',
    lugarEntrega: '',
    observaciones: '',
    items: [{ ...EMPTY_ITEM }],
  };
}

export function ordenToForm(oc: OrdenCompraDto): OrdenCompraFormValues {
  return {
    proveedorId: oc.proveedorId,
    fecha: oc.fecha.slice(0, 10),
    formaPago: oc.formaPago ?? '',
    plazoEntrega: oc.plazoEntrega ?? '',
    lugarEntrega: oc.lugarEntrega ?? '',
    observaciones: oc.observaciones ?? '',
    items: oc.items.map((item) => ({
      descripcion: item.descripcion,
      cantidad: String(item.cantidad),
      precioUnitario: String(item.precioUnitario),
    })),
  };
}

// ─── Live totals (same formula as backend: neto=Σ(cant*precio), iva=round(neto*0.19)) ──

export interface Totales {
  neto: number;
  iva: number;
  total: number;
}

export function calcularTotales(items: OrdenCompraItemFormValues[]): Totales {
  const neto = items.reduce((sum, item) => {
    const cantidad = Number(item.cantidad);
    const precio = Number(item.precioUnitario);
    if (!Number.isFinite(cantidad) || !Number.isFinite(precio) || cantidad <= 0 || precio < 0) {
      return sum;
    }
    return sum + cantidad * precio;
  }, 0);
  const iva = Math.round(neto * 0.19);
  return { neto, iva, total: neto + iva };
}

// ─── Per-field validation (inline messages) ──────────────────────────────────
// Each helper returns the user-facing message, or null when the value is valid.

export const VALIDATION_MESSAGES = {
  proveedor: 'Seleccione un proveedor.',
  fecha: 'La fecha es obligatoria.',
  descripcion: 'La descripción es obligatoria.',
  cantidad: 'Ingrese una cantidad mayor que cero.',
  precio: 'Ingrese un precio válido (0 o mayor).',
} as const;

export function proveedorError(proveedorId: string): string | null {
  return proveedorId ? null : VALIDATION_MESSAGES.proveedor;
}

export function fechaError(fecha: string): string | null {
  return fecha ? null : VALIDATION_MESSAGES.fecha;
}

export function itemDescripcionError(item: OrdenCompraItemFormValues): string | null {
  return item.descripcion.trim() ? null : VALIDATION_MESSAGES.descripcion;
}

// Empty strings are invalid before coercion: Number('') === 0 would silently
// submit a $0 price or fall through the cantidad check.
export function itemCantidadError(item: OrdenCompraItemFormValues): string | null {
  if (!item.cantidad.trim()) return VALIDATION_MESSAGES.cantidad;
  const cantidad = Number(item.cantidad);
  return !Number.isFinite(cantidad) || cantidad <= 0 ? VALIDATION_MESSAGES.cantidad : null;
}

export function itemPrecioError(item: OrdenCompraItemFormValues): string | null {
  if (!item.precioUnitario.trim()) return VALIDATION_MESSAGES.precio;
  const precio = Number(item.precioUnitario);
  return !Number.isFinite(precio) || precio < 0 ? VALIDATION_MESSAGES.precio : null;
}

export function itemInvalido(item: OrdenCompraItemFormValues): boolean {
  return (
    itemDescripcionError(item) !== null ||
    itemCantidadError(item) !== null ||
    itemPrecioError(item) !== null
  );
}

export function formInvalido(form: OrdenCompraFormValues): boolean {
  return !form.proveedorId || !form.fecha || form.items.some(itemInvalido);
}

// ─── Form → request payload ──────────────────────────────────────────────────

export function formToRequest(form: OrdenCompraFormValues): CrearOrdenCompraRequest {
  return {
    proveedorId: form.proveedorId,
    fecha: form.fecha,
    formaPago: form.formaPago.trim() || undefined,
    plazoEntrega: form.plazoEntrega.trim() || undefined,
    lugarEntrega: form.lugarEntrega.trim() || undefined,
    observaciones: form.observaciones.trim() || undefined,
    items: form.items.map((item) => ({
      descripcion: item.descripcion.trim(),
      cantidad: Number(item.cantidad),
      precioUnitario: Number(item.precioUnitario),
    })),
  };
}
