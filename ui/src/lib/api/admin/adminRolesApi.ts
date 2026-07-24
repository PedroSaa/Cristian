import http from '../../http';
import type { PermisoDto } from './adminPermisosApi';

export interface RolDto {
  id: string;
  nombre: string;
  descripcion: string | null;
  esSistema: boolean;
  permisos?: PermisoDto[];
}

export interface CrearRolData {
  nombre: string;
  descripcion?: string;
}

export interface ActualizarRolData {
  nombre: string;
  descripcion?: string;
}

export async function getRoles(): Promise<RolDto[]> {
  const { data } = await http.get<RolDto[]>('/admin/roles');
  return data;
}

export async function crearRol(body: CrearRolData): Promise<RolDto> {
  const { data } = await http.post<RolDto>('/admin/roles', body);
  return data;
}

export async function actualizarRol(id: string, body: ActualizarRolData): Promise<RolDto> {
  const { data } = await http.put<RolDto>(`/admin/roles/${id}`, { id, ...body });
  return data;
}

export async function eliminarRol(id: string): Promise<void> {
  await http.delete(`/admin/roles/${id}`);
}

/**
 * GET /api/admin/roles/{rolId}/permisos — Returns the permissions currently
 * assigned to a specific role.
 */
export async function getPermisosRol(rolId: string): Promise<PermisoDto[]> {
  const { data } = await http.get<{ permisos: PermisoDto[] }>(`/admin/roles/${rolId}/permisos`);
  return data.permisos;
}
