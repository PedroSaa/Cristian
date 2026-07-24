import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminConfiguracionPage from './AdminConfiguracionPage';
import * as adminConfiguracionApi from '../../lib/api/admin/adminConfiguracionApi';
import * as brandingApi from '../../lib/api/brandingApi';
import { useHasPermission } from '../../hooks/usePermissions';

vi.mock('../../lib/api/admin/adminConfiguracionApi', () => ({
  getConfiguraciones: vi.fn(),
  upsertConfiguracion: vi.fn(),
  getConfiguracion: vi.fn(),
}));

vi.mock('../../lib/api/brandingApi', () => ({
  getBranding: vi.fn(),
  uploadBrandingLogo: vi.fn(),
  uploadBrandingLoginBackground: vi.fn(),
  normalizeBranding: vi.fn((data) => ({
    nombreInstitucion: data?.nombreInstitucion || 'DocFlow Infinity',
    logoUrl: data?.logoUrl ?? null,
    loginBackgroundMode: data?.loginBackgroundMode ?? 'gradient',
    loginBackgroundPresetKey: data?.loginBackgroundPresetKey ?? 'midnight-indigo',
    loginBackgroundUrl: data?.loginBackgroundUrl ?? null,
    loginTemplateKey: data?.loginTemplateKey ?? 'centered-brand',
    loginSurfaceTone: data?.loginSurfaceTone ?? 'light',
    loginWelcomeTitle: data?.loginWelcomeTitle ?? null,
    loginWelcomeSubtitle: data?.loginWelcomeSubtitle ?? null,
    loginWelcomeHelpText: data?.loginWelcomeHelpText ?? null,
    loginBrandTagline: data?.loginBrandTagline ?? null,
    loginBrandFooterNote: data?.loginBrandFooterNote ?? null,
  })),
  DEFAULT_BRANDING_NAME: 'DocFlow Infinity',
  DEFAULT_LOGIN_BRAND_TAGLINE: 'Acceso institucional',
  DEFAULT_LOGIN_BRAND_FOOTER_NOTE: 'Acceso seguro a la gestión documental',
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

const baseConfiguraciones = [
  {
    id: '60000000-0000-0000-0000-000000000001',
    clave: 'NombreInstitucion',
    valor: 'Municipalidad de Prueba',
    descripcion: 'Nombre de la institución',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
  {
    id: '60000000-0000-0000-0000-000000000002',
    clave: 'MaxAdjuntosMB',
    valor: '10',
    descripcion: 'Tamaño máximo de adjuntos en MB',
    actualizadoEn: '2026-01-01T00:00:00Z',
  },
];

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(useHasPermission).mockReturnValue(true);
  vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([...baseConfiguraciones]);
  vi.mocked(brandingApi.getBranding).mockResolvedValue({
    nombreInstitucion: 'Municipalidad de Prueba',
    logoUrl: '/branding/logo.png',
    loginBackgroundMode: 'gradient',
    loginBackgroundPresetKey: 'midnight-indigo',
    loginBackgroundUrl: '/branding/login-background.png',
    loginTemplateKey: 'centered-brand',
    loginSurfaceTone: 'light',
    loginWelcomeTitle: null,
    loginWelcomeSubtitle: null,
    loginWelcomeHelpText: null,
    loginBrandTagline: null,
    loginBrandFooterNote: null,
  });
  vi.mocked(brandingApi.uploadBrandingLogo).mockResolvedValue({
    id: '60000000-0000-0000-0000-000000000002',
    clave: 'LogoUrl',
    valor: '/branding/logo-updated.png',
    descripcion: 'URL del logo institucional',
    actualizadoEn: '2026-01-01T00:00:00Z',
  });
  vi.mocked(brandingApi.uploadBrandingLoginBackground).mockResolvedValue({
    id: '60000000-0000-0000-0000-000000000003',
    clave: 'LoginBackgroundUrl',
    valor: '/branding/login-background-updated.png',
    descripcion: 'URL del fondo de acceso',
    actualizadoEn: '2026-01-01T00:00:00Z',
  });
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('AdminConfiguracionPage branding', () => {
  it('renders the five tabs and keeps General free of branding sections', async () => {
    renderWithProviders();

    expect(await screen.findByRole('tab', { name: 'General' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Seguridad' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Acceso' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Logo' })).toBeInTheDocument();
    expect(screen.getByRole('tab', { name: 'Bienvenida' })).toBeInTheDocument();

    expect(screen.getByText('NombreInstitucion')).toBeInTheDocument();
    expect(screen.getByText('MaxAdjuntosMB')).toBeInTheDocument();
    expect(screen.queryByText('Diseño de acceso')).toBeNull();
    expect(screen.queryByText('Fondo de acceso')).toBeNull();
    expect(screen.queryByText('Logo institucional')).toBeNull();
    expect(screen.queryByText('Mensaje de bienvenida')).toBeNull();
  });

  it('renders Acceso with login design and background controls, and saves them separately', async () => {
    const user = userEvent.setup();

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Acceso' });
    await user.click(screen.getByRole('tab', { name: 'Acceso' }));

    expect(screen.getByText('Diseño de acceso')).toBeInTheDocument();
    expect(screen.getByText('Fondo de acceso')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^centrada institucional$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^marca dividida$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^claro$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^oscuro$/i })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /marca dividida/i }));
    await user.click(screen.getByRole('button', { name: /ver vista previa/i }));
    expect(screen.getByRole('region', { name: /vista previa del acceso/i })).toHaveTextContent('Marca dividida');
    expect(screen.getByRole('region', { name: /vista previa del acceso/i })).not.toHaveTextContent('Claro');
    expect(screen.getByRole('complementary', { name: 'Identidad institucional' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /guardar diseño de acceso/i }));

    await waitFor(() => {
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginTemplateKey',
        valor: 'split-brand',
        descripcion: expect.any(String),
      });
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginSurfaceTone',
        valor: 'light',
        descripcion: expect.any(String),
      });
    });

    await user.click(screen.getByRole('button', { name: /modo color/i }));
    await user.click(screen.getByRole('button', { name: /índigo/i }));
    await user.click(screen.getByRole('button', { name: /guardar fondo de acceso/i }));

    await waitFor(() => {
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginBackgroundMode',
        valor: 'color',
        descripcion: expect.any(String),
      });
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginBackgroundPresetKey',
        valor: 'indigo',
        descripcion: expect.any(String),
      });
    });
  });

  it('renders the configured background image in the main login preview when image mode is active', async () => {
    const user = userEvent.setup();

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Acceso' });
    await user.click(screen.getByRole('tab', { name: 'Acceso' }));
    await user.click(screen.getByRole('button', { name: /modo imagen/i }));
    await user.click(screen.getByRole('button', { name: /ver vista previa/i }));

    const preview = screen.getByRole('region', { name: /vista previa del acceso/i });
    const previewFrame = screen.getByTestId('login-design-preview-frame');

    expect(within(preview).getAllByAltText('Vista previa del fondo de acceso')).toHaveLength(2);
    expect(previewFrame.style.backgroundImage).toContain('/branding/login-background.png');
  });

  it('keeps the split-brand institutional panel as a flat tone surface without the background image', async () => {
    const user = userEvent.setup();

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Acceso' });
    await user.click(screen.getByRole('tab', { name: 'Acceso' }));
    await user.click(screen.getByRole('button', { name: /modo imagen/i }));
    await user.click(screen.getByRole('button', { name: /marca dividida/i }));
    await user.click(screen.getByRole('button', { name: /ver vista previa/i }));

    const preview = screen.getByRole('region', { name: /vista previa del acceso/i });
    const identityPanel = within(preview).getByRole('complementary', { name: 'Identidad institucional' });

    expect(identityPanel.style.backgroundImage).not.toContain('/branding/login-background.png');
    expect(within(identityPanel).getByText('Acceso institucional')).toBeInTheDocument();
  });

  it('renders the current image inside the image-mode mini preview when available', async () => {
    const user = userEvent.setup();

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Acceso' });
    await user.click(screen.getByRole('tab', { name: 'Acceso' }));
    await user.click(screen.getByRole('button', { name: /modo imagen/i }));

    const miniPreview = screen.getByTestId('login-background-current-preview');

    expect(within(miniPreview).getByAltText('Vista previa del fondo de acceso')).toHaveAttribute(
      'src',
      '/branding/login-background.png',
    );
    expect(screen.getByText('Base visual: Medianoche índigo')).toBeInTheDocument();
  });

  it('falls back to the preset label in the image-mode mini preview when no image exists', async () => {
    const user = userEvent.setup();

    vi.mocked(brandingApi.getBranding).mockResolvedValue({
      nombreInstitucion: 'Municipalidad de Prueba',
      logoUrl: '/branding/logo.png',
      loginBackgroundMode: 'gradient',
      loginBackgroundPresetKey: 'midnight-indigo',
      loginBackgroundUrl: null,
      loginTemplateKey: 'centered-brand',
      loginSurfaceTone: 'light',
      loginWelcomeTitle: null,
      loginWelcomeSubtitle: null,
      loginWelcomeHelpText: null,
    });

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Acceso' });
    await user.click(screen.getByRole('tab', { name: 'Acceso' }));
    await user.click(screen.getByRole('button', { name: /modo imagen/i }));

    const miniPreview = screen.getByTestId('login-background-current-preview');

    expect(within(miniPreview).getByText('Medianoche índigo')).toBeInTheDocument();
    expect(within(miniPreview).queryByAltText('Vista previa del fondo de acceso')).toBeNull();
    expect(screen.getByText('Sin imagen cargada')).toBeInTheDocument();
  });

  it('renders the selected preset as the main login preview backdrop in color mode', async () => {
    const user = userEvent.setup();

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Acceso' });
    await user.click(screen.getByRole('tab', { name: 'Acceso' }));
    await user.click(screen.getByRole('button', { name: /modo color/i }));
    await user.click(screen.getByRole('button', { name: /índigo/i }));
    await user.click(screen.getByRole('button', { name: /ver vista previa/i }));

    const preview = screen.getByRole('region', { name: /vista previa del acceso/i });
    const styledSurfaces = preview.querySelectorAll('[style*="background-image"], [style*="background-color"]');

    expect(styledSurfaces.length).toBeGreaterThanOrEqual(2);
  });

  it('mirrors the real login hierarchy and removes obsolete Claro/Oscuro copy from the preview', async () => {
    const user = userEvent.setup();

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Acceso' });
    await user.click(screen.getByRole('tab', { name: 'Acceso' }));

    await user.click(screen.getByRole('button', { name: /ver vista previa/i }));

    const centeredPreview = screen.getByRole('region', { name: /vista previa del acceso/i });

    expect(within(centeredPreview).getByRole('region', { name: 'Información de acceso' })).toBeInTheDocument();
    expect(within(centeredPreview).queryByText('Claro')).toBeNull();
    expect(within(centeredPreview).queryByText('Oscuro')).toBeNull();
    expect(within(centeredPreview).queryByText('Acceso institucional')).toBeNull();

    await user.click(screen.getByRole('button', { name: /marca dividida/i }));

    const splitPreview = screen.getByRole('region', { name: /vista previa del acceso/i });
    const identity = within(splitPreview).getByRole('complementary', { name: 'Identidad institucional' });
    const authSurface = within(splitPreview).getByRole('region', { name: 'Superficie de acceso clara' });

    expect(within(identity).getByRole('heading', { name: 'Municipalidad de Prueba' })).toBeInTheDocument();
    expect(within(authSurface).getByRole('heading', { name: 'Inicio de sesión' })).toBeInTheDocument();
    expect(within(authSurface).queryByText('Acceso institucional')).toBeNull();
  });

  it('shows the image upload area only in image mode and uploads a new file', async () => {
    const user = userEvent.setup();
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:background-preview-url');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Acceso' });
    await user.click(screen.getByRole('tab', { name: 'Acceso' }));
    await user.click(screen.getByRole('button', { name: /modo imagen/i }));

    expect(screen.getByLabelText('Archivo de fondo de acceso')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /aurora/i })).toBeNull();

    const fileInput = screen.getByLabelText('Archivo de fondo de acceso');
    const file = new File(['fake-image'], 'login-background.png', { type: 'image/png' });
    await user.upload(fileInput, file);

    expect(screen.getAllByAltText('Vista previa del fondo de acceso')[0]).toHaveAttribute('src', 'blob:background-preview-url');

    await user.click(screen.getByRole('button', { name: /subir fondo/i }));

    await waitFor(() => {
      expect(brandingApi.uploadBrandingLoginBackground).toHaveBeenCalled();
      expect(vi.mocked(brandingApi.uploadBrandingLoginBackground).mock.calls[0][0]).toBe(file);
    });
  });

  it('renders Logo with preview, current state, and upload flow', async () => {
    const user = userEvent.setup();
    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:preview-url');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => {});

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Logo' });
    await user.click(screen.getByRole('tab', { name: 'Logo' }));

    expect(screen.getByText('Logo institucional')).toBeInTheDocument();
    expect(screen.queryByText('LogoUrl')).toBeNull();
    expect(await screen.findByAltText('Vista previa del logo institucional')).toHaveAttribute('src', expect.stringContaining('/branding/logo.png'));
    expect(screen.getByText('Logo cargado actualmente')).toBeInTheDocument();

    const fileInput = screen.getByLabelText('Archivo de logo institucional');
    const file = new File(['fake-image'], 'logo.png', { type: 'image/png' });
    await user.upload(fileInput, file);

    expect(screen.getByAltText('Vista previa del logo institucional')).toHaveAttribute('src', 'blob:preview-url');
    expect(screen.getByText('logo.png')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /subir logo/i }));

    await waitFor(() => {
      expect(brandingApi.uploadBrandingLogo).toHaveBeenCalled();
      expect(vi.mocked(brandingApi.uploadBrandingLogo).mock.calls[0][0]).toBe(file);
    });
  });

  it('renders the split-brand panel texts and saves them independently', async () => {
    const user = userEvent.setup();

    vi.mocked(brandingApi.getBranding).mockResolvedValue({
      nombreInstitucion: 'Municipalidad de Prueba',
      logoUrl: '/branding/logo.png',
      loginBackgroundMode: 'gradient',
      loginBackgroundPresetKey: 'midnight-indigo',
      loginBackgroundUrl: null,
      loginTemplateKey: 'split-brand',
      loginSurfaceTone: 'light',
      loginWelcomeTitle: null,
      loginWelcomeSubtitle: null,
      loginWelcomeHelpText: null,
      loginBrandTagline: 'Portal de trámites',
      loginBrandFooterNote: 'Plataforma oficial',
    });

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Bienvenida' });
    await user.click(screen.getByRole('tab', { name: 'Bienvenida' }));

    expect(screen.getByText('Textos del panel de marca dividida')).toBeInTheDocument();
    expect(screen.getByLabelText('Texto sobre el nombre')).toHaveValue('Portal de trámites');
    expect(screen.getByLabelText('Nota al pie')).toHaveValue('Plataforma oficial');

    await user.clear(screen.getByLabelText('Texto sobre el nombre'));
    await user.type(screen.getByLabelText('Texto sobre el nombre'), 'Acceso del personal');
    await user.clear(screen.getByLabelText('Nota al pie'));
    await user.type(screen.getByLabelText('Nota al pie'), 'Uso interno');

    await user.click(screen.getByRole('button', { name: /guardar textos del panel/i }));

    await waitFor(() => {
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginBrandTagline',
        valor: 'Acceso del personal',
        descripcion: expect.any(String),
      });
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginBrandFooterNote',
        valor: 'Uso interno',
        descripcion: expect.any(String),
      });
    });
  });

  it('renders Bienvenida with fields and saves the welcome message independently', async () => {
    const user = userEvent.setup();

    vi.mocked(brandingApi.getBranding).mockResolvedValue({
      nombreInstitucion: 'Municipalidad de Prueba',
      logoUrl: '/branding/logo.png',
      loginBackgroundMode: 'gradient',
      loginBackgroundPresetKey: 'midnight-indigo',
      loginBackgroundUrl: '/branding/login-background.png',
      loginTemplateKey: 'centered-brand',
      loginSurfaceTone: 'light',
      loginWelcomeTitle: 'Bienvenido',
      loginWelcomeSubtitle: 'Gestiona tus trámites',
      loginWelcomeHelpText: 'Si necesitas ayuda, contacta soporte.',
    });

    vi.mocked(adminConfiguracionApi.getConfiguraciones).mockResolvedValue([
      ...baseConfiguraciones,
      {
        id: '60000000-0000-0000-0000-000000000010',
        clave: 'LoginWelcomeTitle',
        valor: 'Bienvenido',
        descripcion: 'Título de bienvenida',
        actualizadoEn: '2026-01-01T00:00:00Z',
      },
      {
        id: '60000000-0000-0000-0000-000000000011',
        clave: 'LoginWelcomeSubtitle',
        valor: 'Gestiona tus trámites',
        descripcion: 'Subtítulo de bienvenida',
        actualizadoEn: '2026-01-01T00:00:00Z',
      },
      {
        id: '60000000-0000-0000-0000-000000000012',
        clave: 'LoginWelcomeHelpText',
        valor: 'Si necesitas ayuda, contacta soporte.',
        descripcion: 'Ayuda de bienvenida',
        actualizadoEn: '2026-01-01T00:00:00Z',
      },
    ]);

    renderWithProviders();

    await screen.findByRole('tab', { name: 'Bienvenida' });
    await user.click(screen.getByRole('tab', { name: 'Bienvenida' }));

    expect(screen.getByText('Mensaje de bienvenida')).toBeInTheDocument();
    expect(screen.getByLabelText('Título')).toHaveValue('Bienvenido');
    expect(screen.getByLabelText('Subtítulo')).toHaveValue('Gestiona tus trámites');
    expect(screen.getByLabelText('Ayuda (opcional)')).toHaveValue('Si necesitas ayuda, contacta soporte.');

    await user.clear(screen.getByLabelText('Título'));
    await user.type(screen.getByLabelText('Título'), 'Hola');
    await user.clear(screen.getByLabelText('Subtítulo'));
    await user.type(screen.getByLabelText('Subtítulo'), 'Accede a tu cuenta');
    await user.clear(screen.getByLabelText('Ayuda (opcional)'));
    await user.type(screen.getByLabelText('Ayuda (opcional)'), 'Escribe a soporte');

    await user.click(screen.getByRole('button', { name: /guardar mensaje de bienvenida/i }));

    await waitFor(() => {
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginWelcomeTitle',
        valor: 'Hola',
        descripcion: expect.any(String),
      });
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginWelcomeSubtitle',
        valor: 'Accede a tu cuenta',
        descripcion: expect.any(String),
      });
      expect(adminConfiguracionApi.upsertConfiguracion).toHaveBeenCalledWith({
        clave: 'LoginWelcomeHelpText',
        valor: 'Escribe a soporte',
        descripcion: expect.any(String),
      });
    });
  });
});
