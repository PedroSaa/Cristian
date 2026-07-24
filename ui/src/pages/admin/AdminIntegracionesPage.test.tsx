import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminIntegracionesPage from './AdminIntegracionesPage';
import * as adminIntegracionesApi from '../../lib/api/admin/adminIntegracionesApi';
import type { IntegracionDto } from '../../lib/api/admin/adminIntegracionesApi';
import { useHasPermission } from '../../hooks/usePermissions';

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock('../../lib/api/admin/adminIntegracionesApi', () => ({
  getIntegraciones: vi.fn(),
  actualizarIntegracion: vi.fn(),
  probarConexion: vi.fn(),
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
      <AdminIntegracionesPage />
    </QueryClientProvider>,
  );
}

// ── Fixtures ─────────────────────────────────────────────────────────────────

const mockIntegraciones: IntegracionDto[] = [
  {
    id: '50000000-0000-0000-0000-000000000001',
    nombre: 'DocDigital',
    tipo: 'DocDigital',
    baseUrl: 'https://api.doc.digital.gob.cl',
    apiKeyMasked: 'sk_******',
    activo: true,
    settings: {},
  },
  {
    id: '50000000-0000-0000-0000-000000000002',
    nombre: 'FirmaGob',
    tipo: 'FirmaGob',
    baseUrl: 'https://api.firma.digital.gob.cl',
    apiKeyMasked: 'sk_******',
    activo: false,
    settings: {},
  },
  {
    id: '50000000-0000-0000-0000-000000000003',
    nombre: 'SII',
    tipo: 'SII',
    baseUrl: 'https://www.sii.cl',
    apiKeyMasked: 'sk_******',
    activo: true,
    settings: {},
  },
  {
    id: '50000000-0000-0000-0000-000000000006',
    nombre: 'MercadoPublico',
    tipo: 'MercadoPublico',
    baseUrl: 'https://api.mercadopublico.cl',
    apiKeyMasked: 'sk_******',
    activo: true,
    settings: { Ticket: 'TICKET-EXISTENTE' },
  },
];

// ── Tests ────────────────────────────────────────────────────────────────────

describe('AdminIntegracionesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useHasPermission).mockReturnValue(true);
  });

  it('renders integration cards with nombre and tipo', async () => {
    vi.mocked(adminIntegracionesApi.getIntegraciones).mockResolvedValue(mockIntegraciones);

    renderWithProviders();

    // Use heading level-3 to get unique nombre elements (each card has one <h3>)
    const headings = await screen.findAllByRole('heading', { level: 3 });
    expect(headings).toHaveLength(4);
    expect(headings[0]).toHaveTextContent('DocDigital');
    expect(headings[1]).toHaveTextContent('FirmaGob');
    expect(headings[2]).toHaveTextContent('SII');
    expect(headings[3]).toHaveTextContent('MercadoPublico');

    // Estado badges
    expect(screen.getAllByText('Activo')).toHaveLength(3);
    expect(screen.getByText('Inactivo')).toBeInTheDocument();

    // URLs are unique per card
    expect(screen.getByText('https://api.doc.digital.gob.cl')).toBeInTheDocument();
    expect(screen.getByText('https://api.firma.digital.gob.cl')).toBeInTheDocument();
    expect(screen.getByText('https://www.sii.cl')).toBeInTheDocument();
  });

  it('edit button opens modal with pre-filled baseUrl', async () => {
    const user = userEvent.setup();
    vi.mocked(adminIntegracionesApi.getIntegraciones).mockResolvedValue(mockIntegraciones);

    renderWithProviders();

    // Wait for a unique URL to confirm data loaded
    await screen.findByText('https://api.doc.digital.gob.cl');

    const editButtons = screen.getAllByRole('button', { name: /editar configuración/i });
    await user.click(editButtons[0]);

    // Modal should be visible with pre-filled values
    expect(screen.getByText('Editar integración')).toBeInTheDocument();
    expect(screen.getByDisplayValue('https://api.doc.digital.gob.cl')).toBeInTheDocument();
  });

  it('save button calls actualizarIntegracion', async () => {
    const user = userEvent.setup();
    const mockActualizar = vi.fn().mockResolvedValue(mockIntegraciones[0]);
    vi.mocked(adminIntegracionesApi.getIntegraciones).mockResolvedValue(mockIntegraciones);
    vi.mocked(adminIntegracionesApi.actualizarIntegracion).mockImplementation(mockActualizar);

    renderWithProviders();

    // Wait for a unique URL to confirm data loaded
    await screen.findByText('https://api.doc.digital.gob.cl');

    const editButtons = screen.getAllByRole('button', { name: /editar configuración/i });
    await user.click(editButtons[0]);

    await user.click(screen.getByText('Guardar'));

    expect(mockActualizar).toHaveBeenCalledTimes(1);
    expect(mockActualizar).toHaveBeenCalledWith(
      '50000000-0000-0000-0000-000000000001',
      expect.objectContaining({
        baseUrl: 'https://api.doc.digital.gob.cl',
      }),
    );
  });

  describe('Mercado Público', () => {
    beforeEach(() => {
      vi.mocked(adminIntegracionesApi.getIntegraciones).mockResolvedValue(mockIntegraciones);
    });

    it('shows Ticket and Codigo de organismo inputs when editing the MercadoPublico card', async () => {
      const user = userEvent.setup();
      renderWithProviders();

      await screen.findByText('https://api.mercadopublico.cl');
      const editButtons = screen.getAllByRole('button', { name: /editar configuración/i });
      await user.click(editButtons[3]);

      expect(screen.getByText('Ticket de acceso')).toBeInTheDocument();
      expect(screen.getByText('Código de organismo (opcional)')).toBeInTheDocument();
      expect(screen.getByDisplayValue('TICKET-EXISTENTE')).toBeInTheDocument();
    });

    it('sends Ticket and CodigoOrganismo settings on save', async () => {
      const user = userEvent.setup();
      const mockActualizar = vi.fn().mockResolvedValue(mockIntegraciones[3]);
      vi.mocked(adminIntegracionesApi.actualizarIntegracion).mockImplementation(mockActualizar);

      renderWithProviders();

      await screen.findByText('https://api.mercadopublico.cl');
      const editButtons = screen.getAllByRole('button', { name: /editar configuración/i });
      await user.click(editButtons[3]);

      const ticketInput = screen.getByDisplayValue('TICKET-EXISTENTE');
      await user.clear(ticketInput);
      await user.type(ticketInput, 'TICKET-NUEVO');

      const organismoInput = screen.getByPlaceholderText('6937');
      await user.type(organismoInput, '6937');

      await user.click(screen.getByText('Guardar'));

      expect(mockActualizar).toHaveBeenCalledWith(
        '50000000-0000-0000-0000-000000000006',
        expect.objectContaining({
          settings: expect.objectContaining({
            Ticket: 'TICKET-NUEVO',
            CodigoOrganismo: '6937',
          }),
        }),
      );
    });
  });

  describe('Probar conexión', () => {
    beforeEach(() => {
      vi.mocked(adminIntegracionesApi.getIntegraciones).mockResolvedValue(mockIntegraciones);
    });

    async function openEditModal(user: ReturnType<typeof userEvent.setup>, index = 0) {
      await screen.findByText('https://api.doc.digital.gob.cl');
      const editButtons = screen.getAllByRole('button', { name: /editar configuración/i });
      await user.click(editButtons[index]);
    }

    it('(a) old "no hay prueba" info box is gone even with the edit modal open', async () => {
      const user = userEvent.setup();
      renderWithProviders();
      await openEditModal(user);
      // The box used to live inside the modal — opening it proves the box was removed, not just hidden
      expect(screen.getByRole('button', { name: /probar conexión/i })).toBeInTheDocument();
      expect(
        screen.queryByText(/no cuenta con una prueba automática/i),
      ).not.toBeInTheDocument();
    });

    it('(b) "Probar conexión" button is rendered inside the edit modal', async () => {
      const user = userEvent.setup();
      renderWithProviders();
      await openEditModal(user);
      expect(screen.getByRole('button', { name: /probar conexión/i })).toBeInTheDocument();
    });

    it('(c) click Probar conexión → success → shows latency "42"', async () => {
      const user = userEvent.setup();
      vi.mocked(adminIntegracionesApi.probarConexion).mockResolvedValue({
        success: true,
        mensaje: 'ok',
        latencyMs: 42,
      });

      renderWithProviders();
      await openEditModal(user);

      await user.click(screen.getByRole('button', { name: /probar conexión/i }));

      expect(await screen.findByText(/42/)).toBeInTheDocument();
    });

    it('(d) click Probar conexión → failure → shows "DNS fail" error message', async () => {
      const user = userEvent.setup();
      vi.mocked(adminIntegracionesApi.probarConexion).mockResolvedValue({
        success: false,
        mensaje: 'DNS fail',
        latencyMs: null,
      });

      renderWithProviders();
      await openEditModal(user);

      await user.click(screen.getByRole('button', { name: /probar conexión/i }));

      expect(await screen.findByText('DNS fail')).toBeInTheDocument();
      expect(screen.queryByText(/42/)).not.toBeInTheDocument();
    });

    it('(e) "Probar conexión" button is disabled when baseUrl is empty', async () => {
      const user = userEvent.setup();
      renderWithProviders();
      await screen.findByText('https://api.doc.digital.gob.cl');
      const editButtons = screen.getAllByRole('button', { name: /editar configuración/i });
      // Open the edit modal for the first integration
      await user.click(editButtons[0]);

      // Clear the baseUrl field
      const urlInput = screen.getByDisplayValue('https://api.doc.digital.gob.cl');
      await user.clear(urlInput);

      expect(screen.getByRole('button', { name: /probar conexión/i })).toBeDisabled();
    });
  });
});
