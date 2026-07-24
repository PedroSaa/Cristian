import http from '../../http';

export interface DepartamentoAdminDto {
  id: string;
  nombre: string;
  codigo: string;
  activo: boolean;
  totalUsuarios: number;
  creadoEn: string;
}

export interface CrearDepartamentoData {
  nombre: string;
  codigo: string;
}

export interface ActualizarDepartamentoData {
  nombre: string;
  codigo: string;
}

export async function getDepartamentos(activo?: boolean): Promise<DepartamentoAdminDto[]> {
  const params: Record<string, unknown> = {};
  if (activo !== undefined) params.activo = activo;
  const { data } = await http.get<DepartamentoAdminDto[]>('/admin/departamentos', { params });
  return data;
}

export async function getDepartamento(id: string): Promise<DepartamentoAdminDto> {
  const { data } = await http.get<DepartamentoAdminDto>(`/admin/departamentos/${id}`);
  return data;
}

export async function crearDepartamento(body: CrearDepartamentoData): Promise<DepartamentoAdminDto> {
  const { data } = await http.post<DepartamentoAdminDto>('/admin/departamentos', body);
  return data;
}

export async function actualizarDepartamento(id: string, body: ActualizarDepartamentoData): Promise<DepartamentoAdminDto> {
  const { data } = await http.put<DepartamentoAdminDto>(`/admin/departamentos/${id}`, body);
  return data;
}

export async function activarDepartamento(id: string): Promise<void> {
  await http.put(`/admin/departamentos/${id}/activar`);
}

export async function desactivarDepartamento(id: string): Promise<void> {
  await http.put(`/admin/departamentos/${id}/desactivar`);
}

export async function eliminarDepartamento(id: string): Promise<void> {
  await http.delete(`/admin/departamentos/${id}`);
}
