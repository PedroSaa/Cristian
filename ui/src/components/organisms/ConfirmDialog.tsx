import type { ReactNode } from 'react';
import ModalDialog from './ModalDialog';
import Button from '../atoms/Button';

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  message: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  /** Usa el estilo de peligro (rojo) en el botón de confirmar. */
  danger?: boolean;
  loading?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * Diálogo de confirmación reutilizable para acciones que cambian estado
 * (desactivar, activar, eliminar, etc.). Unifica el patrón en todo el módulo admin.
 */
export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  danger = false,
  loading = false,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  return (
    <ModalDialog
      open={open}
      title={title}
      onClose={onCancel}
      size="sm"
      footer={(
        <>
          <Button variant="secondary" onClick={onCancel} disabled={loading}>
            {cancelLabel}
          </Button>
          <Button variant={danger ? 'danger' : 'primary'} onClick={onConfirm} loading={loading}>
            {confirmLabel}
          </Button>
        </>
      )}
    >
      <div className="text-sm text-gray-700">{message}</div>
    </ModalDialog>
  );
}
