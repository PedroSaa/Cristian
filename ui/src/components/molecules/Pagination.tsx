import { useState } from 'react';
import Button from '../atoms/Button';

const TAMANOS = [10, 20, 25, 50, 100];

interface PaginationProps {
  pagina: number;
  totalPaginas: number;
  totalItems: number;
  tamanoPagina: number;
  onChange: (pagina: number) => void;
  onTamanoPaginaChange: (tamano: number) => void;
}

export default function Pagination({
  pagina,
  totalPaginas,
  totalItems,
  tamanoPagina,
  onChange,
  onTamanoPaginaChange,
}: PaginationProps) {
  const [inputPagina, setInputPagina] = useState('');

  const desde = Math.min((pagina - 1) * tamanoPagina + 1, totalItems);
  const hasta = Math.min(pagina * tamanoPagina, totalItems);
  const totalVisible = totalPaginas || 1;
  const pages = Array.from({ length: Math.min(totalVisible, 5) }, (_, i) => {
    const start = Math.max(1, Math.min(totalVisible - 4, pagina - 2));
    return start + i;
  });

  const handleIrAPagina = () => {
    const n = parseInt(inputPagina, 10);
    if (!isNaN(n) && n >= 1 && n <= (totalPaginas || 1)) {
      onChange(n);
    }
    setInputPagina('');
  };

  return (
    <div className="flex flex-col gap-3 border-t border-border-base bg-surface/80 px-3 py-3 text-sm text-text-base/70 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-xs font-semibold uppercase tracking-wide text-text-base/45">Filas por página</span>
        <select
          value={tamanoPagina}
          onChange={(e) => onTamanoPaginaChange(Number(e.target.value))}
          className="rounded-md border border-border-base bg-surface px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
        >
          {TAMANOS.map((t) => (
            <option key={t} value={t}>{t}</option>
          ))}
        </select>
        <span className="rounded-full bg-surface-secondary px-3 py-1 text-xs text-text-base/60">
          {totalItems === 0 ? 'Sin resultados' : `${desde}–${hasta} de ${totalItems}`}
        </span>
      </div>

      <div className="flex flex-wrap items-center gap-1">
        <Button variant="ghost" size="sm" disabled={pagina <= 1} onClick={() => onChange(1)} aria-label="Primera página">«</Button>
        <Button variant="ghost" size="sm" disabled={pagina <= 1} onClick={() => onChange(pagina - 1)} aria-label="Página anterior">‹</Button>

        {pages.map((page) => (
          <Button
            key={page}
            variant={page === pagina ? 'primary' : 'ghost'}
            size="sm"
            onClick={() => onChange(page)}
            aria-label={`Página ${page}`}
          >
            {page}
          </Button>
        ))}

        <Button variant="ghost" size="sm" disabled={pagina >= totalVisible} onClick={() => onChange(pagina + 1)} aria-label="Página siguiente">›</Button>
        <Button variant="ghost" size="sm" disabled={pagina >= totalVisible} onClick={() => onChange(totalVisible)} aria-label="Última página">»</Button>

        <div className="ml-2 flex items-center gap-1 rounded-md border border-border-base bg-surface px-2 py-1">
          <span className="text-xs text-text-base/55">Ir a</span>
          <input
            type="number"
            min={1}
            max={totalVisible}
            value={inputPagina}
            onChange={(e) => setInputPagina(e.target.value)}
            onKeyDown={(e) => e.key === 'Enter' && handleIrAPagina()}
            placeholder="…"
            className="w-12 bg-transparent text-center text-xs focus:outline-none"
          />
          <Button variant="ghost" size="sm" onClick={handleIrAPagina}>↵</Button>
        </div>
      </div>
    </div>
  );
}
