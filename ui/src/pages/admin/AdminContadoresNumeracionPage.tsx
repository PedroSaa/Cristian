import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import Button from '../../components/atoms/Button';
import IconButton from '../../components/atoms/IconButton';
import ModalDialog from '../../components/organisms/ModalDialog';
import ConfirmDialog from '../../components/organisms/ConfirmDialog';
import Pagination from '../../components/molecules/Pagination';
import Spinner from '../../components/atoms/Spinner';
import { useHasPermission } from '../../hooks/usePermissions';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import {
  createCounter,
  deactivateCounter,
  incrementCounter,
  listCounters,
  reactivateCounter,
  setCounterValue,
  type CounterListDto,
  type CreateCounterData,
} from '../../lib/api/admin/numeracionApi';
import { listSeFordoc } from '../../lib/api/admin/adminCatalogosApi';

/**
 * DF (document-flow) types are a fixed legacy domain, not a table — same closed
 * set the Correlativos maintainer uses. A select prevents typos from silently
 * creating a parallel counter series.
 */
const DF_TIPOS = [
  { value: 'DOCINTER', label: 'Interno (DOCINTER)' },
  { value: 'DOCRECIB', label: 'Recibido (DOCRECIB)' },
  { value: 'DOCENVIA', label: 'Enviado (DOCENVIA)' },
  { value: 'TAREAS', label: 'Tareas (TAREAS)' },
] as const;

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
}

const emptyCounterForm = {
  codigoContador: '',
  orgDepCod: '',
  tipoCod: '0',
  dfTipo: '',
  nivelCod: '',
  periodicidad: 'CONTINUO' as CounterPeriodicidad,
  valorInicial: '0',
};

type CounterPeriodicidad = 'CONTINUO' | 'ANUAL' | 'MENSUAL';

export default function AdminContadoresNumeracionPage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_NUMERACION_EDITAR);
  const qc = useQueryClient();
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(20);
  const [activoFilter, setActivoFilter] = useState<boolean | undefined>(undefined);
  const [searchCodigo, setSearchCodigo] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<CounterListDto | null>(null);
  const [togglingCounter, setTogglingCounter] = useState<CounterListDto | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [showSetValue, setShowSetValue] = useState(false);
  const [newValor, setNewValor] = useState('');
  const [createForm, setCreateForm] = useState(emptyCounterForm);

  const queryKey = ['admin-numeracion', 'contadores', { page, tamanoPagina, activoFilter, searchCodigo }] as const;

  // Document formats feed the "Tipo" select (tipo_cod maps to the format's integer code).
  const { data: formatos } = useQuery({
    queryKey: ['admin-catalogos', 'formatos', 'selector'] as const,
    queryFn: listSeFordoc,
    staleTime: 5 * 60 * 1000,
    enabled: showCreate,
  });

  const { data, isLoading, isError, error: queryError } = useQuery({
    queryKey,
    queryFn: () => listCounters({
      page,
      pageSize: tamanoPagina,
      activo: activoFilter,
      codigoContador: searchCodigo || undefined,
    }),
  });

  const createMut = useMutation({
    mutationFn: (body: CreateCounterData) => createCounter(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'contadores'] });
      setShowCreate(false);
      setError(null);
      toast.success('Contador creado correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear el contador.')),
  });

  const setValueMut = useMutation({
    mutationFn: ({ id, valor }: { id: string; valor: number }) => setCounterValue(id, valor),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'contadores'] });
      setShowSetValue(false);
      setError(null);
      toast.success('Valor actualizado correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar el valor.')),
  });

  const incrementMut = useMutation({
    mutationFn: incrementCounter,
    onSuccess: (result) => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'contadores'] });
      toast.success(`Contador incrementado. Nuevo valor: ${result.valor}`);
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo incrementar el contador.')),
  });

  const toggleMut = useMutation({
    mutationFn: async (item: CounterListDto) => {
      if (item.activo) await deactivateCounter(item.id);
      else await reactivateCounter(item.id);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'contadores'] });
      setTogglingCounter(null);
      toast.success('Estado actualizado correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo cambiar el estado.')),
  });

  function openSetValue(item: CounterListDto) {
    setSelected(item);
    setShowCreate(false);
    setNewValor(String(item.ultimoValor));
    setShowSetValue(true);
    setError(null);
  }

  function openCreate() {
    setShowSetValue(false);
    setSelected(null);
    setCreateForm(emptyCounterForm);
    setShowCreate(true);
    setError(null);
  }

  function closeCreate() {
    setShowCreate(false);
    setError(null);
  }

  function handleCreate() {
    const codigoContador = createForm.codigoContador.trim();
    const orgDepCod = createForm.orgDepCod.trim();

    if (!codigoContador) {
      setError('El código del contador es obligatorio.');
      return;
    }

    if (!orgDepCod) {
      setError('El código de organización es obligatorio.');
      return;
    }

    const tipoCod = Number(createForm.tipoCod);
    const valorInicial = Number(createForm.valorInicial);

    if (Number.isNaN(tipoCod)) {
      setError('El tipo debe ser un número válido.');
      return;
    }

    if (Number.isNaN(valorInicial) || valorInicial < 0) {
      setError('El valor inicial debe ser un número válido y no negativo.');
      return;
    }

    createMut.mutate({
      codigoContador,
      orgDepCod,
      tipoCod,
      dfTipo: createForm.dfTipo.trim() || undefined,
      nivelCod: createForm.nivelCod.trim() || undefined,
      periodicidad: createForm.periodicidad,
      valorInicial,
    });
  }

  function handleSetValue() {
    if (!selected) return;
    const valor = Number(newValor);
    if (Number.isNaN(valor)) {
      setError('El valor debe ser un número válido.');
      return;
    }
    setValueMut.mutate({ id: selected.id, valor });
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-gray-800">Contadores de Numeración</h2>
          <p className="mt-1 text-sm text-gray-500">
            Administración de contadores secuenciales atómicos para numeración de documentos.
          </p>
        </div>
        {canEdit && (
          <Button onClick={openCreate} variant="primary">
            Nuevo Contador
          </Button>
        )}
      </div>

      {/* Filtros */}
      <div className="flex flex-wrap gap-3">
        <input
          type="text"
          placeholder="Buscar por código de contador..."
          value={searchCodigo}
          onChange={(e) => { setSearchCodigo(e.target.value); setPage(1); }}
          className="flex-1 min-w-[200px] rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
        />
        <select
          value={activoFilter === undefined ? '' : activoFilter ? 'true' : 'false'}
          onChange={(e) => { setActivoFilter(e.target.value === '' ? undefined : e.target.value === 'true'); setPage(1); }}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
        >
          <option value="">Todos</option>
          <option value="true">Activos</option>
          <option value="false">Inactivos</option>
        </select>
      </div>

      {/* Mensajes de validación */}
      {error && <div className="rounded-md bg-red-50 p-3 text-sm text-red-700">{error}</div>}

      {/* Tabla */}
      <div className="overflow-x-auto rounded-lg border border-gray-200">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Código</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Org/Dep</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Tipo</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Periodicidad</th>
              <th className="px-4 py-3 text-right font-medium text-gray-600">Último Valor</th>
              <th className="px-4 py-3 text-center font-medium text-gray-600">Activo</th>
              {canEdit && <th className="px-4 py-3 text-center font-medium text-gray-600">Acciones</th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr>
                <td colSpan={canEdit ? 7 : 6} className="px-4 py-8">
                  <div className="flex justify-center"><Spinner size="md" /></div>
                </td>
              </tr>
            ) : isError ? (
              <tr>
                <td colSpan={canEdit ? 7 : 6} className="px-4 py-8 text-center text-red-500">
                  Error: {getErrorMessage(queryError, 'Error de conexión')}
                </td>
              </tr>
            ) : !data || data.items.length === 0 ? (
              <tr>
                <td colSpan={canEdit ? 7 : 6} className="px-4 py-8 text-center text-gray-400">
                  No hay contadores registrados.
                </td>
              </tr>
            ) : (
              data.items.map((item) => (
                <tr key={item.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 font-mono text-gray-900">{item.codigoContador}</td>
                  <td className="px-4 py-3 text-gray-600">{item.orgDepCod ?? '-'}</td>
                  <td className="px-4 py-3 text-gray-600">{item.tipoCod}{item.dfTipo ? ` / ${item.dfTipo}` : ''}</td>
                  <td className="px-4 py-3 text-gray-600">{item.periodicidad}</td>
                  <td className="px-4 py-3 text-right font-mono text-gray-900">{item.ultimoValor.toLocaleString()}</td>
                  <td className="px-4 py-3 text-center">
                    <span className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${item.activo ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                      {item.activo ? 'Sí' : 'No'}
                    </span>
                  </td>
                  {canEdit && (
                    <td className="px-4 py-3 text-center">
                      <div className="flex items-center justify-center gap-1">
                        <IconButton
                          name="plus"
                          tooltip="+1"
                          appearance="admin"
                          onClick={() => incrementMut.mutate(item.id)}
                        />
                        <IconButton
                          name="settings"
                          tooltip="Valor"
                          appearance="admin"
                          onClick={() => openSetValue(item)}
                        />
                        <IconButton
                          name={item.activo ? 'x' : 'check'}
                          tooltip={item.activo ? 'Desactivar' : 'Activar'}
                          variant={item.activo ? 'secondary' : 'ghost'}
                          appearance="admin"
                          onClick={() => setTogglingCounter(item)}
                        />
                      </div>
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Paginación */}
      {data && (
        <div className="mt-4">
          <Pagination
            pagina={data.page}
            totalPaginas={data.totalPaginas}
            totalItems={data.total}
            tamanoPagina={tamanoPagina}
            onChange={setPage}
            onTamanoPaginaChange={(t) => { setTamanoPagina(t); setPage(1); }}
          />
        </div>
      )}

      {/* Modal: Set Value */}
      <ModalDialog
        open={showCreate}
        onClose={closeCreate}
        title="Nuevo Contador"
        footer={(
          <>
            <Button variant="secondary" onClick={closeCreate}>Cancelar</Button>
            <Button variant="primary" onClick={handleCreate} loading={createMut.isPending}>
              Crear
            </Button>
          </>
        )}
      >
        <div className="grid gap-3 md:grid-cols-2">
          {error && <div className="md:col-span-2 rounded-md bg-red-50 p-3 text-sm text-red-700">{error}</div>}
          <label className="space-y-1 text-sm text-gray-700 md:col-span-2">
            <span className="block font-medium">Código del contador</span>
            <input
              type="text"
              value={createForm.codigoContador}
              onChange={(e) => setCreateForm((current) => ({ ...current, codigoContador: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
          </label>
          <label className="space-y-1 text-sm text-gray-700">
            <span className="block font-medium">Código organización</span>
            <input
              type="text"
              value={createForm.orgDepCod}
              onChange={(e) => setCreateForm((current) => ({ ...current, orgDepCod: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
          </label>
          <label className="space-y-1 text-sm text-gray-700">
            <span className="block font-medium">Tipo</span>
            <select
              value={createForm.tipoCod}
              onChange={(e) => setCreateForm((current) => ({ ...current, tipoCod: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            >
              <option value="0">(todos los formatos)</option>
              {(formatos ?? []).map((f) => (
                <option key={f.tipoCod} value={String(f.tipoCod)}>
                  {f.tipoCod} — {f.tipoDesc}
                </option>
              ))}
            </select>
          </label>
          <label className="space-y-1 text-sm text-gray-700">
            <span className="block font-medium">Tipo DF</span>
            <select
              value={createForm.dfTipo}
              onChange={(e) => setCreateForm((current) => ({ ...current, dfTipo: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            >
              <option value="">(sin tipo)</option>
              {DF_TIPOS.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </label>
          <label className="space-y-1 text-sm text-gray-700">
            <span className="block font-medium">Nivel</span>
            <input
              type="text"
              value={createForm.nivelCod}
              onChange={(e) => setCreateForm((current) => ({ ...current, nivelCod: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
          </label>
          <label className="space-y-1 text-sm text-gray-700">
            <span className="block font-medium">Periodicidad</span>
            <select
              value={createForm.periodicidad}
              onChange={(e) => setCreateForm((current) => ({ ...current, periodicidad: e.target.value as CounterPeriodicidad }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            >
              <option value="CONTINUO">CONTINUO</option>
              <option value="ANUAL">ANUAL</option>
              <option value="MENSUAL">MENSUAL</option>
            </select>
          </label>
          <label className="space-y-1 text-sm text-gray-700">
            <span className="block font-medium">Valor inicial</span>
            <input
              type="number"
              value={createForm.valorInicial}
              onChange={(e) => setCreateForm((current) => ({ ...current, valorInicial: e.target.value }))}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
          </label>
        </div>
      </ModalDialog>

      <ModalDialog
        open={showSetValue}
        onClose={() => setShowSetValue(false)}
        title="Establecer Valor"
        footer={(
          <>
            <Button variant="secondary" onClick={() => setShowSetValue(false)}>Cancelar</Button>
            <Button variant="primary" onClick={handleSetValue} loading={setValueMut.isPending}>
              Guardar
            </Button>
          </>
        )}
      >
        <div className="space-y-4">
          <p className="text-sm text-gray-600">
            Contador: <span className="font-semibold">{selected?.codigoContador}</span>
          </p>
          <div>
            <label htmlFor="set-valor" className="block text-sm font-medium text-gray-700">Nuevo valor</label>
            <input
              id="set-valor"
              type="number"
              value={newValor}
              onChange={(e) => setNewValor(e.target.value)}
              className="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
          </div>
        </div>
      </ModalDialog>

      <ConfirmDialog
        open={togglingCounter !== null}
        title={togglingCounter?.activo ? 'Desactivar contador' : 'Activar contador'}
        message={togglingCounter
          ? `¿Seguro que querés ${togglingCounter.activo ? 'desactivar' : 'activar'} el contador "${togglingCounter.codigoContador}"?`
          : ''}
        confirmLabel={togglingCounter?.activo ? 'Desactivar' : 'Activar'}
        danger={togglingCounter?.activo ?? false}
        loading={toggleMut.isPending}
        onConfirm={() => { if (togglingCounter) toggleMut.mutate(togglingCounter); }}
        onCancel={() => setTogglingCounter(null)}
      />
    </div>
  );
}
