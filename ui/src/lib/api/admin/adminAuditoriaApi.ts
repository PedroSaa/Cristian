import { type PagedResult } from '../../../types/api';
import http from '../../http';

export interface RegistroAuditoriaDto {
  id: string;
  usuarioId: string;
  usuarioNombre?: string | null;
  accion: string;
  entidad: string;
  entidadId: string;
  detalle?: string | null;
  direccionIp?: string | null;
  userAgent?: string | null;
  creadoEn: string;
}

export interface ValoresFiltro {
  acciones: string[];
  entidades: string[];
}

export interface AuditoriaFilters {
  desde?: string;
  hasta?: string;
  entidad?: string;
  usuarioNombre?: string;
  accion?: string;
}

export async function getAuditoria(
  page = 1,
  pageSize = 20,
  filters: AuditoriaFilters = {},
): Promise<PagedResult<RegistroAuditoriaDto>> {
  const params: Record<string, unknown> = { page, pageSize };
  if (filters.desde) params.desde = filters.desde;
  if (filters.hasta) params.hasta = filters.hasta;
  if (filters.entidad) params.entidad = filters.entidad;
  if (filters.usuarioNombre) params.usuarioNombre = filters.usuarioNombre;
  if (filters.accion) params.accion = filters.accion;

  const { data } = await http.get<PagedResult<RegistroAuditoriaDto>>('/admin/auditoria', { params });
  return data;
}

export async function getRegistroAuditoria(id: string): Promise<RegistroAuditoriaDto> {
  const { data } = await http.get<RegistroAuditoriaDto>(`/admin/auditoria/${id}`);
  return data;
}

export async function getValoresFiltro(): Promise<ValoresFiltro> {
  const { data } = await http.get<ValoresFiltro>('/admin/auditoria/valores-filtro');
  return data;
}

export async function exportAuditoria(filters: AuditoriaFilters = {}): Promise<Blob> {
  const params: Record<string, unknown> = {};
  if (filters.desde) params.desde = filters.desde;
  if (filters.hasta) params.hasta = filters.hasta;
  if (filters.entidad) params.entidad = filters.entidad;
  if (filters.usuarioNombre) params.usuarioNombre = filters.usuarioNombre;
  if (filters.accion) params.accion = filters.accion;

  const { data } = await http.get<Blob>('/admin/auditoria/exportar', {
    params,
    responseType: 'blob',
  });

  return data;
}
