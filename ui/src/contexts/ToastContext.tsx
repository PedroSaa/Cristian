import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react';
import AlertToast from '../components/molecules/AlertToast';

type ToastType = 'success' | 'error' | 'warning' | 'info';

interface ToastItem {
  id: number;
  type: ToastType;
  message: string;
}

interface ToastApi {
  showToast: (type: ToastType, message: string) => void;
  success: (message: string) => void;
  error: (message: string) => void;
  warning: (message: string) => void;
  info: (message: string) => void;
}

const noop = () => {};

// Default no-op para que useToast sea seguro fuera del provider (p. ej. en tests que
// renderizan páginas admin aisladas). En runtime, AdminLayout monta el ToastProvider real.
const defaultToastApi: ToastApi = {
  showToast: noop,
  success: noop,
  error: noop,
  warning: noop,
  info: noop,
};

const ToastContext = createContext<ToastApi>(defaultToastApi);

/**
 * Provee notificaciones tipo toast reutilizando AlertToast.
 * Muestra una notificación a la vez (la nueva reemplaza a la anterior),
 * suficiente para acciones CRUD secuenciales del panel de administración.
 */
export function ToastProvider({ children }: { children: ReactNode }) {
  const [toast, setToast] = useState<ToastItem | null>(null);
  const idRef = useRef(0);

  const showToast = useCallback((type: ToastType, message: string) => {
    idRef.current += 1;
    setToast({ id: idRef.current, type, message });
  }, []);

  const success = useCallback((message: string) => showToast('success', message), [showToast]);
  const error = useCallback((message: string) => showToast('error', message), [showToast]);
  const warning = useCallback((message: string) => showToast('warning', message), [showToast]);
  const info = useCallback((message: string) => showToast('info', message), [showToast]);

  return (
    <ToastContext.Provider value={{ showToast, success, error, warning, info }}>
      {children}
      {toast && (
        <AlertToast
          key={toast.id}
          type={toast.type}
          message={toast.message}
          onClose={() => setToast(null)}
        />
      )}
    </ToastContext.Provider>
  );
}

export function useToast(): ToastApi {
  return useContext(ToastContext);
}
