import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminMantenedorPlantillasNumeracionPage from './AdminMantenedorPlantillasNumeracionPage';
import * as plantillasApi from '../../lib/api/admin/plantillasNumeracionApi';
import { useHasPermission } from '../../hooks/usePermissions';

vi.mock('../../lib/api/admin/plantillasNumeracionApi', () => ({
  listPlantillasNumeracion: vi.fn(),
  createPlantillaNumeracion: vi.fn(),
  updatePlantillaNumeracion: vi.fn(),
  setPlantillaActiva: vi.fn(),
  deletePlantillaNumeracion: vi.fn(),
  getTokensNumeracion: vi.fn(),
}));

vi.mock('../../hooks/usePermissions', () => ({
  useHasPermission: vi.fn(),
}));

const POLICY = {
  porOrganismo: false,
  porTipoDocumento: false,
  porFormatoDocumento: false,
  periodicidad: 'CONTINUO' as const,
  momentoGeneracion: 'AL_INGRESAR' as const,
  rellenoCeros: 0,
  valorInicial: 0,
};

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
      <AdminMantenedorPlantillasNumeracionPage />
    </QueryClientProvider>,
  );
}

describe('AdminMantenedorPlantillasNumeracionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useHasPermission).mockReturnValue(true);
    // Plantilla inactiva: muestra "Usar esta" y habilita "Eliminar".
    vi.mocked(plantillasApi.listPlantillasNumeracion).mockResolvedValue([
      { id: 1, descripcion: 'Documentos', patron: '{correlativo}/{ano}', activo: false, ...POLICY },
    ]);
    vi.mocked(plantillasApi.getTokensNumeracion).mockResolvedValue([
      { token: '{correlativo}', descripcion: 'Correlativo', ejemplo: '00001' },
      { token: '{ano}', descripcion: 'Año', ejemplo: '2026' },
    ]);
  });

  it('keeps row actions available by accessible label and opens/calls their handlers', async () => {
    const user = userEvent.setup();
    vi.mocked(plantillasApi.setPlantillaActiva).mockResolvedValue(undefined);
    vi.mocked(plantillasApi.deletePlantillaNumeracion).mockResolvedValue(undefined);

    renderWithProviders();

    await screen.findByText('Documentos');
    expect(screen.getByRole('button', { name: 'Editar' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Usar como plantilla activa del sistema' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Eliminar' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Editar' }));
    expect(await screen.findByRole('heading', { name: 'Editar Plantilla' })).toBeInTheDocument();
    expect(screen.getByDisplayValue('Documentos')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Cancelar' }));

    // "Usar esta" define la plantilla como activa del sistema.
    await user.click(screen.getByRole('button', { name: 'Usar como plantilla activa del sistema' }));
    await waitFor(() => expect(vi.mocked(plantillasApi.setPlantillaActiva).mock.calls[0][0]).toBe(1));

    // "Eliminar" pide confirmación antes de llamar al handler.
    await user.click(screen.getByRole('button', { name: 'Eliminar' }));
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Eliminar' }));
    await waitFor(() => expect(vi.mocked(plantillasApi.deletePlantillaNumeracion).mock.calls[0][0]).toBe(1));

    expect(useHasPermission).toHaveBeenCalledWith('admin.plantillasNumeracion.editar');
  });

  it('hides edit actions when the user only has catalog edit permission', async () => {
    vi.mocked(useHasPermission).mockImplementation((permission) => permission === 'admin.catalogos.editar');

    renderWithProviders();

    expect(await screen.findByText('Documentos')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /nueva plantilla/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Editar' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Usar como plantilla activa del sistema' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Eliminar' })).not.toBeInTheDocument();
    expect(useHasPermission).toHaveBeenCalledWith('admin.plantillasNumeracion.editar');
  });
});
