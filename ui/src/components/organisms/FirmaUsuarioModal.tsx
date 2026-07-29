import { useCallback, useEffect, useRef, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import ModalDialog from './ModalDialog';
import ConfirmDialog from './ConfirmDialog';
import Button from '../atoms/Button';
import Spinner from '../atoms/Spinner';
import FormField from '../molecules/FormField';
import { useToast } from '../../contexts/ToastContext';
import type {
  FirmaContentType,
  FirmaUsuarioMetadata,
  GuardarFirmaUsuarioRequest,
} from '../../lib/api/admin/firmaUsuarioApi';

const MAX_IMAGE_BYTES = 2 * 1024 * 1024; // 2 MB — mirrors the backend limit.
const SIGLA_MAX_LENGTH = 50;
const ALLOWED_TYPES: readonly FirmaContentType[] = ['image/png', 'image/jpeg'];

/**
 * Signature operations injected into the modal. Decouples the modal from any
 * specific transport (admin `usuarioId`-bound API vs. self-service `/perfil`
 * API): the caller wires the concrete functions and a react-query `cacheKey`.
 */
export interface FirmaOperations {
  /** Fetch the signature metadata (shape shared with the admin DTO). */
  getMetadata: () => Promise<FirmaUsuarioMetadata>;
  /** Fetch the signature image; resolves to `null` when there is none. */
  getImagen: () => Promise<Blob | null>;
  /** Upsert the signature (partial-update). Returns fresh metadata. */
  guardar: (body: GuardarFirmaUsuarioRequest) => Promise<FirmaUsuarioMetadata>;
  /** Delete the signature. */
  eliminar: () => Promise<void>;
  /** react-query cache key that identifies this signature (per user / per scope). */
  cacheKey: readonly unknown[];
}

interface FirmaUsuarioModalProps {
  open: boolean;
  operations: FirmaOperations;
  usuarioNombre: string;
  /** Whether the current user can save/delete. */
  canEdit: boolean;
  onClose: () => void;
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
}

/** Reads a Blob/File into a base64 string WITHOUT the `data:` URI prefix. */
function blobToBase64(blob: Blob): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = String(reader.result ?? '');
      const commaIndex = result.indexOf(',');
      resolve(commaIndex >= 0 ? result.slice(commaIndex + 1) : result);
    };
    reader.onerror = () => reject(reader.error ?? new Error('No se pudo leer el archivo.'));
    reader.readAsDataURL(blob);
  });
}

function isAllowedType(type: string): type is FirmaContentType {
  return (ALLOWED_TYPES as readonly string[]).includes(type);
}

/**
 * Modal to configure a signature: preview / upload-replace the image, set an
 * optional PIN (clave) and an optional sigla, save (upsert) or delete.
 *
 * Transport-agnostic: all data access is injected via `operations`, so the same
 * modal serves admin (editing another user's signature) and self-service (a user
 * editing their own signature from "Mi Perfil").
 *
 * Partial-update: the backend preserves what is not resent. Omitting the image
 * keeps the stored one (image is required only when creating the first
 * signature); omitting the clave keeps the stored PIN. So editing just the sigla
 * leaves image and clave intact.
 */
export default function FirmaUsuarioModal({
  open,
  operations,
  usuarioNombre,
  canEdit,
  onClose,
}: FirmaUsuarioModalProps) {
  const toast = useToast();
  const qc = useQueryClient();

  const { cacheKey } = operations;
  // Stable primitive identity of the cache key — drives the "reset on user change" effect.
  const cacheKeyId = JSON.stringify(cacheKey);

  const metadataQuery = useQuery<FirmaUsuarioMetadata>({
    queryKey: cacheKey,
    queryFn: () => operations.getMetadata(),
    enabled: open,
    staleTime: 0,
  });

  const guardarMut = useMutation({
    mutationFn: (body: GuardarFirmaUsuarioRequest) => operations.guardar(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: cacheKey }),
  });

  const eliminarMut = useMutation({
    mutationFn: () => operations.eliminar(),
    onSuccess: () => qc.invalidateQueries({ queryKey: cacheKey }),
  });

  const metadata = metadataQuery.data;
  const tieneFirma = metadata?.tieneFirma ?? false;

  // Local form state
  const [clave, setClave] = useState('');
  const [sigla, setSigla] = useState('');
  const [localBase64, setLocalBase64] = useState<string | null>(null);
  const [localContentType, setLocalContentType] = useState<FirmaContentType | null>(null);
  const [localPreviewUrl, setLocalPreviewUrl] = useState<string | null>(null);
  const [serverImageUrl, setServerImageUrl] = useState<string | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Reset local state whenever the modal opens for a (new) signature/user.
  useEffect(() => {
    if (!open) return;
    setClave('');
    setLocalBase64(null);
    setLocalContentType(null);
    setLocalPreviewUrl((prev) => {
      if (prev) URL.revokeObjectURL(prev);
      return null;
    });
    setFileError(null);
    setConfirmDelete(false);
    if (fileInputRef.current) fileInputRef.current.value = '';
  }, [open, cacheKeyId]);

  // Seed sigla from server metadata once it loads.
  useEffect(() => {
    setSigla(metadata?.sigla ?? '');
  }, [metadata?.sigla]);

  // Fetch the current signature image (blob → object URL) for preview.
  useEffect(() => {
    let revoked = false;
    let createdUrl: string | null = null;

    if (open && tieneFirma) {
      operations
        .getImagen()
        .then((blob) => {
          if (revoked || !blob) return;
          createdUrl = URL.createObjectURL(blob);
          setServerImageUrl(createdUrl);
        })
        .catch(() => {
          /* preview is best-effort; ignore fetch errors */
        });
    }

    return () => {
      revoked = true;
      if (createdUrl) URL.revokeObjectURL(createdUrl);
      setServerImageUrl(null);
    };
    // `operations` identity is caller-managed (useMemo); we key on cacheKeyId.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, tieneFirma, cacheKeyId]);

  const handleFileChange = useCallback((event: React.ChangeEvent<HTMLInputElement>) => {
    setFileError(null);
    const file = event.target.files?.[0];
    if (!file) return;

    if (!isAllowedType(file.type)) {
      setFileError('La firma debe ser una imagen PNG o JPEG.');
      return;
    }
    if (file.size > MAX_IMAGE_BYTES) {
      setFileError('La imagen no puede superar los 2 MB.');
      return;
    }

    blobToBase64(file)
      .then((base64) => {
        setLocalBase64(base64);
        setLocalContentType(file.type as FirmaContentType);
        setLocalPreviewUrl((prev) => {
          if (prev) URL.revokeObjectURL(prev);
          return URL.createObjectURL(file);
        });
      })
      .catch(() => setFileError('No se pudo leer el archivo seleccionado.'));
  }, []);

  const handleGuardar = useCallback(() => {
    setFileError(null);

    // Creating the first signature requires an image; when one already exists the image
    // is optional — omitting it keeps the stored one (backend preserves on omit).
    if (!localBase64 && !tieneFirma) {
      setFileError('Seleccioná una imagen para la firma.');
      return;
    }

    const body: GuardarFirmaUsuarioRequest = {};
    if (localBase64 && localContentType) {
      body.imagenBase64 = localBase64;
      body.contentType = localContentType;
    }
    // Send clave ONLY when the user typed a new one; omitting keeps the stored PIN.
    if (clave.trim()) body.clave = clave;
    // Sigla is always applied as received (loaded from metadata, so no data loss).
    body.sigla = sigla.trim();

    guardarMut.mutate(body, {
      onSuccess: () => {
        toast.success('Firma guardada correctamente.');
        onClose();
      },
      onError: (error) => toast.error(getErrorMessage(error, 'No se pudo guardar la firma.')),
    });
  }, [localBase64, localContentType, tieneFirma, clave, sigla, guardarMut, toast, onClose]);

  const handleEliminar = useCallback(() => {
    eliminarMut.mutate(undefined, {
      onSuccess: () => {
        toast.success('Firma eliminada correctamente.');
        setConfirmDelete(false);
        onClose();
      },
      onError: (error) => {
        toast.error(getErrorMessage(error, 'No se pudo eliminar la firma.'));
        setConfirmDelete(false);
      },
    });
  }, [eliminarMut, toast, onClose]);

  const previewUrl = localPreviewUrl ?? serverImageUrl;
  const isLoadingMeta = metadataQuery.isLoading;

  return (
    <>
      <ModalDialog
        open={open}
        title="Configurar firma"
        onClose={onClose}
        size="md"
        footer={(
          <>
            <Button variant="secondary" onClick={onClose}>Cerrar</Button>
            {tieneFirma && canEdit && (
              <Button
                variant="danger"
                onClick={() => setConfirmDelete(true)}
                loading={eliminarMut.isPending}
              >
                Eliminar firma
              </Button>
            )}
            {canEdit && (
              <Button type="button" onClick={handleGuardar} loading={guardarMut.isPending}>
                Guardar
              </Button>
            )}
          </>
        )}
      >
        <div className="space-y-4">
          <p className="text-sm text-gray-600">
            Usuario: <strong className="text-gray-800">{usuarioNombre}</strong>
          </p>

          {isLoadingMeta ? (
            <div className="flex justify-center py-8"><Spinner size="lg" /></div>
          ) : (
            <>
              {/* Preview */}
              <div>
                <span className="mb-1 block text-sm font-medium text-gray-700">Firma actual</span>
                {previewUrl ? (
                  <img
                    src={previewUrl}
                    alt="Vista previa de la firma"
                    className="max-h-40 w-auto rounded border border-gray-200 bg-white p-2"
                  />
                ) : (
                  <p className="rounded border border-dashed border-gray-300 px-3 py-4 text-center text-sm text-gray-400">
                    El usuario no tiene una firma configurada.
                  </p>
                )}
              </div>

              {canEdit && (
                <>
                  {/* Image upload */}
                  <FormField label={tieneFirma ? 'Reemplazar imagen' : 'Imagen de la firma'} error={fileError ?? undefined}>
                    <input
                      ref={fileInputRef}
                      type="file"
                      accept="image/png,image/jpeg"
                      onChange={handleFileChange}
                      className="w-full rounded border border-gray-300 px-3 py-2 text-sm file:mr-3 file:rounded file:border-0 file:bg-gray-100 file:px-3 file:py-1 file:text-sm"
                    />
                  </FormField>
                  <p className="text-xs text-gray-500">Formatos aceptados: PNG o JPEG. Tamaño máximo: 2 MB.</p>

                  {/* Clave */}
                  <FormField label="Clave (opcional)">
                    <input
                      type="password"
                      value={clave}
                      onChange={(e) => setClave(e.target.value)}
                      autoComplete="new-password"
                      placeholder={metadata?.tieneClave ? 'Dejar vacío para mantener la clave actual' : 'Dejar vacío para omitir'}
                      className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                    />
                  </FormField>

                  {/* Sigla */}
                  <FormField label="Sigla (opcional)">
                    <input
                      type="text"
                      value={sigla}
                      onChange={(e) => setSigla(e.target.value)}
                      maxLength={SIGLA_MAX_LENGTH}
                      placeholder="Ej: JPG"
                      className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                    />
                  </FormField>
                </>
              )}
            </>
          )}
        </div>
      </ModalDialog>

      <ConfirmDialog
        open={confirmDelete}
        title="Eliminar firma"
        message={`¿Seguro que querés eliminar la firma de "${usuarioNombre}"? Esta acción no se puede deshacer.`}
        confirmLabel="Eliminar"
        danger
        loading={eliminarMut.isPending}
        onConfirm={handleEliminar}
        onCancel={() => setConfirmDelete(false)}
      />
    </>
  );
}
