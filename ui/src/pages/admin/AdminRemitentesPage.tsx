import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useHasPermission } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import Button from '../../components/atoms/Button';
import FormField from '../../components/molecules/FormField';
import ModalDialog from '../../components/organisms/ModalDialog';
import LegacyCrudSection from './LegacyCrudSection';
import {
  createSerem,
  createSeremTipo,
  deleteSerem,
  deleteSeremTipo,
  listSerems,
  listSeremTipos,
  updateSerem,
  updateSeremTipo,
  type ActualizarSeremData,
  type SeremDto,
  type SeremTipoDto,
} from '../../lib/api/admin/adminRemitentesApi';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
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

function parseOptionalNumber(value?: string | null) {
  if (value == null || value.trim() === '') return null;
  const parsed = Number(value);
  return Number.isNaN(parsed) ? null : parsed;
}

const serTipoSchema = z.object({
  remTipo: z.string().min(1, 'El tipo es obligatorio').max(3, 'Máximo 3 caracteres'),
  remDesc: z.string().min(1, 'La descripción es obligatoria').max(30, 'Máximo 30 caracteres'),
});

const seremSchema = z.object({
  remCod: z.string().min(1, 'El código es obligatorio').max(20, 'Máximo 20 caracteres'),
  remTipo: z.string().min(1, 'El tipo es obligatorio'),
  remNomb: z.string().min(1, 'El nombre es obligatorio').max(60, 'Máximo 60 caracteres'),
  remRutValid: z.string().optional(),
  remSector: z.string().max(20, 'Máximo 20 caracteres').optional(),
  remComuna: z.string().max(18, 'Máximo 18 caracteres').optional(),
  remNro: z.string().optional(),
  remEmail: z.string().email('Email inválido').max(30, 'Máximo 30 caracteres').optional().or(z.literal('')),
  remFax: z.string().max(10, 'Máximo 10 caracteres').optional(),
  remRut: z.string().max(12, 'Máximo 12 caracteres').optional(),
  remDirec: z.string().max(60, 'Máximo 60 caracteres').optional(),
  remTelef: z.string().max(10, 'Máximo 10 caracteres').optional(),
  remZip: z.string().max(40, 'Máximo 40 caracteres').optional(),
  remRegion: z.string().max(40, 'Máximo 40 caracteres').optional(),
  remBlock: z.string().max(60, 'Máximo 60 caracteres').optional(),
  remCalle: z.string().max(60, 'Máximo 60 caracteres').optional(),
  remCodDocDigital: z.string().optional(),
});

type SerTipoFormData = z.infer<typeof serTipoSchema>;
type SeremFormData = z.infer<typeof seremSchema>;
type ModalMode = 'crear' | 'editar' | null;

export function SeremTipoSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<SeremTipoDto | null>(null);
  const [deleting, setDeleting] = useState<SeremTipoDto | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const form = useForm<SerTipoFormData>({ resolver: zodResolver(serTipoSchema) });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-remitentes', 'tipos'], queryFn: listSeremTipos });
  const filteredData = useMemo(() => (data ?? []).filter((item) => matchesSearch(search, [item.remTipo, item.remDesc])), [data, search]);

  const createMut = useMutation({ mutationFn: createSeremTipo, onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-remitentes', 'tipos'] }); setModal(null); setStatus('Tipo de remitente creado correctamente.'); setError(null); }, onError: (err) => setError(getErrorMessage(err, 'No se pudo crear el tipo de remitente.')) });
  const updateMut = useMutation({ mutationFn: ({ remTipo, body }: { remTipo: string; body: { remDesc: string } }) => updateSeremTipo(remTipo, body), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-remitentes', 'tipos'] }); setModal(null); setStatus('Tipo de remitente actualizado correctamente.'); setError(null); }, onError: (err) => setError(getErrorMessage(err, 'No se pudo actualizar el tipo de remitente.')) });
  const deleteMut = useMutation({ mutationFn: (remTipo: string) => deleteSeremTipo(remTipo), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-remitentes', 'tipos'] }); setDeleting(null); setStatus('Tipo de remitente eliminado correctamente.'); setError(null); }, onError: (err) => setError(getErrorMessage(err, 'No se pudo eliminar el tipo de remitente.')) });

  function openCreate() { form.reset({ remTipo: '', remDesc: '' }); setSelected(null); setError(null); setStatus(null); setModal('crear'); }
  function openEdit(item: SeremTipoDto) { form.reset({ remTipo: item.remTipo, remDesc: item.remDesc }); setSelected(item); setError(null); setStatus(null); setModal('editar'); }
  function submit(data: SerTipoFormData) { if (modal === 'crear') createMut.mutate({ remTipo: data.remTipo, remDesc: data.remDesc }); if (modal === 'editar' && selected) updateMut.mutate({ remTipo: selected.remTipo, body: { remDesc: data.remDesc } }); }

  return (
    <div className="space-y-4">
      {status && <div className="rounded border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-700">{status}</div>}
      <LegacyCrudSection
        title="Tipos de remitente"
        description="Catálogo SEREMTIP."
        items={filteredData}
        columns={[
          { header: 'Tipo', render: (item) => item.remTipo },
          { header: 'Descripción', render: (item) => item.remDesc },
          { header: 'Remitentes', render: (item) => item.totalSerems },
        ]}
        getRowKey={(item) => item.remTipo}
        isLoading={isLoading}
        isError={isError}
        errorMessage="No se pudieron cargar los tipos de remitente."
        emptyMessage="No hay tipos de remitente cargados."
        canEdit={canEdit}
        onCreate={openCreate}
        onEdit={openEdit}
        onDelete={(item) => setDeleting(item)}
        actionLabel="Crear tipo"
        searchValue={search}
        searchPlaceholder="Buscar tipos..."
        onSearchChange={setSearch}
      />

      {modal && (
        <ModalDialog
          open
          title={modal === 'crear' ? 'Crear tipo de remitente' : 'Editar tipo de remitente'}
          onClose={() => setModal(null)}
          footer={<><Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button><Button type="submit" form="seremtipo-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button></>}
        >
          <form id="seremtipo-form" onSubmit={form.handleSubmit(submit)} className="space-y-3">
            {error && <div className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}
            <FormField label="Tipo" error={form.formState.errors.remTipo?.message}><input {...form.register('remTipo')} disabled={modal === 'editar'} maxLength={3} className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-100" /></FormField>
            <FormField label="Descripción" error={form.formState.errors.remDesc?.message}><input {...form.register('remDesc')} maxLength={30} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
          </form>
        </ModalDialog>
      )}

      <ModalDialog open={deleting !== null} title="Eliminar tipo de remitente" onClose={() => setDeleting(null)} footer={<><Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button><Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate(deleting.remTipo)}>Eliminar tipo</Button></>}>
        <p className="text-sm text-gray-600">Está por eliminarse el tipo <strong>{deleting?.remTipo}</strong>.</p>
      </ModalDialog>
    </div>
  );
}

export function SeremSection({ canEdit }: { canEdit: boolean }) {
  const qc = useQueryClient();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<SeremDto | null>(null);
  const [deleting, setDeleting] = useState<SeremDto | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [tipoFilter, setTipoFilter] = useState<string>('');
  const [search, setSearch] = useState('');

  const form = useForm<SeremFormData>({ resolver: zodResolver(seremSchema) });
  const { data: tipos } = useQuery({ queryKey: ['admin-remitentes', 'tipos'], queryFn: listSeremTipos });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-remitentes', 'serems', tipoFilter || 'all'], queryFn: () => listSerems(tipoFilter || undefined) });
  const filteredData = useMemo(
    () => (data ?? []).filter((item) => matchesSearch(search, [item.remCod, item.remTipoDesc, item.remNomb, item.remComuna, item.remEmail, item.remRut])),
    [data, search],
  );

  const createMut = useMutation({ mutationFn: createSerem, onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-remitentes', 'serems'] }); setModal(null); setStatus('Remitente creado correctamente.'); setError(null); }, onError: (err) => setError(getErrorMessage(err, 'No se pudo crear el remitente.')) });
  const updateMut = useMutation({ mutationFn: ({ remCod, body }: { remCod: string; body: ActualizarSeremData }) => updateSerem(remCod, body), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-remitentes', 'serems'] }); setModal(null); setStatus('Remitente actualizado correctamente.'); setError(null); }, onError: (err) => setError(getErrorMessage(err, 'No se pudo actualizar el remitente.')) });
  const deleteMut = useMutation({ mutationFn: (remCod: string) => deleteSerem(remCod), onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-remitentes', 'serems'] }); setDeleting(null); setStatus('Remitente eliminado correctamente.'); setError(null); }, onError: (err) => setError(getErrorMessage(err, 'No se pudo eliminar el remitente.')) });

  function openCreate() {
    form.reset({ remCod: '', remTipo: tipos?.[0]?.remTipo ?? '', remNomb: '', remRutValid: '', remSector: '', remComuna: '', remNro: '', remEmail: '', remFax: '', remRut: '', remDirec: '', remTelef: '', remZip: '', remRegion: '', remBlock: '', remCalle: '', remCodDocDigital: '' });
    setSelected(null); setError(null); setStatus(null); setModal('crear');
  }

  function openEdit(item: SeremDto) {
    form.reset({
      remCod: item.remCod,
      remTipo: item.remTipo,
      remNomb: item.remNomb,
      remRutValid: item.remRutValid == null ? '' : String(item.remRutValid),
      remSector: item.remSector ?? '',
      remComuna: item.remComuna ?? '',
      remNro: item.remNro == null ? '' : String(item.remNro),
      remEmail: item.remEmail ?? '',
      remFax: item.remFax ?? '',
      remRut: item.remRut ?? '',
      remDirec: item.remDirec ?? '',
      remTelef: item.remTelef ?? '',
      remZip: item.remZip ?? '',
      remRegion: item.remRegion ?? '',
      remBlock: item.remBlock ?? '',
      remCalle: item.remCalle ?? '',
      remCodDocDigital: item.remCodDocDigital == null ? '' : String(item.remCodDocDigital),
    });
    setSelected(item); setError(null); setStatus(null); setModal('editar');
  }

  function submit(data: SeremFormData) {
    const body = {
      remTipo: data.remTipo,
      remNomb: data.remNomb,
      remRutValid: parseOptionalNumber(data.remRutValid),
      remSector: emptyToNull(data.remSector ?? ''),
      remComuna: emptyToNull(data.remComuna ?? ''),
      remNro: parseOptionalNumber(data.remNro),
      remEmail: emptyToNull(data.remEmail ?? ''),
      remFax: emptyToNull(data.remFax ?? ''),
      remRut: emptyToNull(data.remRut ?? ''),
      remDirec: emptyToNull(data.remDirec ?? ''),
      remTelef: emptyToNull(data.remTelef ?? ''),
      remZip: emptyToNull(data.remZip ?? ''),
      remRegion: emptyToNull(data.remRegion ?? ''),
      remBlock: emptyToNull(data.remBlock ?? ''),
      remCalle: emptyToNull(data.remCalle ?? ''),
      remCodDocDigital: parseOptionalNumber(data.remCodDocDigital),
    };
    if (modal === 'crear') createMut.mutate({ remCod: data.remCod, ...body });
    if (modal === 'editar' && selected) updateMut.mutate({ remCod: selected.remCod, body });
  }

  return (
    <div className="space-y-4">
      {status && <div className="rounded border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-700">{status}</div>}
      <div className="flex items-end gap-3">
        <FormField label="Filtrar por tipo">
          <select value={tipoFilter} onChange={(e) => setTipoFilter(e.target.value)} className="rounded border border-gray-300 px-3 py-2 text-sm">
            <option value="">Todos</option>
            {tipos?.map((tipo) => <option key={tipo.remTipo} value={tipo.remTipo}>{tipo.remTipo} — {tipo.remDesc}</option>)}
          </select>
        </FormField>
      </div>

      <LegacyCrudSection
        title="Remitentes"
        description="Catálogo SEREM."
        items={filteredData}
        columns={[
          { header: 'Código', render: (item) => item.remCod },
          { header: 'Tipo', render: (item) => item.remTipoDesc },
          { header: 'Nombre', render: (item) => item.remNomb },
          { header: 'Comuna', render: (item) => item.remComuna ?? '—' },
          { header: 'Email', render: (item) => item.remEmail ?? '—' },
          { header: 'Rut', render: (item) => item.remRut ?? '—' },
        ]}
        getRowKey={(item) => item.remCod}
        isLoading={isLoading}
        isError={isError}
        errorMessage="No se pudieron cargar los remitentes."
        emptyMessage="No hay remitentes cargados."
        canEdit={canEdit}
        onCreate={openCreate}
        onEdit={openEdit}
        onDelete={(item) => setDeleting(item)}
        actionLabel="Crear remitente"
        searchValue={search}
        searchPlaceholder="Buscar remitentes..."
        onSearchChange={setSearch}
      />

      {modal && (
        <ModalDialog open title={modal === 'crear' ? 'Crear remitente' : 'Editar remitente'} onClose={() => setModal(null)} size="lg" footer={<><Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button><Button type="submit" form="serem-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button></>}>
          <form id="serem-form" onSubmit={form.handleSubmit(submit)} className="grid gap-3 md:grid-cols-2">
            {error && <div className="md:col-span-2 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}
            <FormField label="Código" error={form.formState.errors.remCod?.message}><input {...form.register('remCod')} disabled={modal === 'editar'} maxLength={20} className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-100" /></FormField>
            <FormField label="Tipo" error={form.formState.errors.remTipo?.message}><select {...form.register('remTipo')} className="w-full rounded border border-gray-300 px-3 py-2 text-sm">{tipos?.map((tipo) => <option key={tipo.remTipo} value={tipo.remTipo}>{tipo.remTipo} — {tipo.remDesc}</option>)}</select></FormField>
            <FormField label="Nombre" error={form.formState.errors.remNomb?.message} className="md:col-span-2"><input {...form.register('remNomb')} maxLength={60} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
            <FormField label="Rut" error={form.formState.errors.remRut?.message}><input {...form.register('remRut')} maxLength={12} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
            <FormField label="Dirección" error={form.formState.errors.remDirec?.message}><input {...form.register('remDirec')} maxLength={60} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
            <FormField label="Comuna" error={form.formState.errors.remComuna?.message}><input {...form.register('remComuna')} maxLength={18} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
            <FormField label="Email" error={form.formState.errors.remEmail?.message}><input {...form.register('remEmail')} type="email" maxLength={30} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
            <FormField label="Teléfono" error={form.formState.errors.remTelef?.message}><input {...form.register('remTelef')} maxLength={10} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
            <FormField label="Codigo postal" error={form.formState.errors.remZip?.message}><input {...form.register('remZip')} maxLength={40} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" /></FormField>
          </form>
        </ModalDialog>
      )}

      <ModalDialog open={deleting !== null} title="Eliminar remitente" onClose={() => setDeleting(null)} footer={<><Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button><Button variant="danger" loading={deleteMut.isPending} onClick={() => deleting && deleteMut.mutate(deleting.remCod)}>Eliminar remitente</Button></>}>
        <p className="text-sm text-gray-600">Está por eliminarse el remitente <strong>{deleting?.remNomb}</strong>.</p>
      </ModalDialog>
    </div>
  );
}

export default function AdminRemitentesPage() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_CATALOGOS_EDITAR);

  return (
    <div className="space-y-8">
      <div>
        <h2 className="text-lg font-semibold text-gray-800">Remitentes</h2>
        <p className="mt-1 text-sm text-gray-500">Administración de tipos SEREMTIP y remitentes SEREM.</p>
      </div>

      <SeremTipoSection canEdit={canEdit} />
      <SeremSection canEdit={canEdit} />
    </div>
  );
}
