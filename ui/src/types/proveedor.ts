// ─── List item (summary for DataGrid) ─────────────────────────────────────────
export interface ProveedorListItem {
  id: string;
  rut: string;
  nombre: string;
  giro: string;
  estado: string;
}

// ─── Full detail DTO ─────────────────────────────────────────────────────────
export interface ProveedorDto {
  id: string;
  rut: string;
  nombre: string;
  giro: string;
  direccion: string;
  telefono: string;
  email: string;
  contacto: string;
  estado: string;
  creadoEn: string;
}

// ─── Paginated response ──────────────────────────────────────────────────────
export interface PaginatedProveedores {
  items: ProveedorListItem[];
  totalItems: number;
  pagina: number;
  totalPaginas: number;
}

// ─── ChileProveedor verification result ──────────────────────────────────────
export interface ChileProveedorResult {
  encontrado: boolean;
  razonSocial?: string | null;
  giro?: string | null;
  direccion?: string | null;
  advertencia?: string | null;
}

// ─── Request DTOs ───────────────────────────────────────────────────────────
export interface CrearProveedorRequest {
  rut: string;
  nombre: string;
  giro: string;
  direccion?: string;
  telefono?: string;
  email?: string;
  contacto?: string;
  verificarChileProveedor?: boolean;
}

export interface ActualizarProveedorRequest {
  nombre?: string;
  giro?: string;
  direccion?: string;
  telefono?: string;
  email?: string;
  contacto?: string;
}

export interface VerificarRequest {
  rut: string;
}

// ─── List filter params ──────────────────────────────────────────────────────
export interface ProveedorFilters {
  search?: string;
  page: number;
  pageSize: number;
  incluirInactivos: boolean;
}

// ─── Form state for create/edit modal ────────────────────────────────────────
export interface ProveedorFormValues {
  rut: string;
  nombre: string;
  giro: string;
  direccion: string;
  telefono: string;
  email: string;
  contacto: string;
}
