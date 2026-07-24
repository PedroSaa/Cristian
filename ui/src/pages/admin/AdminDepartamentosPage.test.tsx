import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminDepartamentosPage from './AdminDepartamentosPage';
import * as adminDepartamentosApi from '../../lib/api/admin/adminDepartamentosApi';
import { useHasPermission } from '../../hooks/usePermissions';

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock('../../lib/api/admin/adminDepartamentosApi', () => ({
  getDepartamentos: vi.fn(),
  activarDepartamento: vi.fn(),
  desactivarDepartamento: vi.fn(),
  crearDepartamento: vi.fn(),
  actualizarDepartamento: vi.fn(),
  eliminarDepartamento: vi.fn(),
}));

vi.mock('../../hooks/usePermissions', () => ({
  useHasPermission: vi.fn(),
}));

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}

function renderWithProviders() {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <AdminDepartamentosPage />
    </QueryClientProvider>,
  );
}

// ── Fixtures ─────────────────────────────────────────────────────────────────

const mockDepartamentos = [
  {
    id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001',
    nombre: 'Tecnologías de la Información',
    codigo: 'TI',
    activo: true,
    totalUsuarios: 5,
    creadoEn: '2026-01-15T10:00:00Z',
  },
  {
    id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002',
    nombre: 'Recursos Humanos',
    codigo: 'RRHH',
    activo: false,
    totalUsuarios: 0,
    creadoEn: '2026-02-20T14:30:00Z',
  },
];

// ── Tests ────────────────────────────────────────────────────────────────────

describe('AdminDepartamentosPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(adminDepartamentosApi.getDepartamentos).mockResolvedValue(mockDepartamentos);
    vi.mocked(useHasPermission).mockReturnValue(true);
  });

  it('renders department names from API data', async () => {
    renderWithProviders();

    expect(await screen.findByText('Tecnologías de la Información')).toBeInTheDocument();
    expect(screen.getByText('Recursos Humanos')).toBeInTheDocument();
  });

  it('renders delete button for each department row', async () => {
    renderWithProviders();

    // Wait for data to load
    await screen.findByText('Tecnologías de la Información');

    const deleteButtons = screen.getAllByRole('button', { name: /eliminar/i });
    expect(deleteButtons).toHaveLength(2);
  });

  it('shows confirmation dialog before deleting a departamento, then closes on success', async () => {
    const user = userEvent.setup();
    vi.mocked(adminDepartamentosApi.eliminarDepartamento).mockResolvedValue(undefined);

    renderWithProviders();

    // Wait for data to load
    await screen.findByText('Tecnologías de la Información');

    // Click delete on first row
    const deleteButtons = screen.getAllByRole('button', { name: /eliminar/i });
    await user.click(deleteButtons[0]);

    // Confirmation dialog should appear
    expect(screen.getByRole('heading', { name: /eliminar departamento/i })).toBeInTheDocument();
    expect(screen.getByText(/Está por eliminarse el departamento/)).toBeInTheDocument();

    // Confirm deletion
    await user.click(screen.getByRole('button', { name: /^eliminar departamento$/i }));

    await waitFor(() => {
      expect(adminDepartamentosApi.eliminarDepartamento).toHaveBeenCalledWith(
        'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001',
      );
    });

    // On success the confirmation dialog closes (success is now shown via a global toast).
    await waitFor(() => {
      expect(screen.queryByRole('heading', { name: /eliminar departamento/i })).not.toBeInTheDocument();
    });
  });

  describe('adminDepartamentosApi — verb alignment', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('activarDepartamento calls PUT /admin/departamentos/:id/activar', async () => {
    const httpModule = await import('../../lib/http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: undefined });
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: undefined });

    // Use vi.importActual to bypass the hoisted vi.mock at the top of the file
    const api = await vi.importActual<typeof import('../../lib/api/admin/adminDepartamentosApi')>('../../lib/api/admin/adminDepartamentosApi');
    await api.activarDepartamento('dep-id-001');

    // Should use PUT, not POST
    expect(putSpy).toHaveBeenCalledWith('/admin/departamentos/dep-id-001/activar');
    expect(postSpy).not.toHaveBeenCalled();
  });

  it('desactivarDepartamento calls PUT /admin/departamentos/:id/desactivar', async () => {
    const httpModule = await import('../../lib/http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: undefined });
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: undefined });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminDepartamentosApi')>('../../lib/api/admin/adminDepartamentosApi');
    await api.desactivarDepartamento('dep-id-002');

    // Should use PUT, not POST
    expect(putSpy).toHaveBeenCalledWith('/admin/departamentos/dep-id-002/desactivar');
    expect(postSpy).not.toHaveBeenCalled();
  });

  it('eliminarDepartamento calls DELETE /admin/departamentos/:id', async () => {
    const httpModule = await import('../../lib/http');
    const deleteSpy = vi.spyOn(httpModule.default, 'delete').mockResolvedValue({ data: undefined });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminDepartamentosApi')>('../../lib/api/admin/adminDepartamentosApi');
    await api.eliminarDepartamento('dep-id-003');

    expect(deleteSpy).toHaveBeenCalledWith('/admin/departamentos/dep-id-003');
  });
  });
});
