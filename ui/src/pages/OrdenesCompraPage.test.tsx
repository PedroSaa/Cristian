import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/test/utils';
import OrdenesCompraPage, { formatFecha } from './OrdenesCompraPage';
import type { OrdenCompraListItem, PaginatedOrdenesCompra } from '@/types/ordenCompra';

vi.mock('@/lib/api/ordenesCompra', () => ({
  listOrdenesCompra: vi.fn(),
  getOrdenCompra: vi.fn(),
  getOrdenCompraPdf: vi.fn(),
  downloadAdjuntoOrdenCompra: vi.fn(),
  createOrdenCompra: vi.fn(),
  updateOrdenCompra: vi.fn(),
  enviarAprobacionOrdenCompra: vi.fn(),
  aprobarOrdenCompra: vi.fn(),
  rechazarOrdenCompra: vi.fn(),
  marcarEnviadaOrdenCompra: vi.fn(),
  anularOrdenCompra: vi.fn(),
  agregarAdjuntoOrdenCompra: vi.fn(),
  eliminarAdjuntoOrdenCompra: vi.fn(),
  buscarOrdenMercadoPublico: vi.fn(),
  vincularMercadoPublicoOrdenCompra: vi.fn(),
  desvincularMercadoPublicoOrdenCompra: vi.fn(),
}));

vi.mock('@/lib/api/proveedores', () => ({
  listProveedores: vi.fn(),
}));

vi.mock('@/hooks/usePermissions', () => ({
  useHasPermission: () => true,
  usePermissions: () => ({ hasPermission: () => true }),
}));

import {
  buscarOrdenMercadoPublico as mockBuscarOrdenMercadoPublico,
  desvincularMercadoPublicoOrdenCompra as mockDesvincularMercadoPublico,
  eliminarAdjuntoOrdenCompra as mockEliminarAdjunto,
  enviarAprobacionOrdenCompra as mockEnviarAprobacion,
  getOrdenCompra as mockGetOrdenCompra,
  getOrdenCompraPdf as mockGetOrdenCompraPdf,
  listOrdenesCompra as mockListOrdenesCompra,
  marcarEnviadaOrdenCompra as mockMarcarEnviada,
  vincularMercadoPublicoOrdenCompra as mockVincularMercadoPublico,
} from '@/lib/api/ordenesCompra';
import { listProveedores as mockListProveedores } from '@/lib/api/proveedores';
import type { OrdenCompraDto } from '@/types/ordenCompra';

const listItem = (overrides: Partial<OrdenCompraListItem> = {}): OrdenCompraListItem => ({
  id: '00000000-0000-0000-0000-000000000001',
  numero: 'OC-2026-0001',
  proveedorId: '00000000-0000-0000-0000-0000000000aa',
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

const detalle = (overrides: Partial<OrdenCompraDto> = {}): OrdenCompraDto => ({
  id: '00000000-0000-0000-0000-000000000001',
  numero: 'OC-2026-0001',
  proveedorId: '00000000-0000-0000-0000-0000000000aa',
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

const page = (items: OrdenCompraListItem[]): PaginatedOrdenesCompra => ({
  items,
  totalItems: items.length,
  pagina: 1,
  totalPaginas: 1,
});

const proveedoresPage = {
  items: [
    { id: '00000000-0000-0000-0000-0000000000aa', rut: '76.123.456-0', nombre: 'Acme SA', giro: 'Servicios', estado: 'Activo' },
  ],
  totalItems: 1,
  pagina: 1,
  totalPaginas: 1,
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(mockListProveedores).mockResolvedValue(proveedoresPage as never);
});

describe('OrdenesCompraPage', () => {
  it('renders title, paginated list and CLP totals', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(
      page([
        listItem(),
        listItem({ id: '00000000-0000-0000-0000-000000000002', numero: null, estado: 'Borrador', proveedorNombre: 'Servicios Ltda.' }),
      ]),
    );

    renderWithProviders(<OrdenesCompraPage />);

    expect((await screen.findAllByText('Órdenes de Compra')).length).toBeGreaterThan(0);
    expect((await screen.findAllByText('OC-2026-0001')).length).toBeGreaterThan(0);
    expect(screen.getAllByText('Acme SA').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Servicios Ltda.').length).toBeGreaterThan(0);
    // Server-side pagination is wired
    expect((await screen.findAllByText('Filas por página')).length).toBeGreaterThan(0);
  });

  it('opens the create modal with the form fields', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([]));
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);

    await user.click(await screen.findByRole('button', { name: /nueva orden de compra/i }));

    await waitFor(() => {
      expect(screen.getAllByText('Nueva Orden de Compra').length).toBeGreaterThan(0);
    });
    expect(screen.getAllByText('Proveedor').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Fecha').length).toBeGreaterThan(0);
  });

  it('links an order to Mercado Público from the detail modal', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem()]));
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(detalle());
    vi.mocked(mockVincularMercadoPublico).mockResolvedValue(
      detalle({ codigoMercadoPublico: '1123-109-SE13' }),
    );
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getAllByRole('button', { name: 'Ver' })[0]);

    expect((await screen.findAllByText('Mercado Público')).length).toBeGreaterThan(0);

    const codigoInput = screen.getByLabelText('Código OC del portal');
    await user.type(codigoInput, '1123-109-SE13');
    await user.click(screen.getByRole('button', { name: 'Vincular' }));

    await waitFor(() => {
      expect(vi.mocked(mockVincularMercadoPublico)).toHaveBeenCalledWith(
        '00000000-0000-0000-0000-000000000001',
        '1123-109-SE13',
      );
    });
  });

  it('shows the linked code and queries the portal from the detail modal', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(
      page([listItem({ codigoMercadoPublico: '1123-109-SE13' })]),
    );
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(
      detalle({ codigoMercadoPublico: '1123-109-SE13' }),
    );
    vi.mocked(mockBuscarOrdenMercadoPublico).mockResolvedValue({
      codigo: '1123-109-SE13',
      nombre: 'Mantención Áreas verdes',
      estado: 'Aceptada',
      fechaCreacion: '2013-07-05T12:59:15.443',
      compradorNombre: 'INDAP',
      compradorRut: '61.307.000-1',
      proveedorNombre: 'Proveedora SA',
      proveedorRut: '7.445.387-2',
      montoTotal: 110908,
      items: [],
    });
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    // The MP code is visible in the grid, under the number
    expect(screen.getAllByText(/MP 1123-109-SE13/).length).toBeGreaterThan(0);

    await user.click(screen.getAllByRole('button', { name: 'Ver' })[0]);

    await user.click(await screen.findByRole('button', { name: 'Consultar en portal' }));

    await waitFor(() => {
      expect(vi.mocked(mockBuscarOrdenMercadoPublico)).toHaveBeenCalledWith('1123-109-SE13');
    });
    expect((await screen.findAllByText('Aceptada')).length).toBeGreaterThan(0);
    expect(screen.getAllByText('INDAP').length).toBeGreaterThan(0);
    // A linked order offers "Desvincular" instead of the link input
    expect(screen.getByRole('button', { name: 'Desvincular' })).toBeInTheDocument();
  });

  it('requests the list with the selected estado filter', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem()]));
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    const estadoSelect = screen.getByLabelText('Filtrar por estado');
    await user.selectOptions(estadoSelect, 'Aprobada');

    await waitFor(() => {
      expect(vi.mocked(mockListOrdenesCompra)).toHaveBeenCalledWith(
        expect.objectContaining({ estado: 'Aprobada' }),
      );
    });
  });

  // ── F1: double-click guard on modal-less actions ──────────────────────────

  it('fires a single request when "Marcar enviada" is double clicked', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem()])); // estado Aprobada
    let resolveMarcar: (value: OrdenCompraDto) => void = () => {};
    vi.mocked(mockMarcarEnviada).mockImplementation(
      () => new Promise<OrdenCompraDto>((resolve) => { resolveMarcar = resolve; }),
    );
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getAllByRole('button', { name: 'Marcar enviada' })[0]);
    await user.click(screen.getAllByRole('button', { name: 'Marcar enviada' })[0]);

    expect(vi.mocked(mockMarcarEnviada)).toHaveBeenCalledTimes(1);
    resolveMarcar(detalle({ estado: 'Enviada' }));
  });

  // ── F2: ConfirmDialog replaces window.confirm ─────────────────────────────

  it('sends to approval only after confirming in the dialog', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem({ estado: 'Borrador' })]));
    vi.mocked(mockEnviarAprobacion).mockResolvedValue(detalle({ estado: 'PendienteAprobacion' }));
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getAllByRole('button', { name: 'Enviar a aprobación' })[0]);

    expect(vi.mocked(mockEnviarAprobacion)).not.toHaveBeenCalled();
    expect(
      await screen.findByText('¿Enviar la orden de compra OC-2026-0001 a aprobación?'),
    ).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Enviar' }));

    await waitFor(() => {
      expect(vi.mocked(mockEnviarAprobacion)).toHaveBeenCalledWith(
        '00000000-0000-0000-0000-000000000001',
      );
    });
  });

  it('deletes an attachment only after confirming in the dialog', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem()]));
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(
      detalle({
        adjuntos: [
          {
            id: '00000000-0000-0000-0000-0000000000cc',
            nombreArchivo: 'factura.pdf',
            contentType: 'application/pdf',
            tamano: 2048,
            subidoPor: '00000000-0000-0000-0000-0000000000bb',
            creadoEn: '2026-07-01T12:00:00Z',
          },
        ],
      }),
    );
    vi.mocked(mockEliminarAdjunto).mockResolvedValue(undefined);
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getAllByRole('button', { name: 'Ver' })[0]);
    await user.click(await screen.findByRole('button', { name: 'Eliminar' }));

    expect(vi.mocked(mockEliminarAdjunto)).not.toHaveBeenCalled();
    expect(await screen.findByText('Eliminar adjunto')).toBeInTheDocument();
    expect(screen.getByText('¿Eliminar el adjunto factura.pdf?')).toBeInTheDocument();

    const confirmDialog = screen.getAllByRole('dialog').at(-1)!;
    await user.click(within(confirmDialog).getByRole('button', { name: 'Eliminar' }));

    await waitFor(() => {
      expect(vi.mocked(mockEliminarAdjunto)).toHaveBeenCalledWith(
        '00000000-0000-0000-0000-000000000001',
        '00000000-0000-0000-0000-0000000000cc',
      );
    });
  });

  it('unlinks from Mercado Público only after confirming in the dialog', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(
      page([listItem({ codigoMercadoPublico: '1123-109-SE13' })]),
    );
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(
      detalle({ codigoMercadoPublico: '1123-109-SE13' }),
    );
    vi.mocked(mockDesvincularMercadoPublico).mockResolvedValue(detalle());
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getAllByRole('button', { name: 'Ver' })[0]);
    await user.click(await screen.findByRole('button', { name: 'Desvincular' }));

    expect(vi.mocked(mockDesvincularMercadoPublico)).not.toHaveBeenCalled();
    expect(await screen.findByText('Desvincular de Mercado Público')).toBeInTheDocument();

    const confirmDialog = screen.getAllByRole('dialog').at(-1)!;
    await user.click(within(confirmDialog).getByRole('button', { name: 'Desvincular' }));

    await waitFor(() => {
      expect(vi.mocked(mockDesvincularMercadoPublico)).toHaveBeenCalledWith(
        '00000000-0000-0000-0000-000000000001',
      );
    });
  });

  // ── F3: empty price/quantity must not pass validation as 0 ────────────────

  it('disables Crear when an item price is empty', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([]));
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await user.click(await screen.findByRole('button', { name: /nueva orden de compra/i }));

    const proveedorSelect = screen.getByLabelText('Proveedor');
    await within(proveedorSelect).findByRole('option', { name: 'Acme SA' });
    await user.selectOptions(proveedorSelect, '00000000-0000-0000-0000-0000000000aa');
    await user.type(screen.getByLabelText('Descripción ítem 1'), 'Servicio de aseo');

    const precioInput = screen.getByLabelText('Precio unitario ítem 1');
    await user.clear(precioInput);
    expect(screen.getByRole('button', { name: 'Crear' })).toBeDisabled();

    await user.type(precioInput, '1000');
    expect(screen.getByRole('button', { name: 'Crear' })).toBeEnabled();
  });

  // ── F4: PDF downloads via <a download>, not window.open ───────────────────

  it('downloads the PDF through an anchor instead of window.open', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem()]));
    vi.mocked(mockGetOrdenCompraPdf).mockResolvedValue({
      blob: new Blob(['%PDF-1.7'], { type: 'application/pdf' }),
      fileName: null,
    });

    const originalCreateObjectURL = URL.createObjectURL;
    const originalRevokeObjectURL = URL.revokeObjectURL;
    URL.createObjectURL = vi.fn(() => 'blob:mock-url');
    URL.revokeObjectURL = vi.fn();
    const openSpy = vi.spyOn(window, 'open').mockReturnValue(null);
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => {});

    try {
      const user = userEvent.setup();
      renderWithProviders(<OrdenesCompraPage />);
      await screen.findAllByText('OC-2026-0001');

      await user.click(screen.getAllByRole('button', { name: 'Descargar PDF' })[0]);

      await waitFor(() => expect(clickSpy).toHaveBeenCalledTimes(1));
      expect(openSpy).not.toHaveBeenCalled();
      const anchor = clickSpy.mock.contexts[0] as HTMLAnchorElement;
      expect(anchor.download).toBe('orden-compra-OC-2026-0001.pdf');
    } finally {
      openSpy.mockRestore();
      clickSpy.mockRestore();
      URL.createObjectURL = originalCreateObjectURL;
      URL.revokeObjectURL = originalRevokeObjectURL;
    }
  });

  // ── F5: debounced search ───────────────────────────────────────────────────

  it('debounces the search filter so fast typing fires a single request', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem()]));
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');
    vi.mocked(mockListOrdenesCompra).mockClear();

    await user.type(screen.getByLabelText('Búsqueda'), 'acme');

    await waitFor(() => {
      expect(vi.mocked(mockListOrdenesCompra)).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'acme' }),
      );
    });
    expect(vi.mocked(mockListOrdenesCompra)).not.toHaveBeenCalledWith(
      expect.objectContaining({ search: 'a' }),
    );
    expect(vi.mocked(mockListOrdenesCompra)).not.toHaveBeenCalledWith(
      expect.objectContaining({ search: 'ac' }),
    );
    expect(vi.mocked(mockListOrdenesCompra)).not.toHaveBeenCalledWith(
      expect.objectContaining({ search: 'acm' }),
    );
  });

  // ── F6: provider selector loads every page (backend caps pageSize at 100) ──

  it('loads all provider pages for the selector', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([]));
    const proveedorA = { id: '00000000-0000-0000-0000-0000000000aa', rut: '76.123.456-0', nombre: 'Acme SA', giro: 'Servicios', estado: 'Activo' };
    const proveedorB = { id: '00000000-0000-0000-0000-0000000000ab', rut: '77.987.654-3', nombre: 'Beta Ltda.', giro: 'Insumos', estado: 'Activo' };
    vi.mocked(mockListProveedores).mockImplementation(async (params) =>
      params.page === 1
        ? { items: [proveedorA], totalItems: 2, pagina: 1, totalPaginas: 2 }
        : { items: [proveedorB], totalItems: 2, pagina: 2, totalPaginas: 2 },
    );

    renderWithProviders(<OrdenesCompraPage />);

    const proveedorSelect = await screen.findByLabelText('Filtrar por proveedor');
    await waitFor(() => {
      expect(within(proveedorSelect).getByRole('option', { name: 'Acme SA' })).toBeInTheDocument();
      expect(within(proveedorSelect).getByRole('option', { name: 'Beta Ltda.' })).toBeInTheDocument();
    });
    expect(vi.mocked(mockListProveedores)).toHaveBeenCalledWith(
      expect.objectContaining({ page: 2, pageSize: 100 }),
    );
  });
});

// ── Inline validation messages (touched / submit-attempt gated) ─────────────

describe('OrdenesCompraPage — inline validation', () => {
  const abrirFormulario = async (user: ReturnType<typeof userEvent.setup>) => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([]));
    renderWithProviders(<OrdenesCompraPage />);
    await user.click(await screen.findByRole('button', { name: /nueva orden de compra/i }));
    await waitFor(() => {
      expect(screen.getAllByText('Nueva Orden de Compra').length).toBeGreaterThan(0);
    });
  };

  it('does not show validation messages before the fields are touched', async () => {
    const user = userEvent.setup();
    await abrirFormulario(user);

    expect(screen.queryByText('Seleccione un proveedor.')).not.toBeInTheDocument();
    expect(screen.queryByText('La fecha es obligatoria.')).not.toBeInTheDocument();
    expect(screen.queryByText('La descripción es obligatoria.')).not.toBeInTheDocument();
    expect(screen.queryByText('Ingrese una cantidad mayor que cero.')).not.toBeInTheDocument();
    expect(screen.queryByText('Ingrese un precio válido (0 o mayor).')).not.toBeInTheDocument();
  });

  it('shows the price message after blurring an empty price input', async () => {
    const user = userEvent.setup();
    await abrirFormulario(user);

    const precioInput = screen.getByLabelText('Precio unitario ítem 1');
    await user.clear(precioInput);
    expect(screen.queryByText('Ingrese un precio válido (0 o mayor).')).not.toBeInTheDocument();

    await user.tab();

    const mensaje = await screen.findByText('Ingrese un precio válido (0 o mayor).');
    expect(precioInput).toHaveAttribute('aria-invalid', 'true');
    expect(precioInput).toHaveAttribute('aria-describedby', mensaje.id);
  });

  it('shows the quantity message when the quantity is zero', async () => {
    const user = userEvent.setup();
    await abrirFormulario(user);

    const cantidadInput = screen.getByLabelText('Cantidad ítem 1');
    await user.clear(cantidadInput);
    await user.type(cantidadInput, '0');
    await user.tab();

    const mensaje = await screen.findByText('Ingrese una cantidad mayor que cero.');
    expect(cantidadInput).toHaveAttribute('aria-invalid', 'true');
    expect(cantidadInput).toHaveAttribute('aria-describedby', mensaje.id);
  });
});

describe('formatFecha — date-only, timezone-safe', () => {
  it('renders the UTC calendar date regardless of the local timezone', () => {
    // The backend sends midnight UTC; going through `new Date()` in a UTC-4
    // timezone (Chile) would show the previous day (30-06 instead of 01-07).
    expect(formatFecha('2026-07-01T00:00:00.0000000Z')).toBe('01-07-2026');
    expect(formatFecha('2026-12-31T00:00:00Z')).toBe('31-12-2026');
  });

  it('falls back to a dash for malformed values', () => {
    expect(formatFecha('no-es-fecha')).toBe('—');
    expect(formatFecha('')).toBe('—');
  });
});
