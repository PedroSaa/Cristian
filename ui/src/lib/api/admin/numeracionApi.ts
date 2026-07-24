import http from '../../http';

export interface CounterListDto {
  id: string;
  codigoContador: string;
  orgDepCod: string | null;
  nivelCod: string | null;
  tipoCod: number;
  dfTipo: string | null;
  periodicidad: string;
  periodoRef: string | null;
  ultimoValor: number;
  activo: boolean;
}

export interface CounterDto extends CounterListDto {
  createdAt: string;
  updatedAt: string;
}

export interface PagedCounterResult {
  items: CounterListDto[];
  total: number;
  page: number;
  totalPaginas: number;
}

export interface CreateCounterData {
  codigoContador: string;
  orgDepCod: string;
  tipoCod?: number;
  dfTipo?: string;
  nivelCod?: string;
  periodicidad?: string;
  valorInicial?: number;
}

export interface SetCounterValueData {
  valor: number;
}

export interface NextValueResult {
  valor: number;
}

export async function listCounters(params?: {
  page?: number;
  pageSize?: number;
  activo?: boolean;
  codigoContador?: string;
  orgDepCod?: string;
}): Promise<PagedCounterResult> {
  const searchParams = new URLSearchParams();
  if (params?.page) searchParams.set('page', String(params.page));
  if (params?.pageSize) searchParams.set('pageSize', String(params.pageSize));
  if (params?.activo !== undefined) searchParams.set('activo', String(params.activo));
  if (params?.codigoContador) searchParams.set('codigoContador', params.codigoContador);
  if (params?.orgDepCod) searchParams.set('orgDepCod', params.orgDepCod);

  const qs = searchParams.toString();
  const { data } = await http.get<PagedCounterResult>(`/admin/numeracion/contadores${qs ? `?${qs}` : ''}`);
  return data;
}

export async function getCounter(id: string): Promise<CounterDto> {
  const { data } = await http.get<CounterDto>(`/admin/numeracion/contadores/${id}`);
  return data;
}

export async function createCounter(body: CreateCounterData): Promise<CounterDto> {
  const { data } = await http.post<CounterDto>('/admin/numeracion/contadores', body);
  return data;
}

export async function setCounterValue(id: string, valor: number): Promise<CounterDto> {
  const { data } = await http.put<CounterDto>(`/admin/numeracion/contadores/${id}/valor`, { valor });
  return data;
}

export async function incrementCounter(id: string): Promise<NextValueResult> {
  const { data } = await http.post<NextValueResult>(`/admin/numeracion/contadores/${id}/incrementar`);
  return data;
}

export async function deactivateCounter(id: string): Promise<void> {
  await http.delete(`/admin/numeracion/contadores/${id}`);
}

export async function reactivateCounter(id: string): Promise<CounterDto> {
  const { data } = await http.put<CounterDto>(`/admin/numeracion/contadores/${id}/reactivar`);
  return data;
}
