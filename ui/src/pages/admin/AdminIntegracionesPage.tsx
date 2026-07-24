import { useEffect, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getIntegraciones,
  actualizarIntegracion,
  probarConexion,
  type IntegracionDto,
  type ActualizarIntegracionData,
  type ConexionTestResultDto,
} from '../../lib/api/admin/adminIntegracionesApi';
import Spinner from '../../components/atoms/Spinner';
import IconButton from '../../components/atoms/IconButton';
import ModalDialog from '../../components/organisms/ModalDialog';
import Button from '../../components/atoms/Button';
import Pagination from '../../components/molecules/Pagination';
import { useHasPermission } from '../../hooks/usePermissions';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }

  return fallback;
}

function getIntegracionHint(tipo: string): string {
  switch (tipo.toLowerCase()) {
    case 'docdigital':
      return 'Configure la dirección de conexión y la clave más reciente para mantener el servicio operativo.';
    case 'firmagob':
      return 'Verifique que la dirección corresponda al entorno correcto y que la clave sea la adecuada.';
    case 'claveunica':
      return 'Use la dirección del entorno correcto y evite reemplazar la clave si no hubo cambios.';
    case 'onlyoffice':
      return 'Configure la URL del servidor de documentos y la URL interna del backend. La clave secreta se gestiona por variable de entorno.';
    case 'mercadopublico':
      return 'Configure el ticket de acceso de la API pública de Mercado Público para consultar y vincular órdenes de compra del portal.';
    default:
      return 'Aún no hay validación automática disponible para esta integración.';
  }
}

export default function AdminIntegracionesPage() {
  const qc = useQueryClient();
  const toast = useToast();
  const canEditIntegracion = useHasPermission(PERMISSIONS.ADMIN_INTEGRACIONES_EDITAR);
  const [editing, setEditing] = useState<IntegracionDto | null>(null);
  const [formData, setFormData] = useState<ActualizarIntegracionData>({ baseUrl: '' });
  const [formError, setFormError] = useState<string | null>(null);
  const [testState, setTestState] = useState<'idle' | 'testing' | 'ok' | 'error'>('idle');
  const [testResult, setTestResult] = useState<ConexionTestResultDto | null>(null);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-integraciones'],
    queryFn: getIntegraciones,
  });

  const [pagina, setPagina] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(20);
  const totalIntegraciones = data?.length ?? 0;
  const totalPaginas = Math.max(1, Math.ceil(totalIntegraciones / tamanoPagina));

  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(totalIntegraciones / tamanoPagina));
    setPagina((current) => Math.min(current, maxPagina));
  }, [totalIntegraciones, tamanoPagina]);

  const pagedIntegraciones = (data ?? []).slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina);

  const actualizarMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: ActualizarIntegracionData }) =>
      actualizarIntegracion(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-integraciones'] });
      setEditing(null);
      setFormError(null);
      toast.success('Integración actualizada correctamente.');
    },
    onError: (error) => {
      toast.error(getErrorMessage(error, 'No se pudo actualizar la integración.'));
    },
  });

  const probarMut = useMutation({
    mutationFn: (id: string) => probarConexion(id),
    onMutate: () => {
      setTestState('testing');
      setTestResult(null);
    },
    onSuccess: (res) => {
      setTestState(res.success ? 'ok' : 'error');
      setTestResult(res);
      if (res.success) {
        toast.success(`Conexión exitosa. ${res.mensaje}`);
      } else {
        toast.error(`No se pudo conectar. ${res.mensaje}`);
      }
    },
    onError: (error) => {
      setTestState('error');
      const mensaje = getErrorMessage(error, 'No se pudo probar la conexión.');
      setTestResult({
        success: false,
        mensaje,
        latencyMs: null,
      });
      toast.error(mensaje);
    },
  });

  function isDocDigital(tipo: string): boolean {
    return tipo.toLowerCase() === 'docdigital';
  }

  function isOnlyOffice(tipo: string): boolean {
    return tipo.toLowerCase() === 'onlyoffice';
  }

  function isMercadoPublico(tipo: string): boolean {
    return tipo.toLowerCase() === 'mercadopublico';
  }

  function hasEditableSettings(tipo: string): boolean {
    return isDocDigital(tipo) || isOnlyOffice(tipo) || isMercadoPublico(tipo);
  }

  function setSetting(clave: string, valor: string) {
    setFormData((f) => ({ ...f, settings: { ...f.settings, [clave]: valor } }));
  }

  function openEdit(integracion: IntegracionDto) {
    if (!canEditIntegracion) return;
    setEditing(integracion);
    setFormData({
      baseUrl: integracion.baseUrl,
      apiKey: '',
      activo: integracion.activo,
      settings: hasEditableSettings(integracion.tipo) ? { ...integracion.settings } : undefined,
    });
    setFormError(null);
    setTestState('idle');
    setTestResult(null);
  }

  function handleSave() {
    if (!canEditIntegracion) return;
    if (!editing) return;

    const trimmedBaseUrl = formData.baseUrl.trim();
    if (!trimmedBaseUrl) {
      setFormError('La dirección es obligatoria.');
      return;
    }

    try {
      new URL(trimmedBaseUrl);
    } catch {
      setFormError('La dirección debe ser válida.');
      return;
    }

    if (isDocDigital(editing.tipo)) {
      const email = (formData.settings?.SystemUserEmail ?? '').trim();
      if (email && !/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email)) {
        setFormError('El email del usuario de sistema no es válido.');
        return;
      }
      const intervalStr = (formData.settings?.PollingIntervalMinutes ?? '').trim();
      if (intervalStr) {
        const n = Number(intervalStr);
        if (!Number.isInteger(n) || n < 1 || n > 1440) {
          setFormError('El intervalo de sondeo debe ser un entero entre 1 y 1440 minutos.');
          return;
        }
      }
    }

    if (isOnlyOffice(editing.tipo)) {
      const urlSettings: Array<[string, string]> = [
        ['CallbackUrl', 'La URL de callback'],
        ['BackendInternalUrl', 'La URL interna del backend'],
      ];
      for (const [key, label] of urlSettings) {
        const val = (formData.settings?.[key] ?? '').trim();
        if (val) {
          try {
            new URL(val);
          } catch {
            setFormError(`${label} debe ser una dirección válida.`);
            return;
          }
        }
      }
    }

    setFormError(null);
    actualizarMut.mutate({ id: editing.id, body: formData });
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-gray-800">Integraciones</h2>
      </div>

      {isLoading && (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      )}

      {isError && (
        <p className="text-red-600 text-sm">No se pudieron cargar las integraciones.</p>
      )}

      {data && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {pagedIntegraciones.map((integracion) => (
            <div key={integracion.id} className="bg-white border border-gray-200 rounded-lg p-4 shadow-sm">
              <div className="flex items-start justify-between mb-2">
                <div>
                  <h3 className="font-semibold text-gray-800 text-sm">{integracion.nombre}</h3>
                  <span className="text-xs text-gray-400 uppercase tracking-wide">{integracion.tipo}</span>
                </div>
                <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${integracion.activo ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                  {integracion.activo ? 'Activo' : 'Inactivo'}
                </span>
              </div>
              <p className="text-xs text-gray-600 mb-1">
                <span className="font-medium">URL:</span> {integracion.baseUrl}
              </p>
              <p className="text-xs text-gray-600 mb-1">
                <span className="font-medium">Servicio:</span> {integracion.nombre}
              </p>
              <p className="text-xs text-gray-500 font-mono mb-3">{integracion.apiKeyMasked}</p>
              <p className="mb-4 text-xs text-gray-500">{getIntegracionHint(integracion.tipo)}</p>
              {canEditIntegracion && (
                <IconButton
                  name="settings"
                  tooltip="Editar configuración"
                  appearance="admin"
                  onClick={() => openEdit(integracion)}
                />
              )}
            </div>
          ))}
          {data.length === 0 && (
            <div className="col-span-full rounded border border-dashed border-gray-300 bg-white px-6 py-10 text-center text-sm text-gray-500">
              No hay integraciones configuradas todavía.
            </div>
          )}
          {totalIntegraciones > 0 && (
            <div className="col-span-full">
              <Pagination
                pagina={pagina}
                totalPaginas={totalPaginas}
                totalItems={totalIntegraciones}
                tamanoPagina={tamanoPagina}
                onChange={setPagina}
                onTamanoPaginaChange={(tamano) => { setTamanoPagina(tamano); setPagina(1); }}
              />
            </div>
          )}
        </div>
      )}

      {editing && canEditIntegracion && (
        <ModalDialog
          open={editing !== null}
          title="Editar integración"
          onClose={() => { setEditing(null); setFormError(null); }}
          footer={(
            <>
              <Button variant="secondary" onClick={() => { setEditing(null); setFormError(null); }}>
                Cancelar
              </Button>
              <Button onClick={handleSave} loading={actualizarMut.isPending}>
                Guardar
              </Button>
            </>
          )}
        >
          <p className="mb-1 text-sm font-medium text-gray-800">{editing.nombre}</p>
          <p className="mb-4 text-sm text-gray-500">{getIntegracionHint(editing.tipo)}</p>

          {formError && (
            <div className="mb-4 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              {formError}
            </div>
          )}

          <div className="space-y-3">
            <div>
              <label className="mb-1 block text-xs text-gray-600">Dirección base</label>
              <input
                type="url"
                value={formData.baseUrl ?? ''}
                onChange={(e) => setFormData((f) => ({ ...f, baseUrl: e.target.value }))}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                placeholder="https://api.ejemplo.gob.cl"
              />
            </div>
            <div>
              <label className="mb-1 block text-xs text-gray-600">Clave de acceso (dejar vacío para mantener)</label>
              <input
                type="password"
                value={formData.apiKey ?? ''}
                onChange={(e) => setFormData((f) => ({ ...f, apiKey: e.target.value || undefined }))}
                className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                placeholder="Nueva clave de acceso…"
              />
            </div>
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="activo"
                checked={formData.activo ?? false}
                onChange={(e) => setFormData((f) => ({ ...f, activo: e.target.checked }))}
                className="rounded border-gray-300"
              />
              <label htmlFor="activo" className="text-xs text-gray-600">Activo</label>
            </div>
            {isDocDigital(editing.tipo) && (
              <>
                <div>
                  <label className="mb-1 block text-xs text-gray-600">Email del usuario de sistema</label>
                  <input
                    type="email"
                    value={formData.settings?.SystemUserEmail ?? ''}
                    onChange={(e) => setSetting('SystemUserEmail', e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                    placeholder="usuario.sistema@municipio.cl"
                  />
                  <p className="mt-1 text-xs text-gray-400">
                    Usuario provisionado para el auto-registro de documentos de DocDigital.
                  </p>
                </div>
                <div>
                  <label className="mb-1 block text-xs text-gray-600">Intervalo de sondeo (minutos)</label>
                  <input
                    type="number"
                    min={1}
                    max={1440}
                    value={formData.settings?.PollingIntervalMinutes ?? ''}
                    onChange={(e) => setSetting('PollingIntervalMinutes', e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                    placeholder="15"
                  />
                </div>
              </>
            )}
            {isOnlyOffice(editing.tipo) && (
              <>
                <div>
                  <label className="mb-1 block text-xs text-gray-600">URL de callback (opcional)</label>
                  <input
                    type="url"
                    value={formData.settings?.CallbackUrl ?? ''}
                    onChange={(e) => setSetting('CallbackUrl', e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                    placeholder="https://…/callback"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs text-gray-600">URL interna del backend</label>
                  <input
                    type="url"
                    value={formData.settings?.BackendInternalUrl ?? ''}
                    onChange={(e) => setSetting('BackendInternalUrl', e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                    placeholder="http://host.docker.internal:5000"
                  />
                  <p className="mt-1 text-xs text-gray-400">
                    Dirección con la que el servidor de OnlyOffice alcanza este backend
                    (por ejemplo, desde un contenedor). La clave secreta NO se configura acá.
                  </p>
                </div>
              </>
            )}
            {isMercadoPublico(editing.tipo) && (
              <>
                <div>
                  <label className="mb-1 block text-xs text-gray-600">Ticket de acceso</label>
                  <input
                    type="text"
                    value={formData.settings?.Ticket ?? ''}
                    onChange={(e) => setSetting('Ticket', e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                    placeholder="F8537A18-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
                  />
                  <p className="mt-1 text-xs text-gray-400">
                    Ticket entregado por Mercado Público para consumir su API pública.
                  </p>
                </div>
                <div>
                  <label className="mb-1 block text-xs text-gray-600">Código de organismo (opcional)</label>
                  <input
                    type="text"
                    value={formData.settings?.CodigoOrganismo ?? ''}
                    onChange={(e) => setSetting('CodigoOrganismo', e.target.value)}
                    className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
                    placeholder="6937"
                  />
                </div>
              </>
            )}
            <div className="space-y-2">
              <Button
                variant="secondary"
                disabled={testState === 'testing' || !formData.baseUrl.trim()}
                onClick={() => editing && probarMut.mutate(editing.id)}
              >
                Probar conexión
              </Button>
              {testState === 'testing' && (
                <div className="flex items-center gap-2 text-xs text-gray-500">
                  <Spinner size="sm" />
                  <span>Probando…</span>
                </div>
              )}
              {testState === 'ok' && testResult && (
                <div className="rounded border border-green-200 bg-green-50 px-3 py-2 text-xs text-green-700">
                  <span>Conexión alcanzable — {testResult.mensaje}</span>
                  {testResult.latencyMs !== null && (
                    <span className="ml-2 font-mono">{testResult.latencyMs}ms</span>
                  )}
                </div>
              )}
              {testState === 'error' && testResult && (
                <div className="rounded border border-red-200 bg-red-50 px-3 py-2 text-xs text-red-700">
                  {testResult.mensaje}
                </div>
              )}
            </div>
          </div>
        </ModalDialog>
      )}
    </div>
  );
}
