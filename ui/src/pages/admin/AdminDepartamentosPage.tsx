import { useEffect, useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getDepartamentos,
  activarDepartamento,
  desactivarDepartamento,
  eliminarDepartamento,
  crearDepartamento,
  actualizarDepartamento,
  type DepartamentoAdminDto,
} from '../../lib/api/admin/adminDepartamentosApi';
import Spinner from '../../components/atoms/Spinner';
import IconButton from '../../components/atoms/IconButton';
import FormField from '../../components/molecules/FormField';
import ModalDialog from '../../components/organisms/ModalDialog';
import ConfirmDialog from '../../components/organisms/ConfirmDialog';
import Button from '../../components/atoms/Button';
import Pagination from '../../components/molecules/Pagination';
import { useHasPermission } from '../../hooks/usePermissions';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';

const departamentoSchema = z.object({
  nombre: z.string().min(1, 'El nombre es obligatorio').max(200, 'Máximo 200 caracteres'),
  codigo: z.string().min(1, 'El código es obligatorio').max(20, 'Máximo 20 caracteres'),
});
type DepartamentoFormData = z.infer<typeof departamentoSchema>;

type ModalMode = 'crear' | 'editar' | null;

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }

  return fallback;
}

export default function AdminDepartamentosPage() {
  const canEditDepartamento = useHasPermission(PERMISSIONS.ADMIN_DEPARTAMENTOS_EDITAR);
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<DepartamentoAdminDto | null>(null);
  const [deletingDepartamento, setDeletingDepartamento] = useState<DepartamentoAdminDto | null>(null);
  const [togglingDepartamento, setTogglingDepartamento] = useState<DepartamentoAdminDto | null>(null);
  const [activoFilter, setActivoFilter] = useState<boolean | undefined>(undefined);
  const [searchTerm, setSearchTerm] = useState('');

  const form = useForm<DepartamentoFormData>({
    resolver: zodResolver(departamentoSchema),
  });

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-departamentos', activoFilter],
    queryFn: () => getDepartamentos(activoFilter),
  });

  const visibleDepartamentos = useMemo(() => {
    if (!data) return [];
    const normalizedSearch = searchTerm.trim().toLowerCase();
    if (!normalizedSearch) return data;

    return data.filter((departamento) =>
      departamento.nombre.toLowerCase().includes(normalizedSearch)
      || departamento.codigo.toLowerCase().includes(normalizedSearch),
    );
  }, [data, searchTerm]);

  const [pagina, setPagina] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(20);
  const totalItems = visibleDepartamentos.length;
  const totalPaginas = Math.max(1, Math.ceil(totalItems / tamanoPagina));

  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(visibleDepartamentos.length / tamanoPagina));
    setPagina((current) => Math.min(current, maxPagina));
  }, [visibleDepartamentos.length, tamanoPagina]);

  const pagedDepartamentos = visibleDepartamentos.slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina);

  const crearMut = useMutation({
    mutationFn: (body: DepartamentoFormData) => crearDepartamento(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-departamentos'] });
      qc.invalidateQueries({ queryKey: ['catalogos', 'departamentos'] });
      setModal(null);
      toast.success('Departamento creado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo crear el departamento.')),
  });

  const actualizarMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: DepartamentoFormData }) =>
      actualizarDepartamento(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-departamentos'] });
      qc.invalidateQueries({ queryKey: ['catalogos', 'departamentos'] });
      setModal(null);
      toast.success('Departamento actualizado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo actualizar el departamento.')),
  });

  const activarMut = useMutation({
    mutationFn: (id: string) => activarDepartamento(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-departamentos'] });
      qc.invalidateQueries({ queryKey: ['catalogos', 'departamentos'] });
      setTogglingDepartamento(null);
      toast.success('Departamento activado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo activar el departamento.')),
  });

  const desactivarMut = useMutation({
    mutationFn: (id: string) => desactivarDepartamento(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-departamentos'] });
      qc.invalidateQueries({ queryKey: ['catalogos', 'departamentos'] });
      setTogglingDepartamento(null);
      toast.success('Departamento desactivado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo desactivar el departamento.')),
  });

  const eliminarMut = useMutation({
    mutationFn: (id: string) => eliminarDepartamento(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-departamentos'] });
      qc.invalidateQueries({ queryKey: ['catalogos', 'departamentos'] });
      setDeletingDepartamento(null);
      toast.success('Departamento eliminado correctamente.');
    },
    onError: (error) => {
      setDeletingDepartamento(null);
      toast.error(getErrorMessage(error, 'No se pudo eliminar el departamento.'));
    },
  });

  function onValidSubmit(data: DepartamentoFormData) {
    if (modal === 'crear') {
      crearMut.mutate(data);
    } else if (modal === 'editar' && selected) {
      actualizarMut.mutate({ id: selected.id, body: data });
    }
  }

  function openCrear() {
    if (!canEditDepartamento) return;
    form.reset({ nombre: '', codigo: '' });
    setSelected(null);
    setModal('crear');
  }

  function openEditar(d: DepartamentoAdminDto) {
    if (!canEditDepartamento) return;
    form.reset({ nombre: d.nombre, codigo: d.codigo });
    setSelected(d);
    setModal('editar');
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-800">Departamentos</h2>
        {canEditDepartamento && (
          <Button size="sm" onClick={openCrear}>
            + Crear Departamento
          </Button>
        )}
      </div>

      {/* Filtro por estado */}
      <div className="mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-0.5 block text-xs text-gray-500">Buscar</label>
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Nombre o código"
            className="rounded border border-gray-300 px-3 py-1.5 text-sm"
          />
        </div>
        <select
          value={activoFilter === undefined ? '' : String(activoFilter)}
          onChange={(e) => setActivoFilter(e.target.value === '' ? undefined : e.target.value === 'true')}
          className="border border-gray-300 rounded px-3 py-1.5 text-sm"
        >
          <option value="">Todos</option>
          <option value="true">Activos</option>
          <option value="false">Inactivos</option>
        </select>
      </div>

      {isLoading && (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      )}

      {isError && (
        <p className="text-red-600 text-sm">No se pudieron cargar los departamentos.</p>
      )}

      {data && (
        <div className="overflow-x-auto rounded border border-gray-200">
          <table className="min-w-full text-sm">
            <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
              <tr>
                <th className="px-4 py-2 text-left">Nombre</th>
                <th className="px-4 py-2 text-left">Código</th>
                <th className="px-4 py-2 text-left">Estado</th>
                <th className="px-4 py-2 text-left">Usuarios</th>
                <th className="px-4 py-2 text-left">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {pagedDepartamentos.map((d) => (
                <tr key={d.id} className="hover:bg-gray-50">
                  <td className="px-4 py-2 font-medium text-gray-800">{d.nombre}</td>
                  <td className="px-4 py-2 text-gray-500">{d.codigo}</td>
                  <td className="px-4 py-2">
                    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium ${d.activo ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                      {d.activo ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-gray-600">{d.totalUsuarios}</td>
                  <td className="px-4 py-2 flex gap-1">
                    {canEditDepartamento && (
                      <>
                        <IconButton
                          name="edit"
                          tooltip="Editar"
                          appearance="admin"
                          onClick={() => openEditar(d)}
                        />
                        {d.activo ? (
                          <IconButton
                            name="x"
                            tooltip="Desactivar"
                            variant="secondary"
                            appearance="admin"
                            onClick={() => setTogglingDepartamento(d)}
                          />
                        ) : (
                          <IconButton
                            name="check"
                            tooltip="Activar"
                            appearance="admin"
                            onClick={() => setTogglingDepartamento(d)}
                          />
                        )}
                        <IconButton
                          name="trash"
                          tooltip="Eliminar"
                          variant="danger"
                          appearance="admin"
                          onClick={() => {
                            setDeletingDepartamento(d);
                          }}
                        />
                      </>
                    )}
                  </td>
                </tr>
              ))}
              {visibleDepartamentos.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-sm text-gray-500">
                    No hay departamentos para los filtros aplicados.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          {totalItems > 0 && (
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
      )}

      {modal && (
        <ModalDialog
          open={modal !== null}
          title={modal === 'crear' ? 'Crear Departamento' : 'Editar Departamento'}
          onClose={() => setModal(null)}
          footer={(
            <>
              <Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button>
              <Button type="submit" form="departamento-form" loading={crearMut.isPending || actualizarMut.isPending}>Guardar</Button>
            </>
          )}
        >
          <form id="departamento-form" onSubmit={form.handleSubmit(onValidSubmit)} className="space-y-3">
            <FormField label="Nombre" error={form.formState.errors.nombre?.message}>
              <input
                {...form.register('nombre')}
                placeholder="Nombre"
                maxLength={200}
                className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
              />
            </FormField>
            <FormField label="Código" error={form.formState.errors.codigo?.message}>
              <input
                {...form.register('codigo')}
                placeholder="Código (ej: RRHH)"
                maxLength={20}
                className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
              />
            </FormField>
            {selected && (
              <div className="rounded border border-blue-100 bg-blue-50 px-3 py-2 text-xs text-blue-700">
                Este departamento tiene {selected.totalUsuarios} usuario(s) asignado(s).
              </div>
            )}
          </form>
        </ModalDialog>
      )}

      <ModalDialog
        open={deletingDepartamento !== null}
        title="Eliminar departamento"
        onClose={() => setDeletingDepartamento(null)}
        footer={(
          <>
            <Button variant="secondary" onClick={() => setDeletingDepartamento(null)}>Cancelar</Button>
            <Button
              variant="danger"
              loading={eliminarMut.isPending}
              onClick={() => {
                if (deletingDepartamento) {
                  eliminarMut.mutate(deletingDepartamento.id);
                }
              }}
            >
              Eliminar departamento
            </Button>
          </>
        )}
      >
        <p className="text-sm text-gray-600">
          Está por eliminarse el departamento <strong>{deletingDepartamento?.nombre}</strong>.
          {deletingDepartamento && deletingDepartamento.totalUsuarios > 0 && (
            <> Tiene usuarios asignados y la operación puede no estar permitida.</>
          )}
        </p>
      </ModalDialog>

      <ConfirmDialog
        open={togglingDepartamento !== null}
        title={togglingDepartamento?.activo ? 'Desactivar departamento' : 'Activar departamento'}
        message={togglingDepartamento
          ? `¿Seguro que querés ${togglingDepartamento.activo ? 'desactivar' : 'activar'} el departamento "${togglingDepartamento.nombre}"?`
          : ''}
        confirmLabel={togglingDepartamento?.activo ? 'Desactivar' : 'Activar'}
        danger={togglingDepartamento?.activo ?? false}
        loading={activarMut.isPending || desactivarMut.isPending}
        onConfirm={() => {
          if (!togglingDepartamento) return;
          if (togglingDepartamento.activo) desactivarMut.mutate(togglingDepartamento.id);
          else activarMut.mutate(togglingDepartamento.id);
        }}
        onCancel={() => setTogglingDepartamento(null)}
      />
    </div>
  );
}
