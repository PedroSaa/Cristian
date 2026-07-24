import http from '../../http';

export interface RespaldoDto {
  id: string;
  nombre: string;
  fechaCreacion: string;
  tamanioBytes: number;
  estado: string;
  ruta: string;
}

export interface RespaldoConfigDto {
  id: string;
  intervaloMinutos: number;
  habilitado: boolean;
  maxBackupCount: number;
  retentionDays: number;
  outputPath: string;
  timeoutMinutos: number;
  actualizadoEn: string;
}

export async function getRespaldos(): Promise<RespaldoDto[]> {
  const { data } = await http.get<RespaldoDto[]>('/admin/respaldos');
  return data;
}

export async function triggerRespaldo(): Promise<RespaldoDto> {
  const { data } = await http.post<RespaldoDto>('/admin/respaldos/trigger');
  return data;
}

export interface RestoreLogDto {
  id: string;
  respaldoId: string;
  fechaInicio: string;
  fechaFin: string | null;
  estado: string;
  mensajeError: string | null;
}

export async function restoreRespaldo(id: string, confirmName: string): Promise<RestoreLogDto> {
  const { data } = await http.post<RestoreLogDto>(
    `/admin/respaldos/${id}/restore`,
    null,
    { headers: { 'X-Confirm-Restore': confirmName } },
  );
  return data;
}

export async function getRestoreLogs(id: string): Promise<RestoreLogDto[]> {
  const { data } = await http.get<RestoreLogDto[]>(`/admin/respaldos/${id}/restore-logs`);
  return data;
}

export async function downloadRespaldo(id: string): Promise<void> {
  const response = await http.get(`/admin/respaldos/${id}/download`, {
    responseType: 'blob',
  });

  const disposition = response.headers?.['content-disposition'] as string | undefined;
  const filenameMatch = disposition?.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
  const filename = filenameMatch?.[1]?.replace(/['"]/g, '') ?? `respaldo-${id}.sql.gz`;

  const url = URL.createObjectURL(new Blob([response.data]));
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}

export async function getRespaldoConfig(): Promise<RespaldoConfigDto> {
  const { data } = await http.get<RespaldoConfigDto>('/admin/respaldos/config');
  return data;
}

export async function updateRespaldoConfig(
  body: Omit<RespaldoConfigDto, 'id' | 'actualizadoEn'>,
): Promise<RespaldoConfigDto> {
  const { data } = await http.put<RespaldoConfigDto>('/admin/respaldos/config', body);
  return data;
}
