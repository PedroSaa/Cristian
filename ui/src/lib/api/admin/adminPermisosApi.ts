import http from '../../http';

export interface PermisoDto {
  id: string;
  nombre: string;
  descripcion: string;
  grupo: string;
}

/**
 * GET /api/admin/permisos — Returns the full permissions catalog.
 */
export async function getPermisos(): Promise<PermisoDto[]> {
  const { data } = await http.get<PermisoDto[]>('/admin/permisos');
  return data;
}

/**
 * PUT /api/admin/roles/{rolId}/permisos — Atomically replaces all permissions
 * of a role with the given set of permission IDs.
 */
export async function assignPermisosRol(rolId: string, permisoIds: string[]): Promise<void> {
  await http.put(`/admin/roles/${rolId}/permisos`, { permisoIds });
}
