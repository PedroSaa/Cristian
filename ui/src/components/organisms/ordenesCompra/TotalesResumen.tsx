import { formatCLP } from '../../../lib/ordenesCompra/format';
import type { Totales } from '../../../lib/ordenesCompra/form';

export default function TotalesResumen({ totales }: { totales: Totales }) {
  return (
    <div className="ml-auto w-full max-w-xs space-y-1 rounded border border-border-base bg-surface-secondary/40 p-3 text-sm">
      <div className="flex justify-between">
        <span className="text-text-base/65">Neto</span>
        <span className="tabular-nums" data-testid="totales-neto">{formatCLP(totales.neto)}</span>
      </div>
      <div className="flex justify-between">
        <span className="text-text-base/65">IVA (19%)</span>
        <span className="tabular-nums" data-testid="totales-iva">{formatCLP(totales.iva)}</span>
      </div>
      <div className="flex justify-between font-semibold">
        <span>Total</span>
        <span className="tabular-nums" data-testid="totales-total">{formatCLP(totales.total)}</span>
      </div>
    </div>
  );
}
