import { useMemo, useState } from 'react';
import { useForm, useWatch, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useHasPermission } from '../../hooks/usePermissions';
import Button from '../../components/atoms/Button';
import FormField from '../../components/molecules/FormField';
import ModalDialog from '../../components/organisms/ModalDialog';
import LegacyCrudSection from './LegacyCrudSection';
import AdminPlantillasSection from './AdminPlantillasSection';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import {
  createCatalogoCategoria,
  createCatalogoSubcategoria,
  createSeClaseg,
  createSeCorfor,
  createSeFordoc,
  createSeFormaEnvio,
  createSeTiptar,
  deleteCatalogoCategoria,
  deleteCatalogoSubcategoria,
  deleteSeClaseg,
  deleteSeCorfor,
  deleteSeFordoc,
  deleteSeFormaEnvio,
  deleteSeTiptar,
  listCatalogoCategorias,
  listCatalogoSubcategorias,
  listSeClaseg,
  listSeCorfor,
  listSeFordoc,
  listSeFormaEnvio,
  listSeTiptar,
  updateCatalogoCategoria,
  updateCatalogoSubcategoria,
  updateSeClaseg,
  updateSeCorfor,
  updateSeFordoc,
  updateSeFormaEnvio,
  updateSeTiptar,
  type CatalogoCategoriaDto,
  type CatalogoSubcategoriaDto,
  type ActualizarSeCorforData,
  type ActualizarSeFordocData,
  type SeClasegDto,
  type SeCorforDto,
  type SeFordocDto,
  type SeFormaEnvioDto,
  type SeTiptarDto,
} from '../../lib/api/admin/adminCatalogosApi';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
}

function toDateInput(value?: string | null) {
  return value ? new Date(value).toISOString().slice(0, 10) : '';
}


function emptyToNull(value: string) {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

function matchesSearch(search: string, values: Array<string | number | null | undefined>) {
  const q = search.trim().toLowerCase();
  if (!q) return true;
  return values.some((value) => String(value ?? '').toLowerCase().includes(q));
}

const CORR_TIPO_OPTIONS = [
  { value: 'DOCINTER', label: 'Interno' },
  { value: 'DOCRECIB', label: 'Recibido' },
  { value: 'DOCENVIA', label: 'Enviado' },
  { value: 'TAREAS', label: 'Tareas' },
] as const;

const CORR_TIPO_LABELS: Record<string, string> = {
  DOCINTER: 'Interno',
  DOCRECIB: 'Recibido',
  DOCENVIA: 'Enviado',
  TAREAS: 'Tareas',
};

function corrTipoLabel(value: string) {
  return CORR_TIPO_LABELS[value] ?? value;
}

const categoriaSchema = z.object({
  catDesc: z.string().min(1, 'La descripción es obligatoria').max(60, 'Máximo 60 caracteres'),
});

const subcategoriaSchema = z.object({
  catCod: z.coerce.number().int().positive('La categoría es obligatoria'),
  subcatNombre: z.string().min(1, 'El nombre es obligatorio').max(200, 'Máximo 200 caracteres'),
  subcatDescripcion: z.string().max(200, 'Máximo 200 caracteres').optional(),
});

const clasegSchema = z.object({
  dfnClasif: z.string().min(1, 'La abreviatura es obligatoria').max(2, 'La abreviatura no puede superar los 2 caracteres'),
  dfdClasif: z.string().min(1, 'La descripción es obligatoria').max(15, 'La descripción no puede superar los 15 caracteres'),
});

const formaEnvioSchema = z.object({
  formaEnvio: z.string().min(1, 'La forma de envío es obligatoria').max(50, 'Máximo 50 caracteres'),
});

const tiptarSchema = z.object({
  dftaccion: z.string().min(1, 'La tarea es obligatoria').max(30, 'Máximo 30 caracteres'),
  dftacobsv: z.string().optional(),
  dftacdesc: z.string().max(60, 'Máximo 60 caracteres').optional(),
});

const fordocSchema = z.object({
  tipoRec: z.coerce.number().int().min(0, 'Debe ser un número válido').max(32767, 'Debe ser un número válido').default(0),
  tipoInt: z.coerce.number().int().min(0, 'Debe ser un número válido').max(32767, 'Debe ser un número válido').default(0),
  tipoDesc: z.string().min(1, 'La descripción es obligatoria').max(100, 'Máximo 100 caracteres'),
  corrN: z.coerce.number().int().min(0, 'Debe ser un número válido').max(2147483647, 'Debe ser un número válido'),
  tipoEnv: z.string().optional(),
  seFordocVistaI: z.coerce.number().int().min(0).max(32767).default(0),
  seFordocVistaE: z.coerce.number().int().min(0).max(32767).default(0),
  seFordocVistaR: z.coerce.number().int().min(0).max(32767).default(0),
  seFordocFormatoNum: z.string().max(40, 'Máximo 40 caracteres').optional(),
});

const corforSchema = z.object({
  corrTip: z.enum(['DOCINTER', 'DOCRECIB', 'DOCENVIA', 'TAREAS'], {
    message: 'El tipo es obligatorio',
  }),
  corrNro: z.coerce.number().int().min(0, 'Debe ser un número válido').max(2147483647, 'Debe ser un número válido'),
  corrDes: z.string().min(1, 'La descripción es obligatoria').max(60, 'Máximo 60 caracteres'),
  corrFch: z.string().min(1, 'La fecha es obligatoria'),
});

type CategoriaFormData = z.infer<typeof categoriaSchema>;
type SubcategoriaFormData = z.infer<typeof subcategoriaSchema>;
type ClasegFormData = z.infer<typeof clasegSchema>;
type FormaEnvioFormData = z.infer<typeof formaEnvioSchema>;
type TiptarFormData = z.infer<typeof tiptarSchema>;
type FordocFormData = z.infer<typeof fordocSchema>;
type CorforFormData = z.infer<typeof corforSchema>;

type ModalMode = 'crear' | 'editar' | null;

function fieldError(error?: string) {
  return error;
}

function parseOptionalNumber(value?: string | null) {
  if (value == null || value.trim() === '') return null;
  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

export function CatalogoCategoriaSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<CatalogoCategoriaDto | null>(null);
  const [deleting, setDeleting] = useState<CatalogoCategoriaDto | null>(null);

  const form = useForm<CategoriaFormData>({ resolver: zodResolver(categoriaSchema) as Resolver<CategoriaFormData> });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-catalogos', 'categorias'], queryFn: listCatalogoCategorias });

  const createMut = useMutation({
    mutationFn: createCatalogoCategoria,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'categorias'] }); setModal(null); toast.success('Categoría creada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la categoría.')),
  });
  const updateMut = useMutation({
    mutationFn: ({ catCod, body }: { catCod: number; body: { catDesc: string } }) => updateCatalogoCategoria(catCod, body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'categorias'] }); setModal(null); toast.success('Categoría actualizada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la categoría.')),
  });
  const deleteMut = useMutation({
    mutationFn: (catCod: number) => deleteCatalogoCategoria(catCod),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'categorias'] }); setDeleting(null); toast.success('Categoría eliminada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la categoría.')),
  });

  function openCreate() {
    form.reset({ catDesc: '' });
    setSelected(null); setModal('crear');
  }

  function openEdit(item: CatalogoCategoriaDto) {
    form.reset({ catDesc: item.catDesc });
    setSelected(item); setModal('editar');
  }

  function submit(data: CategoriaFormData) {
    if (modal === 'crear') createMut.mutate({ catDesc: data.catDesc });
    if (modal === 'editar' && selected) updateMut.mutate({ catCod: selected.catCod, body: { catDesc: data.catDesc } });
  }

  return (
    <div className="space-y-4">
      <LegacyCrudSection
        title="Categorías"
        description="Catálogo principal SECATALO."
        items={data ?? []}
        columns={[
          { header: 'Código', render: (item) => item.catCod },
          { header: 'Descripción', render: (item) => item.catDesc },
          { header: 'Subcategorías', render: (item) => item.totalSubcategorias },
        ]}
        getRowKey={(item) => String(item.catCod)}
        isLoading={isLoading}
        isError={isError}
        errorMessage="No se pudieron cargar las categorías."
        emptyMessage="No hay categorías cargadas."
        canEdit={canEdit}
        onCreate={openCreate}
        onEdit={openEdit}
        onDelete={(item) => setDeleting(item)}
        actionLabel="Crear categoría"
      />

      {modal && (
        <ModalDialog
          open
          title={modal === 'crear' ? 'Crear categoría' : 'Editar categoría'}
          onClose={() => setModal(null)}
          footer={(
            <>
              <Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button>
              <Button type="submit" form="categoria-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button>
            </>
          )}
        >
          <form id="categoria-form" onSubmit={form.handleSubmit(submit)} className="space-y-3">
            {modal === 'editar' && selected && (
              <FormField label="Código">
                <span className="flex w-full items-center rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">{selected.catCod}</span>
              </FormField>
            )}
            <FormField label="Descripción" error={fieldError(form.formState.errors.catDesc?.message)}>
              <input {...form.register('catDesc')} maxLength={60} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
          </form>
        </ModalDialog>
      )}

      <ModalDialog
        open={deleting !== null}
        title="Eliminar categoría"
        onClose={() => setDeleting(null)}
        footer={(
          <>
            <Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button>
            <Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate(deleting.catCod)}>Eliminar categoría</Button>
          </>
        )}
      >
        <p className="text-sm text-gray-600">Está por eliminarse la categoría <strong>{deleting?.catDesc}</strong>.</p>
      </ModalDialog>
    </div>
  );
}

export function SubcategoriaSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<CatalogoSubcategoriaDto | null>(null);
  const [deleting, setDeleting] = useState<CatalogoSubcategoriaDto | null>(null);
  const [catFilter, setCatFilter] = useState<string>('');

  const form = useForm<SubcategoriaFormData>({ resolver: zodResolver(subcategoriaSchema) as Resolver<SubcategoriaFormData> });
  const { data: categorias } = useQuery({ queryKey: ['admin-catalogos', 'categorias'], queryFn: listCatalogoCategorias });
  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-catalogos', 'subcategorias', catFilter || 'all'],
    queryFn: () => listCatalogoSubcategorias(catFilter ? Number(catFilter) : undefined),
  });

  const createMut = useMutation({
    mutationFn: createCatalogoSubcategoria,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'subcategorias'] }); setModal(null); toast.success('Subcategoría creada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la subcategoría.')),
  });
  const updateMut = useMutation({
    mutationFn: ({ catCod, idSubcategoria, body }: { catCod: number; idSubcategoria: number; body: { subcatNombre: string; subcatDescripcion?: string | null } }) => updateCatalogoSubcategoria(catCod, idSubcategoria, body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'subcategorias'] }); setModal(null); toast.success('Subcategoría actualizada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la subcategoría.')),
  });
  const deleteMut = useMutation({
    mutationFn: ({ catCod, idSubcategoria }: { catCod: number; idSubcategoria: number }) => deleteCatalogoSubcategoria(catCod, idSubcategoria),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'subcategorias'] }); setDeleting(null); toast.success('Subcategoría eliminada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la subcategoría.')),
  });

  function openCreate() {
    const defaultCat = categorias?.[0]?.catCod ?? 0;
    form.reset({ catCod: defaultCat, subcatNombre: '', subcatDescripcion: '' });
    setSelected(null); setModal('crear');
  }

  function openEdit(item: CatalogoSubcategoriaDto) {
    form.reset({ catCod: item.catCod, subcatNombre: item.subcatNombre, subcatDescripcion: item.subcatDescripcion ?? '' });
    setSelected(item); setModal('editar');
  }

  function submit(data: SubcategoriaFormData) {
    const payload = { subcatNombre: data.subcatNombre, subcatDescripcion: emptyToNull(data.subcatDescripcion ?? '') };
    if (modal === 'crear') createMut.mutate({ catCod: data.catCod, ...payload });
    if (modal === 'editar' && selected) updateMut.mutate({ catCod: selected.catCod, idSubcategoria: selected.idSubcategoria, body: payload });
  }

  const visibleItems = (data ?? []).filter((item) => !catFilter || String(item.catCod) === catFilter);

  return (
    <div className="space-y-4">
      <LegacyCrudSection
        title="Subcategorías"
        description="Catálogo SESUBCATEGORIAS."
        items={visibleItems}
        columns={[
          { header: 'Cat.', render: (item) => item.catCod },
          { header: 'Categoría', render: (item) => item.categoriaDesc },
          { header: 'ID', render: (item) => item.idSubcategoria },
          { header: 'Nombre', render: (item) => item.subcatNombre },
          { header: 'Descripción', render: (item) => item.subcatDescripcion ?? '—' },
        ]}
        getRowKey={(item) => `${item.catCod}-${item.idSubcategoria}`}
        isLoading={isLoading}
        isError={isError}
        errorMessage="No se pudieron cargar las subcategorías."
        emptyMessage="No hay subcategorías cargadas."
        canEdit={canEdit}
        onCreate={openCreate}
        onEdit={openEdit}
        onDelete={(item) => setDeleting(item)}
        actionLabel="Crear subcategoría"
      />

      <div className="flex items-end gap-3">
        <FormField label="Filtrar por categoría">
          <select value={catFilter} onChange={(e) => setCatFilter(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
            <option value="">Todas</option>
            {categorias?.map((categoria) => (
              <option key={categoria.catCod} value={categoria.catCod}>{categoria.catCod} — {categoria.catDesc}</option>
            ))}
          </select>
        </FormField>
      </div>

      {modal && (
        <ModalDialog
          open
          title={modal === 'crear' ? 'Crear subcategoría' : 'Editar subcategoría'}
          onClose={() => setModal(null)}
          size="lg"
          footer={(
            <>
              <Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button>
              <Button type="submit" form="subcategoria-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button>
            </>
          )}
        >
          <form id="subcategoria-form" onSubmit={form.handleSubmit(submit)} className="grid gap-3 md:grid-cols-2">
            <FormField label="Categoría" error={fieldError(form.formState.errors.catCod?.message)}>
              <select {...form.register('catCod', { valueAsNumber: true })} disabled={modal === 'editar'} className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-100">
                {categorias?.map((categoria) => <option key={categoria.catCod} value={categoria.catCod}>{categoria.catCod} — {categoria.catDesc}</option>)}
              </select>
            </FormField>
            {modal === 'editar' && selected && (
              <FormField label="ID subcategoría">
                <span className="flex w-full items-center rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">{selected.idSubcategoria}</span>
              </FormField>
            )}
            <FormField label="Nombre" error={fieldError(form.formState.errors.subcatNombre?.message)} className="md:col-span-2">
              <input {...form.register('subcatNombre')} maxLength={200} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
            <FormField label="Descripción" error={fieldError(form.formState.errors.subcatDescripcion?.message)} className="md:col-span-2">
              <textarea {...form.register('subcatDescripcion')} maxLength={200} rows={3} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
          </form>
        </ModalDialog>
      )}

      <ModalDialog
        open={deleting !== null}
        title="Eliminar subcategoría"
        onClose={() => setDeleting(null)}
        footer={(
          <>
            <Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button>
            <Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate({ catCod: deleting.catCod, idSubcategoria: deleting.idSubcategoria })}>Eliminar subcategoría</Button>
          </>
        )}
      >
        <p className="text-sm text-gray-600">Está por eliminarse la subcategoría <strong>{deleting?.subcatNombre}</strong>.</p>
      </ModalDialog>
    </div>
  );
}

export function ClasegSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<SeClasegDto | null>(null);
  const [deleting, setDeleting] = useState<SeClasegDto | null>(null);
  const [search, setSearch] = useState('');
  const form = useForm<ClasegFormData>({ resolver: zodResolver(clasegSchema) as Resolver<ClasegFormData> });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-catalogos', 'clasificaciones'], queryFn: listSeClaseg });
  const filteredData = useMemo(
    () => (data ?? []).filter((item) => matchesSearch(search, [item.dfClasif, item.dfnClasif, item.dfdClasif])),
    [data, search],
  );

  const createMut = useMutation({ mutationFn: createSeClaseg, onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'clasificaciones'] }); setModal(null); toast.success('Clasificación creada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la clasificación.')) });
  const updateMut = useMutation({ mutationFn: ({ dfClasif, body }: { dfClasif: number; body: { dfnClasif: string; dfdClasif: string } }) => updateSeClaseg(dfClasif, body), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'clasificaciones'] }); setModal(null); toast.success('Clasificación actualizada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la clasificación.')) });
  const deleteMut = useMutation({ mutationFn: (dfClasif: number) => deleteSeClaseg(dfClasif), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'clasificaciones'] }); setDeleting(null); toast.success('Clasificación eliminada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la clasificación.')) });

  function openCreate() { form.reset({ dfnClasif: '', dfdClasif: '' }); setSelected(null); setModal('crear'); }
  function openEdit(item: SeClasegDto) { form.reset({ dfnClasif: item.dfnClasif, dfdClasif: item.dfdClasif }); setSelected(item); setModal('editar'); }
  function submit(data: ClasegFormData) { const body = { dfnClasif: data.dfnClasif, dfdClasif: data.dfdClasif }; if (modal === 'crear') createMut.mutate(body); if (modal === 'editar' && selected) updateMut.mutate({ dfClasif: selected.dfClasif, body }); }

  return (
    <div className="space-y-4">
      <LegacyCrudSection
        title="Clasificaciones"
        description="Catálogo SECLASEG."
        items={filteredData}
        columns={[
          { header: 'Código', render: (item) => item.dfClasif },
          { header: 'Abreviatura', render: (item) => item.dfnClasif },
          { header: 'Descripción', render: (item) => item.dfdClasif },
        ]}
        getRowKey={(item) => String(item.dfClasif)}
        isLoading={isLoading}
        isError={isError}
        errorMessage="No se pudieron cargar las clasificaciones."
        emptyMessage="No hay clasificaciones cargadas."
        canEdit={canEdit}
        onCreate={openCreate}
        onEdit={openEdit}
        onDelete={(item) => setDeleting(item)}
        actionLabel="Crear clasificación"
        searchValue={search}
        searchPlaceholder="Buscar clasificaciones..."
        onSearchChange={setSearch}
      />
      {modal && (
        <ModalDialog open title={modal === 'crear' ? 'Crear clasificación' : 'Editar clasificación'} onClose={() => setModal(null)} footer={<><Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button><Button type="submit" form="claseg-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button></>}>
          <form id="claseg-form" onSubmit={form.handleSubmit(submit)} className="grid gap-3 md:grid-cols-2">
            {modal === 'editar' && selected && (
              <FormField label="Código">
                <span className="flex w-full items-center rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">{selected.dfClasif}</span>
              </FormField>
            )}
            <FormField label="Descripción" error={fieldError(form.formState.errors.dfdClasif?.message)} className="md:col-span-2">
              <input {...form.register('dfdClasif')} maxLength={15} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
            <FormField label="Abreviatura" error={fieldError(form.formState.errors.dfnClasif?.message)}>
              <input {...form.register('dfnClasif')} maxLength={2} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
          </form>
        </ModalDialog>
      )}
      <ModalDialog open={deleting !== null} title="Eliminar clasificación" onClose={() => setDeleting(null)} footer={<><Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button><Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate(deleting.dfClasif)}>Eliminar clasificación</Button></>}>
        <p className="text-sm text-gray-600">Está por eliminarse la clasificación <strong>{deleting?.dfnClasif}</strong>.</p>
      </ModalDialog>
    </div>
  );
}

export function FormaEnvioSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<SeFormaEnvioDto | null>(null);
  const [deleting, setDeleting] = useState<SeFormaEnvioDto | null>(null);
  const [search, setSearch] = useState('');
  const form = useForm<FormaEnvioFormData>({ resolver: zodResolver(formaEnvioSchema) as Resolver<FormaEnvioFormData> });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-catalogos', 'formas-envio'], queryFn: listSeFormaEnvio });
  const filteredData = useMemo(
    () => (data ?? []).filter((item) => matchesSearch(search, [item.idFormaEnvio, item.formaEnvio])),
    [data, search],
  );
  const createMut = useMutation({ mutationFn: createSeFormaEnvio, onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'formas-envio'] }); setModal(null); toast.success('Forma de envío creada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la forma de envío.')) });
  const updateMut = useMutation({ mutationFn: ({ idFormaEnvio, body }: { idFormaEnvio: number; body: { formaEnvio: string } }) => updateSeFormaEnvio(idFormaEnvio, body), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'formas-envio'] }); setModal(null); toast.success('Forma de envío actualizada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la forma de envío.')) });
  const deleteMut = useMutation({ mutationFn: (idFormaEnvio: number) => deleteSeFormaEnvio(idFormaEnvio), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'formas-envio'] }); setDeleting(null); toast.success('Forma de envío eliminada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la forma de envío.')) });
  function openCreate() { form.reset({ formaEnvio: '' }); setSelected(null); setModal('crear'); }
  function openEdit(item: SeFormaEnvioDto) { form.reset({ formaEnvio: item.formaEnvio }); setSelected(item); setModal('editar'); }
  function submit(data: FormaEnvioFormData) { if (modal === 'crear') createMut.mutate({ formaEnvio: data.formaEnvio }); if (modal === 'editar' && selected) updateMut.mutate({ idFormaEnvio: selected.idFormaEnvio, body: { formaEnvio: data.formaEnvio } }); }
  return (
    <div className="space-y-4">
      <LegacyCrudSection title="Formas de envío" description="Catálogo SeFormaEnvio." items={filteredData} columns={[{ header: 'Código', render: (item) => item.idFormaEnvio }, { header: 'Descripción', render: (item) => item.formaEnvio }]} getRowKey={(item) => String(item.idFormaEnvio)} isLoading={isLoading} isError={isError} errorMessage="No se pudieron cargar las formas de envío." emptyMessage="No hay formas de envío cargadas." canEdit={canEdit} onCreate={openCreate} onEdit={openEdit} onDelete={(item) => setDeleting(item)} actionLabel="Crear forma de envío" searchValue={search} searchPlaceholder="Buscar formas de envío..." onSearchChange={setSearch} />
      {modal && (<ModalDialog open title={modal === 'crear' ? 'Crear forma de envío' : 'Editar forma de envío'} onClose={() => setModal(null)} footer={<><Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button><Button type="submit" form="formaenvio-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button></>}> <form id="formaenvio-form" onSubmit={form.handleSubmit(submit)} className="space-y-3"> {modal === 'editar' && selected && (<FormField label="Código"><span className="flex w-full items-center rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">{selected.idFormaEnvio}</span></FormField>)} <FormField label="Descripción" error={fieldError(form.formState.errors.formaEnvio?.message)}><input {...form.register('formaEnvio')} maxLength={50} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField> </form></ModalDialog>)}
      <ModalDialog open={deleting !== null} title="Eliminar forma de envío" onClose={() => setDeleting(null)} footer={<><Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button><Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate(deleting.idFormaEnvio)}>Eliminar forma de envío</Button></>}><p className="text-sm text-gray-600">Está por eliminarse la forma de envío <strong>{deleting?.formaEnvio}</strong>.</p></ModalDialog>
    </div>
  );
}

export function TiptarSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<SeTiptarDto | null>(null);
  const [deleting, setDeleting] = useState<SeTiptarDto | null>(null);
  const [search, setSearch] = useState('');
  const form = useForm<TiptarFormData>({ resolver: zodResolver(tiptarSchema) });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-catalogos', 'acciones-tarea'], queryFn: listSeTiptar });
  const filteredData = useMemo(
    () => (data ?? []).filter((item) => matchesSearch(search, [item.dftaccion, item.dftacobsv, item.dftacdesc])),
    [data, search],
  );
  const createMut = useMutation({ mutationFn: createSeTiptar, onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'acciones-tarea'] }); setModal(null); toast.success('Acción de tarea creada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la acción de tarea.')) });
  const updateMut = useMutation({ mutationFn: ({ dftaccion, body }: { dftaccion: string; body: { dftacobsv?: string | null; dftacdesc?: string | null } }) => updateSeTiptar(dftaccion, body), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'acciones-tarea'] }); setModal(null); toast.success('Acción de tarea actualizada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la acción de tarea.')) });
  const deleteMut = useMutation({ mutationFn: (dftaccion: string) => deleteSeTiptar(dftaccion), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'acciones-tarea'] }); setDeleting(null); toast.success('Acción de tarea eliminada correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la acción de tarea.')) });
  function openCreate() { form.reset({ dftaccion: '', dftacobsv: '', dftacdesc: '' }); setSelected(null); setModal('crear'); }
  function openEdit(item: SeTiptarDto) { form.reset({ dftaccion: item.dftaccion, dftacobsv: item.dftacobsv ?? '', dftacdesc: item.dftacdesc ?? '' }); setSelected(item); setModal('editar'); }
  function submit(data: TiptarFormData) { const body = { dftacobsv: emptyToNull(data.dftacobsv ?? ''), dftacdesc: emptyToNull(data.dftacdesc ?? '') }; if (modal === 'crear') createMut.mutate({ dftaccion: data.dftaccion, ...body }); if (modal === 'editar' && selected) updateMut.mutate({ dftaccion: selected.dftaccion, body }); }
  return (<div className="space-y-4"><LegacyCrudSection title="Acciones de tarea" description="Catálogo SETIPTAR." items={filteredData} columns={[{ header: 'Tarea', render: (item) => item.dftaccion }, { header: 'Observación', render: (item) => item.dftacobsv ?? '—' }, { header: 'Descripción', render: (item) => item.dftacdesc ?? '—' }]} getRowKey={(item) => item.dftaccion} isLoading={isLoading} isError={isError} errorMessage="No se pudieron cargar las acciones de tarea." emptyMessage="No hay acciones de tarea cargadas." canEdit={canEdit} onCreate={openCreate} onEdit={openEdit} onDelete={(item) => setDeleting(item)} actionLabel="Crear acción" searchValue={search} searchPlaceholder="Buscar acciones..." onSearchChange={setSearch} />{modal && <ModalDialog open title={modal === 'crear' ? 'Crear acción de tarea' : 'Editar acción de tarea'} onClose={() => setModal(null)} footer={<><Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button><Button type="submit" form="tiptar-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button></>}><form id="tiptar-form" onSubmit={form.handleSubmit(submit)} className="space-y-3"><FormField label="Tarea" error={fieldError(form.formState.errors.dftaccion?.message)}><input {...form.register('dftaccion')} disabled={modal === 'editar'} maxLength={30} className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-100" /></FormField><FormField label="Descripción" error={fieldError(form.formState.errors.dftacdesc?.message)}><textarea {...form.register('dftacdesc')} maxLength={60} rows={3} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField><FormField label="Observación" error={fieldError(form.formState.errors.dftacobsv?.message)}><textarea {...form.register('dftacobsv')} rows={3} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField></form></ModalDialog>}<ModalDialog open={deleting !== null} title="Eliminar acción de tarea" onClose={() => setDeleting(null)} footer={<><Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button><Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate(deleting.dftaccion)}>Eliminar acción</Button></>}><p className="text-sm text-gray-600">Vas a eliminar la acción <strong>{deleting?.dftaccion}</strong>.</p></ModalDialog></div>);
}

export function FordocSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<SeFordocDto | null>(null);
  const [deleting, setDeleting] = useState<SeFordocDto | null>(null);
  const [search, setSearch] = useState('');
  const form = useForm<FordocFormData>({ resolver: zodResolver(fordocSchema) as Resolver<FordocFormData> });
  const vistaI = useWatch({ control: form.control, name: 'seFordocVistaI' });
  const vistaE = useWatch({ control: form.control, name: 'seFordocVistaE' });
  const vistaR = useWatch({ control: form.control, name: 'seFordocVistaR' });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-catalogos', 'formatos-documento'], queryFn: listSeFordoc });
  const filteredData = useMemo(
    () => (data ?? []).filter((item) => matchesSearch(search, [item.tipoCod, item.tipoDesc, item.tipoEnv, item.seFordocFormatoNum, toDateInput(item.corrFecha)])),
    [data, search],
  );
  const createMut = useMutation({ mutationFn: createSeFordoc, onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'formatos-documento'] }); setModal(null); toast.success('Formato de documento creado correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear el formato de documento.')) });
  const updateMut = useMutation({ mutationFn: ({ tipoCod, body }: { tipoCod: number; body: ActualizarSeFordocData }) => updateSeFordoc(tipoCod, body), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'formatos-documento'] }); setModal(null); toast.success('Formato de documento actualizado correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar el formato de documento.')) });
  const deleteMut = useMutation({ mutationFn: (tipoCod: number) => deleteSeFordoc(tipoCod), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'formatos-documento'] }); setDeleting(null); toast.success('Formato de documento eliminado correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar el formato de documento.')) });
  function openCreate() {
    form.reset({ tipoRec: 0, tipoInt: 0, tipoDesc: '', corrN: 0, tipoEnv: '', seFordocVistaI: 0, seFordocVistaE: 0, seFordocVistaR: 0, seFordocFormatoNum: '' });
    setSelected(null);
    setModal('crear');
  }
  function openEdit(item: SeFordocDto) {
    form.reset({
      tipoRec: item.tipoRec,
      tipoInt: item.tipoInt,
      tipoDesc: item.tipoDesc,
      corrN: item.corrN,
      tipoEnv: item.tipoEnv == null ? '' : String(item.tipoEnv),
      seFordocVistaI: item.seFordocVistaI,
      seFordocVistaE: item.seFordocVistaE,
      seFordocVistaR: item.seFordocVistaR,
      seFordocFormatoNum: item.seFordocFormatoNum ?? '',
    });
    setSelected(item);
    setModal('editar');
  }
  function submit(data: FordocFormData) {
    const body: ActualizarSeFordocData = {
      tipoRec: data.tipoRec,
      tipoInt: data.tipoInt,
      tipoDesc: data.tipoDesc,
      corrN: data.corrN,
      tipoEnv: parseOptionalNumber(data.tipoEnv),
      seFordocVistaI: data.seFordocVistaI ?? 0,
      seFordocVistaE: data.seFordocVistaE ?? 0,
      seFordocVistaR: data.seFordocVistaR ?? 0,
      seFordocFormatoNum: emptyToNull(data.seFordocFormatoNum ?? ''),
    };
    if (modal === 'crear') createMut.mutate(body);
    if (modal === 'editar' && selected) updateMut.mutate({ tipoCod: selected.tipoCod, body });
  }

  return (
    <div className="space-y-4">
      <LegacyCrudSection
        title="Formatos de documento"
        description="Catálogo SEFORDOC."
        items={filteredData}
        columns={[
          { header: 'Código', render: (item) => item.tipoCod },
          { header: 'Descripción', render: (item) => item.tipoDesc },
          { header: 'Correlativo', render: (item) => item.corrN },
          { header: 'Fecha', render: (item) => toDateInput(item.corrFecha) },
          { header: 'Formato doc. interno', render: (item) => (item.seFordocVistaI === 1 ? 'Sí' : 'No') },
          { header: 'Formato doc. enviado', render: (item) => (item.seFordocVistaE === 1 ? 'Sí' : 'No') },
          { header: 'Formato doc. recibido', render: (item) => (item.seFordocVistaR === 1 ? 'Sí' : 'No') },
        ]}
        getRowKey={(item) => String(item.tipoCod)}
        isLoading={isLoading}
        isError={isError}
        errorMessage="No se pudieron cargar los formatos de documento."
        emptyMessage="No hay formatos de documento cargados."
        canEdit={canEdit}
        onCreate={openCreate}
        onEdit={openEdit}
        onDelete={(item) => setDeleting(item)}
        actionLabel="Crear formato"
        searchValue={search}
        searchPlaceholder="Buscar formatos..."
        onSearchChange={setSearch}
      />

      {modal && (
        <ModalDialog
          open
          title={modal === 'crear' ? 'Crear formato de documento' : 'Editar formato de documento'}
          onClose={() => setModal(null)}
          size="lg"
          footer={(
            <>
              <Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button>
              <Button type="submit" form="fordoc-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button>
            </>
          )}
        >
          <form id="fordoc-form" onSubmit={form.handleSubmit(submit)} className="grid gap-3 md:grid-cols-2">
            {modal === 'editar' && selected && (
              <FormField label="Código">
                <span className="flex w-full items-center rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">{selected.tipoCod}</span>
              </FormField>
            )}
            <FormField label="Descripción" error={fieldError(form.formState.errors.tipoDesc?.message)} className="md:col-span-2">
              <input {...form.register('tipoDesc')} maxLength={100} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
            <FormField label="Correlativo" error={fieldError(form.formState.errors.corrN?.message)}>
              <input {...form.register('corrN', { valueAsNumber: true })} type="number" min={0} max={2147483647} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
            <FormField label="Fecha">
              <span
                data-testid="corrFecha-display"
                className="flex w-full items-center rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600"
              >
                {selected ? toDateInput(selected.corrFecha) : '—'}
              </span>
            </FormField>
            <FormField label="Formato doc. interno" error={fieldError(form.formState.errors.seFordocVistaI?.message)}>
              <label className="flex items-center gap-3 rounded border border-gray-300 px-3 py-2 text-sm">
                <input
                  type="checkbox"
                  checked={vistaI === 1}
                  onChange={(e) => form.setValue('seFordocVistaI', e.target.checked ? 1 : 0, { shouldDirty: true, shouldValidate: true })}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span>Formato doc. interno</span>
              </label>
            </FormField>
            <FormField label="Formato doc. enviado" error={fieldError(form.formState.errors.seFordocVistaE?.message)}>
              <label className="flex items-center gap-3 rounded border border-gray-300 px-3 py-2 text-sm">
                <input
                  type="checkbox"
                  checked={vistaE === 1}
                  onChange={(e) => form.setValue('seFordocVistaE', e.target.checked ? 1 : 0, { shouldDirty: true, shouldValidate: true })}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span>Formato doc. enviado</span>
              </label>
            </FormField>
            <FormField label="Formato doc. recibido" error={fieldError(form.formState.errors.seFordocVistaR?.message)}>
              <label className="flex items-center gap-3 rounded border border-gray-300 px-3 py-2 text-sm">
                <input
                  type="checkbox"
                  checked={vistaR === 1}
                  onChange={(e) => form.setValue('seFordocVistaR', e.target.checked ? 1 : 0, { shouldDirty: true, shouldValidate: true })}
                  className="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span>Formato doc. recibido</span>
              </label>
            </FormField>
          </form>
        </ModalDialog>
      )}

      <ModalDialog
        open={deleting !== null}
        title="Eliminar formato de documento"
        onClose={() => setDeleting(null)}
        footer={(
          <>
            <Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button>
            <Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate(deleting.tipoCod)}>Eliminar formato</Button>
          </>
        )}
      >
        <p className="text-sm text-gray-600">Está por eliminarse el formato <strong>{deleting?.tipoDesc}</strong>.</p>
      </ModalDialog>
    </div>
  );
}

export function ForplaSection({ canEdit }: { canEdit: boolean }) {
  void canEdit;
  return <AdminPlantillasSection />;
}

export function CorforSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<SeCorforDto | null>(null);
  const [deleting, setDeleting] = useState<SeCorforDto | null>(null);
  const [search, setSearch] = useState('');
  const form = useForm<CorforFormData>({ resolver: zodResolver(corforSchema) as Resolver<CorforFormData> });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-catalogos', 'correlativos'], queryFn: listSeCorfor });
  const filteredData = useMemo(
    () => (data ?? []).filter((item) => matchesSearch(search, [item.corrTip, item.corrNro, item.corrDes, toDateInput(item.corrFch)])),
    [data, search],
  );
  const createMut = useMutation({ mutationFn: createSeCorfor, onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'correlativos'] }); setModal(null); toast.success('Correlativo creado correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear el correlativo.')) });
  const updateMut = useMutation({ mutationFn: ({ corrTip, body }: { corrTip: string; body: ActualizarSeCorforData }) => updateSeCorfor(corrTip, body), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'correlativos'] }); setModal(null); toast.success('Correlativo actualizado correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar el correlativo.')) });
  const deleteMut = useMutation({ mutationFn: (corrTip: string) => deleteSeCorfor(corrTip), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'correlativos'] }); setDeleting(null); toast.success('Correlativo eliminado correctamente.'); }, onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar el correlativo.')) });
  function openCreate() { form.reset({ corrTip: 'DOCINTER', corrNro: 0, corrDes: '', corrFch: '' }); setSelected(null); setModal('crear'); }
  function openEdit(item: SeCorforDto) { form.reset({ corrTip: item.corrTip as CorforFormData['corrTip'], corrNro: item.corrNro, corrDes: item.corrDes, corrFch: toDateInput(item.corrFch) }); setSelected(item); setModal('editar'); }
  function submit(data: CorforFormData) { const body: ActualizarSeCorforData = { corrNro: data.corrNro, corrDes: data.corrDes, corrFch: new Date(data.corrFch).toISOString() }; if (modal === 'crear') createMut.mutate({ corrTip: data.corrTip, ...body }); if (modal === 'editar' && selected) updateMut.mutate({ corrTip: selected.corrTip, body }); }
  return (<div className="space-y-4"><LegacyCrudSection title="Correlativos" description="Catálogo SECORFOR." items={filteredData} columns={[{ header: 'Tipo', render: (item) => corrTipoLabel(item.corrTip) }, { header: 'Correlativo', render: (item) => item.corrNro }, { header: 'Descripción', render: (item) => item.corrDes }, { header: 'Fecha', render: (item) => toDateInput(item.corrFch) }]} getRowKey={(item) => item.corrTip} isLoading={isLoading} isError={isError} errorMessage="No se pudieron cargar los correlativos." emptyMessage="No hay correlativos cargados." canEdit={canEdit} onCreate={openCreate} onEdit={openEdit} onDelete={(item) => setDeleting(item)} actionLabel="Crear correlativo" searchValue={search} searchPlaceholder="Buscar correlativos..." onSearchChange={setSearch} />{modal && <ModalDialog open title={modal === 'crear' ? 'Crear correlativo' : 'Editar correlativo'} onClose={() => setModal(null)} footer={<><Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button><Button type="submit" form="corfor-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button></>}><form id="corfor-form" onSubmit={form.handleSubmit(submit)} className="grid gap-3 md:grid-cols-2"><FormField label="Tipo" error={fieldError(form.formState.errors.corrTip?.message)}>{modal === 'editar' && selected ? (<span className="flex w-full items-center rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">{corrTipoLabel(selected.corrTip)}</span>) : (<select {...form.register('corrTip')} className="w-full rounded border border-gray-300 px-3 py-2 text-sm">{CORR_TIPO_OPTIONS.map((option) => (<option key={option.value} value={option.value}>{option.label}</option>))}</select>)}</FormField><FormField label="Correlativo" error={fieldError(form.formState.errors.corrNro?.message)}><input {...form.register('corrNro', { valueAsNumber: true })} type="number" min={0} max={2147483647} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField><FormField label="Descripción" error={fieldError(form.formState.errors.corrDes?.message)} className="md:col-span-2"><input {...form.register('corrDes')} maxLength={60} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField><FormField label="Fecha" error={fieldError(form.formState.errors.corrFch?.message)} className="md:col-span-2"><input type="date" {...form.register('corrFch')} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField></form></ModalDialog>}<ModalDialog open={deleting !== null} title="Eliminar correlativo" onClose={() => setDeleting(null)} footer={<><Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button><Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate(deleting.corrTip)}>Eliminar correlativo</Button></>}><p className="text-sm text-gray-600">Vas a eliminar el correlativo <strong>{deleting?.corrTip}</strong>.</p></ModalDialog></div>);
}

export default function AdminCatalogosPage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_CATALOGOS_EDITAR);

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Catálogos</h2>
        <p className="mt-1 text-sm text-gray-500">Administración de catálogos SECATALO, SESUBCATEGORIAS y relacionados.</p>
      </div>

      <CatalogoCategoriaSection canEdit={canEdit} />
      <SubcategoriaSection canEdit={canEdit} />
      <ClasegSection canEdit={canEdit} />
      <FormaEnvioSection canEdit={canEdit} />
      <TiptarSection canEdit={canEdit} />
      <FordocSection canEdit={canEdit} />
      <AdminPlantillasSection />
      <CorforSection canEdit={canEdit} />
    </div>
  );
}



