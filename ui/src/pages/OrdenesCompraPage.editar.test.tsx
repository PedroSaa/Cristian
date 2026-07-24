import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/test/utils';
import OrdenesCompraPage from './OrdenesCompraPage';
import {
  OC_ID,
  PROVEEDOR_ID,
  detalle,
  item,
  listItem,
  page,
  proveedoresPage,
} from './__tests__/ordenesCompra.fixtures';

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

import {
  getOrdenCompra as mockGetOrdenCompra,
  listOrdenesCompra as mockListOrdenesCompra,
  updateOrdenCompra as mockUpdateOrdenCompra,
} from '@/lib/api/ordenesCompra';
import { listProveedores as mockListProveedores } from '@/lib/api/proveedores';

const borradorConItem = () =>
  detalle({
    estado: 'Borrador',
    formaPago: 'Transferencia a 30 días',
    items: [item({ descripcion: 'Servicio de aseo', cantidad: 2, precioUnitario: 1500 })],
  });

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(mockListProveedores).mockResolvedValue(proveedoresPage as never);
  vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem({ estado: 'Borrador' })]));
  vi.mocked(mockGetOrdenCompra).mockResolvedValue(borradorConItem());
});

// ── C. Edit flow ─────────────────────────────────────────────────────────────

describe('OrdenesCompraPage — edit flow', () => {
  it('opens the edit modal prefilled with the order values', async () => {
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getAllByRole('button', { name: 'Editar' })[0]);

    await waitFor(() => {
      expect(vi.mocked(mockGetOrdenCompra)).toHaveBeenCalledWith(OC_ID);
    });
    expect(await screen.findByText('Editar Orden de Compra OC-2026-0001')).toBeInTheDocument();

    // ordenToForm: dates trimmed to yyyy-MM-dd, item numbers become strings.
    expect(screen.getByLabelText('Proveedor')).toHaveValue(PROVEEDOR_ID);
    expect(screen.getByLabelText('Fecha')).toHaveValue('2026-07-01');
    expect(screen.getByLabelText('Forma de pago')).toHaveValue('Transferencia a 30 días');
    expect(screen.getByLabelText('Descripción ítem 1')).toHaveValue('Servicio de aseo');
    expect(screen.getByLabelText('Cantidad ítem 1')).toHaveValue(2);
    expect(screen.getByLabelText('Precio unitario ítem 1')).toHaveValue(1500);
  });

  it('saves the edit calling updateOrdenCompra with numeric items', async () => {
    vi.mocked(mockUpdateOrdenCompra).mockResolvedValue(borradorConItem());
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getAllByRole('button', { name: 'Editar' })[0]);
    expect(await screen.findByText('Editar Orden de Compra OC-2026-0001')).toBeInTheDocument();

    const precioInput = screen.getByLabelText('Precio unitario ítem 1');
    await user.clear(precioInput);
    await user.type(precioInput, '2000');

    await user.click(screen.getByRole('button', { name: 'Guardar' }));

    await waitFor(() => {
      expect(vi.mocked(mockUpdateOrdenCompra)).toHaveBeenCalledWith(
        OC_ID,
        expect.objectContaining({
          proveedorId: PROVEEDOR_ID,
          fecha: '2026-07-01',
          formaPago: 'Transferencia a 30 días',
          items: [{ descripcion: 'Servicio de aseo', cantidad: 2, precioUnitario: 2000 }],
        }),
      );
    });
    expect(
      await screen.findByText('Orden de compra actualizada correctamente.'),
    ).toBeInTheDocument();
    // The modal closes after a successful save.
    await waitFor(() => {
      expect(screen.queryByText('Editar Orden de Compra OC-2026-0001')).not.toBeInTheDocument();
    });
  });
});
