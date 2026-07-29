import { useEffect, useRef, useState, type PointerEvent as ReactPointerEvent } from 'react';
import * as pdfjsLib from 'pdfjs-dist';
import pdfWorkerUrl from 'pdfjs-dist/build/pdf.worker.min.mjs?url';

// El worker de pdf.js se resuelve como asset de Vite via el sufijo `?url`; en build queda
// hasheado y servido estáticamente, y en dev lo sirve el propio Vite. Se asigna una sola vez.
pdfjsLib.GlobalWorkerOptions.workerSrc = pdfWorkerUrl;

/**
 * Fila editable de medidas. Las coordenadas se guardan como texto para permitir vaciar
 * el campo mientras se escribe; se normalizan a entero al construir el payload de guardado.
 */
export interface MedidaRow {
  idForplaMed: number;
  objeto: string;
  x: string;
  y: string;
  ancho: string;
  alto: string;
}

export const QRFIRMA_OBJETO = 'QRFIRMA';
/** El legacy persiste QRFIRMA siempre con alto/ancho fijos en 200 (solo se mueve). */
export const QRFIRMA_TAMANO_FIJO = 200;
export const MEDIDA_MAX = 32767;

export function toCoordinate(value: string | number): number {
  const parsed = Math.trunc(Number(value));
  if (!Number.isFinite(parsed) || parsed < 0) return 0;
  return Math.min(parsed, MEDIDA_MAX);
}

// ── Sistema de coordenadas ─────────────────────────────────────────────────
// La página se asume tamaño Carta (Letter): 612 × 792 puntos PDF.
// El ORIGEN del PDF está ABAJO-IZQUIERDA (convención iTextSharp/bottom-left que usa el
// legacy). El estampado real todavía no está implementado en Infinity, así que fijamos la
// convención acá; lo importante es que ROUND-TRIPEE: arrastrar produce los mismos números
// que, tipeados a mano, reproducen exactamente la posición.
//
//   escala      = anchoCanvasPx / anchoPaginaPt
//   screenLeft  = x * escala
//   screenTop   = (alturaPaginaPt - y - alto) * escala   (y = borde INFERIOR de la caja)
//   screenW     = ancho * escala
//   screenH     = alto  * escala
//
// Inverso (drag/resize), con top-left de pantalla:
//   x = screenLeft / escala
//   y = alturaPaginaPt - alto - screenTop / escala
const PAGE_WIDTH_PT = 612;
const PAGE_HEIGHT_PT = 792;
const FRAME_WIDTH_PX = 600;
const ESCALA = FRAME_WIDTH_PX / PAGE_WIDTH_PT;
const FRAME_HEIGHT_PX = PAGE_HEIGHT_PT * ESCALA;
const HANDLE_PX = 12;
// Footprint visual mínimo de una caja: suficiente para leer la etiqueta y agarrarla con el
// mouse aunque el objeto tenga tamaño 0/1 en los datos (no cambia el valor guardado).
const MIN_BOX_W = 84;
const MIN_BOX_H = 26;
// Footprint mínimo CUADRADO para objetos que deben mantener aspecto 1:1 (QR).
const MIN_QR_PX = 40;

type PdfState = 'loading' | 'ready' | 'error';

interface ScreenRect {
  left: number;
  top: number;
  width: number;
  height: number;
}

function effectiveSize(row: MedidaRow): { ancho: number; alto: number } {
  return { ancho: toCoordinate(row.ancho), alto: toCoordinate(row.alto) };
}

/** Convierte una fila (coordenadas PDF bottom-left) al rectángulo en píxeles de pantalla. */
function rowToScreen(row: MedidaRow): ScreenRect {
  const x = toCoordinate(row.x);
  const y = toCoordinate(row.y);
  const { ancho, alto } = effectiveSize(row);
  return {
    left: x * ESCALA,
    top: (PAGE_HEIGHT_PT - y - alto) * ESCALA,
    width: ancho * ESCALA,
    height: alto * ESCALA,
  };
}

/**
 * Tamaño en píxeles con el que REALMENTE se dibuja la caja (aplica el footprint mínimo).
 * Se usa tanto para renderizar como para clampear el movimiento, así el clamp coincide con
 * lo que se ve y la caja nunca queda fuera del documento (aunque el objeto tenga tamaño 0).
 */
function displaySizePx(row: MedidaRow): { width: number; height: number } {
  const screen = rowToScreen(row);
  if (row.objeto === QRFIRMA_OBJETO) {
    const lado = Math.max(screen.width, screen.height, MIN_QR_PX);
    return { width: lado, height: lado };
  }
  return {
    width: Math.max(screen.width, MIN_BOX_W),
    height: Math.max(screen.height, MIN_BOX_H),
  };
}

function clampCoord(value: number): number {
  if (!Number.isFinite(value)) return 0;
  return Math.min(Math.max(0, Math.round(value)), MEDIDA_MAX);
}

interface PlantillaMedidasEditorProps {
  codForm: string;
  rows: MedidaRow[];
  onChange: (idForplaMed: number, field: 'x' | 'y' | 'ancho' | 'alto', value: string) => void;
  /** Inyectable en tests para evitar depender del render real de pdf.js. */
  fetchPdf: (codForm: string) => Promise<Blob>;
}

type DragState =
  | { kind: 'move'; id: number; pointerId: number; offsetX: number; offsetY: number }
  | { kind: 'resize'; id: number; pointerId: number };

export default function PlantillaMedidasEditor({ codForm, rows, onChange, fetchPdf }: PlantillaMedidasEditorProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const overlayRef = useRef<HTMLDivElement>(null);
  const dragRef = useRef<DragState | null>(null);
  const [pdfState, setPdfState] = useState<PdfState>('loading');

  // ── Render de la página 1 del PDF de fondo ────────────────────────────────
  useEffect(() => {
    let cancelled = false;
    let renderTask: { cancel: () => void } | null = null;
    setPdfState('loading');

    (async () => {
      try {
        const blob = await fetchPdf(codForm);
        const buffer = await blob.arrayBuffer();
        if (cancelled) return;
        const doc = await pdfjsLib.getDocument({ data: buffer }).promise;
        if (cancelled) return;
        const page = await doc.getPage(1);
        if (cancelled) return;
        const canvas = canvasRef.current;
        if (!canvas) return;
        // Escalamos la página para llenar el ancho del frame (Carta encaja en FRAME_HEIGHT_PX).
        const baseViewport = page.getViewport({ scale: 1 });
        const scale = FRAME_WIDTH_PX / baseViewport.width;
        const viewport = page.getViewport({ scale });
        canvas.width = Math.round(viewport.width);
        canvas.height = Math.round(viewport.height);
        const ctx = canvas.getContext('2d');
        if (!ctx) return;
        renderTask = page.render({ canvas, canvasContext: ctx, viewport }) as unknown as { cancel: () => void };
        await (renderTask as unknown as { promise: Promise<void> }).promise;
        if (!cancelled) setPdfState('ready');
      } catch {
        // OnlyOffice caído/no configurado (500/503) o PDF ilegible: caemos a hoja en blanco.
        if (!cancelled) setPdfState('error');
      }
    })();

    return () => {
      cancelled = true;
      try {
        renderTask?.cancel();
      } catch {
        /* el render ya pudo haberse resuelto */
      }
    };
  }, [codForm, fetchPdf]);

  // ── Drag & resize sobre la capa de cajas ──────────────────────────────────
  function pointerToFrame(e: ReactPointerEvent | PointerEvent): { fx: number; fy: number } {
    const rect = overlayRef.current?.getBoundingClientRect();
    const fx = rect ? e.clientX - rect.left : 0;
    const fy = rect ? e.clientY - rect.top : 0;
    return { fx, fy };
  }

  function commitMove(row: MedidaRow, screenLeft: number, screenTop: number) {
    const { alto } = effectiveSize(row);
    // Clampeamos con el tamaño MOSTRADO (no el real), así la caja visible nunca sale del
    // documento — ni siquiera las anclas de tamaño 0, que se dibujan con footprint mínimo.
    const disp = displaySizePx(row);
    const clampedLeft = Math.min(Math.max(0, screenLeft), Math.max(0, FRAME_WIDTH_PX - disp.width));
    const clampedTop = Math.min(Math.max(0, screenTop), Math.max(0, FRAME_HEIGHT_PX - disp.height));
    const x = clampCoord(clampedLeft / ESCALA);
    const y = clampCoord(PAGE_HEIGHT_PT - alto - clampedTop / ESCALA);
    onChange(row.idForplaMed, 'x', String(x));
    onChange(row.idForplaMed, 'y', String(y));
  }

  function commitResize(row: MedidaRow, pointerFx: number, pointerFy: number) {
    const screen = rowToScreen(row);
    const newWidthPx = Math.max(HANDLE_PX, pointerFx - screen.left);
    const newHeightPx = Math.max(HANDLE_PX, pointerFy - screen.top);
    let ancho = clampCoord(newWidthPx / ESCALA);
    let alto = clampCoord(newHeightPx / ESCALA);
    // El QR debe permanecer CUADRADO para poder escanearse: usamos el lado mayor para ambos.
    if (row.objeto === QRFIRMA_OBJETO) {
      const lado = Math.max(ancho, alto);
      ancho = lado;
      alto = lado;
    }
    // El top de pantalla queda fijo al redimensionar; como cambió el alto, recalculamos y.
    const y = clampCoord(PAGE_HEIGHT_PT - alto - screen.top / ESCALA);
    onChange(row.idForplaMed, 'ancho', String(ancho));
    onChange(row.idForplaMed, 'alto', String(alto));
    onChange(row.idForplaMed, 'y', String(y));
  }

  // Cambio de tamaño desde el panel numérico: el QR se mantiene cuadrado espejando el lado.
  function handleSizeChange(row: MedidaRow, field: 'ancho' | 'alto', value: string) {
    onChange(row.idForplaMed, field, value);
    if (row.objeto === QRFIRMA_OBJETO) {
      onChange(row.idForplaMed, field === 'ancho' ? 'alto' : 'ancho', value);
    }
  }

  function handlePointerMove(e: PointerEvent) {
    const drag = dragRef.current;
    if (!drag) return;
    const row = rows.find((r) => r.idForplaMed === drag.id);
    if (!row) return;
    const { fx, fy } = pointerToFrame(e);
    if (drag.kind === 'move') {
      commitMove(row, fx - drag.offsetX, fy - drag.offsetY);
    } else {
      commitResize(row, fx, fy);
    }
  }

  function endDrag() {
    dragRef.current = null;
    window.removeEventListener('pointermove', handlePointerMove);
    window.removeEventListener('pointerup', endDrag);
  }

  function startMove(e: ReactPointerEvent, row: MedidaRow) {
    e.preventDefault();
    const screen = rowToScreen(row);
    const { fx, fy } = pointerToFrame(e);
    dragRef.current = { kind: 'move', id: row.idForplaMed, pointerId: e.pointerId, offsetX: fx - screen.left, offsetY: fy - screen.top };
    window.addEventListener('pointermove', handlePointerMove);
    window.addEventListener('pointerup', endDrag);
  }

  function startResize(e: ReactPointerEvent, row: MedidaRow) {
    e.preventDefault();
    e.stopPropagation();
    dragRef.current = { kind: 'resize', id: row.idForplaMed, pointerId: e.pointerId };
    window.addEventListener('pointermove', handlePointerMove);
    window.addEventListener('pointerup', endDrag);
  }

  // Limpieza defensiva: si el componente se desmonta a mitad de un arrastre, sacamos los
  // listeners globales para no dejarlos colgados.
  useEffect(() => () => {
    window.removeEventListener('pointermove', handlePointerMove);
    window.removeEventListener('pointerup', endDrag);
  }, []); // eslint-disable-line react-hooks/exhaustive-deps

  const inputClass = 'w-24 rounded border border-gray-300 px-2 py-1 text-sm';

  return (
    <div className="flex flex-col gap-4 lg:flex-row lg:items-start">
      {/* Lienzo + capa de cajas */}
      <div className="shrink-0">
        <div
          className="relative rounded border border-gray-300 bg-white shadow-sm"
          style={{ width: FRAME_WIDTH_PX, height: FRAME_HEIGHT_PX }}
        >
          {/* Fondo: PDF real o, si falla, hoja en blanco con grilla */}
          {pdfState === 'error' ? (
            <div
              aria-hidden="true"
              className="absolute inset-0"
              style={{
                backgroundColor: '#ffffff',
                backgroundImage:
                  'linear-gradient(to right, rgba(0,0,0,0.06) 1px, transparent 1px), linear-gradient(to bottom, rgba(0,0,0,0.06) 1px, transparent 1px)',
                backgroundSize: '24px 24px',
              }}
            />
          ) : (
            <canvas
              ref={canvasRef}
              className="absolute inset-0 h-full w-full"
              style={{ width: FRAME_WIDTH_PX, height: FRAME_HEIGHT_PX }}
            />
          )}

          {/* Capa de cajas arrastrables (mismas dimensiones que el lienzo) */}
          <div ref={overlayRef} className="absolute inset-0" style={{ width: FRAME_WIDTH_PX, height: FRAME_HEIGHT_PX }}>
            {rows.map((row) => {
              const screen = rowToScreen(row);
              // Objetos sin tamaño definido (ancho/alto 0) son anclas: se muestran con un
              // footprint mínimo cómodo y borde punteado para que se vean y se puedan agarrar,
              // sin alterar el valor guardado (sigue 0 hasta que se redimensionen).
              const sinTamano = toCoordinate(row.ancho) === 0 && toCoordinate(row.alto) === 0;
              // Tamaño con el que se dibuja (footprint mínimo; QR cuadrado). Mismo que usa el
              // clamp del movimiento, para que la caja nunca quede fuera del documento.
              const disp = displaySizePx(row);
              return (
                <div
                  key={row.idForplaMed}
                  data-testid={`medida-box-${row.objeto}`}
                  role="button"
                  tabIndex={0}
                  aria-label={`Posición ${row.objeto}`}
                  onPointerDown={(e) => startMove(e, row)}
                  className={`absolute box-border cursor-move select-none rounded border-2 bg-indigo-500/15 text-[10px] font-medium text-indigo-900 ${sinTamano ? 'border-dashed border-indigo-400' : 'border-indigo-500'}`}
                  style={{ left: screen.left, top: screen.top, width: disp.width, height: disp.height }}
                >
                  <span className="pointer-events-none block truncate px-1 leading-tight">{row.objeto}</span>
                  <span
                    data-testid={`medida-resize-${row.objeto}`}
                    onPointerDown={(e) => startResize(e, row)}
                    className="absolute -bottom-1 -right-1 h-3 w-3 cursor-se-resize rounded-sm border border-white bg-indigo-600"
                  />
                </div>
              );
            })}
          </div>
        </div>
        {pdfState === 'error' && (
          <p role="status" className="mt-2 text-xs text-amber-600">
            No se pudo cargar la vista del documento; posicionando sobre página en blanco.
          </p>
        )}
      </div>

      {/* Panel numérico sincronizado en dos vías */}
      <div className="min-w-0 flex-1 overflow-x-auto rounded border border-gray-200">
        <table className="min-w-full text-sm">
          <thead className="bg-gray-50 text-xs uppercase text-gray-500">
            <tr>
              <th className="px-3 py-2 text-left">Descripción</th>
              <th className="px-3 py-2 text-left">Alto</th>
              <th className="px-3 py-2 text-left">Ancho</th>
              <th className="px-3 py-2 text-left">X</th>
              <th className="px-3 py-2 text-left">Y</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {rows.map((row) => {
              return (
                <tr key={row.idForplaMed}>
                  <td className="px-3 py-2 font-medium text-gray-700">{row.objeto}</td>
                  <td className="px-3 py-2">
                    <input
                      aria-label={`Alto ${row.objeto}`}
                      type="number"
                      min={0}
                      max={MEDIDA_MAX}
                      value={row.alto}
                      onChange={(e) => handleSizeChange(row, 'alto', e.target.value)}
                      className={inputClass}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      aria-label={`Ancho ${row.objeto}`}
                      type="number"
                      min={0}
                      max={MEDIDA_MAX}
                      value={row.ancho}
                      onChange={(e) => handleSizeChange(row, 'ancho', e.target.value)}
                      className={inputClass}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      aria-label={`X ${row.objeto}`}
                      type="number"
                      min={0}
                      max={MEDIDA_MAX}
                      value={row.x}
                      onChange={(e) => onChange(row.idForplaMed, 'x', e.target.value)}
                      className={inputClass}
                    />
                  </td>
                  <td className="px-3 py-2">
                    <input
                      aria-label={`Y ${row.objeto}`}
                      type="number"
                      min={0}
                      max={MEDIDA_MAX}
                      value={row.y}
                      onChange={(e) => onChange(row.idForplaMed, 'y', e.target.value)}
                      className={inputClass}
                    />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
