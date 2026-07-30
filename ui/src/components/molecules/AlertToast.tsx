import { useEffect, useMemo } from 'react';

type ToastType = 'success' | 'danger' | 'warning' | 'info' | 'error';

interface AlertToastProps {
  type: ToastType;
  message: string;
  onClose: () => void;
  duration?: number;
}

const typeConfig: Record<ToastType, { shell: string; bar: string; icon: string; title: string }> = {
  success: { shell: 'border-emerald-200 bg-emerald-50 text-emerald-900', bar: 'bg-emerald-500', icon: '✓', title: 'Éxito' },
  danger: { shell: 'border-rose-200 bg-rose-50 text-rose-900', bar: 'bg-rose-500', icon: '✕', title: 'Error' },
  error: { shell: 'border-rose-200 bg-rose-50 text-rose-900', bar: 'bg-rose-500', icon: '✕', title: 'Error' },
  warning: { shell: 'border-amber-200 bg-amber-50 text-amber-900', bar: 'bg-amber-500', icon: '⚠', title: 'Atención' },
  info: { shell: 'border-sky-200 bg-sky-50 text-sky-900', bar: 'bg-sky-500', icon: 'ℹ', title: 'Info' },
};

export default function AlertToast({ type, message, onClose, duration = 6500 }: AlertToastProps) {
  const cfg = useMemo(() => typeConfig[type], [type]);

  useEffect(() => {
    const timer = setTimeout(onClose, duration);
    return () => clearTimeout(timer);
  }, [onClose, duration]);

  return (
    <div
      role="alert"
      className={[
        'fixed bottom-4 left-4 right-4 z-[60] flex items-start gap-3 overflow-hidden rounded-xl border px-4 py-3 shadow-2xl backdrop-blur sm:left-auto sm:right-6 sm:max-w-md',
        cfg.shell,
      ].join(' ')}
    >
      <span className={['mt-0.5 h-10 w-1 rounded-full', cfg.bar].join(' ')} aria-hidden="true" />
      <div className="flex min-w-0 flex-1 flex-col gap-0.5">
        <span className="text-[11px] font-semibold uppercase tracking-wide opacity-70">{cfg.title}</span>
        <span className="text-sm leading-5">{message}</span>
      </div>
      <button
        onClick={onClose}
        className="ml-1 rounded-full p-1 opacity-70 transition hover:bg-white/60 hover:opacity-100"
        aria-label="Cerrar notificación"
      >
        <span className="text-base leading-none">✕</span>
      </button>
    </div>
  );
}
