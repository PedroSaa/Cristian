import { useState, useEffect, useMemo } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getUsuarios,
  getUsuario,
  activarUsuario,
  desactivarUsuario,
  crearUsuario,
  actualizarUsuario,
  resetPassword,
  bloquearUsuario,
  desbloquearUsuario,
  type UsuarioAdminDto,
  type ActualizarUsuarioData,
} from '../../lib/api/admin/adminUsuariosApi';
import { getDepartamentosCatalogo } from '../../lib/api/catalogos';
import { getRoles } from '../../lib/api/admin/adminRolesApi';
import Spinner from '../../components/atoms/Spinner';
import Toggle from '../../components/atoms/Toggle';
import Tooltip from '../../components/atoms/Tooltip';
import Button from '../../components/atoms/Button';
import IconButton from '../../components/atoms/IconButton';
import FormField from '../../components/molecules/FormField';
import SearchableSelect from '../../components/molecules/SearchableSelect';
import ModalDialog from '../../components/organisms/ModalDialog';
import ConfirmDialog from '../../components/organisms/ConfirmDialog';
import FirmaUsuarioModal, { type FirmaOperations } from '../../components/organisms/FirmaUsuarioModal';
import {
  getFirmaMetadata,
  getFirmaImagen,
  guardarFirma,
  eliminarFirma,
} from '../../lib/api/admin/firmaUsuarioApi';
import Pagination from '../../components/molecules/Pagination';
import { useHasPermission } from '../../hooks/usePermissions';
import { usePasswordPolicy } from '../../hooks/usePasswordPolicy';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import { buildPasswordPolicySchema, type PasswordPolicy } from '../../lib/validations/auth';

type ModalMode = 'crear' | 'editar' | 'reset' | 'bloquear' | null;

const buildUsuarioSchema = (policy: PasswordPolicy) => z.object({
  nombres: z.string().min(1, 'Los nombres son obligatorios').max(150, 'Máximo 150 caracteres'),
  apellidoPaterno: z.string().max(100, 'Máximo 100 caracteres').or(z.literal('')),
  apellidoMaterno: z.string().max(100, 'Máximo 100 caracteres').or(z.literal('')),
  telefono: z.string().max(30, 'Máximo 30 caracteres').or(z.literal('')),
  direccion: z.string().max(250, 'Máximo 250 caracteres').or(z.literal('')),
  email: z.string().min(1, 'El email es obligatorio').email('Email inválido').max(200, 'Máximo 200 caracteres'),
  rut: z.string().max(20, 'Máximo 20 caracteres').or(z.literal('')),
  rol: z.string().min(1, 'El rol es obligatorio'),
  departamentoId: z.string().nullable().optional(),
  usucod: z.string().max(25, 'Máximo 25 caracteres').or(z.literal('')),
  password: buildPasswordPolicySchema(policy).or(z.literal('')),
});

type UsuarioFormData = z.infer<ReturnType<typeof buildUsuarioSchema>>;

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }

  return fallback;
}

const emptyUserForm: UsuarioFormData = {
  nombres: '',
  apellidoPaterno: '',
  apellidoMaterno: '',
  telefono: '',
  direccion: '',
  email: '',
  rut: '',
  rol: '',
  departamentoId: null,
  usucod: '',
  password: '',
};

export default function AdminUsuariosPage() {
  const canCreateUsuario = useHasPermission(PERMISSIONS.ADMIN_USUARIOS_CREAR);
  const canEditUsuario = useHasPermission(PERMISSIONS.ADMIN_USUARIOS_EDITAR);
  const canActivateUsuario = useHasPermission(PERMISSIONS.ADMIN_USUARIOS_ACTIVAR);
  const canDeactivateUsuario = useHasPermission(PERMISSIONS.ADMIN_USUARIOS_DESACTIVAR);
  const canResetPassword = useHasPermission(PERMISSIONS.ADMIN_USUARIOS_RESET_PASSWORD);
  const canBloquearUsuario = useHasPermission(PERMISSIONS.ADMIN_USUARIOS_BLOQUEAR);

  const qc = useQueryClient();
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(20);
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<UsuarioAdminDto | null>(null);
  const [newPassword, setNewPassword] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [actionError, setActionError] = useState<string | null>(null);
  const [togglingUser, setTogglingUser] = useState<UsuarioAdminDto | null>(null);
  const [firmaUser, setFirmaUser] = useState<UsuarioAdminDto | null>(null);

  // Signature operations bound to the selected user's id (admin API). Injected
  // into the shared FirmaUsuarioModal so it stays transport-agnostic.
  const firmaUserId = firmaUser?.id ?? null;
  const firmaOperations = useMemo<FirmaOperations>(() => {
    const id = firmaUserId ?? '__none__';
    return {
      getMetadata: () => getFirmaMetadata(id),
      getImagen: () => getFirmaImagen(id),
      guardar: (body) => guardarFirma(id, body),
      eliminar: () => eliminarFirma(id),
      cacheKey: ['admin', 'usuarios', 'firma', id] as const,
    };
  }, [firmaUserId]);

  const [filters, setFilters] = useState<{ rol: string; departamentoId: string; activo: boolean | undefined }>({
    rol: '',
    departamentoId: '',
    activo: undefined,
  });

  const passwordPolicy = usePasswordPolicy();
  const usuarioSchema = useMemo(() => buildUsuarioSchema(passwordPolicy), [passwordPolicy]);
  const form = useForm<UsuarioFormData>({
    resolver: zodResolver(usuarioSchema),
    defaultValues: emptyUserForm,
  });

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin', 'usuarios', page, tamanoPagina, filters, searchTerm],
    queryFn: () => getUsuarios(page, tamanoPagina, { ...filters, search: searchTerm.trim() || undefined }),
  });

  const { data: departamentos } = useQuery({
    queryKey: ['catalogos', 'departamentos'],
    queryFn: getDepartamentosCatalogo,
    staleTime: 30_000,
  });

  const { data: roles } = useQuery({
    queryKey: ['admin', 'roles-catalog'],
    queryFn: getRoles,
    staleTime: 30_000,
  });

  const editUserQuery = useQuery({
    queryKey: ['admin', 'usuarios', 'detail', selected?.id],
    queryFn: () => getUsuario(selected!.id),
    enabled: modal === 'editar' && selected !== null,
    staleTime: 0,
  });
  const isEditDetailLoading = modal === 'editar' && editUserQuery.isFetching && !editUserQuery.data;

  useEffect(() => {
    if (modal !== 'editar') return;
    if (!selected) return;

    form.reset({
      nombres: selected.nombres ?? '',
      apellidoPaterno: selected.apellidoPaterno ?? '',
      apellidoMaterno: selected.apellidoMaterno ?? '',
      telefono: selected.telefono ?? '',
      direccion: selected.direccion ?? '',
      email: selected.email,
      rut: selected.rut ?? '',
      rol: selected.rol,
      departamentoId: selected.departamentoId,
      password: '',
    });
  }, [modal, selected, form]);

  useEffect(() => {
    if (modal !== 'editar') return;
    if (!editUserQuery.data || !selected) return;
    if (editUserQuery.data.id !== selected.id) return;

    form.reset({
      nombres: editUserQuery.data.nombres ?? '',
      apellidoPaterno: editUserQuery.data.apellidoPaterno ?? '',
      apellidoMaterno: editUserQuery.data.apellidoMaterno ?? '',
      telefono: editUserQuery.data.telefono ?? '',
      direccion: editUserQuery.data.direccion ?? '',
      email: editUserQuery.data.email,
      rut: editUserQuery.data.rut ?? '',
      rol: editUserQuery.data.rol,
      departamentoId: editUserQuery.data.departamentoId,
      password: '',
    });
  }, [editUserQuery.data, modal, selected, form]);

  const visibleUsers = data?.items ?? [];

  const crearMut = useMutation({
    mutationFn: crearUsuario,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'usuarios'] });
      closeModal();
      toast.success('Usuario creado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo crear el usuario.')),
  });

  const actualizarMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: ActualizarUsuarioData }) =>
      actualizarUsuario(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'usuarios'] });
      closeModal();
      toast.success('Usuario actualizado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo actualizar el usuario.')),
  });

  const activarMut = useMutation({
    mutationFn: activarUsuario,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'usuarios'] });
      setTogglingUser(null);
      toast.success('Usuario activado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo activar el usuario.')),
  });

  const desactivarMut = useMutation({
    mutationFn: desactivarUsuario,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'usuarios'] });
      setTogglingUser(null);
      toast.success('Usuario desactivado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo desactivar el usuario.')),
  });

  const bloquearMut = useMutation({
    mutationFn: bloquearUsuario,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'usuarios'] });
      closeModal();
      toast.success('Usuario bloqueado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo bloquear el usuario.')),
  });

  const desbloquearMut = useMutation({
    mutationFn: desbloquearUsuario,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'usuarios'] });
      toast.success('Usuario desbloqueado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo desbloquear el usuario.')),
  });

  const resetMut = useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) => resetPassword(id, { nuevaPassword: password }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin', 'usuarios'] });
      closeModal();
      toast.success('Contraseña restablecida correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo restablecer la contraseña.')),
  });

  function closeModal() {
    setModal(null);
    setSelected(null);
    setNewPassword('');
    setActionError(null);
    form.reset(emptyUserForm);
  }

  function openCrear() {
    form.reset(emptyUserForm);
    setSelected(null);
    setActionError(null);
    setModal('crear');
  }

  function openEditar(user: UsuarioAdminDto) {
    setSelected(user);
    setActionError(null);
    setModal('editar');
  }

  function openReset(user: UsuarioAdminDto) {
    setSelected(user);
    setNewPassword('');
    setActionError(null);
    setModal('reset');
  }

  function openBloquear(user: UsuarioAdminDto) {
    setSelected(user);
    setActionError(null);
    setModal('bloquear');
  }

  function onSubmit(values: UsuarioFormData) {
    setActionError(null);

    if (modal === 'crear') {
      if (!values.email) {
        form.setError('email', { message: 'El email es obligatorio.' });
        return;
      }
      if (!values.password) {
        form.setError('password', { message: 'La contraseña inicial es obligatoria.' });
        return;
      }

      crearMut.mutate({
        nombres: values.nombres,
        apellidoPaterno: values.apellidoPaterno?.trim() || '',
        apellidoMaterno: values.apellidoMaterno?.trim() || '',
        telefono: values.telefono?.trim() || null,
        direccion: values.direccion?.trim() || null,
        email: values.email,
        rut: values.rut?.trim() || null,
        rol: values.rol,
        departamentoId: values.departamentoId ?? null,
        password: values.password,
        usucod: values.usucod?.trim() || null,
      });
      return;
    }

    if (modal === 'editar' && selected) {
      actualizarMut.mutate({
        id: selected.id,
        body: {
          nombres: values.nombres,
          apellidoPaterno: values.apellidoPaterno?.trim() || '',
          apellidoMaterno: values.apellidoMaterno?.trim() || '',
          telefono: values.telefono?.trim() || null,
          direccion: values.direccion?.trim() || null,
          email: values.email,
          rut: values.rut?.trim() || null,
          rol: values.rol,
          departamentoId: values.departamentoId ?? null,
        },
      });
    }
  }

  async function saveUser() {
    if (modal === 'crear') {
      // Validamos zod y la contraseña (obligatoria solo al crear) a la vez, para que
      // TODOS los errores de campos obligatorios se muestren en el primer intento.
      const valid = await form.trigger();
      let passwordOk = true;
      if (!form.getValues('password')) {
        form.setError('password', { message: 'La contraseña inicial es obligatoria.' });
        passwordOk = false;
      }
      if (!valid || !passwordOk) return;
    }
    onSubmit(form.getValues());
  }

  const isSavingForm = crearMut.isPending || actualizarMut.isPending;

  return (
    <div>
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-gray-800">Usuarios del Sistema</h2>
        {canCreateUsuario && <Button onClick={openCrear}>Crear usuario</Button>}
      </div>

      <div className="mb-4 flex flex-wrap items-end gap-3">
        <div>
          <label className="mb-0.5 block text-xs text-gray-500">Buscar</label>
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => { setSearchTerm(e.target.value); setPage(1); }}
            placeholder="Nombre, email, RUT o código"
            className="rounded border border-gray-300 px-3 py-1.5 text-sm"
          />
        </div>
        <div>
          <label className="mb-0.5 block text-xs text-gray-500">Rol</label>
          {roles ? (
            <SearchableSelect
              options={roles}
              value={filters.rol}
              onChange={(v) => { setFilters((f) => ({ ...f, rol: v })); setPage(1); }}
              getOptionLabel={(r: { id: string; nombre: string }) => r.nombre}
              getOptionValue={(r: { id: string; nombre: string }) => r.nombre}
              placeholder="Todos los roles"
              allLabel="Todos los roles"
            />
          ) : (
            <Tooltip content="El catálogo de roles no está disponible. Intente más tarde.">
              <span className="inline-block cursor-help rounded border border-gray-300 px-3 py-1.5 text-sm italic text-gray-400">
                Roles no disponibles
              </span>
            </Tooltip>
          )}
        </div>
        <div>
          <label className="mb-0.5 block text-xs text-gray-500">Departamento</label>
          {departamentos ? (
            <SearchableSelect
              options={departamentos}
              value={filters.departamentoId}
              onChange={(v) => { setFilters((f) => ({ ...f, departamentoId: v })); setPage(1); }}
              getOptionLabel={(d: { id: string; nombre: string }) => d.nombre}
              getOptionValue={(d: { id: string; nombre: string }) => d.id}
              placeholder="Todos los departamentos"
              allLabel="Todos los departamentos"
            />
          ) : (
            <Tooltip content="El catálogo de departamentos no está disponible. Intente más tarde.">
              <span className="inline-block cursor-help rounded border border-gray-300 px-3 py-1.5 text-sm italic text-gray-400">
                Departamento no disponible
              </span>
            </Tooltip>
          )}
        </div>
        <div>
          <label className="mb-0.5 block text-xs text-gray-500">Estado</label>
          <Toggle
            label="Solo activos"
            checked={filters.activo === true}
            onChange={(e) => setFilters((f) => ({ ...f, activo: e.target.checked ? true : undefined }))}
          />
        </div>
      </div>

      {isLoading && (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      )}

      {isError && (
        <p className="text-sm text-red-600">No se pudieron cargar los usuarios.</p>
      )}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="min-w-full text-sm">
              <thead className="bg-gray-50 text-xs uppercase text-gray-600">
                <tr>
                  <th className="px-4 py-2 text-left">Nombre</th>
                  <th className="px-4 py-2 text-left">Email</th>
                  <th className="px-4 py-2 text-left">Usuario</th>
                  <th className="px-4 py-2 text-left">RUT</th>
                  <th className="px-4 py-2 text-left">Rol</th>
                  <th className="px-4 py-2 text-left">Departamento</th>
                  <th className="px-4 py-2 text-left">Estado</th>
                  <th className="sticky right-0 bg-gray-50 px-4 py-2 text-left shadow-[-8px_0_8px_-8px_rgba(0,0,0,0.12)]">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {visibleUsers.map((user) => (
                  <tr key={user.id} className="group hover:bg-gray-50">
                    <td className="px-4 py-2 font-medium text-gray-800">{user.nombreCompleto}</td>
                    <td className="px-4 py-2 text-gray-600">{user.email}</td>
                    <td className="px-4 py-2 font-mono text-xs text-gray-600">{user.usucod ?? '—'}</td>
                    <td className="px-4 py-2 font-mono text-xs text-gray-600">{user.rut ?? '—'}</td>
                    <td className="px-4 py-2">{user.rol}</td>
                    <td className="px-4 py-2 text-gray-500">{user.departamentoNombre ?? '—'}</td>
                    <td className="px-4 py-2">
                      <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${user.estaBloqueado ? 'bg-amber-100 text-amber-700' : user.activo ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-700'}`}>
                        {user.estaBloqueado ? 'Bloqueado' : user.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td className="sticky right-0 whitespace-nowrap bg-white px-4 py-2 shadow-[-8px_0_8px_-8px_rgba(0,0,0,0.12)] group-hover:bg-gray-50">
                      <div className="flex items-center gap-1">
                        {canEditUsuario && (
                          <IconButton
                            name="edit"
                            tooltip="Editar"
                            appearance="admin"
                            onClick={() => openEditar(user)}
                          />
                        )}
                        {canEditUsuario && (
                          <IconButton
                            name="signature"
                            tooltip="Configurar firma"
                            appearance="admin"
                            onClick={() => setFirmaUser(user)}
                          />
                        )}
                        {(() => {
                          const protectedReason = user.esCuentaPropia
                            ? 'No puedes desactivar tu propia cuenta.'
                            : user.esUltimoAdminActivo
                              ? 'No puedes desactivar al último administrador activo.'
                              : undefined;

                          if (user.activo && canDeactivateUsuario) {
                            return (
                            <IconButton
                              name="x"
                              tooltip="Desactivar"
                              variant="secondary"
                              appearance="admin"
                              disabled={protectedReason != null}
                              disabledTooltip={protectedReason}
                              onClick={() => {
                                setActionError(null);
                                setTogglingUser(user);
                              }}
                            />
                            );
                          }

                          if (!user.activo && canActivateUsuario) {
                            return (
                            <IconButton
                              name="check"
                              tooltip="Activar"
                              appearance="admin"
                              onClick={() => {
                                setActionError(null);
                                setTogglingUser(user);
                              }}
                            />
                            );
                          }

                          return null;
                        })()}
                        {canBloquearUsuario && (user.estaBloqueado ? (
                          <IconButton
                            name="archive-restore"
                            tooltip="Desbloquear"
                            appearance="admin"
                            loading={desbloquearMut.isPending}
                            onClick={() => {
                              setActionError(null);
                              desbloquearMut.mutate(user.id);
                            }}
                          />
                        ) : (
                          <IconButton
                            name="alert-circle"
                            tooltip="Bloquear"
                            variant="danger"
                            appearance="admin"
                            disabled={user.esCuentaPropia || user.esUltimoAdminActivo}
                            disabledTooltip={user.esCuentaPropia
                              ? 'No puedes bloquear tu propia cuenta.'
                              : user.esUltimoAdminActivo
                                ? 'No puedes bloquear al último administrador activo.'
                                : undefined}
                            onClick={() => openBloquear(user)}
                          />
                        ))}
                        {canResetPassword && (
                          <IconButton
                            name="key-round"
                            tooltip="Restablecer contraseña"
                            appearance="admin"
                            onClick={() => openReset(user)}
                          />
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
                {visibleUsers.length === 0 && (
                  <tr>
                    <td colSpan={8} className="px-4 py-8 text-center text-sm text-gray-500">
                      No hay usuarios para los filtros o búsqueda aplicados.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

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
        </>
      )}

      <ModalDialog
        open={modal === 'crear' || modal === 'editar'}
        title={modal === 'crear' ? 'Crear usuario' : 'Editar usuario'}
        onClose={closeModal}
        size="md"
        footer={(
          <>
            <Button variant="secondary" onClick={closeModal}>Cancelar</Button>
            <Button type="button" onClick={saveUser} loading={isSavingForm || isEditDetailLoading}>Guardar</Button>
          </>
        )}
      >
        <form className="space-y-3">
          <div className="grid grid-cols-1 gap-3 md:grid-cols-3">
            <FormField label="Nombres" required error={form.formState.errors.nombres?.message}>
              <input {...form.register('nombres')} maxLength={150} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
            <FormField label="Apellido paterno" error={form.formState.errors.apellidoPaterno?.message}>
              <input {...form.register('apellidoPaterno')} maxLength={100} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
            <FormField label="Apellido materno" error={form.formState.errors.apellidoMaterno?.message}>
              <input {...form.register('apellidoMaterno')} maxLength={100} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
            </FormField>
          </div>

          <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
            <FormField label="Teléfono" error={form.formState.errors.telefono?.message}>
              <input {...form.register('telefono')} maxLength={30} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" placeholder="+56 9 1234 5678" />
            </FormField>
            <FormField label="Dirección" error={form.formState.errors.direccion?.message}>
              <input {...form.register('direccion')} maxLength={250} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" placeholder="Calle, número, comuna" />
            </FormField>
          </div>

          {modal === 'crear' && (
            <>
              <FormField label="Email" required error={form.formState.errors.email?.message}>
                <input {...form.register('email')} type="email" maxLength={200} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
              </FormField>
              <FormField label="RUT" error={form.formState.errors.rut?.message}>
                <input {...form.register('rut')} maxLength={20} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" placeholder="12.345.678-9" />
              </FormField>
              <FormField label="Nombre de usuario" error={form.formState.errors.usucod?.message}>
                <input {...form.register('usucod')} maxLength={25} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" placeholder="jperez" />
              </FormField>
              <FormField label="Contraseña inicial" required error={form.formState.errors.password?.message}>
                <input {...form.register('password')} type="password" className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
              </FormField>
            </>
          )}

          {modal === 'editar' && (
            <>
              {isEditDetailLoading && (
                <div className="rounded border border-blue-200 bg-blue-50 px-3 py-2 text-sm text-blue-700">
                  Cargando datos del usuario...
                </div>
              )}

              <FormField label="Email" error={form.formState.errors.email?.message}>
                <input {...form.register('email')} type="email" maxLength={200} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" />
              </FormField>
              <FormField label="RUT" error={form.formState.errors.rut?.message}>
                <input {...form.register('rut')} maxLength={20} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" placeholder="12.345.678-9" />
              </FormField>
              <div className="rounded border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-600">
                <span className="font-medium text-gray-700">Usuario:</span> {selected?.usucod ?? '—'}
              </div>
            </>
          )}

          <FormField label="Rol" required error={form.formState.errors.rol?.message}>
            {roles ? (
              <SearchableSelect
                options={roles}
                value={form.watch('rol')}
                onChange={(v) => form.setValue('rol', v, { shouldValidate: true })}
                getOptionLabel={(r: { id: string; nombre: string }) => r.nombre}
                getOptionValue={(r: { id: string; nombre: string }) => r.nombre}
                placeholder="— Seleccionar rol —"
              />
            ) : (
              <Tooltip content="El catálogo de roles no está disponible. Intente más tarde.">
                <span className="block cursor-help rounded border border-gray-300 px-3 py-2 text-sm italic text-gray-400">
                  Roles no disponibles
                </span>
              </Tooltip>
            )}
          </FormField>

          <FormField label="Departamento">
            {departamentos ? (
              <SearchableSelect
                options={departamentos}
                value={form.watch('departamentoId') ?? ''}
                onChange={(v) => form.setValue('departamentoId', v || null)}
                getOptionLabel={(d: { id: string; nombre: string }) => d.nombre}
                getOptionValue={(d: { id: string; nombre: string }) => d.id}
                placeholder="— Sin departamento —"
              />
            ) : (
              <Tooltip content="El catálogo de departamentos no está disponible. Intente más tarde.">
                <span className="block cursor-help rounded border border-gray-300 px-3 py-2 text-sm italic text-gray-400">
                  Departamento no disponible
                </span>
              </Tooltip>
            )}
          </FormField>
        </form>
      </ModalDialog>

      <ModalDialog
        open={modal === 'reset'}
        title="Restablecer contraseña"
        onClose={closeModal}
        footer={(
          <>
            <Button variant="secondary" onClick={closeModal}>Cancelar</Button>
            <Button
              loading={resetMut.isPending}
              onClick={() => {
                if (!selected) return;
                const check = buildPasswordPolicySchema(passwordPolicy).safeParse(newPassword);
                if (!check.success) {
                  setActionError(check.error.issues[0].message);
                  return;
                }
                setActionError(null);
                resetMut.mutate({ id: selected.id, password: newPassword });
              }}
            >
              Restablecer
            </Button>
          </>
        )}
      >
        <div className="space-y-3">
          {actionError && (
            <div className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              {actionError}
            </div>
          )}

          <p className="text-sm text-gray-600">Usuario: <strong>{selected?.nombreCompleto}</strong></p>
          <FormField label="Nueva contraseña">
            <input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </FormField>
        </div>
      </ModalDialog>

      <ModalDialog
        open={modal === 'bloquear'}
        title="Bloquear usuario"
        onClose={closeModal}
        footer={(
          <>
            <Button variant="secondary" onClick={closeModal}>Cancelar</Button>
            <Button
              variant="danger"
              loading={bloquearMut.isPending}
              onClick={() => {
                if (!selected) return;
                setActionError(null);
                bloquearMut.mutate(selected.id);
              }}
            >
              Bloquear usuario
            </Button>
          </>
        )}
      >
        <div className="space-y-2 text-sm text-gray-600">
          <p>
            Estás por <strong>suspender temporalmente</strong> a <strong>{selected?.nombreCompleto}</strong>.
          </p>
          <ul className="list-disc pl-5 space-y-1">
            <li>El bloqueo dura <strong>30 minutos</strong> y se desactiva solo.</li>
            <li>No puede iniciar sesión durante ese período.</li>
            <li>Utilice <em>Desactivar</em> si desea revocar el acceso hasta que otro administrador lo reactive.</li>
          </ul>
        </div>
      </ModalDialog>

      <ConfirmDialog
        open={togglingUser !== null}
        title={togglingUser?.activo ? 'Desactivar usuario' : 'Activar usuario'}
        message={togglingUser
          ? `¿Seguro que querés ${togglingUser.activo ? 'desactivar' : 'activar'} a "${togglingUser.nombreCompleto}"?${togglingUser.activo ? ' No podrá iniciar sesión hasta que se reactive.' : ''}`
          : ''}
        confirmLabel={togglingUser?.activo ? 'Desactivar' : 'Activar'}
        danger={togglingUser?.activo ?? false}
        loading={activarMut.isPending || desactivarMut.isPending}
        onConfirm={() => {
          if (!togglingUser) return;
          if (togglingUser.activo) desactivarMut.mutate(togglingUser.id);
          else activarMut.mutate(togglingUser.id);
        }}
        onCancel={() => setTogglingUser(null)}
      />

      <FirmaUsuarioModal
        open={firmaUser !== null}
        operations={firmaOperations}
        usuarioNombre={firmaUser?.nombreCompleto ?? ''}
        canEdit={canEditUsuario}
        onClose={() => setFirmaUser(null)}
      />
    </div>
  );
}
