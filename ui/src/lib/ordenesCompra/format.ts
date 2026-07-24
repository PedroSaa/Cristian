// ─── Shared formatting helpers for the Órdenes de Compra module ──────────────

const clpFormatter = new Intl.NumberFormat('es-CL', {
  style: 'currency',
  currency: 'CLP',
  maximumFractionDigits: 0,
});

export function formatCLP(value: number): string {
  return clpFormatter.format(value);
}

// Date-only field: format from the ISO string directly. Going through `new Date()`
// would shift midnight-UTC dates to the previous day in western timezones (Chile).
export function formatFecha(iso: string): string {
  const match = /^(\d{4})-(\d{2})-(\d{2})/.exec(iso);
  return match ? `${match[3]}-${match[2]}-${match[1]}` : '—';
}

export function formatFechaHora(iso: string): string {
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? '—' : date.toLocaleString('es-CL');
}

// ─── Blob download (shared by PDF and attachments — no window.open, so popup
// blockers can't interfere) ──────────────────────────────────────────────────

export function descargarBlob(blob: Blob, nombreArchivo: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = nombreArchivo;
  document.body.appendChild(link);
  link.click();
  link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 60_000);
}

// ─── API error → user-facing message ─────────────────────────────────────────

export function extractErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object' && 'userMessage' in error) {
    const mensaje = (error as { userMessage?: unknown }).userMessage;
    if (typeof mensaje === 'string' && mensaje) return mensaje;
  }
  return fallback;
}
