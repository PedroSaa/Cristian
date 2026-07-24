import Badge from '../../atoms/Badge';
import type { EstadoOrdenCompra } from '../../../types/ordenCompra';

// ─── Estado presentation ─────────────────────────────────────────────────────

const ESTADO_META: Record<EstadoOrdenCompra, { label: string; className: string }> = {
  Borrador: { label: 'Borrador', className: 'bg-gray-200 text-gray-700' },
  PendienteAprobacion: { label: 'Pendiente aprobación', className: 'bg-amber-100 text-amber-800' },
  Aprobada: { label: 'Aprobada', className: 'bg-green-100 text-green-800' },
  Rechazada: { label: 'Rechazada', className: 'bg-red-100 text-red-700' },
  Enviada: { label: 'Enviada', className: 'bg-blue-100 text-blue-800' },
  Anulada: { label: 'Anulada', className: 'bg-red-900 text-white' },
};

export const ESTADO_FILTER_OPTIONS = (Object.keys(ESTADO_META) as EstadoOrdenCompra[]).map(
  (estado) => ({ value: estado, label: ESTADO_META[estado].label }),
);

export default function EstadoOrdenBadge({ estado }: { estado: EstadoOrdenCompra }) {
  const meta = ESTADO_META[estado] ?? { label: estado, className: 'bg-gray-200 text-gray-700' };
  return (
    <Badge variant="neutral" className={meta.className}>
      {meta.label}
    </Badge>
  );
}
