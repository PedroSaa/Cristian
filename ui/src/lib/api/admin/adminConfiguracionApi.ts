import http from '../../http';

export interface ConfiguracionDto {
  id: string;
  clave: string;
  valor: string;
  descripcion: string;
  actualizadoEn: string;
  grupo?: string;
  tipo?: string;
  minValue?: number | null;
  maxValue?: number | null;
}

export interface UpsertConfiguracionData {
  clave: string;
  valor: string;
  descripcion?: string;
}

export async function getConfiguraciones(): Promise<ConfiguracionDto[]> {
  const { data } = await http.get<ConfiguracionDto[]>('/admin/configuracion');
  return data;
}

export async function getConfiguracion(clave: string): Promise<ConfiguracionDto> {
  const { data } = await http.get<ConfiguracionDto>(`/admin/configuracion/${clave}`);
  return data;
}

export async function upsertConfiguracion(body: UpsertConfiguracionData): Promise<ConfiguracionDto> {
  const { data } = await http.put<ConfiguracionDto>('/admin/configuracion', body);
  return data;
}
