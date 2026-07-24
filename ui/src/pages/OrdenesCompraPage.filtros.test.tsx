import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/test/utils';
import OrdenesCompraPage from './OrdenesCompraPage';
import { PROVEEDOR_ID, listItem, page, proveedoresPage } from './__tests__/ordenesCompra.fixtures';

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
}));

import { listOrdenesCompra as mockListOrdenesCompra } from '@/lib/api/ordenesCompra';
import { listProveedores as mockListProveedores } from '@/lib/api/proveedores';

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(mockListProveedores).mockResolvedValue(proveedoresPage as never);
});

// ── B. Server-side pagination and filters ────────────────────────────────────

describe('OrdenesCompraPage — server-side pagination', () => {
  it('requests page 2 when clicking "Página siguiente"', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(
      page([listItem()], { totalItems: 60, totalPaginas: 3 }),
    );
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getByRole('button', { name: 'Página siguiente' }));

    await waitFor(() => {
      expect(vi.mocked(mockListOrdenesCompra)).toHaveBeenCalledWith(
        expect.objectContaining({ page: 2, pageSize: 20 }),
      );
    });
  });

  it('requests the new pageSize and resets to page 1 when changing "Filas por página"', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(
      page([listItem()], { totalItems: 60, totalPaginas: 3, pagina: 2 }),
    );
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    // The page-size select is the only control displaying the current size.
    const pageSizeSelect = screen.getByDisplayValue('20');
    await user.selectOptions(pageSizeSelect, '50');

    await waitFor(() => {
      expect(vi.mocked(mockListOrdenesCompra)).toHaveBeenCalledWith(
        expect.objectContaining({ pageSize: 50, page: 1 }),
      );
    });
  });
});

describe('OrdenesCompraPage — filters', () => {
  it('filters by proveedor and resets to the default filters with "Limpiar"', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem()]));
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    const proveedorSelect = screen.getByLabelText('Filtrar por proveedor');
    await screen.findByRole('option', { name: 'Acme SA' });
    await user.selectOptions(proveedorSelect, PROVEEDOR_ID);

    await waitFor(() => {
      expect(vi.mocked(mockListOrdenesCompra)).toHaveBeenCalledWith(
        expect.objectContaining({ proveedorId: PROVEEDOR_ID, page: 1 }),
      );
    });

    await user.click(screen.getByRole('button', { name: 'Limpiar' }));

    await waitFor(() => {
      expect(vi.mocked(mockListOrdenesCompra)).toHaveBeenLastCalledWith({
        page: 1,
        pageSize: 20,
      });
    });
  });
});

// ── E. Grid error state ──────────────────────────────────────────────────────

describe('OrdenesCompraPage — list error state', () => {
  it('shows the error banner when the list request fails', async () => {
    vi.mocked(mockListOrdenesCompra).mockRejectedValue(new Error('network down'));

    renderWithProviders(<OrdenesCompraPage />);

    expect(
      await screen.findByText(/Error al cargar las órdenes de compra/),
    ).toBeInTheDocument();
  });
});
