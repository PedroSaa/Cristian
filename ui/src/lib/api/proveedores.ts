import http from '../http';
import type {
  PaginatedProveedores,
  ProveedorDto,
  ChileProveedorResult,
  CrearProveedorRequest,
  ActualizarProveedorRequest,
  VerificarRequest,
  ProveedorFilters,
} from '../../types/proveedor';

// ─── List (paginated + searchable) ──────────────────────────────────────────

export async function listProveedores(
  params: ProveedorFilters,
): Promise<PaginatedProveedores> {
  const cleanParams: Record<string, string | number | boolean | undefined> = {
    search: params.search || undefined,
    page: params.page,
    pageSize: params.pageSize,
    incluirInactivos: params.incluirInactivos,
  };
  Object.keys(cleanParams).forEach(
    (k) => cleanParams[k] === undefined && delete cleanParams[k],
  );
  const { data } = await http.get<PaginatedProveedores>('/proveedores', {
    params: cleanParams,
  });
  return data;
}

// ─── Detail ─────────────────────────────────────────────────────────────────

export async function getProveedor(id: string): Promise<ProveedorDto> {
  const { data } = await http.get<ProveedorDto>(`/proveedores/${id}`);
  return data;
}

// ─── Create ─────────────────────────────────────────────────────────────────

export async function createProveedor(
  req: CrearProveedorRequest,
): Promise<ProveedorDto> {
  const { data } = await http.post<ProveedorDto>('/proveedores', req);
  return data;
}

// ─── Update ─────────────────────────────────────────────────────────────────

export async function updateProveedor(
  id: string,
  req: ActualizarProveedorRequest,
): Promise<void> {
  await http.put(`/proveedores/${id}`, req);
}

// ─── Activate ───────────────────────────────────────────────────────────────

export async function activateProveedor(id: string): Promise<void> {
  await http.put(`/proveedores/${id}/activar`);
}

// ─── Deactivate ─────────────────────────────────────────────────────────────

export async function deactivateProveedor(id: string): Promise<void> {
  await http.put(`/proveedores/${id}/desactivar`);
}

// ─── ChileProveedor verification ────────────────────────────────────────────

export async function verifyChileProveedor(
  req: VerificarRequest,
): Promise<ChileProveedorResult> {
  const { data } = await http.post<ChileProveedorResult>(
    '/proveedores/verificar',
    req,
  );
  return data;
}
