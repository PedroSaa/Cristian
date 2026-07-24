import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminConfiguracionPage from './AdminConfiguracionPage';
import * as adminConfiguracionApi from '../../lib/api/admin/adminConfiguracionApi';
import { useHasPermission } from '../../hooks/usePermissions';

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock('../../lib/api/admin/adminConfiguracionApi', () => ({
  getConfiguraciones: vi.fn(),
  upsertConfiguracion: vi.fn(),
  getConfiguracion: vi.fn(),
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
      <AdminConfiguracionPage />
    </QueryClientProvider>,
  );
}

// ── Fixtures ─────────────────────────────────────────────────────────────────

const mockConfiguraciones = [
  {
    id: '60000000-0000-0000-0000-000000000001',
    clave: 'NombreInstitucion',
    valor: '',
    descripcion: 'Nombre de la institución',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
  {
    id: '60000000-0000-0000-0000-000000000002',
    clave: 'UrlPortalPublica',
    valor: 'https://portal.docflow.cl',
    descripcion: 'URL del portal público',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
  {
    id: '60000000-0000-0000-0000-000000000003',
    clave: 'MaxAdjuntosMB',
    valor: '10',
    descripcion: 'Tamaño máximo de adjuntos en MB',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
  {
    id: '60000000-0000-0000-0000-000000000004',
    clave: 'EmailSoporte',
    valor: '',
    descripcion: 'Correo de soporte técnico',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
  {
    id: '60000000-0000-0000-0000-000000000005',
    clave: 'LoginWelcomeTitle',
    valor: 'Bienvenido',
    descripcion: 'Título de bienvenida',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
  {
    id: '60000000-0000-0000-0000-000000000006',
    clave: 'LoginWelcomeSubtitle',
    valor: 'Gestiona tus trámites',
    descripcion: 'Subtítulo de bienvenida',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
  {
    id: '60000000-0000-0000-0000-000000000007',
    clave: 'LoginWelcomeHelpText',
    valor: 'Si necesitas ayuda, contacta soporte.',
    descripcion: 'Ayuda de bienvenida',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
];

// ── Tests ────────────────────────────────────────────────────────────────────

describe('AdminConfiguracionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue(mockConfiguraciones);
    vi.mocked(useHasPermission).mockReturnValue(true);
  });

  it('renders config entries from API data (clave and valor displayed)', async () => {
    renderWithProviders();

    expect(await screen.findByRole('tab', { name: 'General' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Seguridad' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Acceso' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Logo' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Bienvenida' })).toBeInTheDocument();
    expect(await screen.findByText('NombreInstitucion')).toBeInTheDocument();
    expect(screen.getByText('MaxAdjuntosMB')).toBeInTheDocument();
    expect(screen.getByText('UrlPortalPublica')).toBeInTheDocument();
    expect(screen.getByText('EmailSoporte')).toBeInTheDocument();
    expect(screen.queryByText('Diseño de acceso')).toBeNull();
    expect(screen.queryByText('Fondo de acceso')).toBeNull();
    expect(screen.queryByText('Logo institucional')).toBeNull();
    expect(screen.queryByText('Mensaje de bienvenida')).toBeNull();

    // Valor should be displayed for entries with a value
    expect(screen.getByText('10')).toBeInTheDocument();
  });

  it('renders Seguridad section heading and human-friendly labels for security config entries, with no password input', async () => {
    const securityEntries = [
      {
        id: '70000000-0000-0000-0000-000000000000',
        clave: 'JwtExpirationMinutos',
        valor: '480',
        descripcion: 'JWT access token validez',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'seguridad' as const,
        tipo: 'int' as const,
        minValue: 60,
        maxValue: 1440,
      },
      {
        id: '70000000-0000-0000-0000-000000000001',
        clave: 'LockoutMaxIntentos',
        valor: '5',
        descripcion: 'Máximo de intentos fallidos antes del bloqueo',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'seguridad' as const,
        tipo: 'int' as const,
        minValue: 1,
        maxValue: 10,
      },
      {
        id: '70000000-0000-0000-0000-000000000002',
        clave: 'PasswordMinLength',
        valor: '8',
        descripcion: 'Longitud mínima de contraseña',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'seguridad' as const,
        tipo: 'int' as const,
        minValue: 6,
        maxValue: 32,
      },
      {
        id: '70000000-0000-0000-0000-000000000003',
        clave: 'TotpWindowSegundos',
        valor: '90',
        descripcion: 'Ventana TOTP',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'seguridad' as const,
        tipo: 'int' as const,
        minValue: 90,
        maxValue: 300,
      },
    ];

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...mockConfiguraciones,
      ...securityEntries,
    ]);

    renderWithProviders();

    // Wait for human-friendly labels to appear (not raw technical keys)
    expect(await screen.findByRole('tab', { name: 'Seguridad' })).toHaveAttribute('aria-selected', 'true');
    expect(await screen.findByText('Vigencia del token de acceso')).toBeInTheDocument();
    expect(screen.getByText('Intentos fallidos antes de bloqueo')).toBeInTheDocument();
    expect(screen.getByText('Longitud mínima de contraseña')).toBeInTheDocument();
    expect(screen.getByText('Margen de tiempo para validar el código de autenticación en dos pasos')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /guardar cambios/i })).toBeInTheDocument();

    // Human-readable range hints for int entries with min/max
    expect(screen.getByText('Rango válido: 15 — 1440 minutos')).toBeInTheDocument();
    expect(screen.getByText('Rango válido: 1 — 10 intentos')).toBeInTheDocument();
    expect(screen.getByText('Rango válido: 8 — 32 caracteres')).toBeInTheDocument();
    expect(screen.getByText('Rango válido: 90 — 300 segundos')).toBeInTheDocument();

    // Technical keys should not be exposed in the Seguridad UX
    expect(screen.queryByText('JwtExpirationMinutos')).toBeNull();
    expect(screen.queryByText('TotpWindowSegundos')).toBeNull();

    // No password input anywhere
    expect(document.querySelector('input[type="password"]')).toBeNull();

  });

  it('defaults to General tab when there are no security entries', async () => {
    renderWithProviders();

    await screen.findByText('NombreInstitucion');

    expect(screen.getByRole('tab', { name: 'General' })).toHaveAttribute('aria-selected', 'true');

    // General entries still render in the card layout
    expect(screen.getByText('EmailSoporte')).toBeInTheDocument();
  });

  it('does not show range hints for bool security entries without minValue/maxValue', async () => {
    const boolEntry = {
      id: '70000000-0000-0000-0000-000000000010',
      clave: 'PasswordRequireUpper',
      valor: 'true',
      descripcion: 'Requiere mayúsculas',
      actualizadoEn: '2026-01-01T00:00:00Z',
      grupo: 'seguridad' as const,
      tipo: 'bool' as const,
      // no minValue/maxValue — should NOT show range hint
    };

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...mockConfiguraciones,
      boolEntry,
    ]);

    renderWithProviders();

    // Human-friendly label for bool entry (not raw clave)
    expect(await screen.findByText('Requerir mayúsculas')).toBeInTheDocument();

    expect(screen.getByRole('tab', { name: 'Seguridad' })).toHaveAttribute('aria-selected', 'true');

    // Toggle should be checked because valor === 'true'
    const toggle = screen.getByRole('switch');
    expect(toggle).toBeChecked();

    // Range hint pattern should NOT appear for this entry
    expect(screen.queryByText(/^Rango válido:/)).toBeNull();
  });

  it('renders dedicated MFA policy toggles with human-friendly labels', async () => {
    const mfaPolicyEntries = [
      {
        id: '70000000-0000-0000-0000-000000000020',
        clave: 'RequireMfaAdministradores',
        valor: 'false',
        descripcion: 'Exigir MFA a administradores',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'seguridad' as const,
        tipo: 'bool' as const,
      },
      {
        id: '70000000-0000-0000-0000-000000000021',
        clave: 'RequireMfaOtrosUsuarios',
        valor: 'true',
        descripcion: 'Exigir MFA al resto de usuarios',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'seguridad' as const,
        tipo: 'bool' as const,
      },
    ];

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...mockConfiguraciones,
      ...mfaPolicyEntries,
    ]);

    renderWithProviders();

    expect(await screen.findByText('Requerir MFA para administradores')).toBeInTheDocument();
    expect(screen.getByText('Requerir MFA para el resto de usuarios')).toBeInTheDocument();
    expect(screen.getAllByRole('switch')).toHaveLength(2);
    expect(screen.getByText('Desactivado')).toBeInTheDocument();
    expect(screen.getByText('Activado')).toBeInTheDocument();
  });

  it('renders MFA policy entries inside Seguridad and not General', async () => {
    const mfaPolicyEntries = [
      {
        id: '70000000-0000-0000-0000-000000000020',
        clave: 'RequireMfaAdministradores',
        valor: 'false',
        descripcion: 'Exigir MFA a administradores',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'general' as const,
        tipo: 'bool' as const,
      },
      {
        id: '70000000-0000-0000-0000-000000000021',
        clave: 'RequireMfaOtrosUsuarios',
        valor: 'true',
        descripcion: 'Exigir MFA al resto de usuarios',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'general' as const,
        tipo: 'bool' as const,
      },
    ];

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...mockConfiguraciones,
      ...mfaPolicyEntries,
    ]);

    renderWithProviders();

    expect(await screen.findByRole('tab', { name: 'Seguridad' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByText('Requerir MFA para administradores')).toBeInTheDocument();
    expect(screen.getByText('Requerir MFA para el resto de usuarios')).toBeInTheDocument();
    expect(screen.getAllByRole('switch')).toHaveLength(2);

    await userEvent.click(screen.getByRole('tab', { name: 'General' }));

    expect(screen.queryByText('Requerir MFA para administradores')).toBeNull();
    expect(screen.queryByText('Requerir MFA para el resto de usuarios')).toBeNull();
  });

  it('blocks saving malformed toggle values in security configuration', async () => {
    const malformedToggle = {
      id: '70000000-0000-0000-0000-000000000022',
        clave: 'RequireMfaAdministradores',
      valor: 'maybe',
      descripcion: 'Exigir MFA a administradores',
      actualizadoEn: '2026-01-01T00:00:00Z',
      grupo: 'seguridad' as const,
      tipo: 'bool' as const,
    };

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...mockConfiguraciones,
      malformedToggle,
    ]);

    renderWithProviders();

    expect(await screen.findByText('Requerir MFA para administradores')).toBeInTheDocument();

    await userEvent.setup().click(screen.getByRole('button', { name: /guardar cambios/i }));

    expect(screen.getByText('Ingresa un valor booleano válido.')).toBeInTheDocument();
    expect(vi.mocked(adminConfiguracionApi.upsertConfiguracion)).not.toHaveBeenCalled();
  });

  it('disables toggle while upsert mutation is pending (saving feedback)', async () => {
    const user = userEvent.setup();
    let resolveUpsert!: (value: unknown) => void;
    const upsertPromise = new Promise((resolve) => { resolveUpsert = resolve; });
    vi.mocked(adminConfiguracionApi.upsertConfiguracion).mockReturnValue(upsertPromise as ReturnType<typeof adminConfiguracionApi.upsertConfiguracion>);

    const boolEntry = {
      id: '70000000-0000-0000-0000-000000000010',
      clave: 'PasswordRequireUpper',
      valor: 'true',
      descripcion: 'Requiere mayúsculas',
      actualizadoEn: '2026-01-01T00:00:00Z',
      grupo: 'seguridad' as const,
      tipo: 'bool' as const,
    };

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...mockConfiguraciones,
      boolEntry,
    ]);

    renderWithProviders();

    // Wait for security section to render
    expect(await screen.findByText('Requerir mayúsculas')).toBeInTheDocument();

    // Toggle should be enabled before click
    const toggle = screen.getByRole('switch');
    expect(toggle).not.toBeDisabled();

    // Click the toggle to change the draft
    await user.click(toggle);

    // Save should trigger the mutation and lock the controls
    await user.click(screen.getByRole('button', { name: /guardar cambios/i }));

    // Toggle should be disabled while mutation is pending
    expect(toggle).toBeDisabled();

    // Resolve the mutation so the test can finish
    resolveUpsert({ id: 'new-id', clave: 'PasswordRequireUpper', valor: 'false', descripcion: '', actualizadoEn: new Date().toISOString() });
  });

  it('shows validation errors when a numeric security value is below the minimum', async () => {
    const user = userEvent.setup();

    const securityEntries = [
      {
        id: '70000000-0000-0000-0000-000000000000',
        clave: 'JwtExpirationMinutos',
        valor: '480',
        descripcion: 'JWT access token validez',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'seguridad' as const,
        tipo: 'int' as const,
        minValue: 15,
        maxValue: 1440,
      },
    ];

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...mockConfiguraciones,
      ...securityEntries,
    ]);

    renderWithProviders();

    const input = await screen.findByRole('spinbutton');
    await user.clear(input);
    await user.type(input, '10');

    expect(screen.getByText('El valor mínimo es 15.')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /guardar cambios/i }));

    expect(screen.getByText(/Corrige estos campos antes de guardar/)).toBeInTheDocument();
    expect(vi.mocked(adminConfiguracionApi.upsertConfiguracion)).not.toHaveBeenCalled();
  });

  it('only saves security changes when clicking Guardar cambios', async () => {
    const user = userEvent.setup();
    const mockUpsert = vi.mocked(adminConfiguracionApi.upsertConfiguracion);
    mockUpsert.mockResolvedValue({
      id: '70000000-0000-0000-0000-000000000000',
      clave: 'JwtExpirationMinutos',
      valor: '500',
      descripcion: 'JWT access token validez',
      actualizadoEn: '2026-01-01T00:00:00Z',
    });

    const securityEntries = [
      {
        id: '70000000-0000-0000-0000-000000000000',
        clave: 'JwtExpirationMinutos',
        valor: '480',
        descripcion: 'JWT access token validez',
        actualizadoEn: '2026-01-01T00:00:00Z',
        grupo: 'seguridad' as const,
        tipo: 'int' as const,
        minValue: 15,
        maxValue: 1440,
      },
    ];

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...mockConfiguraciones,
      ...securityEntries,
    ]);

    renderWithProviders();

    const spinbutton = await screen.findByRole('spinbutton');
    await user.clear(spinbutton);
    await user.type(spinbutton, '500');

    expect(mockUpsert).not.toHaveBeenCalled();

    await user.click(screen.getByRole('button', { name: /guardar cambios/i }));

    await waitFor(() => expect(mockUpsert).toHaveBeenCalledTimes(1));
    expect(mockUpsert).toHaveBeenCalledWith(expect.objectContaining({
      clave: 'JwtExpirationMinutos',
      valor: '500',
    }));
  });

  it('submits upsert payload when editing and saving a config entry', async () => {
    const user = userEvent.setup();
    const mockUpsert = vi.mocked(adminConfiguracionApi.upsertConfiguracion);
    mockUpsert.mockResolvedValue({
      id: '60000000-0000-0000-0000-000000000003',
      clave: 'MaxAdjuntosMB',
      valor: '20',
      descripcion: 'Tamaño máximo de adjuntos en MB',
      actualizadoEn: '2026-05-16T00:00:00Z',
    });

    renderWithProviders();

    // Wait for data to load
    await screen.findByText('MaxAdjuntosMB');

    // Find and click "Editar" button for the MaxAdjuntosMB row
    const editButtons = screen.getAllByRole('button', { name: /editar/i });
    expect(editButtons).toHaveLength(4);

    // Click the third "Editar" — it should be MaxAdjuntosMB
    await user.click(editButtons[2]);

    // The input should now appear with current value
    const valorInput = screen.getByRole('spinbutton');
    expect(valorInput).toHaveValue(10);

    // Clear and type new value
    await user.clear(valorInput);
    await user.type(valorInput, '20');

    // Click "Guardar"
    const guardarButton = screen.getByRole('button', { name: /^Guardar$/i });
    await user.click(guardarButton);

    // Verify upsert was called with correct payload
      expect(mockUpsert).toHaveBeenCalledWith({
      clave: 'MaxAdjuntosMB',
      valor: '20',
      descripcion: 'Tamaño máximo de adjuntos en MB',
    });
  });

  it('opens the create configuration modal and submits a new entry', async () => {
    const user = userEvent.setup();
    const mockUpsert = vi.mocked(adminConfiguracionApi.upsertConfiguracion);
    mockUpsert.mockResolvedValue({
      id: '60000000-0000-0000-0000-000000000099',
      clave: 'NuevaClave',
      valor: 'nuevo-valor',
      descripcion: 'Nueva configuración',
      actualizadoEn: '2026-05-16T00:00:00Z',
    });

    renderWithProviders();

    await screen.findByText('NombreInstitucion');

    await user.click(screen.getByRole('button', { name: /nueva configuración/i }));

    const dialog = screen.getByRole('dialog', { name: /nueva configuración/i });
    const inputs = dialog.querySelectorAll('input, textarea');
    await user.type(inputs[0], 'NuevaClave');
    await user.type(inputs[1], 'nuevo-valor');
    await user.type(inputs[2], 'Nueva configuración');

    await user.click(screen.getByRole('button', { name: /^Guardar$/i }));

    expect(mockUpsert).toHaveBeenCalledWith({
      clave: 'NuevaClave',
      valor: 'nuevo-valor',
      descripcion: 'Nueva configuración',
    });
  });
});
