// ─── Estados (mirror of EstadoOrdenCompra enum) ──────────────────────────────
export type EstadoOrdenCompra =
  | 'Borrador'
  | 'PendienteAprobacion'
  | 'Aprobada'
  | 'Rechazada'
  | 'Enviada'
  | 'Anulada';

export const ESTADOS_ORDEN_COMPRA: readonly EstadoOrdenCompra[] = [
  'Borrador',
  'PendienteAprobacion',
  'Aprobada',
  'Rechazada',
  'Enviada',
  'Anulada',
] as const;

// ─── Item detail (OrdenCompraItemDto) ────────────────────────────────────────
export interface OrdenCompraItem {
  id: string;
  numeroLinea: number;
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
  totalLinea: number;
}

// ─── Item input (create/update payloads) ─────────────────────────────────────
export interface OrdenCompraItemInput {
  descripcion: string;
  cantidad: number;
  precioUnitario: number;
}

// ─── Attachment metadata (OrdenCompraAdjuntoDto — no binary content) ─────────
export interface OrdenCompraAdjunto {
  id: string;
  nombreArchivo: string;
  contentType: string;
  tamano: number;
  subidoPor: string;
  creadoEn: string;
}

// ─── Full detail DTO (OrdenCompraDto) ────────────────────────────────────────
export interface OrdenCompraDto {
  id: string;
  numero: string | null;
  proveedorId: string;
  proveedorNombre: string;
  proveedorRut: string;
  fecha: string;
  moneda: string;
  formaPago: string | null;
  plazoEntrega: string | null;
  lugarEntrega: string | null;
  observaciones: string | null;
  neto: number;
  iva: number;
  total: number;
  estado: EstadoOrdenCompra;
  creadoPor: string;
  creadoEn: string;
  actualizadoEn: string;
  aprobadoPor: string | null;
  aprobadoEn: string | null;
  comentarioAprobacion: string | null;
  motivoAnulacion: string | null;
  items: OrdenCompraItem[];
  adjuntos: OrdenCompraAdjunto[];
  codigoMercadoPublico: string | null;
}

// ─── Summary for list (OrdenCompraListItemDto) ───────────────────────────────
export interface OrdenCompraListItem {
  id: string;
  numero: string | null;
  proveedorId: string;
  proveedorNombre: string;
  fecha: string;
  moneda: string;
  neto: number;
  iva: number;
  total: number;
  estado: EstadoOrdenCompra;
  creadoEn: string;
  codigoMercadoPublico: string | null;
}

// ─── Mercado Público portal order (MercadoPublicoOrdenDto) ──────────────────
export interface MercadoPublicoOrdenItem {
  descripcion: string | null;
  cantidad: number | null;
  precioUnitario: number | null;
}

export interface MercadoPublicoOrden {
  codigo: string;
  nombre: string | null;
  estado: string | null;
  fechaCreacion: string | null;
  compradorNombre: string | null;
  compradorRut: string | null;
  proveedorNombre: string | null;
  proveedorRut: string | null;
  montoTotal: number | null;
  items: MercadoPublicoOrdenItem[];
}

// ─── Paginated response (PaginatedOrdenesCompraResponse) ─────────────────────
export interface PaginatedOrdenesCompra {
  items: OrdenCompraListItem[];
  totalItems: number;
  pagina: number;
  totalPaginas: number;
}

// ─── Request DTOs ────────────────────────────────────────────────────────────
export interface CrearOrdenCompraRequest {
  proveedorId: string;
  fecha: string;
  moneda?: string;
  formaPago?: string;
  plazoEntrega?: string;
  lugarEntrega?: string;
  observaciones?: string;
  items?: OrdenCompraItemInput[];
}

export type ActualizarOrdenCompraRequest = CrearOrdenCompraRequest;

export interface AgregarAdjuntoOrdenCompraRequest {
  nombreArchivo: string;
  contentType: string;
  contenidoBase64: string;
}

// ─── List filter params ──────────────────────────────────────────────────────
export interface OrdenCompraFilters {
  estado?: EstadoOrdenCompra;
  proveedorId?: string;
  search?: string;
  page: number;
  pageSize: number;
}

// ─── Form state for create/edit modal ────────────────────────────────────────
export interface OrdenCompraItemFormValues {
  descripcion: string;
  cantidad: string;
  precioUnitario: string;
}

export interface OrdenCompraFormValues {
  proveedorId: string;
  fecha: string;
  formaPago: string;
  plazoEntrega: string;
  lugarEntrega: string;
  observaciones: string;
  items: OrdenCompraItemFormValues[];
}
