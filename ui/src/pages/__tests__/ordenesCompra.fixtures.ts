// Shared factories for the Órdenes de Compra page test suite.
// Mirrors the factories in OrdenesCompraPage.test.tsx so the sibling test
// files (acciones, filtros, editar, adjuntos) don't duplicate them.
// NOTE: vi.mock blocks cannot live here (they are hoisted per test file),
// so each test file declares its own module mocks and imports these factories.
import type {
  OrdenCompraAdjunto,
  OrdenCompraDto,
  OrdenCompraItem,
  OrdenCompraListItem,
  PaginatedOrdenesCompra,
} from '@/types/ordenCompra';

export const OC_ID = '00000000-0000-0000-0000-000000000001';
export const PROVEEDOR_ID = '00000000-0000-0000-0000-0000000000aa';
export const ADJUNTO_ID = '00000000-0000-0000-0000-0000000000cc';

export const listItem = (
  overrides: Partial<OrdenCompraListItem> = {},
): OrdenCompraListItem => ({
  id: OC_ID,
  numero: 'OC-2026-0001',
  proveedorId: PROVEEDOR_ID,
  proveedorNombre: 'Acme SA',
  fecha: '2026-07-01T00:00:00Z',
  moneda: 'CLP',
  neto: 100000,
  iva: 19000,
  total: 119000,
  estado: 'Aprobada',
  creadoEn: '2026-07-01T00:00:00Z',
  codigoMercadoPublico: null,
  ...overrides,
});

export const detalle = (overrides: Partial<OrdenCompraDto> = {}): OrdenCompraDto => ({
  id: OC_ID,
  numero: 'OC-2026-0001',
  proveedorId: PROVEEDOR_ID,
  proveedorNombre: 'Acme SA',
  proveedorRut: '76.123.456-0',
  fecha: '2026-07-01T00:00:00Z',
  moneda: 'CLP',
  formaPago: null,
  plazoEntrega: null,
  lugarEntrega: null,
  observaciones: null,
  neto: 100000,
  iva: 19000,
  total: 119000,
  estado: 'Aprobada',
  creadoPor: '00000000-0000-0000-0000-0000000000bb',
  creadoEn: '2026-07-01T00:00:00Z',
  actualizadoEn: '2026-07-01T00:00:00Z',
  aprobadoPor: null,
  aprobadoEn: null,
  comentarioAprobacion: null,
  motivoAnulacion: null,
  items: [],
  adjuntos: [],
  codigoMercadoPublico: null,
  ...overrides,
});

export const item = (overrides: Partial<OrdenCompraItem> = {}): OrdenCompraItem => ({
  id: '00000000-0000-0000-0000-0000000000dd',
  numeroLinea: 1,
  descripcion: 'Servicio de aseo',
  cantidad: 2,
  precioUnitario: 1500,
  totalLinea: 3000,
  ...overrides,
});

export const adjunto = (
  overrides: Partial<OrdenCompraAdjunto> = {},
): OrdenCompraAdjunto => ({
  id: ADJUNTO_ID,
  nombreArchivo: 'factura.pdf',
  contentType: 'application/pdf',
  tamano: 2048,
  subidoPor: '00000000-0000-0000-0000-0000000000bb',
  creadoEn: '2026-07-01T12:00:00Z',
  ...overrides,
});

export const page = (
  items: OrdenCompraListItem[],
  overrides: Partial<PaginatedOrdenesCompra> = {},
): PaginatedOrdenesCompra => ({
  items,
  totalItems: items.length,
  pagina: 1,
  totalPaginas: 1,
  ...overrides,
});

export const proveedoresPage = {
  items: [
    {
      id: PROVEEDOR_ID,
      rut: '76.123.456-0',
      nombre: 'Acme SA',
      giro: 'Servicios',
      estado: 'Activo',
    },
  ],
  totalItems: 1,
  pagina: 1,
  totalPaginas: 1,
};
