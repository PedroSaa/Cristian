import { useState } from 'react';
import Button from '../../atoms/Button';
import Input from '../../atoms/Input';
import Select from '../../atoms/Select';
import TotalesResumen from './TotalesResumen';
import { formatCLP } from '../../../lib/ordenesCompra/format';
import {
  EMPTY_ITEM,
  fechaError,
  itemCantidadError,
  itemDescripcionError,
  itemPrecioError,
  proveedorError,
  type Totales,
} from '../../../lib/ordenesCompra/form';
import type {
  OrdenCompraFormValues,
  OrdenCompraItemFormValues,
} from '../../../types/ordenCompra';

// ─── Touched tracking (errors show only after blur or a submit attempt) ──────

interface ItemTouched {
  descripcion: boolean;
  cantidad: boolean;
  precioUnitario: boolean;
}

const UNTOUCHED_ITEM: ItemTouched = {
  descripcion: false,
  cantidad: false,
  precioUnitario: false,
};

function FieldError({ id, message }: { id: string; message: string | null }) {
  if (!message) return null;
  return (
    <p id={id} className="mt-1 text-xs font-medium text-red-600">
      {message}
    </p>
  );
}

export interface OrdenCompraFormFieldsProps {
  form: OrdenCompraFormValues;
  onChange: (form: OrdenCompraFormValues) => void;
  proveedorOptions: Array<{ value: string; label: string }>;
  totales: Totales;
  /** When true (e.g. after an invalid submit attempt) every field shows its error. */
  showAllErrors?: boolean;
}

// Presentational form for create/edit: state lives in the page, this component
// only renders the fields and reports changes. Touched flags are local UI state
// (the modal unmounts on close, so they reset per session).
export default function OrdenCompraFormFields({
  form,
  onChange,
  proveedorOptions,
  totales,
  showAllErrors = false,
}: OrdenCompraFormFieldsProps) {
  const [touched, setTouched] = useState<{ proveedorId: boolean; fecha: boolean }>({
    proveedorId: false,
    fecha: false,
  });
  const [touchedItems, setTouchedItems] = useState<ItemTouched[]>(() =>
    form.items.map(() => ({ ...UNTOUCHED_ITEM })),
  );

  const set = (partial: Partial<OrdenCompraFormValues>) => onChange({ ...form, ...partial });

  const setItem = (index: number, partial: Partial<OrdenCompraItemFormValues>) => {
    const items = form.items.map((item, i) => (i === index ? { ...item, ...partial } : item));
    set({ items });
  };

  const touchItem = (index: number, field: keyof ItemTouched) => {
    setTouchedItems((prev) =>
      form.items.map((_, i) => ({
        ...(prev[i] ?? UNTOUCHED_ITEM),
        ...(i === index ? { [field]: true } : {}),
      })),
    );
  };

  const addItem = () => {
    setTouchedItems((prev) => [...prev, { ...UNTOUCHED_ITEM }]);
    set({ items: [...form.items, { ...EMPTY_ITEM }] });
  };

  const removeItem = (index: number) => {
    setTouchedItems((prev) => prev.filter((_, i) => i !== index));
    set({ items: form.items.filter((_, i) => i !== index) });
  };

  const mostrarProveedorError = (touched.proveedorId || showAllErrors)
    ? proveedorError(form.proveedorId)
    : null;
  const mostrarFechaError = (touched.fecha || showAllErrors) ? fechaError(form.fecha) : null;

  return (
    <div className="space-y-4">
      {/* Proveedor + Fecha */}
      <div className="flex flex-wrap gap-4">
        <div className="flex min-w-56 flex-1 flex-col gap-1">
          <label className="text-xs font-medium text-text-base/70">
            Proveedor <span className="text-red-500">*</span>
          </label>
          <Select
            aria-label="Proveedor"
            options={proveedorOptions}
            placeholder="Seleccione un proveedor"
            value={form.proveedorId}
            aria-invalid={mostrarProveedorError ? true : undefined}
            aria-describedby={mostrarProveedorError ? 'oc-proveedor-error' : undefined}
            onChange={(e) => set({ proveedorId: e.target.value })}
            onBlur={() => setTouched((prev) => ({ ...prev, proveedorId: true }))}
          />
          <FieldError id="oc-proveedor-error" message={mostrarProveedorError} />
        </div>
        <div className="flex w-44 flex-col gap-1">
          <label className="text-xs font-medium text-text-base/70">
            Fecha <span className="text-red-500">*</span>
          </label>
          <Input
            type="date"
            aria-label="Fecha"
            value={form.fecha}
            aria-invalid={mostrarFechaError ? true : undefined}
            aria-describedby={mostrarFechaError ? 'oc-fecha-error' : undefined}
            onChange={(e) => set({ fecha: e.target.value })}
            onBlur={() => setTouched((prev) => ({ ...prev, fecha: true }))}
          />
          <FieldError id="oc-fecha-error" message={mostrarFechaError} />
        </div>
      </div>

      {/* Forma de pago + Plazo entrega */}
      <div className="flex flex-wrap gap-4">
        <div className="flex min-w-56 flex-1 flex-col gap-1">
          <label className="text-xs font-medium text-text-base/65" htmlFor="oc-forma-pago">
            Forma de pago
          </label>
          <Input
            id="oc-forma-pago"
            type="text"
            maxLength={200}
            placeholder="Ej: Transferencia a 30 días"
            value={form.formaPago}
            onChange={(e) => set({ formaPago: e.target.value })}
          />
        </div>
        <div className="flex min-w-56 flex-1 flex-col gap-1">
          <label className="text-xs font-medium text-text-base/65" htmlFor="oc-plazo-entrega">
            Plazo de entrega
          </label>
          <Input
            id="oc-plazo-entrega"
            type="text"
            maxLength={200}
            placeholder="Ej: 15 días hábiles"
            value={form.plazoEntrega}
            onChange={(e) => set({ plazoEntrega: e.target.value })}
          />
        </div>
      </div>

      {/* Lugar entrega */}
      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-text-base/65" htmlFor="oc-lugar-entrega">
          Lugar de entrega
        </label>
        <Input
          id="oc-lugar-entrega"
          type="text"
          maxLength={300}
          placeholder="Dirección de entrega"
          value={form.lugarEntrega}
          onChange={(e) => set({ lugarEntrega: e.target.value })}
        />
      </div>

      {/* Observaciones */}
      <div className="flex flex-col gap-1">
        <label className="text-xs font-medium text-text-base/65">Observaciones</label>
        <textarea
          aria-label="Observaciones"
          className="block w-full rounded border border-border-base bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
          rows={2}
          maxLength={2000}
          value={form.observaciones}
          onChange={(e) => set({ observaciones: e.target.value })}
        />
      </div>

      {/* Items */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-text-base">Ítems</h3>
          <Button variant="secondary" size="sm" onClick={addItem}>
            + Agregar ítem
          </Button>
        </div>

        {form.items.length === 0 && (
          <p className="rounded border border-dashed border-border-base px-3 py-4 text-center text-xs text-text-base/55">
            Sin ítems. Agregá al menos un ítem para poder enviar la orden a aprobación.
          </p>
        )}

        {form.items.map((item, index) => {
          const cantidad = Number(item.cantidad);
          const precio = Number(item.precioUnitario);
          const totalLinea =
            Number.isFinite(cantidad) && Number.isFinite(precio) && cantidad > 0 && precio >= 0
              ? cantidad * precio
              : null;
          const itemTouched = touchedItems[index] ?? UNTOUCHED_ITEM;
          const descripcionError = (itemTouched.descripcion || showAllErrors)
            ? itemDescripcionError(item)
            : null;
          const cantidadError = (itemTouched.cantidad || showAllErrors)
            ? itemCantidadError(item)
            : null;
          const precioError = (itemTouched.precioUnitario || showAllErrors)
            ? itemPrecioError(item)
            : null;
          return (
            <div key={index} className="flex flex-wrap items-start gap-2 rounded border border-border-base p-2">
              <div className="flex min-w-48 flex-1 flex-col gap-1">
                <label className="text-[11px] font-medium text-text-base/60">
                  Descripción <span className="text-red-500">*</span>
                </label>
                <Input
                  type="text"
                  maxLength={300}
                  aria-label={`Descripción ítem ${index + 1}`}
                  placeholder="Descripción del ítem"
                  value={item.descripcion}
                  aria-invalid={descripcionError ? true : undefined}
                  aria-describedby={descripcionError ? `oc-item-${index}-descripcion-error` : undefined}
                  onChange={(e) => setItem(index, { descripcion: e.target.value })}
                  onBlur={() => touchItem(index, 'descripcion')}
                />
                <FieldError id={`oc-item-${index}-descripcion-error`} message={descripcionError} />
              </div>
              <div className="flex w-28 flex-col gap-1">
                <label className="text-[11px] font-medium text-text-base/60">Cantidad</label>
                <Input
                  type="number"
                  min={0}
                  step="any"
                  aria-label={`Cantidad ítem ${index + 1}`}
                  value={item.cantidad}
                  aria-invalid={cantidadError ? true : undefined}
                  aria-describedby={cantidadError ? `oc-item-${index}-cantidad-error` : undefined}
                  onChange={(e) => setItem(index, { cantidad: e.target.value })}
                  onBlur={() => touchItem(index, 'cantidad')}
                />
                <FieldError id={`oc-item-${index}-cantidad-error`} message={cantidadError} />
              </div>
              <div className="flex w-36 flex-col gap-1">
                <label className="text-[11px] font-medium text-text-base/60">Precio unitario</label>
                <Input
                  type="number"
                  min={0}
                  step="any"
                  aria-label={`Precio unitario ítem ${index + 1}`}
                  value={item.precioUnitario}
                  aria-invalid={precioError ? true : undefined}
                  aria-describedby={precioError ? `oc-item-${index}-precio-error` : undefined}
                  onChange={(e) => setItem(index, { precioUnitario: e.target.value })}
                  onBlur={() => touchItem(index, 'precioUnitario')}
                />
                <FieldError id={`oc-item-${index}-precio-error`} message={precioError} />
              </div>
              <div className="flex w-32 flex-col gap-1">
                <span className="text-[11px] font-medium text-text-base/60">Total línea</span>
                <span className="px-1 py-2 text-sm tabular-nums">
                  {totalLinea === null ? '—' : formatCLP(totalLinea)}
                </span>
              </div>
              <Button
                variant="ghost"
                size="sm"
                aria-label={`Quitar ítem ${index + 1}`}
                className="mt-5 text-red-600 hover:text-red-800"
                onClick={() => removeItem(index)}
              >
                ✕
              </Button>
            </div>
          );
        })}
      </div>

      {/* Live totals */}
      <TotalesResumen totales={totales} />
    </div>
  );
}
