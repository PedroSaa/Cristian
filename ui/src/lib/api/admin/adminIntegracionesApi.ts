import http from '../../http';

export interface IntegracionDto {
  id: string;
  nombre: string;
  tipo: string;
  baseUrl: string;
  apiKeyMasked: string;
  activo: boolean;
  settings: Record<string, string>;
}

export interface ActualizarIntegracionData {
  baseUrl: string;
  apiKey?: string;
  activo?: boolean;
  settings?: Record<string, string>;
}

export async function getIntegraciones(): Promise<IntegracionDto[]> {
  const { data } = await http.get<IntegracionDto[]>('/admin/integraciones');
  return data;
}

export async function getIntegracion(id: string): Promise<IntegracionDto> {
  const { data } = await http.get<IntegracionDto>(`/admin/integraciones/${id}`);
  return data;
}

export async function actualizarIntegracion(id: string, body: ActualizarIntegracionData): Promise<IntegracionDto> {
  const { data } = await http.put<IntegracionDto>(`/admin/integraciones/${id}`, body);
  return data;
}

export interface ConexionTestResultDto {
  success: boolean;
  mensaje: string;
  latencyMs: number | null;
}

export async function probarConexion(id: string): Promise<ConexionTestResultDto> {
  const { data } = await http.post<ConexionTestResultDto>(`/admin/integraciones/${id}/test`);
  return data;
}
