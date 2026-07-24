import { type PagedResult } from '../../../types/api';
import http from '../../http';

export interface UsuarioAdminDto {
  id: string;
  nombreCompleto: string;
  email: string;
  rut: string | null;
  rol: string;
  /** Nullable Guid — null means user has no department assigned. */
  departamentoId: string | null;
  departamentoNombre: string | null;
  activo: boolean;
  creadoEn: string;
  rolId: string | null;
  esCuentaPropia?: boolean;
  esUltimoAdminActivo?: boolean;
  usucod?: string | null;
  nombres?: string | null;
  apellidoPaterno?: string | null;
  apellidoMaterno?: string | null;
  telefono?: string | null;
  direccion?: string | null;
  estaBloqueado?: boolean;
  bloqueadoHasta?: string | null;
}

export interface ListUsuariosParams {
  page?: number;
  pageSize?: number;
  rol?: string;
  departamentoId?: string;
  activo?: boolean;
  search?: string;
}

export interface CrearUsuarioData {
  nombres: string;
  apellidoPaterno?: string;
  apellidoMaterno?: string;
  telefono?: string | null;
  direccion?: string | null;
  email: string;
  rut?: string | null;
  rol: string;
  departamentoId?: string | null;
  password: string;
  usucod?: string | null;
}

export interface ActualizarUsuarioData {
  nombres: string;
  apellidoPaterno?: string;
  apellidoMaterno?: string;
  telefono?: string | null;
  direccion?: string | null;
  email?: string;
  rut?: string | null;
  rol?: string;
  departamentoId?: string | null;
}

export interface ResetPasswordData {
  nuevaPassword: string;
}

export async function getUsuarios(
  page = 1,
  pageSize = 20,
  filters: Omit<ListUsuariosParams, 'page' | 'pageSize'> = {},
): Promise<PagedResult<UsuarioAdminDto>> {
  const params: Record<string, unknown> = { page, pageSize };
  if (filters.rol) params.rol = filters.rol;
  if (filters.departamentoId) params.departamentoId = filters.departamentoId;
  if (filters.activo !== undefined) params.activo = filters.activo;
  if (filters.search) params.search = filters.search;

  const { data } = await http.get<PagedResult<UsuarioAdminDto>>('/admin/usuarios', { params });
  return data;
}

export async function getUsuario(id: string): Promise<UsuarioAdminDto> {
  const { data } = await http.get<UsuarioAdminDto>(`/admin/usuarios/${id}`);
  return data;
}

export async function crearUsuario(body: CrearUsuarioData): Promise<UsuarioAdminDto> {
  const { data } = await http.post<UsuarioAdminDto>('/admin/usuarios', body);
  return data;
}

export async function actualizarUsuario(id: string, body: ActualizarUsuarioData): Promise<void> {
  await http.put(`/admin/usuarios/${id}`, body);
}

export async function activarUsuario(id: string): Promise<void> {
  await http.put(`/admin/usuarios/${id}/activar`);
}

export async function desactivarUsuario(id: string): Promise<void> {
  await http.put(`/admin/usuarios/${id}/desactivar`);
}

export async function bloquearUsuario(id: string): Promise<void> {
  await http.put(`/admin/usuarios/${id}/bloquear`);
}

export async function desbloquearUsuario(id: string): Promise<void> {
  await http.put(`/admin/usuarios/${id}/desbloquear`);
}

export async function resetPassword(id: string, body: ResetPasswordData): Promise<void> {
  await http.post(`/admin/usuarios/${id}/reset-password`, body);
}
