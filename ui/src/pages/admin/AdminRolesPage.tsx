import { useState, useCallback, useMemo, useRef, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getRoles,
  crearRol,
  actualizarRol,
  eliminarRol,
  getPermisosRol,
  type RolDto,
} from '../../lib/api/admin/adminRolesApi';
import {
  getPermisos,
  assignPermisosRol,
  type PermisoDto,
} from '../../lib/api/admin/adminPermisosApi';
import Spinner from '../../components/atoms/Spinner';
import IconButton from '../../components/atoms/IconButton';
import FormField from '../../components/molecules/FormField';
import ModalDialog from '../../components/organisms/ModalDialog';
import Button from '../../components/atoms/Button';
import Pagination from '../../components/molecules/Pagination';
import { useHasPermission } from '../../hooks/usePermissions';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';

const rolSchema = z.object({
  nombre: z.string().min(1, 'El nombre es obligatorio').max(100, 'Máximo 100 caracteres'),
  descripcion: z.string().max(500, 'Máximo 500 caracteres').optional(),
});

type RolFormData = z.infer<typeof rolSchema>;
type ModalMode = 'crear' | 'editar' | null;

// ── Permission group display names (fallback: capitalized raw key) ──────────

const GRUPO_LABELS: Record<string, string> = {
  admin: 'Administración',
  ordenescompra: 'Órdenes de Compra',
  rrhh: 'RRHH',
  oirs: 'OIRS',
};

function grupoLabel(grupo: string): string {
  return GRUPO_LABELS[grupo] ?? grupo.charAt(0).toUpperCase() + grupo.slice(1);
}

// ── Local helper: group select-all checkbox with indeterminate support ──────

function GroupCheckbox({
  checked,
  indeterminate,
  onChange,
  ariaLabel,
}: {
  checked: boolean;
  indeterminate: boolean;
  onChange: () => void;
  ariaLabel: string;
}) {
  const ref = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (ref.current) {
      ref.current.indeterminate = indeterminate;
    }
  }, [indeterminate]);

  return (
    <input
      ref={ref}
      type="checkbox"
      checked={checked}
      onChange={onChange}
      onClick={(e) => e.stopPropagation()}
      aria-label={ariaLabel}
      className="h-4 w-4 rounded border-gray-300 text-blue-700 focus:ring-2 focus:ring-blue-500"
    />
  );
}

export default function AdminRolesPage() {
  const canCreateRole = useHasPermission(PERMISSIONS.ADMIN_ROLES_CREAR);
  const canEditRole = useHasPermission(PERMISSIONS.ADMIN_ROLES_EDITAR);
  const canDeleteRole = useHasPermission(PERMISSIONS.ADMIN_ROLES_ELIMINAR);
  const canManageRolePermissions = useHasPermission(PERMISSIONS.ADMIN_ROLES_PERMISOS);

  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<ModalMode>(null);
  const [selected, setSelected] = useState<RolDto | null>(null);
  const [deletingRole, setDeletingRole] = useState<RolDto | null>(null);
  const [selectedPermisoIds, setSelectedPermisoIds] = useState<Set<string>>(new Set());
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set());
  const permisosInitialisedRef = useRef(false);
  const [permisosExpanded, setPermisosExpanded] = useState<Set<string>>(new Set());
  const [search, setSearch] = useState('');

  const form = useForm<RolFormData>({
    resolver: zodResolver(rolSchema),
  });

  // ── Queries ────────────────────────────────────────────────────────────────

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-roles'],
    queryFn: getRoles,
  });

  const { data: permisosCatalog, isLoading: catalogLoading } = useQuery({
    queryKey: ['admin-permisos'],
    queryFn: getPermisos,
    enabled: modal === 'editar' || modal === 'crear',
  });

  const { data: rolePermisos, isLoading: rolePermisosLoading } = useQuery({
    queryKey: ['admin-rol-permisos', selected?.id],
    queryFn: () => (selected ? getPermisosRol(selected.id) : Promise.resolve([])),
    enabled: modal === 'editar' && !!selected,
  });

  // When role's current permissions finish loading, initialise the selected set
  useEffect(() => {
    if (rolePermisos && modal === 'editar' && !permisosInitialisedRef.current) {
      setSelectedPermisoIds(new Set(rolePermisos.map((p) => p.id)));
      permisosInitialisedRef.current = true;
    }
  }, [rolePermisos, modal]);

  // Reset init flag when modal closes
  useEffect(() => {
    if (modal !== 'editar') {
      permisosInitialisedRef.current = false;
    }
  }, [modal]);

  // ── Mutations ──────────────────────────────────────────────────────────────

  const crearMut = useMutation({
    mutationFn: (body: RolFormData) =>
      crearRol({ nombre: body.nombre, descripcion: body.descripcion || undefined }),
    onSuccess: (createdRole) => {
      if (selectedPermisoIds.size > 0) {
        assignPermisosMut.mutate({
          rolId: createdRole.id,
          permisoIds: Array.from(selectedPermisoIds),
        });
        return;
      }

      qc.invalidateQueries({ queryKey: ['admin-roles'] });
      qc.invalidateQueries({ queryKey: ['admin', 'roles-catalog'] });
      setModal(null);
      toast.success('Rol creado correctamente.');
    },
    onError: (err: Error) => {
      const msg = (err as any).userMessage || err.message;
      form.setError('nombre', { message: msg });
    },
  });

  const actualizarMut = useMutation({
    mutationFn: ({ id, body }: { id: string; body: RolFormData }) =>
      actualizarRol(id, { nombre: body.nombre, descripcion: body.descripcion || undefined }),
    onSuccess: () => {
      if (selected && modal === 'editar') {
        assignPermisosMut.mutate({
          rolId: selected.id,
          permisoIds: Array.from(selectedPermisoIds),
        });
      } else {
        qc.invalidateQueries({ queryKey: ['admin-roles'] });
        setModal(null);
        toast.success('Rol actualizado correctamente.');
      }
    },
    onError: (err: Error) => {
      const msg = (err as any).userMessage || err.message;
      form.setError('nombre', { message: msg });
    },
  });

  const assignPermisosMut = useMutation({
    mutationFn: ({ rolId, permisoIds }: { rolId: string; permisoIds: string[] }) =>
      assignPermisosRol(rolId, permisoIds),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-roles'] });
      qc.invalidateQueries({ queryKey: ['admin', 'roles-catalog'] });
      qc.invalidateQueries({ queryKey: ['admin-rol-permisos'] });
      setModal(null);
      toast.success(selected ? 'Rol actualizado correctamente.' : 'Permisos del rol guardados correctamente.');
    },
    onError: (err: Error) => {
      const msg = (err as any).userMessage || err.message;
      form.setError('nombre', { message: msg });
    },
  });

  const eliminarMut = useMutation({
    mutationFn: (id: string) => eliminarRol(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-roles'] });
      qc.invalidateQueries({ queryKey: ['admin', 'roles-catalog'] });
      setDeletingRole(null);
      toast.success('Rol eliminado correctamente.');
    },
    onError: (err: Error) => {
      toast.error((err as any).userMessage || err.message || 'No se pudo eliminar el rol.');
    },
  });

  // ── Permissions grouping ───────────────────────────────────────────────────

  const groupedPermisos = useMemo(() => {
    if (!permisosCatalog) return {};
    return permisosCatalog.reduce<Record<string, PermisoDto[]>>((acc, p) => {
      (acc[p.grupo] ??= []).push(p);
      return acc;
    }, {});
  }, [permisosCatalog]);

  const togglePermiso = useCallback((id: string) => {
    setSelectedPermisoIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }, []);

  const toggleGroup = useCallback((grupo: string) => {
    setExpandedGroups((prev) => {
      const next = new Set(prev);
      if (next.has(grupo)) next.delete(grupo);
      else next.add(grupo);
      return next;
    });
  }, []);

  const toggleGroupAll = useCallback(
    (_: string, permisos: PermisoDto[]) => {
      const allSelected = permisos.every((p) => selectedPermisoIds.has(p.id));
      setSelectedPermisoIds((prev) => {
        const next = new Set(prev);
        for (const p of permisos) {
          if (allSelected) next.delete(p.id);
          else next.add(p.id);
        }
        return next;
      });
    },
    [selectedPermisoIds],
  );

  // Check maestro: marca/desmarca TODOS los permisos de todos los módulos a la vez.
  const allPermisoIds = useMemo(
    () => (permisosCatalog ?? []).map((p) => p.id),
    [permisosCatalog],
  );
  const allPermisosSelected = allPermisoIds.length > 0 && allPermisoIds.every((id) => selectedPermisoIds.has(id));
  const somePermisosSelected = allPermisoIds.some((id) => selectedPermisoIds.has(id));
  const toggleAllPermisos = useCallback(() => {
    setSelectedPermisoIds((prev) => {
      const allSelected = allPermisoIds.length > 0 && allPermisoIds.every((id) => prev.has(id));
      return allSelected ? new Set<string>() : new Set(allPermisoIds);
    });
  }, [allPermisoIds]);

  // ── Handlers ───────────────────────────────────────────────────────────────

  function onValidSubmit(data: RolFormData) {
    if (modal === 'crear') {
      crearMut.mutate(data);
    } else if (modal === 'editar' && selected) {
      actualizarMut.mutate({ id: selected.id, body: data });
    }
  }

  function openCrear() {
    if (!canCreateRole) return;
    form.reset({ nombre: '', descripcion: '' });
    setSelected(null);
    setSelectedPermisoIds(new Set());
    setExpandedGroups(new Set());
    permisosInitialisedRef.current = false;
    setModal('crear');
  }

  function openEditar(d: RolDto) {
    if (!canEditRole) return;
    form.reset({ nombre: d.nombre, descripcion: d.descripcion ?? '' });
    setSelected(d);
    setSelectedPermisoIds(new Set());
    setExpandedGroups(new Set());
    permisosInitialisedRef.current = false;
    setModal('editar');
  }

  function confirmEliminar(d: RolDto) {
    setDeletingRole(d);
  }

  const isSaving = crearMut.isPending || actualizarMut.isPending || assignPermisosMut.isPending;

  // ── Derived data ──────────────────────────────────────────────────────────

  const systemRoles = useMemo(() => data?.filter((r) => r.esSistema) ?? [], [data]);
  const customRoles = useMemo(() => data?.filter((r) => !r.esSistema) ?? [], [data]);

  const q = search.trim().toLowerCase();
  const matchesSearch = (r: RolDto) =>
    !q || r.nombre.toLowerCase().includes(q) || (r.descripcion ?? '').toLowerCase().includes(q);
  const filteredSystemRoles = useMemo(() => systemRoles.filter(matchesSearch), [systemRoles, q]);
  const filteredCustomRoles = useMemo(() => customRoles.filter(matchesSearch), [customRoles, q]);

  // Client-side pagination state per table (RoleTable is re-created each render,
  // so the state must live here in the parent).
  const [paginaSystem, setPaginaSystem] = useState(1);
  const [tamanoSystem, setTamanoSystem] = useState(20);
  const [paginaCustom, setPaginaCustom] = useState(1);
  const [tamanoCustom, setTamanoCustom] = useState(20);

  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(filteredSystemRoles.length / tamanoSystem));
    setPaginaSystem((current) => Math.min(current, maxPagina));
  }, [filteredSystemRoles.length, tamanoSystem]);

  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(filteredCustomRoles.length / tamanoCustom));
    setPaginaCustom((current) => Math.min(current, maxPagina));
  }, [filteredCustomRoles.length, tamanoCustom]);

  // ── Helper: format permissions list for display ───────────────────────

  function PermisosCell({ rol }: { rol: RolDto }) {
    const perms = rol.permisos;
    const isExpanded = permisosExpanded.has(rol.id);
    const canExpand = perms && perms.length > 5;

    if (!perms || perms.length === 0) {
      return (
        <span className="text-xs text-gray-400 italic">
          {rol.esSistema ? 'Sin permisos asignados' : 'Seleccionar al editar'}
        </span>
      );
    }

    const displayPerms = canExpand && !isExpanded ? perms.slice(0, 5) : perms;
    const extraCount = perms.length - displayPerms.length;

    return (
      <div className="text-xs text-gray-600 space-y-0.5">
        {displayPerms.map((p) => (
          <span key={p.id} className="block truncate max-w-[240px]" title={p.nombre}>
            {p.descripcion ?? p.nombre}
          </span>
        ))}
        {canExpand && (
          <button
            type="button"
            onClick={() =>
              setPermisosExpanded((prev) => {
                const next = new Set(prev);
                if (next.has(rol.id)) next.delete(rol.id);
                else next.add(rol.id);
                return next;
              })
            }
            className="text-blue-600 hover:underline mt-0.5"
          >
            {isExpanded ? 'Mostrar menos' : `+${extraCount} más`}
          </button>
        )}
        <span className="block text-gray-400 mt-0.5">
          {perms.length} permiso{perms.length !== 1 ? 's' : ''}
        </span>
      </div>
    );
  }

  function RoleTable({
    roles,
    label,
    pagina,
    tamanoPagina,
    onPaginaChange,
    onTamanoPaginaChange,
  }: {
    roles: RolDto[];
    label: string;
    pagina: number;
    tamanoPagina: number;
    onPaginaChange: (pagina: number) => void;
    onTamanoPaginaChange: (tamano: number) => void;
  }) {
    if (roles.length === 0) return null;

    const totalPaginas = Math.max(1, Math.ceil(roles.length / tamanoPagina));
    const pagedRoles = roles.slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina);

    return (
      <div className="mb-6">
        <h3 className="text-sm font-semibold text-gray-700 mb-2">{label}</h3>
        <div className="overflow-x-auto rounded border border-gray-200">
          <table className="min-w-full text-sm">
            <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
              <tr>
                <th className="px-4 py-2 text-left">Nombre</th>
                <th className="px-4 py-2 text-left">Descripción</th>
                <th className="px-4 py-2 text-left">Permisos</th>
                <th className="px-4 py-2 text-left">Acciones</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {pagedRoles.map((r) => (
                <tr key={r.id} className="hover:bg-gray-50">
                  <td className="px-4 py-2 font-medium text-gray-800">
                    <span className="flex items-center gap-2">
                      {r.nombre}
                      {r.esSistema ? (
                        <span className="inline-block px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-yellow-100 text-yellow-700">
                          Sistema
                        </span>
                      ) : (
                        <span className="inline-block px-1.5 py-0.5 rounded-full text-[10px] font-medium bg-blue-100 text-blue-700">
                          Personalizado
                        </span>
                      )}
                    </span>
                  </td>
                  <td className="px-4 py-2 text-gray-500">{r.descripcion ?? '—'}</td>
                  <td className="px-4 py-2">
                    <PermisosCell rol={r} />
                  </td>
                  <td className="px-4 py-2 flex gap-1">
                    {canEditRole && (
                      <IconButton
                        name="edit"
                        tooltip="Editar"
                        appearance="admin"
                        onClick={() => openEditar(r)}
                      />
                    )}
                    {canDeleteRole && (
                      r.esSistema ? (
                        <IconButton
                          name="trash"
                          tooltip="Eliminar"
                          variant="danger"
                          appearance="admin"
                          disabled
                          disabledTooltip="No se puede eliminar un rol del sistema"
                          onClick={() => {}}
                        />
                      ) : (
                        <IconButton
                          name="trash"
                          tooltip="Eliminar"
                          variant="danger"
                          appearance="admin"
                          onClick={() => confirmEliminar(r)}
                        />
                      )
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination
            pagina={pagina}
            totalPaginas={totalPaginas}
            totalItems={roles.length}
            tamanoPagina={tamanoPagina}
            onChange={onPaginaChange}
            onTamanoPaginaChange={(tamano) => { onTamanoPaginaChange(tamano); onPaginaChange(1); }}
          />
        </div>
      </div>
    );
  }

  // ── Render ─────────────────────────────────────────────────────────────────

  return (
    <div>
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-gray-800">Roles</h2>
        {canCreateRole && (
          <Button size="sm" onClick={openCrear}>
            + Nuevo Rol
          </Button>
        )}
      </div>

      {isLoading && (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      )}

      {isError && (
        <p className="text-red-600 text-sm">No se pudieron cargar los roles.</p>
      )}

      {data && (
        <>
          <div className="mb-4">
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Buscar rol por nombre o descripción"
              className="w-full max-w-sm rounded border border-gray-300 px-3 py-2 text-sm"
            />
          </div>
          <RoleTable
            roles={filteredSystemRoles}
            label="Roles del Sistema"
            pagina={paginaSystem}
            tamanoPagina={tamanoSystem}
            onPaginaChange={setPaginaSystem}
            onTamanoPaginaChange={setTamanoSystem}
          />
          <RoleTable
            roles={filteredCustomRoles}
            label="Roles Personalizados"
            pagina={paginaCustom}
            tamanoPagina={tamanoCustom}
            onPaginaChange={setPaginaCustom}
            onTamanoPaginaChange={setTamanoCustom}
          />
          {filteredSystemRoles.length === 0 && filteredCustomRoles.length === 0 && (
            <p className="rounded border border-dashed border-gray-300 bg-white px-6 py-10 text-center text-sm text-gray-500">
              {q ? 'No se encontraron roles para la búsqueda.' : 'No hay roles registrados.'}
            </p>
          )}
        </>
      )}

      <ModalDialog
        open={modal !== null}
        title={modal === 'crear' ? 'Nuevo Rol' : `Editar Rol: ${selected?.nombre ?? ''}`}
        onClose={() => setModal(null)}
        size="lg"
        footer={(
          <>
            <Button type="button" variant="secondary" onClick={() => setModal(null)}>
              Cancelar
            </Button>
            <Button type="submit" form="admin-role-form" loading={isSaving}>
              {isSaving ? 'Guardando...' : 'Guardar'}
            </Button>
          </>
        )}
      >
        {modal && (
          <form id="admin-role-form" onSubmit={form.handleSubmit(onValidSubmit)} className="space-y-3">
              <FormField label="Nombre" error={form.formState.errors.nombre?.message}>
                <input
                  {...form.register('nombre')}
                  placeholder="Nombre del rol"
                  maxLength={100}
                  className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
                />
              </FormField>
              <FormField label="Descripción" error={form.formState.errors.descripcion?.message}>
                <input
                  {...form.register('descripcion')}
                  placeholder="Descripción opcional"
                  maxLength={500}
                  className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
                />
              </FormField>

              {/* ── Permissions panel ── */}
              {(modal === 'editar' || modal === 'crear') && canManageRolePermissions && (
                <div className="border border-gray-200 rounded-md">
                  <div className="px-3 py-2 bg-gray-50 border-b border-gray-200 rounded-t-md flex items-center justify-between">
                    <span className="text-xs font-semibold text-gray-600 uppercase tracking-wider">
                      Permisos
                    </span>
                    <label className="flex items-center gap-2 text-xs font-medium text-gray-500 cursor-pointer">
                      <span>Seleccionar todos</span>
                      <GroupCheckbox
                        ariaLabel="Seleccionar todos los permisos de todos los módulos"
                        checked={allPermisosSelected}
                        indeterminate={!allPermisosSelected && somePermisosSelected}
                        onChange={toggleAllPermisos}
                      />
                    </label>
                  </div>

                  {catalogLoading || (modal === 'editar' && rolePermisosLoading) ? (
                    <div className="flex justify-center py-6"><Spinner size="md" /></div>
                  ) : (
                    <div className="divide-y divide-gray-100 max-h-72 overflow-y-auto">
                      {Object.entries(groupedPermisos).map(([grupo, perms]) => {
                        const isExpanded = expandedGroups.has(grupo);
                        const allSelected = perms.every((p) => selectedPermisoIds.has(p.id));
                        const someSelected = perms.some((p) => selectedPermisoIds.has(p.id));

                        return (
                          <div key={grupo}>
                            {/* Group header */}
                            <button
                              type="button"
                              onClick={() => toggleGroup(grupo)}
                              className="flex items-center justify-between w-full px-3 py-2 text-left hover:bg-gray-50 text-sm font-medium text-gray-700"
                            >
                              <span className="flex items-center gap-2">
                                <span className="text-xs transition-transform">
                                  {isExpanded ? '▼' : '▶'}
                                </span>
                                <span>{grupoLabel(grupo)}</span>
                                <span className="text-xs text-gray-400">({perms.length})</span>
                              </span>
                              <GroupCheckbox
                                ariaLabel={`Seleccionar todos los permisos de ${grupoLabel(grupo)}`}
                                checked={allSelected}
                                indeterminate={!allSelected && someSelected}
                                onChange={() => toggleGroupAll(grupo, perms)}
                              />
                            </button>

                            {/* Permissions within group */}
                            {isExpanded && (
                              <div className="px-6 pb-2 space-y-1">
                                {perms.map((perm) => (
                                  <label
                                    key={perm.id}
                                    className="flex items-center gap-2 py-1 cursor-pointer group"
                                  >
                                    <input
                                      type="checkbox"
                                      checked={selectedPermisoIds.has(perm.id)}
                                      onChange={() => togglePermiso(perm.id)}
                                      className="h-4 w-4 rounded border-gray-300 text-blue-700 focus:ring-2 focus:ring-blue-500"
                                    />
                                    <span className="text-sm text-gray-700 group-hover:text-gray-900">
                                      {perm.descripcion ?? perm.nombre}
                                    </span>
                                    {/* Raw key on hover, for admins that grep the codebase */}
                                    {perm.descripcion && (
                                      <span className="text-xs text-gray-400 ml-1 hidden group-hover:inline">
                                        — {perm.nombre}
                                      </span>
                                    )}
                                  </label>
                                ))}
                              </div>
                            )}
                          </div>
                        );
                      })}
                    </div>
                  )}
                </div>
              )}

          </form>
        )}
      </ModalDialog>

      <ModalDialog
        open={deletingRole !== null}
        title="Eliminar rol"
        onClose={() => setDeletingRole(null)}
        footer={(
          <>
            <Button variant="secondary" onClick={() => setDeletingRole(null)}>Cancelar</Button>
            <Button
              variant="danger"
              loading={eliminarMut.isPending}
              onClick={() => {
                if (deletingRole) {
                  eliminarMut.mutate(deletingRole.id);
                }
              }}
            >
              Eliminar rol
            </Button>
          </>
        )}
      >
        <p className="text-sm text-gray-600">
          Está por eliminarse el rol <strong>{deletingRole?.nombre}</strong>. Si está en uso o tiene restricciones,
          la operación puede no estar permitida.
        </p>
      </ModalDialog>
    </div>
  );
}
