import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/test/utils';
import OrdenesCompraPage from './OrdenesCompraPage';
import type { EstadoOrdenCompra } from '@/types/ordenCompra';
import { listItem, page, proveedoresPage } from './__tests__/ordenesCompra.fixtures';

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

// Configurable permissions: each test can revoke a specific permission.
const permisos = vi.hoisted(() => ({
  'ordenescompra.crear': true,
  'ordenescompra.aprobar': true,
  'ordenescompra.anular': true,
}));

vi.mock('@/hooks/usePermissions', () => ({
  useHasPermission: (permiso: string) =>
    permiso in permisos ? permisos[permiso as keyof typeof permisos] : true,
}));

import {
  aprobarOrdenCompra as mockAprobar,
  listOrdenesCompra as mockListOrdenesCompra,
} from '@/lib/api/ordenesCompra';
import { listProveedores as mockListProveedores } from '@/lib/api/proveedores';

const ACTION_NAMES = [
  'Ver',
  'Editar',
  'Enviar a aprobación',
  'Aprobar',
  'Rechazar',
  'Marcar enviada',
  'Descargar PDF',
  'Anular',
] as const;

type ActionName = (typeof ACTION_NAMES)[number];

const renderRows = async (estados: EstadoOrdenCompra[]) => {
  vi.mocked(mockListOrdenesCompra).mockResolvedValue(
    page(
      estados.map((estado, i) =>
        listItem({ id: `00000000-0000-0000-0000-00000000000${i + 1}`, estado }),
      ),
    ),
  );
  renderWithProviders(<OrdenesCompraPage />);
  await screen.findAllByText('OC-2026-0001');
};

const expectActions = (expected: readonly ActionName[]) => {
  for (const name of ACTION_NAMES) {
    const buttons = screen.queryAllByRole('button', { name });
    if (expected.includes(name)) {
      expect(buttons.length, `expected action "${name}" to be visible`).toBeGreaterThan(0);
    } else {
      expect(buttons.length, `expected action "${name}" to be hidden`).toBe(0);
    }
  }
};

beforeEach(() => {
  vi.clearAllMocks();
  permisos['ordenescompra.crear'] = true;
  permisos['ordenescompra.aprobar'] = true;
  permisos['ordenescompra.anular'] = true;
  vi.mocked(mockListProveedores).mockResolvedValue(proveedoresPage as never);
});

// ── A. Estado × permission action matrix ─────────────────────────────────────

describe('OrdenesCompraPage — row actions by estado (full permissions)', () => {
  const matrix: Array<[EstadoOrdenCompra, readonly ActionName[]]> = [
    ['Borrador', ['Ver', 'Editar', 'Enviar a aprobación', 'Anular']],
    ['PendienteAprobacion', ['Ver', 'Aprobar', 'Rechazar', 'Anular']],
    ['Aprobada', ['Ver', 'Marcar enviada', 'Descargar PDF', 'Anular']],
    ['Enviada', ['Ver', 'Descargar PDF', 'Anular']],
    ['Rechazada', ['Ver', 'Editar', 'Enviar a aprobación', 'Anular']],
    ['Anulada', ['Ver']],
  ];

  it.each(matrix)('estado %s shows exactly: %s', async (estado, expected) => {
    await renderRows([estado]);
    expectActions(expected);
  });
});

describe('OrdenesCompraPage — row actions with revoked permissions', () => {
  it('without ordenescompra.crear hides Editar, Enviar a aprobación, Marcar enviada and the create button', async () => {
    permisos['ordenescompra.crear'] = false;
    await renderRows(['Borrador', 'Aprobada']);

    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Enviar a aprobación' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Marcar enviada' })).not.toBeInTheDocument();
    expect(
      screen.queryByRole('button', { name: /nueva orden de compra/i }),
    ).not.toBeInTheDocument();
    // Actions not gated by crear stay visible. DataGrid renders every row
    // twice (mobile cards + desktop table), so counts are 2× the row count.
    expect(screen.getAllByRole('button', { name: 'Ver' })).toHaveLength(4);
    expect(screen.getAllByRole('button', { name: 'Descargar PDF' })).toHaveLength(2);
    expect(screen.getAllByRole('button', { name: 'Anular' })).toHaveLength(4);
  });

  it('without ordenescompra.aprobar hides Aprobar and Rechazar on pending orders', async () => {
    permisos['ordenescompra.aprobar'] = false;
    await renderRows(['PendienteAprobacion']);

    expect(screen.queryByRole('button', { name: 'Aprobar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Rechazar' })).not.toBeInTheDocument();
    // 1 row × 2 renders (mobile cards + desktop table).
    expect(screen.getAllByRole('button', { name: 'Ver' })).toHaveLength(2);
    expect(screen.getAllByRole('button', { name: 'Anular' })).toHaveLength(2);
  });

  it('without ordenescompra.anular hides Anular in every non-anulada state', async () => {
    permisos['ordenescompra.anular'] = false;
    await renderRows(['Borrador', 'PendienteAprobacion', 'Aprobada', 'Enviada', 'Rechazada']);

    expect(screen.queryByRole('button', { name: 'Anular' })).not.toBeInTheDocument();
    // 5 rows × 2 renders (mobile cards + desktop table).
    expect(screen.getAllByRole('button', { name: 'Ver' })).toHaveLength(10);
    expect(screen.getAllByRole('button', { name: 'Marcar enviada' })).toHaveLength(2);
    expect(screen.getAllByRole('button', { name: 'Descargar PDF' })).toHaveLength(4);
  });
});

// ── E. Visible error handling on actions ─────────────────────────────────────

describe('OrdenesCompraPage — action errors surface in the toast', () => {
  it('shows the backend userMessage when approving fails', async () => {
    vi.mocked(mockListOrdenesCompra).mockResolvedValue(
      page([listItem({ estado: 'PendienteAprobacion' })]),
    );
    vi.mocked(mockAprobar).mockRejectedValue({
      userMessage: 'Un usuario no puede aprobar su propia orden de compra.',
    });
    const user = userEvent.setup();

    renderWithProviders(<OrdenesCompraPage />);
    await screen.findAllByText('OC-2026-0001');

    await user.click(screen.getAllByRole('button', { name: 'Aprobar' })[0]);
    expect(await screen.findByText('Aprobar orden de compra')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Confirmar aprobación' }));

    await waitFor(() => {
      expect(vi.mocked(mockAprobar)).toHaveBeenCalledWith(
        '00000000-0000-0000-0000-000000000001',
        undefined,
      );
    });
    expect(
      await screen.findByText('Un usuario no puede aprobar su propia orden de compra.'),
    ).toBeInTheDocument();
    // The action modal stays open so the user can retry or cancel.
    expect(screen.getByText('Aprobar orden de compra')).toBeInTheDocument();
  });
});
