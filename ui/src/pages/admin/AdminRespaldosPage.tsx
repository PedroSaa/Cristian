import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getRespaldos,
  triggerRespaldo,
  downloadRespaldo,
  restoreRespaldo,
  getRestoreLogs,
  getRespaldoConfig,
  updateRespaldoConfig,
} from '../../lib/api/admin/adminRespaldosApi';
import type { RespaldoConfigDto, RestoreLogDto } from '../../lib/api/admin/adminRespaldosApi';
import Spinner from '../../components/atoms/Spinner';
import IconButton from '../../components/atoms/IconButton';
import ModalDialog from '../../components/organisms/ModalDialog';
import Button from '../../components/atoms/Button';
import Pagination from '../../components/molecules/Pagination';
import TabPanel from '../../components/organisms/TabPanel';
import type { Tab } from '../../components/organisms/TabPanel';
import RespaldoConfigForm from './RespaldoConfigForm';
import type { RespaldoConfigFormData } from './RespaldoConfigForm';
import { useHasPermission } from '../../hooks/usePermissions';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';

function estadoBadgeClass(estado: string) {
  if (estado === 'Completado') return 'bg-green-100 text-green-700';
  if (estado === 'Fallido') return 'bg-red-100 text-red-700';
  if (estado === 'EnProceso') return 'bg-blue-100 text-blue-700';
  return 'bg-yellow-100 text-yellow-700';
}

function estadoLabel(estado: string) {
  if (estado === 'EnProceso') return 'En Proceso';
  return estado;
}

function estadoRestoreLabel(estado: string) {
  switch (estado) {
    case 'EnProceso': return 'En Proceso';
    case 'Completado': return 'Completado';
    case 'Fallido': return 'Fallido';
    default: return 'Pendiente';
  }
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function isBackupInProgress(estado: string) {
  return estado === 'Pendiente' || estado === 'EnProceso';
}

function isRestoreInProgress(estado: string) {
  return estado === 'Pendiente' || estado === 'EnProceso';
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }

  return fallback;
}

export default function AdminRespaldosPage() {
  const canCreateRespaldo = useHasPermission(PERMISSIONS.ADMIN_RESPALDOS_CREAR);
  const canConfigureRespaldo = useHasPermission(PERMISSIONS.ADMIN_RESPALDOS_CONFIGURAR);
  const canDownloadRespaldo = useHasPermission(PERMISSIONS.ADMIN_RESPALDOS_DESCARGAR);
  const canRestoreRespaldo = useHasPermission(PERMISSIONS.ADMIN_RESPALDOS_RESTAURAR);
  const qc = useQueryClient();
  const toast = useToast();
  const [restoringId, setRestoringId] = useState<string | null>(null);
  const [confirmName, setConfirmName] = useState('');
  const [selectedBackupId, setSelectedBackupId] = useState<string | null>(null);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-respaldos'],
    queryFn: getRespaldos,
    refetchInterval: (query) => {
      const respaldos = query.state.data as Array<{ estado: string }> | undefined;
      return respaldos?.some((respaldo) => isBackupInProgress(respaldo.estado)) ? 2000 : false;
    },
    refetchIntervalInBackground: true,
  });

  const [pagina, setPagina] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(20);
  const totalRespaldos = data?.length ?? 0;
  const totalPaginas = Math.max(1, Math.ceil(totalRespaldos / tamanoPagina));

  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(totalRespaldos / tamanoPagina));
    setPagina((current) => Math.min(current, maxPagina));
  }, [totalRespaldos, tamanoPagina]);

  const pagedRespaldos = (data ?? []).slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina);

  const triggerMut = useMutation({
    mutationFn: triggerRespaldo,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-respaldos'] });
      toast.success('Respaldo iniciado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo generar el respaldo.')),
  });

  const downloadMut = useMutation({
    mutationFn: downloadRespaldo,
    onSuccess: () => toast.success('Descarga del respaldo iniciada.'),
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo descargar el respaldo.')),
  });

  const restoreMut = useMutation({
    mutationFn: ({ id, nombre }: { id: string; nombre: string }) =>
      restoreRespaldo(id, nombre),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-respaldos'] });
      qc.invalidateQueries({ queryKey: ['admin-restore-logs'] });
      setRestoringId(null);
      setConfirmName('');
      toast.success('Restauración iniciada correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo iniciar la restauración.')),
  });

  const configQuery = useQuery({
    queryKey: ['admin-respaldos-config'],
    queryFn: getRespaldoConfig,
  });

  const configMut = useMutation({
    mutationFn: (body: Omit<RespaldoConfigDto, 'id' | 'actualizadoEn'>) =>
      updateRespaldoConfig(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-respaldos-config'] });
      toast.success('Configuración de respaldos guardada correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo guardar la configuración de respaldos.')),
  });

  function triggerBackup() {
    if (!canCreateRespaldo) return;
    triggerMut.mutate();
  }

  useEffect(() => {
    if (!data?.length) {
      setSelectedBackupId(null);
      return;
    }

    const selectedStillExists = selectedBackupId
      ? data.some((respaldo) => respaldo.id === selectedBackupId && respaldo.estado === 'Completado')
      : false;

    if (selectedStillExists) {
      return;
    }

    const firstCompleted = data.find((respaldo) => respaldo.estado === 'Completado');
    setSelectedBackupId(firstCompleted?.id ?? null);
  }, [data, selectedBackupId]);

  const restoreLogsQuery = useQuery({
    queryKey: ['admin-restore-logs', selectedBackupId],
    queryFn: () => getRestoreLogs(selectedBackupId!),
    enabled: selectedBackupId !== null,
    refetchInterval: (query) => {
      const logs = query.state.data as Array<{ estado: string }> | undefined;
      return logs?.some((log) => isRestoreInProgress(log.estado)) ? 2000 : false;
    },
    refetchIntervalInBackground: true,
  });

  const restoringBackup = restoringId
    ? data?.find(r => r.id === restoringId)
    : null;

  const tabs: Tab[] = [
    {
      id: 'respaldos',
      label: 'Respaldos',
      content: (
        <div>
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-base font-semibold text-gray-800">Historial de Respaldos</h3>
            {canCreateRespaldo && (
              <Button size="sm" onClick={triggerBackup} loading={triggerMut.isPending}>
                Generar Respaldo
              </Button>
            )}
          </div>

          {isLoading && (
            <div className="flex justify-center py-12"><Spinner size="lg" /></div>
          )}

          {isError && (
            <p className="text-red-600 text-sm">No se pudieron cargar los respaldos.</p>
          )}

          {data && (
            <div className="rounded border border-gray-200 overflow-hidden">
              <table className="min-w-full text-sm">
                <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                  <tr>
                    <th className="px-4 py-2 text-left">Nombre</th>
                    <th className="px-4 py-2 text-left">Fecha Creación</th>
                    <th className="px-4 py-2 text-left">Tamaño</th>
                    <th className="px-4 py-2 text-left">Estado</th>
                    <th className="px-4 py-2 text-left">Acciones</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {pagedRespaldos.map((r) => (
                    <tr key={r.id} className="hover:bg-gray-50">
                      <td className="px-4 py-2 font-medium text-gray-800">{r.nombre}</td>
                      <td className="px-4 py-2 text-gray-500">
                        {new Date(r.fechaCreacion).toLocaleString('es-CL')}
                      </td>
                      <td className="px-4 py-2 text-gray-500">{formatBytes(r.tamanioBytes)}</td>
                      <td className="px-4 py-2">
                        <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${estadoBadgeClass(r.estado)}`}>
                          {estadoLabel(r.estado)}
                        </span>
                      </td>
                      <td className="px-4 py-2">
                        <div className="flex gap-1">
                          {r.estado === 'Completado' && (
                            <>
                              <IconButton
                                name="clock"
                                tooltip="Ver historial"
                                appearance="admin"
                                onClick={() => setSelectedBackupId(r.id)}
                              />
                              {canDownloadRespaldo && (
                                <IconButton
                                  name="download"
                                  tooltip="Descargar"
                                  appearance="admin"
                                  onClick={() => downloadMut.mutate(r.id)}
                                />
                              )}
                              {canRestoreRespaldo && (
                                <IconButton
                                  name="archive-restore"
                                  tooltip="Restaurar"
                                  appearance="admin"
                                  onClick={() => {
                                    setRestoringId(r.id);
                                    setConfirmName('');
                                    setSelectedBackupId(r.id);
                                  }}
                                />
                              )}
                            </>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                  {data.length === 0 && (
                    <tr>
                      <td colSpan={5} className="px-4 py-8 text-center text-gray-400 text-sm">
                        No hay respaldos registrados.
                      </td>
                    </tr>
                  )}
                </tbody>
              </table>
              {totalRespaldos > 0 && (
                <Pagination
                  pagina={pagina}
                  totalPaginas={totalPaginas}
                  totalItems={totalRespaldos}
                  tamanoPagina={tamanoPagina}
                  onChange={setPagina}
                  onTamanoPaginaChange={(tamano) => { setTamanoPagina(tamano); setPagina(1); }}
                />
              )}
            </div>
          )}

          {/* Restore History Section */}
          {selectedBackupId && (
            <div className="mt-6 mb-6 rounded border border-slate-200 bg-slate-50 p-4">
              <div className="mb-3 flex items-center justify-between gap-3">
                <h4 className="text-base font-semibold text-gray-800">Historial de Restauraciones</h4>
                {restoreLogsQuery.isFetching && (
                  <span className="text-xs text-gray-500">Actualizando…</span>
                )}
              </div>
              {restoreLogsQuery.isLoading ? (
                <div className="flex justify-center py-6"><Spinner size="md" /></div>
              ) : restoreLogsQuery.isError ? (
                <p className="text-sm text-red-600">No se pudo cargar el historial de restauraciones del respaldo seleccionado.</p>
              ) : restoreLogsQuery.data && restoreLogsQuery.data.length > 0 ? (
                <div className="rounded border border-gray-200 overflow-hidden bg-white">
                  <table className="min-w-full text-sm">
                    <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                      <tr>
                        <th className="px-4 py-2 text-left">Inicio</th>
                        <th className="px-4 py-2 text-left">Fin</th>
                        <th className="px-4 py-2 text-left">Estado</th>
                        <th className="px-4 py-2 text-left">Error</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {restoreLogsQuery.data.map((log: RestoreLogDto) => (
                        <tr key={log.id} className="hover:bg-gray-50">
                          <td className="px-4 py-2 text-gray-500">
                            {new Date(log.fechaInicio).toLocaleString('es-CL')}
                          </td>
                          <td className="px-4 py-2 text-gray-500">
                            {log.fechaFin ? new Date(log.fechaFin).toLocaleString('es-CL') : '—'}
                          </td>
                          <td className="px-4 py-2">
                            <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${estadoBadgeClass(log.estado)}`}>
                              {estadoRestoreLabel(log.estado)}
                            </span>
                          </td>
                          <td className="px-4 py-2 text-gray-500 text-xs">{log.mensajeError ?? '—'}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : (
                <p className="text-sm text-gray-500">El respaldo seleccionado todavía no tiene restauraciones registradas.</p>
              )}
            </div>
          )}
        </div>
      ),
    },
    {
      id: 'configuracion',
      label: 'Configuración',
      content: (
                    <RespaldoConfigForm
            config={configQuery.data}
            isLoading={configQuery.isLoading}
            isError={configQuery.isError}
            isSaving={configMut.isPending}
            saveError={null}
            canEdit={canConfigureRespaldo}
            onSave={(data: RespaldoConfigFormData) => {
              if (!canConfigureRespaldo) return;
              configMut.mutate(data);
            }}
          />
        ),
    },
  ];

  return (
    <div>
      <h2 className="text-lg font-semibold text-gray-800 mb-4">Respaldos</h2>
      <TabPanel tabs={tabs} />

      {/* Restore Confirmation Dialog */}
      <ModalDialog
        open={restoringId !== null}
        title="Confirmar Restauración"
        onClose={() => { setRestoringId(null); setConfirmName(''); }}
        footer={
          <>
            <Button variant="secondary" onClick={() => { setRestoringId(null); setConfirmName(''); }}>
              Cancelar
            </Button>
            <Button
              variant="danger"
              loading={restoreMut.isPending}
              disabled={confirmName !== (restoringBackup?.nombre ?? '')}
              onClick={() => {
                if (restoringId && restoringBackup) {
                  restoreMut.mutate({ id: restoringId, nombre: restoringBackup.nombre });
                }
              }}
            >
              Confirmar Restauración
            </Button>
          </>
        }
      >
        <p className="text-sm text-gray-600 mb-3">
          Estás a punto de restaurar el respaldo <strong>{restoringBackup?.nombre}</strong>.
          Esta operación <strong className="text-red-600">sobrescribirá</strong> la base de datos actual.
        </p>
        <p className="text-sm text-gray-600 mb-2">
          Escribe el nombre exacto del respaldo para confirmar:
        </p>
        <input
          type="text"
          placeholder="Escribe el nombre del respaldo"
          value={confirmName}
          onChange={(e) => setConfirmName(e.target.value)}
          className="w-full border border-gray-300 rounded px-3 py-2 text-sm focus:outline-none focus:border-blue-500"
        />
        {restoreMut.isPending && (
          <p className="mt-3 text-sm text-blue-700">La restauración ya fue iniciada y se está procesando en segundo plano.</p>
        )}
      </ModalDialog>
    </div>
  );
}
