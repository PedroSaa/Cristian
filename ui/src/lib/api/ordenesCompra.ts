import http from '../http';
import type {
  ActualizarOrdenCompraRequest,
  AgregarAdjuntoOrdenCompraRequest,
  CrearOrdenCompraRequest,
  MercadoPublicoOrden,
  OrdenCompraAdjunto,
  OrdenCompraDto,
  OrdenCompraFilters,
  PaginatedOrdenesCompra,
} from '../../types/ordenCompra';

const BASE = '/ordenes-compra';

// ─── List (paginated + filters) ──────────────────────────────────────────────

export async function listOrdenesCompra(
  params: OrdenCompraFilters,
): Promise<PaginatedOrdenesCompra> {
  const cleanParams: Record<string, string | number | undefined> = {
    estado: params.estado || undefined,
    proveedorId: params.proveedorId || undefined,
    search: params.search || undefined,
    page: params.page,
    pageSize: params.pageSize,
  };
  Object.keys(cleanParams).forEach(
    (k) => cleanParams[k] === undefined && delete cleanParams[k],
  );
  const { data } = await http.get<PaginatedOrdenesCompra>(BASE, {
    params: cleanParams,
  });
  return data;
}

// ─── Detail ──────────────────────────────────────────────────────────────────

export async function getOrdenCompra(id: string): Promise<OrdenCompraDto> {
  const { data } = await http.get<OrdenCompraDto>(`${BASE}/${id}`);
  return data;
}

// ─── PDF (binary) ────────────────────────────────────────────────────────────

export interface DescargaPdf {
  blob: Blob;
  /** File name from the Content-Disposition header, if the backend provides one. */
  fileName: string | null;
}

function extraerNombreArchivo(contentDisposition: unknown): string | null {
  if (typeof contentDisposition !== 'string' || !contentDisposition) return null;
  const utf8 = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition);
  if (utf8) {
    try {
      return decodeURIComponent(utf8[1].trim());
    } catch {
      // Malformed encoding — fall back to the plain filename parameter.
    }
  }
  const plain = /filename="?([^";]+)"?/i.exec(contentDisposition);
  return plain ? plain[1].trim() : null;
}

export async function getOrdenCompraPdf(id: string): Promise<DescargaPdf> {
  const response = await http.get<Blob>(`${BASE}/${id}/pdf`, {
    responseType: 'blob',
  });
  return {
    blob: response.data,
    fileName: extraerNombreArchivo(response.headers['content-disposition']),
  };
}

// ─── Attachment download (binary) ────────────────────────────────────────────

export async function downloadAdjuntoOrdenCompra(
  id: string,
  adjuntoId: string,
): Promise<Blob> {
  const { data } = await http.get<Blob>(
    `${BASE}/${id}/adjuntos/${adjuntoId}/download`,
    { responseType: 'blob' },
  );
  return data;
}

// ─── Create (draft) ──────────────────────────────────────────────────────────

export async function createOrdenCompra(
  req: CrearOrdenCompraRequest,
): Promise<OrdenCompraDto> {
  const { data } = await http.post<OrdenCompraDto>(BASE, req);
  return data;
}

// ─── Update (draft / rejected only) ──────────────────────────────────────────

export async function updateOrdenCompra(
  id: string,
  req: ActualizarOrdenCompraRequest,
): Promise<OrdenCompraDto> {
  const { data } = await http.put<OrdenCompraDto>(`${BASE}/${id}`, req);
  return data;
}

// ─── State transitions ───────────────────────────────────────────────────────

export async function enviarAprobacionOrdenCompra(
  id: string,
): Promise<OrdenCompraDto> {
  const { data } = await http.post<OrdenCompraDto>(
    `${BASE}/${id}/enviar-aprobacion`,
  );
  return data;
}

export async function aprobarOrdenCompra(
  id: string,
  comentario?: string,
): Promise<OrdenCompraDto> {
  const { data } = await http.post<OrdenCompraDto>(`${BASE}/${id}/aprobar`, {
    comentario: comentario || undefined,
  });
  return data;
}

export async function rechazarOrdenCompra(
  id: string,
  comentario: string,
): Promise<OrdenCompraDto> {
  const { data } = await http.post<OrdenCompraDto>(`${BASE}/${id}/rechazar`, {
    comentario,
  });
  return data;
}

export async function marcarEnviadaOrdenCompra(
  id: string,
): Promise<OrdenCompraDto> {
  const { data } = await http.post<OrdenCompraDto>(
    `${BASE}/${id}/marcar-enviada`,
  );
  return data;
}

export async function anularOrdenCompra(
  id: string,
  motivo: string,
): Promise<OrdenCompraDto> {
  const { data } = await http.post<OrdenCompraDto>(`${BASE}/${id}/anular`, {
    motivo,
  });
  return data;
}

// ─── Attachments (base64 payload, max 10 MB) ─────────────────────────────────

export async function agregarAdjuntoOrdenCompra(
  id: string,
  req: AgregarAdjuntoOrdenCompraRequest,
): Promise<OrdenCompraAdjunto> {
  const { data } = await http.post<OrdenCompraAdjunto>(
    `${BASE}/${id}/adjuntos`,
    req,
  );
  return data;
}

export async function eliminarAdjuntoOrdenCompra(
  id: string,
  adjuntoId: string,
): Promise<void> {
  await http.delete(`${BASE}/${id}/adjuntos/${adjuntoId}`);
}

// ─── Mercado Público (ChileCompra) ───────────────────────────────────────────

export async function buscarOrdenMercadoPublico(
  codigo: string,
): Promise<MercadoPublicoOrden> {
  const { data } = await http.get<MercadoPublicoOrden>(
    `${BASE}/mercado-publico/${encodeURIComponent(codigo)}`,
  );
  return data;
}

export async function vincularMercadoPublicoOrdenCompra(
  id: string,
  codigo: string,
): Promise<OrdenCompraDto> {
  const { data } = await http.post<OrdenCompraDto>(
    `${BASE}/${id}/vincular-mercado-publico`,
    { codigo },
  );
  return data;
}

export async function desvincularMercadoPublicoOrdenCompra(
  id: string,
): Promise<OrdenCompraDto> {
  const { data } = await http.delete<OrdenCompraDto>(
    `${BASE}/${id}/vincular-mercado-publico`,
  );
  return data;
}
