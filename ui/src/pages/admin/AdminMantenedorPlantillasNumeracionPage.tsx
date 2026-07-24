import { useEffect, useState } from 'react';
import { useForm, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import Button from '../../components/atoms/Button';
import IconButton from '../../components/atoms/IconButton';
import FormField from '../../components/molecules/FormField';
import ModalDialog from '../../components/organisms/ModalDialog';
import ConfirmDialog from '../../components/organisms/ConfirmDialog';
import Pagination from '../../components/molecules/Pagination';
import Spinner from '../../components/atoms/Spinner';
import { useHasPermission } from '../../hooks/usePermissions';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import {
  createPlantillaNumeracion,
  listPlantillasNumeracion,
  setPlantillaActiva,
  deletePlantillaNumeracion,
  getTokensNumeracion,
  updatePlantillaNumeracion,
  type PlantillaNumeracionDto,
  type UpdatePlantillaData,
  type TokenNumeracion,
} from '../../lib/api/admin/plantillasNumeracionApi';

/** Reemplaza los tokens del patrón por sus ejemplos para la vista previa. */
function previewPatron(patron: string, tokens: TokenNumeracion[], relleno: number): string {
  let out = patron;
  for (const t of tokens) {
    let val = t.ejemplo;
    if (t.token === '{correlativo}' && relleno > 0) val = val.padStart(relleno, '0');
    out = out.split(t.token).join(val);
  }
  return out || '(sin número)';
}

/** Quita el último token o separador del patrón (para el botón ⌫). */
function quitarUltimo(p: string): string {
  if (p.endsWith('}')) {
    const i = p.lastIndexOf('{');
    return i >= 0 ? p.slice(0, i) : p;
  }
  return p.slice(0, -1);
}

const SEPARADORES = ['/', '-', '.', ' '];

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
}

const schema = z.object({
  // El Id lo asigna el servidor (autogenerado); el formulario no lo pide.
  descripcion: z.string().min(1, 'La descripción es obligatoria.').max(255, 'Máximo 255 caracteres.'),
  patron: z.string().min(1, 'El patrón es obligatorio.').max(200, 'Máximo 200 caracteres.'),
  // Política de conteo:
  porOrganismo: z.boolean(),
  porTipoDocumento: z.boolean(),
  porFormatoDocumento: z.boolean(),
  periodicidad: z.enum(['CONTINUO', 'ANUAL', 'MENSUAL']),
  momentoGeneracion: z.enum(['AL_INGRESAR', 'AL_FIRMAR', 'AMBOS', 'MANUAL']),
  rellenoCeros: z.coerce.number().int().min(0, 'Mínimo 0.').max(20, 'Máximo 20.'),
  valorInicial: z.coerce.number().int().min(0, 'No puede ser negativo.'),
});

const POLITICA_DEFAULT = {
  porOrganismo: false,
  porTipoDocumento: false,
  porFormatoDocumento: false,
  periodicidad: 'CONTINUO',
  momentoGeneracion: 'AL_INGRESAR',
  rellenoCeros: 0,
  valorInicial: 0,
} as const;

type FormData = z.infer<typeof schema>;
type Mode = 'crear' | 'editar' | null;

export default function AdminMantenedorPlantillasNumeracionPage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_PLANTILLAS_NUMERACION_EDITAR);
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<Mode>(null);
  const [selected, setSelected] = useState<PlantillaNumeracionDto | null>(null);
  const [deletingItem, setDeletingItem] = useState<PlantillaNumeracionDto | null>(null);
  const [search, setSearch] = useState('');

  const form = useForm<FormData>({ resolver: zodResolver(schema) as Resolver<FormData> });
  const isEdit = modal === 'editar';

  const { data, isLoading, isError, error: queryError } = useQuery({
    queryKey: ['admin-numeracion', 'plantillas'],
    queryFn: () => listPlantillasNumeracion(),
  });

  const { data: tokens = [] } = useQuery({
    queryKey: ['admin-numeracion', 'tokens'],
    queryFn: getTokensNumeracion,
  });

  const filteredData = (data ?? []).filter((item) => {
    const q = search.trim().toLowerCase();
    if (!q) return true;
    return String(item.id).includes(q)
      || item.descripcion.toLowerCase().includes(q)
      || (item.patron ?? '').toLowerCase().includes(q);
  });

  const [pagina, setPagina] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(20);
  const totalItems = filteredData.length;
  const totalPaginas = Math.max(1, Math.ceil(totalItems / tamanoPagina));

  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(totalItems / tamanoPagina));
    setPagina((current) => Math.min(current, maxPagina));
  }, [totalItems, tamanoPagina]);

  const pagedData = filteredData.slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina);

  const createMut = useMutation({
    mutationFn: createPlantillaNumeracion,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'plantillas'] });
      setModal(null);
      toast.success('Plantilla creada correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la plantilla.')),
  });

  const updateMut = useMutation({
    mutationFn: ({ id, body }: { id: number; body: UpdatePlantillaData }) =>
      updatePlantillaNumeracion(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'plantillas'] });
      setModal(null);
      toast.success('Plantilla actualizada correctamente.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la plantilla.')),
  });

  const setActivaMut = useMutation({
    mutationFn: setPlantillaActiva,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'plantillas'] });
      toast.success('Plantilla activa del sistema actualizada.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo activar la plantilla.')),
  });

  const deleteMut = useMutation({
    mutationFn: deletePlantillaNumeracion,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'plantillas'] });
      setDeletingItem(null);
      toast.success('Plantilla eliminada.');
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la plantilla.')),
  });

  function openCreate() {
    setSelected(null);
    form.reset({ descripcion: '', patron: '', ...POLITICA_DEFAULT });
    setModal('crear');
  }

  function openEdit(item: PlantillaNumeracionDto) {
    setSelected(item);
    form.reset({
      descripcion: item.descripcion,
      patron: item.patron ?? '',
      porOrganismo: item.porOrganismo,
      porTipoDocumento: item.porTipoDocumento,
      porFormatoDocumento: item.porFormatoDocumento,
      periodicidad: item.periodicidad,
      momentoGeneracion: item.momentoGeneracion,
      rellenoCeros: item.rellenoCeros,
      valorInicial: item.valorInicial,
    });
    setModal('editar');
  }

  function handleSubmit(data: FormData) {
    if (isEdit && selected) {
      updateMut.mutate({ id: selected.id, body: data });
    } else {
      createMut.mutate(data);
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-lg font-semibold text-gray-800">Plantillas de Numeración</h2>
          <p className="mt-1 text-sm text-gray-500">
            Administración de patrones para numeración automática de documentos.
          </p>
        </div>
        {canEdit && (
          <Button onClick={openCreate} variant="primary">
            Nueva Plantilla
          </Button>
        )}
      </div>

      {/* Búsqueda */}
      <div>
        <input
          type="text"
          placeholder="Buscar por Id, descripción o patrón..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
        />
      </div>

      {/* Tabla */}
      <div className="overflow-x-auto rounded-lg border border-gray-200">
        <table className="min-w-full divide-y divide-gray-200 text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Descripción</th>
              <th className="px-4 py-3 text-left font-medium text-gray-600">Patrón</th>
              <th className="px-4 py-3 text-center font-medium text-gray-600">Estado</th>
              {canEdit && <th className="px-4 py-3 text-center font-medium text-gray-600">Acciones</th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-100">
            {isLoading ? (
              <tr>
                <td colSpan={canEdit ? 4 : 3} className="px-4 py-8">
                  <div className="flex justify-center"><Spinner size="md" /></div>
                </td>
              </tr>
            ) : isError ? (
              <tr>
                <td colSpan={canEdit ? 4 : 3} className="px-4 py-8 text-center text-red-500">
                  Error al cargar: {getErrorMessage(queryError, 'Error de conexión')}
                </td>
              </tr>
            ) : filteredData.length === 0 ? (
              <tr>
                <td colSpan={canEdit ? 4 : 3} className="px-4 py-8 text-center text-gray-400">
                  No hay plantillas registradas.
                </td>
              </tr>
            ) : (
              pagedData.map((item) => (
                <tr key={item.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-900">{item.descripcion}</td>
                  <td className="px-4 py-3 font-mono text-sm text-gray-600">{item.patron}</td>
                  <td className="px-4 py-3 text-center">
                    <span
                      className={`inline-flex rounded-full px-2 py-0.5 text-xs font-medium ${
                        item.activo
                          ? 'bg-green-100 text-green-700'
                          : 'bg-red-100 text-red-700'
                      }`}
                    >
                      {item.activo ? 'Activa' : 'Inactiva'}
                    </span>
                  </td>
                  {canEdit && (
                    <td className="px-4 py-3 text-center">
                      <div className="flex items-center justify-center gap-2">
                        <IconButton
                          name="edit"
                          tooltip="Editar"
                          appearance="admin"
                          onClick={() => openEdit(item)}
                        />
                        <IconButton
                          name="trash"
                          tooltip="Eliminar"
                          variant="danger"
                          appearance="admin"
                          disabled={item.activo}
                          disabledTooltip="No se puede eliminar la plantilla activa del sistema"
                          onClick={() => setDeletingItem(item)}
                        />
                        {item.activo ? (
                          <span className="h-10 w-10 shrink-0" aria-hidden="true" />
                        ) : (
                          <IconButton
                            name="check"
                            tooltip="Usar como plantilla activa del sistema"
                            appearance="admin"
                            loading={setActivaMut.isPending}
                            onClick={() => setActivaMut.mutate(item.id)}
                          />
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
        {!isLoading && !isError && totalItems > 0 && (
          <Pagination
            pagina={pagina}
            totalPaginas={totalPaginas}
            totalItems={totalItems}
            tamanoPagina={tamanoPagina}
            onChange={setPagina}
            onTamanoPaginaChange={(tamano) => { setTamanoPagina(tamano); setPagina(1); }}
          />
        )}
      </div>

      {/* Modal */}
      <ModalDialog
        open={modal !== null}
        onClose={() => setModal(null)}
        title={isEdit ? 'Editar Plantilla' : 'Nueva Plantilla'}
        footer={(
          <>
            <Button variant="secondary" onClick={() => setModal(null)} type="button">
              Cancelar
            </Button>
            <Button variant="primary" type="submit" form="plantilla-numeracion-form" loading={createMut.isPending || updateMut.isPending}>
              Guardar
            </Button>
          </>
        )}
      >
        <form id="plantilla-numeracion-form" onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
          <FormField label="Descripción" error={form.formState.errors.descripcion?.message}>
            <input
              {...form.register('descripcion')}
              className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none"
            />
          </FormField>
          <FormField label="Patrón" error={form.formState.errors.patron?.message}>
            {/* El valor se construye con los botones; no se tipea libre (evita patrones inválidos). */}
            <input type="hidden" {...form.register('patron')} />
            <div className="min-h-[2.25rem] w-full rounded-md border border-gray-300 bg-gray-50 px-3 py-2 font-mono text-sm">
              {form.watch('patron')
                ? form.watch('patron')
                : <span className="text-gray-400">Armá el patrón con los botones de abajo</span>}
            </div>
            <div className="mt-2 flex flex-wrap gap-1">
              {tokens.map((t) => (
                <button
                  key={t.token}
                  type="button"
                  title={`${t.descripcion} (ej: ${t.ejemplo})`}
                  onClick={() => form.setValue('patron', (form.getValues('patron') ?? '') + t.token, { shouldValidate: true, shouldDirty: true })}
                  className="rounded border border-blue-200 bg-blue-50 px-2 py-1 font-mono text-xs text-blue-700 hover:bg-blue-100"
                >
                  {t.token}
                </button>
              ))}
            </div>
            <div className="mt-1 flex flex-wrap items-center gap-1">
              <span className="text-xs text-gray-500">Separadores:</span>
              {SEPARADORES.map((s) => (
                <button
                  key={s}
                  type="button"
                  onClick={() => form.setValue('patron', (form.getValues('patron') ?? '') + s, { shouldValidate: true, shouldDirty: true })}
                  className="rounded border border-gray-300 px-2 py-1 font-mono text-xs hover:bg-gray-100"
                >
                  {s === ' ' ? '␣' : s}
                </button>
              ))}
              <button
                type="button"
                onClick={() => form.setValue('patron', quitarUltimo(form.getValues('patron') ?? ''), { shouldValidate: true, shouldDirty: true })}
                className="ml-2 rounded border border-gray-300 px-2 py-1 text-xs hover:bg-gray-100"
              >
                ⌫ Borrar
              </button>
              <button
                type="button"
                onClick={() => form.setValue('patron', '', { shouldValidate: true, shouldDirty: true })}
                className="rounded border border-gray-300 px-2 py-1 text-xs hover:bg-gray-100"
              >
                Limpiar
              </button>
            </div>
            <p className="mt-2 text-xs text-gray-500">
              Vista previa: <span className="font-mono text-gray-800">{previewPatron(form.watch('patron') ?? '', tokens, Number(form.watch('rellenoCeros')) || 0)}</span>
            </p>
          </FormField>

          <fieldset className="rounded-md border border-gray-200 p-3">
            <legend className="px-1 text-xs font-semibold uppercase tracking-wide text-gray-500">Cuenta por (ejes del correlativo)</legend>
            <p className="mb-2 text-xs text-gray-500">Sin marcar nada, el correlativo es global. Combiná los ejes según necesites.</p>
            <div className="space-y-2">
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" {...form.register('porOrganismo')} className="h-4 w-4 rounded border-gray-300" />
                Por organismo
              </label>
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" {...form.register('porTipoDocumento')} className="h-4 w-4 rounded border-gray-300" />
                Por tipo de documento (Recibido / Enviado / Interno)
              </label>
              <label className="flex items-center gap-2 text-sm">
                <input type="checkbox" {...form.register('porFormatoDocumento')} className="h-4 w-4 rounded border-gray-300" />
                Por formato de documento (Memo / Informe / Contrato / …)
              </label>
            </div>
          </fieldset>

          <div className="grid grid-cols-2 gap-3">
            <FormField label="Periodicidad (reinicio)" error={form.formState.errors.periodicidad?.message}>
              <select {...form.register('periodicidad')} className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none">
                <option value="CONTINUO">Continuo (no reinicia)</option>
                <option value="ANUAL">Anual</option>
                <option value="MENSUAL">Mensual</option>
              </select>
            </FormField>
            <FormField label="Momento de generación" error={form.formState.errors.momentoGeneracion?.message}>
              <select {...form.register('momentoGeneracion')} className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none">
                <option value="AL_INGRESAR">Al ingresar</option>
                <option value="AL_FIRMAR">Al firmar</option>
                <option value="AMBOS">Ambos</option>
                <option value="MANUAL">Manual</option>
              </select>
            </FormField>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <FormField label="Relleno de ceros" error={form.formState.errors.rellenoCeros?.message}>
              <input type="number" min={0} max={20} {...form.register('rellenoCeros')} className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none" />
            </FormField>
            <FormField label="Valor inicial" error={form.formState.errors.valorInicial?.message}>
              <input type="number" min={0} {...form.register('valorInicial')} className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none" />
            </FormField>
          </div>
        </form>
      </ModalDialog>

      <ConfirmDialog
        open={deletingItem !== null}
        title="Eliminar plantilla"
        message={deletingItem
          ? `¿Seguro que querés eliminar la plantilla "${deletingItem.descripcion}"? Esta acción no se puede deshacer.`
          : ''}
        confirmLabel="Eliminar"
        danger
        loading={deleteMut.isPending}
        onConfirm={() => { if (deletingItem) deleteMut.mutate(deletingItem.id); }}
        onCancel={() => setDeletingItem(null)}
      />
    </div>
  );
}
