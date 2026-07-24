import http from '../../http';

export interface SeremTipoDto {
  remTipo: string;
  remDesc: string;
  totalSerems: number;
}

export interface SeremDto {
  remCod: string;
  remTipo: string;
  remTipoDesc: string;
  remRutValid: number | null;
  remSector: string | null;
  remNomb: string;
  remComuna: string | null;
  remNro: number | null;
  remEmail: string | null;
  remFax: string | null;
  remRut: string | null;
  remDirec: string | null;
  remTelef: string | null;
  remZip: string | null;
  remRegion: string | null;
  remBlock: string | null;
  remCalle: string | null;
  remCodDocDigital: number | null;
}

export interface CrearSeremTipoData {
  remTipo: string;
  remDesc: string;
}

export interface ActualizarSeremTipoData {
  remDesc: string;
}

export interface CrearSeremData {
  remCod: string;
  remTipo: string;
  remNomb: string;
  remRutValid?: number | null;
  remSector?: string | null;
  remComuna?: string | null;
  remNro?: number | null;
  remEmail?: string | null;
  remFax?: string | null;
  remRut?: string | null;
  remDirec?: string | null;
  remTelef?: string | null;
  remZip?: string | null;
  remRegion?: string | null;
  remBlock?: string | null;
  remCalle?: string | null;
  remCodDocDigital?: number | null;
}

export interface ActualizarSeremData {
  remTipo: string;
  remNomb: string;
  remRutValid?: number | null;
  remSector?: string | null;
  remComuna?: string | null;
  remNro?: number | null;
  remEmail?: string | null;
  remFax?: string | null;
  remRut?: string | null;
  remDirec?: string | null;
  remTelef?: string | null;
  remZip?: string | null;
  remRegion?: string | null;
  remBlock?: string | null;
  remCalle?: string | null;
  remCodDocDigital?: number | null;
}

export async function listSeremTipos(): Promise<SeremTipoDto[]> {
  const { data } = await http.get<SeremTipoDto[]>('/admin/remitentes-legado/tipos');
  return data;
}

export async function createSeremTipo(body: CrearSeremTipoData): Promise<SeremTipoDto> {
  const { data } = await http.post<SeremTipoDto>('/admin/remitentes-legado/tipos', body);
  return data;
}

export async function updateSeremTipo(remTipo: string, body: ActualizarSeremTipoData): Promise<void> {
  await http.put(`/admin/remitentes-legado/tipos/${encodeURIComponent(remTipo)}`, body);
}

export async function deleteSeremTipo(remTipo: string): Promise<void> {
  await http.delete(`/admin/remitentes-legado/tipos/${encodeURIComponent(remTipo)}`);
}

export async function listSerems(remTipo?: string): Promise<SeremDto[]> {
  const params: Record<string, unknown> = {};
  if (remTipo) params.remTipo = remTipo;
  const { data } = await http.get<SeremDto[]>('/admin/remitentes-legado', { params });
  return data;
}

export async function createSerem(body: CrearSeremData): Promise<SeremDto> {
  const { data } = await http.post<SeremDto>('/admin/remitentes-legado', body);
  return data;
}

export async function updateSerem(remCod: string, body: ActualizarSeremData): Promise<void> {
  await http.put(`/admin/remitentes-legado/${encodeURIComponent(remCod)}`, body);
}

export async function deleteSerem(remCod: string): Promise<void> {
  await http.delete(`/admin/remitentes-legado/${encodeURIComponent(remCod)}`);
}
