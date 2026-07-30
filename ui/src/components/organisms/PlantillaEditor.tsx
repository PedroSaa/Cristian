import { useEffect, useRef, useState } from 'react';
import Spinner from '../atoms/Spinner';

declare global {
  interface Window {
    DocsAPI?: {
      DocEditor: new (id: string, config: Record<string, unknown>) => { destroyEditor?: () => void };
    };
  }
}

/** Carga dinámica del api.js del Document Server (una sola vez por sesión). */
function loadOnlyOfficeApi(editorUrl: string): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window.DocsAPI) {
      resolve();
      return;
    }
    const existing = document.querySelector<HTMLScriptElement>('script[data-onlyoffice="true"]');
    if (existing) {
      existing.addEventListener('load', () => resolve());
      existing.addEventListener('error', () => reject(new Error('No se pudo cargar el editor OnlyOffice.')));
      return;
    }
    const script = document.createElement('script');
    script.src = `${editorUrl.replace(/\/$/, '')}/web-apps/apps/api/documents/api.js`;
    script.async = true;
    script.dataset.onlyoffice = 'true';
    script.onload = () => resolve();
    script.onerror = () => reject(new Error('No se pudo cargar el editor OnlyOffice. ¿Está corriendo el Document Server?'));
    document.body.appendChild(script);
  });
}

interface PlantillaEditorProps {
  editorUrl: string;
  config: Record<string, unknown>;
  title?: string;
  /** Cuando true, el editor está guardando los cambios al cerrar (bloquea el botón). */
  busy?: boolean;
  onClose: () => void;
}

/**
 * Editor OnlyOffice embebido a pantalla completa para editar el .docx de una plantilla
 * in-place. El guardado lo maneja OnlyOffice contra el callbackUrl del backend; al cerrar,
 * el padre refresca la lista.
 */
export default function PlantillaEditor({ editorUrl, config, title, busy = false, onClose }: PlantillaEditorProps) {
  const editorRef = useRef<{ destroyEditor?: () => void } | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let cancelled = false;

    loadOnlyOfficeApi(editorUrl)
      .then(() => {
        if (cancelled || !window.DocsAPI) return;
        const merged: Record<string, unknown> = {
          ...config,
          width: '100%',
          height: '100%',
          events: {
            onAppReady: () => setLoading(false),
            onError: (e: unknown) => console.error('OnlyOffice error', e),
          },
        };
        editorRef.current = new window.DocsAPI.DocEditor('plantilla-editor-container', merged);
      })
      .catch((e: Error) => {
        if (!cancelled) {
          setError(e.message);
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
      try {
        editorRef.current?.destroyEditor?.();
      } catch {
        /* el editor ya pudo haberse desmontado */
      }
    };
  }, [editorUrl, config]);

  // El overlay se mantiene SIEMPRE montado y solo alterna opacidad. Montar/desmontar
  // nodos hermanos del contenedor que controla OnlyOffice rompe la reconciliación de
  // React (NotFoundError: insertBefore … not a child), así que lo evitamos.
  const overlayVisible = (loading && !error) || !!error || busy;

  return (
    <div className="fixed inset-0 z-[70] flex flex-col bg-slate-900">
      <div className="flex items-center justify-between gap-3 bg-slate-800 px-4 py-2 text-white">
        <span className="truncate text-sm font-medium">{title ?? 'Editor de plantilla'}</span>
        <button
          type="button"
          onClick={onClose}
          disabled={busy}
          className="rounded px-3 py-1 text-sm font-medium transition-colors hover:bg-white/10 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {busy ? 'Guardando…' : 'Cerrar'}
        </button>
      </div>
      <div className="flex-1">
        <div id="plantilla-editor-container" className="h-full w-full" />
      </div>
      <div
        className={`fixed inset-0 z-[71] flex items-center justify-center gap-3 px-6 text-center transition-opacity duration-200 ${
          overlayVisible ? 'bg-slate-900' : 'pointer-events-none opacity-0'
        }`}
        aria-hidden={!overlayVisible}
      >
        {error ? (
          <span className="text-sm text-rose-200">{error}</span>
        ) : (
          <>
            <Spinner size="lg" />
            <span className="text-sm text-white/80">{busy ? 'Guardando cambios…' : 'Cargando editor…'}</span>
          </>
        )}
      </div>
    </div>
  );
}
