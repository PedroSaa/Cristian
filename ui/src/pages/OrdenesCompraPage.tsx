import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  useAgregarAdjuntoOrdenCompra,
  useActualizarOrdenCompra,
  useAnularOrdenCompra,
  useAprobarOrdenCompra,
  useCrearOrdenCompra,
  useEliminarAdjuntoOrdenCompra,
  useEnviarAprobacionOrdenCompra,
  useMarcarEnviadaOrdenCompra,
  useOrdenCompra,
  useOrdenesCompraList,
  useProveedoresActivos,
  useRechazarOrdenCompra,
} from '../hooks/useOrdenesCompra';
import {
  downloadAdjuntoOrdenCompra,
  getOrdenCompra,
  getOrdenCompraPdf,
} from '../lib/api/ordenesCompra';
import { useHasPermission } from '../hooks/usePermissions';
import { PERMISSIONS } from '../lib/generated/permissionCatalog';
import DataGrid, { type ColumnDef } from '../components/organisms/DataGrid';
import Pagination from '../components/molecules/Pagination';
import ModalDialog from '../components/organisms/ModalDialog';
import ConfirmDialog from '../components/organisms/ConfirmDialog';
import Button from '../components/atoms/Button';
import IconButton from '../components/atoms/IconButton';
import Input from '../components/atoms/Input';
import Select from '../components/atoms/Select';
import AlertToast from '../components/molecules/AlertToast';
import EstadoOrdenBadge, {
  ESTADO_FILTER_OPTIONS,
} from '../components/organisms/ordenesCompra/EstadoOrdenBadge';
import OrdenCompraFormFields from '../components/organisms/ordenesCompra/OrdenCompraFormFields';
import OrdenCompraDetalle from '../components/organisms/ordenesCompra/OrdenCompraDetalle';
import {
  descargarBlob,
  extractErrorMessage,
  formatCLP,
  formatFecha,
} from '../lib/ordenesCompra/format';
import {
  calcularTotales,
  emptyForm,
  formInvalido,
  formToRequest,
  ordenToForm,
} from '../lib/ordenesCompra/form';
import type {
  EstadoOrdenCompra,
  OrdenCompraDto,
  OrdenCompraFormValues,
  OrdenCompraListItem,
} from '../types/ordenCompra';

// Re-exported so tests (and other consumers) keep importing from the page.
export { formatCLP, formatFecha } from '../lib/ordenesCompra/format';
export { itemInvalido } from '../lib/ordenesCompra/form';

// ─── Estado rules (mirror of backend state machine) ──────────────────────────

const EDITABLE_STATES: readonly EstadoOrdenCompra[] = ['Borrador', 'Rechazada'];

function isEditable(estado: EstadoOrdenCompra): boolean {
  return EDITABLE_STATES.includes(estado);
}

// ─── Confirm dialog state (replaces window.confirm) ──────────────────────────

interface ConfirmacionState {
  titulo: string;
  mensaje: string;
  confirmLabel: string;
  danger: boolean;
  action: () => Promise<void>;
}

// ─── Action modal state ──────────────────────────────────────────────────────

type AccionTipo = 'aprobar' | 'rechazar' | 'anular';

interface AccionTarget {
  id: string;
  numero: string | null;
}

const ACCION_META: Record<AccionTipo, {
  titulo: string;
  campo: string;
  requerido: boolean;
  confirmar: string;
}> = {
  aprobar: {
    titulo: 'Aprobar orden de compra',
    campo: 'Comentario (opcional)',
    requerido: false,
    confirmar: 'Confirmar aprobación',
  },
  rechazar: {
    titulo: 'Rechazar orden de compra',
    campo: 'Comentario (obligatorio)',
    requerido: true,
    confirmar: 'Confirmar rechazo',
  },
  anular: {
    titulo: 'Anular orden de compra',
    campo: 'Motivo (obligatorio)',
    requerido: true,
    confirmar: 'Confirmar anulación',
  },
};

// ─── Page (container: state, hooks and handlers; presentational pieces live
// under components/organisms/ordenesCompra) ──────────────────────────────────

export default function OrdenesCompraPage() {
  const canCrear = useHasPermission(PERMISSIONS.ORDENES_COMPRA_CREAR);
  const canAprobar = useHasPermission(PERMISSIONS.ORDENES_COMPRA_APROBAR);
  const canAnular = useHasPermission(PERMISSIONS.ORDENES_COMPRA_ANULAR);

  const {
    data,
    isLoading,
    isError,
    filtros,
    setFiltros,
    resetFiltros,
    handlePaginaChange,
    handleTamanoPaginaChange,
  } = useOrdenesCompraList();

  const proveedoresQuery = useProveedoresActivos();
  const proveedorOptions = useMemo(
    () =>
      (proveedoresQuery.data?.items ?? []).map((p) => ({
        value: p.id,
        label: p.nombre,
      })),
    [proveedoresQuery.data],
  );

  const createMutation = useCrearOrdenCompra();
  const updateMutation = useActualizarOrdenCompra();
  const enviarMutation = useEnviarAprobacionOrdenCompra();
  const aprobarMutation = useAprobarOrdenCompra();
  const rechazarMutation = useRechazarOrdenCompra();
  const marcarEnviadaMutation = useMarcarEnviadaOrdenCompra();
  const anularMutation = useAnularOrdenCompra();
  const agregarAdjuntoMutation = useAgregarAdjuntoOrdenCompra();
  const eliminarAdjuntoMutation = useEliminarAdjuntoOrdenCompra();

  // ─── UI state ────────────────────────────────────────────────────────────
  const [toast, setToast] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editTarget, setEditTarget] = useState<OrdenCompraDto | null>(null);
  const [form, setForm] = useState<OrdenCompraFormValues>(emptyForm);
  const [submitAttempted, setSubmitAttempted] = useState(false);
  const [detalleId, setDetalleId] = useState<string | null>(null);
  const [accion, setAccion] = useState<{ tipo: AccionTipo; target: AccionTarget } | null>(null);
  const [accionComentario, setAccionComentario] = useState('');
  const [confirmacion, setConfirmacion] = useState<ConfirmacionState | null>(null);
  const [confirmacionPendiente, setConfirmacionPendiente] = useState(false);
  // Debounced search: the input updates immediately, the server-side filter
  // fires only after 300 ms without typing.
  const [searchInput, setSearchInput] = useState('');

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setFiltros({ search: searchInput.trim() || undefined });
    }, 300);
    return () => window.clearTimeout(timer);
  }, [searchInput, setFiltros]);

  const detalleQuery = useOrdenCompra(detalleId);

  const notifyError = useCallback((error: unknown, fallback: string) => {
    setToast({ type: 'error', message: extractErrorMessage(error, fallback) });
  }, []);

  // ─── Handlers: create / edit ─────────────────────────────────────────────
  const openCreate = useCallback(() => {
    setEditTarget(null);
    setForm(emptyForm());
    setSubmitAttempted(false);
    setShowForm(true);
  }, []);

  const openEdit = useCallback(
    async (id: string) => {
      try {
        const oc = await getOrdenCompra(id);
        setEditTarget(oc);
        setForm(ordenToForm(oc));
        setSubmitAttempted(false);
        setShowForm(true);
      } catch (error) {
        notifyError(error, 'No se pudo cargar la orden de compra.');
      }
    },
    [notifyError],
  );

  const handleSubmitForm = useCallback(async () => {
    if (formInvalido(form)) {
      // Mark every field as touched so each one shows its inline message.
      setSubmitAttempted(true);
      return;
    }
    const req = formToRequest(form);
    try {
      if (editTarget) {
        await updateMutation.mutateAsync({ id: editTarget.id, req });
        setToast({ type: 'success', message: 'Orden de compra actualizada correctamente.' });
      } else {
        await createMutation.mutateAsync(req);
        setToast({ type: 'success', message: 'Orden de compra creada correctamente.' });
      }
      setShowForm(false);
    } catch (error) {
      notifyError(error, 'No se pudo guardar la orden de compra.');
    }
  }, [form, editTarget, createMutation, updateMutation, notifyError]);

  // ─── Handlers: confirm dialog (replaces window.confirm) ──────────────────
  const handleAceptarConfirmacion = useCallback(async () => {
    if (!confirmacion || confirmacionPendiente) return;
    setConfirmacionPendiente(true);
    try {
      await confirmacion.action();
    } finally {
      setConfirmacionPendiente(false);
      setConfirmacion(null);
    }
  }, [confirmacion, confirmacionPendiente]);

  // ─── Handlers: state transitions ─────────────────────────────────────────
  const handleEnviarAprobacion = useCallback(
    (target: AccionTarget) => {
      if (enviarMutation.isPending) return;
      setConfirmacion({
        titulo: 'Enviar a aprobación',
        mensaje: `¿Enviar la orden de compra ${target.numero ?? '(borrador)'} a aprobación?`,
        confirmLabel: 'Enviar',
        danger: false,
        action: async () => {
          try {
            await enviarMutation.mutateAsync(target.id);
            setToast({ type: 'success', message: 'Orden de compra enviada a aprobación.' });
          } catch (error) {
            notifyError(error, 'No se pudo enviar la orden a aprobación.');
          }
        },
      });
    },
    [enviarMutation, notifyError],
  );

  const handleMarcarEnviada = useCallback(
    async (target: AccionTarget) => {
      if (marcarEnviadaMutation.isPending) return;
      try {
        await marcarEnviadaMutation.mutateAsync(target.id);
        setToast({ type: 'success', message: 'Orden de compra marcada como enviada.' });
      } catch (error) {
        notifyError(error, 'No se pudo marcar la orden como enviada.');
      }
    },
    [marcarEnviadaMutation, notifyError],
  );

  const openAccion = useCallback((tipo: AccionTipo, target: AccionTarget) => {
    setAccionComentario('');
    setAccion({ tipo, target });
  }, []);

  const handleConfirmAccion = useCallback(async () => {
    if (!accion) return;
    const comentario = accionComentario.trim();
    try {
      if (accion.tipo === 'aprobar') {
        await aprobarMutation.mutateAsync({ id: accion.target.id, comentario: comentario || undefined });
        setToast({ type: 'success', message: 'Orden de compra aprobada.' });
      } else if (accion.tipo === 'rechazar') {
        await rechazarMutation.mutateAsync({ id: accion.target.id, comentario });
        setToast({ type: 'success', message: 'Orden de compra rechazada.' });
      } else {
        await anularMutation.mutateAsync({ id: accion.target.id, motivo: comentario });
        setToast({ type: 'success', message: 'Orden de compra anulada.' });
      }
      setAccion(null);
    } catch (error) {
      notifyError(error, 'No se pudo completar la acción.');
    }
  }, [accion, accionComentario, aprobarMutation, rechazarMutation, anularMutation, notifyError]);

  // ─── Handlers: PDF / adjuntos ────────────────────────────────────────────
  // Download via <a download> instead of window.open: opening a window after an
  // await gets killed by popup blockers.
  const handlePdf = useCallback(
    async (id: string, numero: string | null) => {
      try {
        const { blob, fileName } = await getOrdenCompraPdf(id);
        descargarBlob(blob, fileName ?? `orden-compra-${numero ?? id}.pdf`);
      } catch (error) {
        notifyError(error, 'No se pudo generar el PDF.');
      }
    },
    [notifyError],
  );

  const handleDownloadAdjunto = useCallback(
    async (id: string, adjuntoId: string, nombreArchivo: string) => {
      try {
        const blob = await downloadAdjuntoOrdenCompra(id, adjuntoId);
        descargarBlob(blob, nombreArchivo);
      } catch (error) {
        notifyError(error, 'No se pudo descargar el adjunto.');
      }
    },
    [notifyError],
  );

  const handleUploadAdjunto = useCallback(
    async (id: string, file: File) => {
      if (file.size > 10 * 1024 * 1024) {
        setToast({ type: 'error', message: 'El adjunto no puede superar los 10 MB.' });
        return;
      }
      try {
        const contenidoBase64 = await new Promise<string>((resolve, reject) => {
          const reader = new FileReader();
          reader.onload = () => {
            const result = String(reader.result ?? '');
            resolve(result.slice(result.indexOf(',') + 1));
          };
          reader.onerror = () => reject(reader.error);
          reader.readAsDataURL(file);
        });
        await agregarAdjuntoMutation.mutateAsync({
          id,
          req: {
            nombreArchivo: file.name,
            contentType: file.type || 'application/octet-stream',
            contenidoBase64,
          },
        });
        setToast({ type: 'success', message: 'Adjunto agregado correctamente.' });
      } catch (error) {
        notifyError(error, 'No se pudo subir el adjunto.');
      }
    },
    [agregarAdjuntoMutation, notifyError],
  );

  const handleEliminarAdjunto = useCallback(
    (id: string, adjuntoId: string, nombreArchivo: string) => {
      if (eliminarAdjuntoMutation.isPending) return;
      setConfirmacion({
        titulo: 'Eliminar adjunto',
        mensaje: `¿Eliminar el adjunto ${nombreArchivo}?`,
        confirmLabel: 'Eliminar',
        danger: true,
        action: async () => {
          try {
            await eliminarAdjuntoMutation.mutateAsync({ id, adjuntoId });
            setToast({ type: 'success', message: 'Adjunto eliminado.' });
          } catch (error) {
            notifyError(error, 'No se pudo eliminar el adjunto.');
          }
        },
      });
    },
    [eliminarAdjuntoMutation, notifyError],
  );

  // ─── Row actions by estado + permission ──────────────────────────────────
  const renderAcciones = useCallback(
    (row: { id: string; numero: string | null; estado: EstadoOrdenCompra }) => {
      const target: AccionTarget = { id: row.id, numero: row.numero };
      const buttons: React.ReactNode[] = [];

      buttons.push(
        <IconButton
          key="ver"
          name="eye"
          tooltip="Ver"
          appearance="admin"
          onClick={() => setDetalleId(row.id)}
        />,
      );

      if (isEditable(row.estado) && canCrear) {
        buttons.push(
          <IconButton
            key="editar"
            name="edit"
            tooltip="Editar"
            appearance="admin"
            onClick={() => openEdit(row.id)}
          />,
          <IconButton
            key="enviar"
            name="upload"
            tooltip="Enviar a aprobación"
            appearance="admin"
            loading={enviarMutation.isPending && enviarMutation.variables === row.id}
            disabled={enviarMutation.isPending}
            onClick={() => handleEnviarAprobacion(target)}
          />,
        );
      }

      if (row.estado === 'PendienteAprobacion' && canAprobar) {
        buttons.push(
          <IconButton
            key="aprobar"
            name="check"
            tooltip="Aprobar"
            appearance="admin"
            onClick={() => openAccion('aprobar', target)}
          />,
          <IconButton
            key="rechazar"
            name="x"
            tooltip="Rechazar"
            variant="danger"
            appearance="admin"
            onClick={() => openAccion('rechazar', target)}
          />,
        );
      }

      if (row.estado === 'Aprobada' && canCrear) {
        buttons.push(
          <IconButton
            key="marcar-enviada"
            name="mail"
            tooltip="Marcar enviada"
            appearance="admin"
            loading={marcarEnviadaMutation.isPending && marcarEnviadaMutation.variables === row.id}
            disabled={marcarEnviadaMutation.isPending}
            onClick={() => handleMarcarEnviada(target)}
          />,
        );
      }

      if (row.estado === 'Aprobada' || row.estado === 'Enviada') {
        buttons.push(
          <IconButton
            key="pdf"
            name="download"
            tooltip="Descargar PDF"
            appearance="admin"
            onClick={() => handlePdf(row.id, row.numero)}
          />,
        );
      }

      if (row.estado !== 'Anulada' && canAnular) {
        buttons.push(
          <IconButton
            key="anular"
            name="alert-circle"
            tooltip="Anular"
            variant="danger"
            appearance="admin"
            onClick={() => openAccion('anular', target)}
          />,
        );
      }

      return (
        <div className="flex flex-wrap gap-1" onClick={(e) => e.stopPropagation()}>
          {buttons}
        </div>
      );
    },
    [
      canCrear,
      canAprobar,
      canAnular,
      openEdit,
      handleEnviarAprobacion,
      handleMarcarEnviada,
      handlePdf,
      openAccion,
      enviarMutation.isPending,
      enviarMutation.variables,
      marcarEnviadaMutation.isPending,
      marcarEnviadaMutation.variables,
    ],
  );

  // ─── Columns ─────────────────────────────────────────────────────────────
  const columns: ColumnDef<OrdenCompraListItem>[] = useMemo(
    () => [
      {
        key: 'numero',
        header: 'Número',
        width: '120px',
        render: (row) => (
          <div className="flex flex-col">
            <span className="font-mono text-xs">{row.numero ?? '—'}</span>
            {row.codigoMercadoPublico && (
              <span
                className="font-mono text-[10px] text-text-base/45"
                title="Código Mercado Público"
              >
                MP {row.codigoMercadoPublico}
              </span>
            )}
          </div>
        ),
      },
      {
        key: 'fecha',
        header: 'Fecha',
        width: '110px',
        render: (row) => formatFecha(row.fecha),
      },
      { key: 'proveedorNombre', header: 'Proveedor', truncate: true },
      {
        key: 'estado',
        header: 'Estado',
        width: '160px',
        render: (row) => <EstadoOrdenBadge estado={row.estado} />,
      },
      {
        key: 'total',
        header: 'Total',
        width: '130px',
        render: (row) => (
          <span className="tabular-nums">{formatCLP(row.total)}</span>
        ),
      },
      {
        key: 'acciones',
        header: '',
        width: '320px',
        render: (row) => renderAcciones(row),
      },
    ],
    [renderAcciones],
  );

  const totales = useMemo(() => calcularTotales(form.items), [form.items]);
  const accionMeta = accion ? ACCION_META[accion.tipo] : null;
  const accionPendiente =
    aprobarMutation.isPending || rechazarMutation.isPending || anularMutation.isPending;

  // ─── Render ──────────────────────────────────────────────────────────────
  return (
    <div className="flex flex-col gap-3">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h2 className="text-lg font-semibold text-text-base">Órdenes de Compra</h2>
        {canCrear && (
          <Button variant="primary" size="sm" onClick={openCreate}>
            + Nueva Orden de Compra
          </Button>
        )}
      </div>

      {/* Error */}
      {isError && (
        <div className="mb-3 rounded border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-700">
          Error al cargar las órdenes de compra. Por favor recargá la página.
        </div>
      )}

      {/* Filters */}
      <div className="bg-surface border border-border-base rounded p-4">
        <div className="flex flex-wrap gap-3 items-end">
          <div className="flex min-w-0 flex-1 flex-col gap-1">
            <label className="text-xs font-medium text-text-base/65" htmlFor="oc-filtro-search">
              Búsqueda
            </label>
            <Input
              id="oc-filtro-search"
              type="text"
              placeholder="Número u observaciones..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>

          <div className="flex w-48 flex-col gap-1">
            <label className="text-xs font-medium text-text-base/65">Estado</label>
            <Select
              aria-label="Filtrar por estado"
              options={ESTADO_FILTER_OPTIONS}
              placeholder="Todos"
              value={filtros.estado ?? ''}
              onChange={(e) =>
                setFiltros({ estado: (e.target.value || undefined) as EstadoOrdenCompra | undefined })
              }
            />
          </div>

          <div className="flex w-56 flex-col gap-1">
            <label className="text-xs font-medium text-text-base/65">Proveedor</label>
            <Select
              aria-label="Filtrar por proveedor"
              options={proveedorOptions}
              placeholder="Todos"
              value={filtros.proveedorId ?? ''}
              onChange={(e) => setFiltros({ proveedorId: e.target.value || undefined })}
            />
          </div>

          <Button
            variant="secondary"
            size="sm"
            onClick={() => {
              setSearchInput('');
              resetFiltros();
            }}
          >
            Limpiar
          </Button>
        </div>
      </div>

      {/* DataGrid */}
      <DataGrid<OrdenCompraListItem>
        columns={columns}
        data={data?.items ?? []}
        loading={isLoading}
        emptyMessage="No hay órdenes de compra para los filtros seleccionados."
      />

      {/* Pagination */}
      {data && (
        <Pagination
          pagina={data.pagina}
          totalPaginas={data.totalPaginas}
          totalItems={data.totalItems}
          tamanoPagina={filtros.pageSize}
          onChange={handlePaginaChange}
          onTamanoPaginaChange={handleTamanoPaginaChange}
        />
      )}

      {/* ── Create / Edit modal ─────────────────────────────────────────── */}
      <ModalDialog
        open={showForm}
        title={editTarget ? `Editar Orden de Compra ${editTarget.numero ?? '(borrador)'}` : 'Nueva Orden de Compra'}
        onClose={() => setShowForm(false)}
        size="xl"
        footer={
          <>
            <Button variant="secondary" size="sm" onClick={() => setShowForm(false)}>
              Cancelar
            </Button>
            <Button
              variant="primary"
              size="sm"
              loading={createMutation.isPending || updateMutation.isPending}
              disabled={formInvalido(form)}
              onClick={handleSubmitForm}
            >
              {editTarget ? 'Guardar' : 'Crear'}
            </Button>
          </>
        }
      >
        <OrdenCompraFormFields
          form={form}
          onChange={setForm}
          proveedorOptions={proveedorOptions}
          totales={totales}
          showAllErrors={submitAttempted}
        />
      </ModalDialog>

      {/* ── Detail modal ────────────────────────────────────────────────── */}
      <ModalDialog
        open={!!detalleId}
        title={
          detalleQuery.data
            ? `Orden de Compra ${detalleQuery.data.numero ?? '(borrador)'}`
            : 'Orden de Compra'
        }
        onClose={() => setDetalleId(null)}
        size="xl"
        footer={
          <Button variant="secondary" size="sm" onClick={() => setDetalleId(null)}>
            Cerrar
          </Button>
        }
      >
        {detalleQuery.isLoading && (
          <p className="py-6 text-center text-sm text-text-base/55">Cargando detalle…</p>
        )}
        {detalleQuery.isError && (
          <p className="py-6 text-center text-sm text-red-600">
            No se pudo cargar el detalle de la orden de compra.
          </p>
        )}
        {detalleQuery.data && (
          <OrdenCompraDetalle
            oc={detalleQuery.data}
            canCrear={canCrear}
            uploading={agregarAdjuntoMutation.isPending}
            onUploadAdjunto={handleUploadAdjunto}
            onDownloadAdjunto={handleDownloadAdjunto}
            onEliminarAdjunto={handleEliminarAdjunto}
            onNotify={(type, message) => setToast({ type, message })}
          />
        )}
      </ModalDialog>

      {/* ── Action modal (aprobar / rechazar / anular) ──────────────────── */}
      <ModalDialog
        open={!!accion}
        title={accionMeta?.titulo ?? ''}
        onClose={() => setAccion(null)}
        size="md"
        footer={
          <>
            <Button variant="secondary" size="sm" onClick={() => setAccion(null)}>
              Cancelar
            </Button>
            <Button
              variant={accion?.tipo === 'aprobar' ? 'primary' : 'danger'}
              size="sm"
              loading={accionPendiente}
              disabled={!!accionMeta?.requerido && !accionComentario.trim()}
              onClick={handleConfirmAccion}
            >
              {accionMeta?.confirmar ?? 'Confirmar'}
            </Button>
          </>
        }
      >
        {accion && accionMeta && (
          <div className="space-y-3">
            <p className="text-sm text-text-base/70">
              Orden de compra {accion.target.numero ?? '(borrador)'}.
            </p>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-text-base/70" htmlFor="oc-accion-comentario">
                {accionMeta.campo}
              </label>
              <textarea
                id="oc-accion-comentario"
                className="block w-full rounded border border-border-base bg-surface px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary-500"
                rows={3}
                maxLength={1000}
                value={accionComentario}
                onChange={(e) => setAccionComentario(e.target.value)}
              />
            </div>
          </div>
        )}
      </ModalDialog>

      {/* ── Confirm dialog (enviar a aprobación / eliminar adjunto) ─────── */}
      <ConfirmDialog
        open={!!confirmacion}
        title={confirmacion?.titulo ?? ''}
        message={confirmacion?.mensaje ?? ''}
        confirmLabel={confirmacion?.confirmLabel}
        danger={confirmacion?.danger}
        loading={confirmacionPendiente}
        onConfirm={handleAceptarConfirmacion}
        onCancel={() => {
          if (!confirmacionPendiente) setConfirmacion(null);
        }}
      />

      {/* Toast */}
      {toast && (
        <AlertToast type={toast.type} message={toast.message} onClose={() => setToast(null)} />
      )}
    </div>
  );
}
