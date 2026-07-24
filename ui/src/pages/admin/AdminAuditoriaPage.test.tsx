import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminAuditoriaPage from './AdminAuditoriaPage';

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock('../../lib/api/admin/adminAuditoriaApi', () => ({
  getAuditoria: vi.fn(),
  getRegistroAuditoria: vi.fn(),
  getValoresFiltro: vi.fn().mockResolvedValue({ acciones: [], entidades: [] }),
  exportAuditoria: vi.fn(),
}));

import * as adminAuditoriaApi from '../../lib/api/admin/adminAuditoriaApi';

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
      <AdminAuditoriaPage />
    </QueryClientProvider>,
  );
}

// ── Fixtures ─────────────────────────────────────────────────────────────────

const mockRegistros = [
  {
    id: '50000000-0000-0000-0000-000000000001',
    usuarioId: '60000000-0000-0000-0000-000000000001',
    usuarioNombre: 'Admin Usuario',
    accion: 'Login',
    entidad: 'Usuario',
    entidadId: 'usr-001',
    detalle: null,
    direccionIp: null,
    userAgent: null,
    creadoEn: '2025-01-15T10:30:00Z',
  },
  {
    id: '50000000-0000-0000-0000-000000000002',
    usuarioId: '60000000-0000-0000-0000-000000000002',
    usuarioNombre: 'Config Admin',
    accion: 'UpsertConfiguracion',
    entidad: 'ConfiguracionSistema',
    entidadId: 'cfg-001',
    detalle: 'Configuración actualizada',
    direccionIp: '192.168.1.1',
    userAgent: 'Mozilla/5.0',
    creadoEn: '2025-01-15T11:00:00Z',
  },
  {
    id: '50000000-0000-0000-0000-000000000003',
    usuarioId: '60000000-0000-0000-0000-000000000001',
    usuarioNombre: 'Admin Usuario',
    accion: 'Logout',
    entidad: 'Usuario',
    entidadId: 'usr-001',
    detalle: null,
    direccionIp: null,
    userAgent: null,
    creadoEn: '2025-01-15T12:00:00Z',
  },
];

const mockSinglePageResult = {
  items: mockRegistros,
  total: 3,
  page: 1,
  totalPaginas: 1,
};

const mockMultiPageResult = {
  items: mockRegistros,
  total: 13,
  page: 1,
  totalPaginas: 5,
};

// ── Tests ────────────────────────────────────────────────────────────────────

describe('AdminAuditoriaPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(adminAuditoriaApi.getValoresFiltro).mockResolvedValue({ acciones: [], entidades: [] });
  });

  it('renders audit list from API with all columns visible', async () => {
    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockSinglePageResult);

    renderWithProviders();

    // Wait for table rows to render
    const rows = await screen.findAllByRole('row');
    // 1 header row + 3 data rows
    expect(rows).toHaveLength(4);

    // Column headers should be visible
    expect(screen.getByRole('columnheader', { name: 'Fecha' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Usuario' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Acción' })).toBeInTheDocument();
    expect(screen.getByRole('columnheader', { name: 'Entidad' })).toBeInTheDocument();
    // La columna "ID del registro" (GUID) se quitó de la tabla; el ID solo se ve en el detalle.
    expect(screen.queryByRole('columnheader', { name: 'ID del registro' })).not.toBeInTheDocument();

    // Data content should be rendered (text inside badges still findable)
    expect(screen.getByText('Login')).toBeInTheDocument();
    expect(screen.getByText('UpsertConfiguracion')).toBeInTheDocument();
    expect(screen.getByText('Logout')).toBeInTheDocument();
    // "Usuario" appears in column header (th) + 2 entidad data cells (td)
    expect(screen.getAllByText('Usuario')).toHaveLength(3);
    expect(screen.getByText('ConfiguracionSistema')).toBeInTheDocument();

    // UsuarioNombre should be shown in Usuario column (Admin Usuario appears twice)
    expect(screen.getAllByText('Admin Usuario')).toHaveLength(2);
    expect(screen.getByText('Config Admin')).toBeInTheDocument();

    // Pagination renders once data is loaded
    expect(screen.getByText('Filas por página')).toBeInTheDocument();
  });

  it('shows pagination controls when there are multiple pages', async () => {
    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockMultiPageResult);

    renderWithProviders();

    // Wait for data to load
    await screen.findByText('Filas por página');

    // First/previous controls disabled on page 1
    expect(screen.getByLabelText('Página anterior')).toBeDisabled();

    // Next control enabled when not on the last page
    expect(screen.getByLabelText('Página siguiente')).not.toBeDisabled();

    // Active numbered page is visible
    expect(screen.getByLabelText('Página 1')).toBeInTheDocument();
  });

  it('shows loading spinner while fetching', async () => {
    // Never resolve — keep loading
    vi.mocked(adminAuditoriaApi.getAuditoria).mockImplementationOnce(
      () => new Promise(() => {}),
    );

    renderWithProviders();

    // Title should be visible while loading
    expect(screen.getByText('Auditoría')).toBeInTheDocument();
  });

  it('shows error message when API call fails', async () => {
    vi.mocked(adminAuditoriaApi.getAuditoria).mockRejectedValueOnce(new Error('Network error'));

    renderWithProviders();

    const errorMessage = await screen.findByText('No se pudo cargar el registro de auditoría.');
    expect(errorMessage).toBeInTheDocument();
  });

  it('renders filter dropdowns populated from getValoresFiltro', async () => {
    vi.mocked(adminAuditoriaApi.getValoresFiltro).mockResolvedValue({
      acciones: ['Login', 'CrearUsuario'],
      entidades: ['Usuario', 'Documento'],
    });
    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockSinglePageResult);

    renderWithProviders();

    // Wait for data to render
    await screen.findByText('Filas por página');

    // Both selects should have their options rendered ("Todas" in both Acción and Entidad)
    expect(screen.getAllByText('Todas')).toHaveLength(2);

    // Dropdown options from valores-filtro should be rendered
    expect(screen.getByText('CrearUsuario')).toBeInTheDocument();
    expect(screen.getByText('Documento')).toBeInTheDocument();
  });

  it('degradates to text inputs when getValoresFiltro fails', async () => {
    vi.mocked(adminAuditoriaApi.getValoresFiltro).mockRejectedValue(new Error('API error'));
    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockSinglePageResult);

    renderWithProviders();

    // Wait for data to render
    await screen.findByText('Filas por página');

    // Text inputs should be rendered instead of selects for Acción and Entidad
    const accionInput = screen.getByPlaceholderText('ej: Login, Crear, Actualizar');
    expect(accionInput).toBeInTheDocument();
    const entidadInput = screen.getByPlaceholderText('ej: Usuario, Documento');
    expect(entidadInput).toBeInTheDocument();
  });

  it("shows 'Sistema' in Usuario column when usuarioNombre is null (never the UUID)", async () => {
    const registrosSinNombre = mockRegistros.map(r => ({ ...r, usuarioNombre: null }));
    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue({
      ...mockSinglePageResult,
      items: registrosSinNombre,
    });

    renderWithProviders();

    // Wait for data
    await screen.findByText('Filas por página');

    // Sin nombre, la columna Usuario muestra "Sistema" — nunca el UUID del usuario.
    expect(screen.getAllByText('Sistema')).toHaveLength(3);
    expect(screen.queryByText('60000000-000…')).not.toBeInTheDocument();
  });

  it('shows IP and UserAgent in detail modal when data is present', async () => {
    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockSinglePageResult);
    vi.mocked(adminAuditoriaApi.getRegistroAuditoria).mockResolvedValue(mockRegistros[1]);

    renderWithProviders();

    // Wait for list, then click detalle button for second row
    await screen.findByText('Filas por página');

    // Find detail buttons by aria-label (IconButton renders with aria-label={tooltip})
    const detailButtons = screen.getAllByRole('button', { name: /ver detalle/i });
    // Click the second button (corresponds to mockRegistros[1] which has IP/UA)
    detailButtons[1].click();

    // Wait for detail modal and data to load
    await screen.findByText('Detalle de auditoría');

    // IP and UA should be visible — use findByText to wait for data resolution
    expect(await screen.findByText('192.168.1.1')).toBeInTheDocument();
    expect(await screen.findByText('Mozilla/5.0')).toBeInTheDocument();
  });

  it('shows placeholder when IP and UserAgent are null', async () => {
    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockSinglePageResult);
    vi.mocked(adminAuditoriaApi.getRegistroAuditoria).mockResolvedValue(mockRegistros[0]);

    renderWithProviders();

    await screen.findByText('Filas por página');

    // Click the first detail button (mockRegistros[0] has null IP/UA)
    const detailButtons = screen.getAllByRole('button', { name: /ver detalle/i });
    detailButtons[0].click();

    // Wait for detail modal — use findByText to wait for data resolution
    await screen.findByText('Detalle de auditoría');

    // The IP value should be em dash (both IP and UA show —)
    const dashes = await screen.findAllByText('—');
    expect(dashes.length).toBeGreaterThanOrEqual(1);
  });

  it('renders usuarioNombre text input filter', async () => {
    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockSinglePageResult);

    renderWithProviders();

    await screen.findByText('Filas por página');

    // Name input should be present
    const nameInput = screen.getByPlaceholderText('Nombre del usuario');
    expect(nameInput).toBeInTheDocument();
  });

  it('shows structured detalle in detail modal when detalle is valid JSON', async () => {
    const structuredDetalle = JSON.stringify({
      valorAnterior: 'old@test.com',
      valorNuevo: 'new@test.com',
      metadata: 'Actualización de email',
    });
    const registroConDetalleEstructurado = {
      ...mockRegistros[1],
      detalle: structuredDetalle,
    };

    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockSinglePageResult);
    vi.mocked(adminAuditoriaApi.getRegistroAuditoria).mockResolvedValue(registroConDetalleEstructurado);

    renderWithProviders();

    await screen.findByText('Filas por página');

    const detailButtons = screen.getAllByRole('button', { name: /ver detalle/i });
    detailButtons[1].click();

    await screen.findByText('Detalle de auditoría');

    // Should show "estructurado" label
    expect(await screen.findByText(/estructurado/i)).toBeInTheDocument();

    // Should show structured field values
    expect(await screen.findByText(/Valor anterior: old@test.com/i)).toBeInTheDocument();
    expect(screen.getByText(/Valor nuevo: new@test.com/i)).toBeInTheDocument();
    expect(screen.getByText(/Metadata: Actualización de email/i)).toBeInTheDocument();
  });

  it('shows raw plain text detalle when JSON parsing fails', async () => {
    const registroConDetallePlano = {
      ...mockRegistros[1],
      detalle: 'Configuración actualizada',
    };

    vi.mocked(adminAuditoriaApi.getAuditoria).mockResolvedValue(mockSinglePageResult);
    vi.mocked(adminAuditoriaApi.getRegistroAuditoria).mockResolvedValue(registroConDetallePlano);

    renderWithProviders();

    await screen.findByText('Filas por página');

    const detailButtons = screen.getAllByRole('button', { name: /ver detalle/i });
    detailButtons[1].click();

    await screen.findByText('Detalle de auditoría');

    // The plain text should still be visible
    expect(await screen.findByText('Configuración actualizada')).toBeInTheDocument();

    // Should NOT show "(estructurado)" label
    expect(screen.queryByText(/estructurado/i)).not.toBeInTheDocument();
  });
});
