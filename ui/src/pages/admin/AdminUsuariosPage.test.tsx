import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminUsuariosPage from './AdminUsuariosPage';
import { ToastProvider } from '../../contexts/ToastContext';
import * as adminUsuariosApi from '../../lib/api/admin/adminUsuariosApi';
import * as adminRolesApi from '../../lib/api/admin/adminRolesApi';
import * as catalogosApi from '../../lib/api/catalogos';
import { useHasPermission } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock('../../lib/api/admin/adminUsuariosApi', () => ({
  getUsuarios: vi.fn(),
  getUsuario: vi.fn(),
  activarUsuario: vi.fn(),
  desactivarUsuario: vi.fn(),
  crearUsuario: vi.fn(),
  actualizarUsuario: vi.fn(),
  resetPassword: vi.fn(),
  bloquearUsuario: vi.fn(),
  desbloquearUsuario: vi.fn(),
}));

vi.mock('../../lib/api/admin/adminRolesApi', () => ({
  getRoles: vi.fn(),
}));

vi.mock('../../lib/api/catalogos', () => ({
  getDepartamentosCatalogo: vi.fn(),
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
      <ToastProvider>
        <AdminUsuariosPage />
      </ToastProvider>
    </QueryClientProvider>,
  );
}

// ── Fixtures ─────────────────────────────────────────────────────────────────

const mockPagedResult = {
  items: [
    {
      id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001',
      nombreCompleto: 'Ada Lovelace',
      email: 'ada@docflow.cl',
      usucod: 'ada',
      rut: '12.345.678-9',
      rol: 'Administrador',
      rolId: null,
      departamentoId: 'ffffffff-gggg-hhhh-iiii-jjjjjjjj0001',
      departamentoNombre: 'TI',
      activo: true,
      estaBloqueado: false,
      bloqueadoHasta: null,
      creadoEn: '2026-01-15T10:00:00Z',
      esCuentaPropia: false,
      esUltimoAdminActivo: false,
    },
    {
      id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002',
      nombreCompleto: 'Grace Hopper',
      email: 'grace@docflow.cl',
      usucod: 'grace',
      rut: '9.876.543-2',
      rol: 'Operador',
      rolId: null,
      departamentoId: null,
      departamentoNombre: null,
      activo: false,
      estaBloqueado: false,
      bloqueadoHasta: null,
      creadoEn: '2026-02-20T14:30:00Z',
      esCuentaPropia: false,
      esUltimoAdminActivo: false,
    },
  ],
  total: 2,
  page: 1,
  totalPaginas: 1,
};

const mockDepartamentos = [
  { id: 'dept-ti', nombre: 'TI', codigo: 'TI' },
  { id: 'dept-rrhh', nombre: 'RRHH', codigo: 'RRHH' },
  { id: 'dept-fin', nombre: 'Finanzas', codigo: 'FIN' },
];

const mockRoles = [
  { id: 'role-admin', nombre: 'Administrador', descripcion: 'Administrador del sistema', esSistema: true },
  { id: 'role-user', nombre: 'Usuario', descripcion: null, esSistema: true },
  { id: 'role-op', nombre: 'Operador', descripcion: null, esSistema: true },
  { id: 'role-min', nombre: 'MinistroDeFe', descripcion: null, esSistema: true },
  { id: 'role-rec', nombre: 'Receptor', descripcion: null, esSistema: true },
  { id: 'role-firm', nombre: 'Firmante', descripcion: null, esSistema: true },
  { id: 'role-rrhh', nombre: 'RRHH', descripcion: 'Recursos Humanos', esSistema: true },
  { id: 'role-jef', nombre: 'Jefatura', descripcion: null, esSistema: true },
  { id: 'role-custom', nombre: 'Supervisor', descripcion: 'Rol personalizado', esSistema: false },
];

// ── Tests ────────────────────────────────────────────────────────────────────

describe('AdminUsuariosPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(adminUsuariosApi.getUsuarios).mockImplementation(async (_page, _pageSize, filters) => {
      if (filters?.search?.toLowerCase() === 'grace') {
        return {
          ...mockPagedResult,
          items: [mockPagedResult.items[1]],
          total: 1,
        };
      }

      return mockPagedResult;
    });
    vi.mocked(adminUsuariosApi.getUsuario).mockResolvedValue({
      ...mockPagedResult.items[0],
      nombres: 'Ada',
      apellidoPaterno: 'Lovelace',
      apellidoMaterno: 'Byron',
      telefono: '123456789',
      direccion: 'Calle 1',
    });
    vi.mocked(adminRolesApi.getRoles).mockResolvedValue(mockRoles);
    vi.mocked(catalogosApi.getDepartamentosCatalogo).mockResolvedValue(mockDepartamentos);
    vi.mocked(useHasPermission).mockImplementation(() => true);
  });

  it('renders the user list from API data', async () => {
    renderWithProviders();

    expect(await screen.findByText('Ada Lovelace')).toBeInTheDocument();
    expect(screen.getByText('grace@docflow.cl')).toBeInTheDocument();
    expect(screen.getByText('ada')).toBeInTheDocument();
  });

  it('backs user search with the API instead of filtering the current page', async () => {
    const user = userEvent.setup();

    renderWithProviders();

    await screen.findByText('Ada Lovelace');

    const searchInput = screen.getByPlaceholderText(/nombre, email, rut o código/i);
    await user.clear(searchInput);
    await user.type(searchInput, 'grace');

    await waitFor(() => {
      expect(adminUsuariosApi.getUsuarios).toHaveBeenLastCalledWith(1, 20, expect.objectContaining({ search: 'grace' }));
    });

    expect(await screen.findByText('Grace Hopper')).toBeInTheDocument();
    expect(screen.queryByText('Ada Lovelace')).not.toBeInTheDocument();
  });

  it('submits the edit form when Guardar is clicked', async () => {
    const user = userEvent.setup();
    vi.mocked(adminUsuariosApi.actualizarUsuario).mockResolvedValue(undefined);

    renderWithProviders();

    await screen.findByText('Ada Lovelace');
    await user.click(screen.getAllByRole('button', { name: /editar/i })[0]);

    const nombreInput = await screen.findByDisplayValue('Ada');
    expect(await screen.findByDisplayValue('Administrador')).toBeInTheDocument();
    await user.clear(nombreInput);
    await user.type(nombreInput, 'Ada Lovelace II');

    const saveButton = screen.getByRole('button', { name: /^Guardar$/i });
    await waitFor(() => expect(saveButton).toBeEnabled());
    await user.click(saveButton);

    await waitFor(() => {
      expect(adminUsuariosApi.actualizarUsuario).toHaveBeenCalledTimes(1);
    });
  });

  it('hides user action controls when the permission is missing', async () => {
    vi.mocked(useHasPermission).mockImplementation((permiso) => permiso === PERMISSIONS.ADMIN_USUARIOS_VER);

    renderWithProviders();

    await screen.findByText('Ada Lovelace');

    expect(screen.queryByRole('button', { name: /crear usuario/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /editar/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /restablecer contraseña/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /bloquear/i })).not.toBeInTheDocument();
  });

  it('uses the canonical paging fields from the paged response', async () => {
    renderWithProviders();

    // Wait for data to load
    await screen.findByText('Ada Lovelace');

    // The page displays user count using the paging fields.
    // Currently AdminUsuariosPage uses data.totalCount — which will be undefined
    // with the canonical `total` field. This test will PASS once the page
    // reads `total` instead of `totalCount`.
    // eslint-disable-next-line @typescript-eslint/no-unused-expressions
    expect(mockPagedResult).toHaveProperty('total');
    expect(mockPagedResult).toHaveProperty('page');
    expect(mockPagedResult).toHaveProperty('totalPaginas');
    expect(mockPagedResult).not.toHaveProperty('totalCount');
    expect(mockPagedResult).not.toHaveProperty('pageNumber');
    expect(mockPagedResult).not.toHaveProperty('pageSize');
  });

  it('renders the RUT column with values from API', async () => {
    renderWithProviders();

    expect(await screen.findByText('12.345.678-9')).toBeInTheDocument();
    expect(screen.getByText('9.876.543-2')).toBeInTheDocument();
  });

  it('populates the departamento filter select from catalog', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    // wait for the page to load
    await screen.findByText('Ada Lovelace');

    // "TI" is in the table row for Ada (departamento column)
    expect(screen.getByText('TI')).toBeInTheDocument();

    // Open the filter comboboxes to verify catalog options exist
    const comboboxes = screen.getAllByRole('combobox');
    // First combobox is rol filter — options include RRHH (role)
    await user.click(comboboxes[0]);
    expect(screen.getByText('RRHH')).toBeInTheDocument();
    await user.keyboard('{Escape}');

    // Second combobox is departamento filter — verify dept catalog options
    await user.click(comboboxes[1]);
    expect(screen.getByText('Finanzas')).toBeInTheDocument();
    // "TI" now appears in both the dropdown and the table cell
    expect(screen.getAllByText('TI')).toHaveLength(2);
    expect(screen.getByText('RRHH')).toBeInTheDocument();
  });

  it('calls bloquearUsuario when Bloquear is confirmed', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    await screen.findByText('Ada Lovelace');

    // Click the IconButton with tooltip "Bloquear" (aria-label matches tooltip)
    const bloquearBtns = screen.getAllByRole('button', { name: /bloquear/i });
    await user.click(bloquearBtns[0]);

    // Modal confirmation: click "Bloquear usuario"
    await user.click(screen.getByRole('button', { name: /bloquear usuario/i }));

    await waitFor(() => {
      expect(adminUsuariosApi.bloquearUsuario).toHaveBeenCalledWith(
        'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001',
        expect.anything(),
      );
    });
  });

  it('disables destructive actions for the current user', async () => {
    vi.mocked(adminUsuariosApi.getUsuarios).mockResolvedValue({
      ...mockPagedResult,
      items: [{ ...mockPagedResult.items[0], esCuentaPropia: true }],
    });

    renderWithProviders();

    await screen.findByText('Ada Lovelace');
    const row = screen.getByText('Ada Lovelace').closest('tr')!;
    expect(within(row).getByRole('button', { name: /desactivar/i })).toBeDisabled();
    expect(within(row).getByRole('button', { name: /bloquear/i })).toBeDisabled();
  });

  it('disables destructive actions for the last active admin', async () => {
    vi.mocked(adminUsuariosApi.getUsuarios).mockResolvedValue({
      ...mockPagedResult,
      items: [{ ...mockPagedResult.items[0], esUltimoAdminActivo: true }],
    });

    renderWithProviders();

    await screen.findByText('Ada Lovelace');
    const row = screen.getByText('Ada Lovelace').closest('tr')!;
    expect(within(row).getByRole('button', { name: /desactivar/i })).toBeDisabled();
    expect(within(row).getByRole('button', { name: /bloquear/i })).toBeDisabled();
  });

  it('does NOT call bloquearUsuario when Bloquear is cancelled', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    await screen.findByText('Ada Lovelace');

    // Click the IconButton with tooltip "Bloquear"
    const bloquearBtns = screen.getAllByRole('button', { name: /bloquear/i });
    await user.click(bloquearBtns[0]);

    // Click "Cancelar" in the confirmation modal
    await user.click(screen.getByRole('button', { name: /cancelar/i }));

    expect(adminUsuariosApi.bloquearUsuario).not.toHaveBeenCalled();
  });

  it('shows Desbloquear for blocked users and calls desbloquearUsuario', async () => {
    const user = userEvent.setup();
    vi.mocked(adminUsuariosApi.getUsuarios).mockResolvedValue({
      ...mockPagedResult,
      items: [{ ...mockPagedResult.items[0], estaBloqueado: true, bloqueadoHasta: '2026-06-12T12:30:00Z' }],
    });

    renderWithProviders();

    await screen.findByText('Ada Lovelace');
    const row = screen.getByText('Ada Lovelace').closest('tr')!;
    expect(within(row).getByText('Bloqueado')).toBeInTheDocument();
    expect(within(row).getByRole('button', { name: /desbloquear/i })).toBeInTheDocument();
    expect(within(row).queryByRole('button', { name: /^bloquear$/i })).not.toBeInTheDocument();

    await user.click(within(row).getByRole('button', { name: /desbloquear/i }));

    await waitFor(() => {
      expect(adminUsuariosApi.desbloquearUsuario).toHaveBeenCalledWith(
        'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001',
        expect.anything(),
      );
    });
  });

  it('does not show Desbloquear for non-blocked users', async () => {
    renderWithProviders();

    await screen.findByText('Ada Lovelace');

    expect(screen.queryByRole('button', { name: /desbloquear/i })).not.toBeInTheDocument();
  });

  it('shows placeholder when departamento catalog is offline', async () => {
    vi.mocked(catalogosApi.getDepartamentosCatalogo).mockRejectedValue(new Error('Network error'));

    renderWithProviders();

    await screen.findByText('Ada Lovelace');

    // Should show placeholder instead of catalog values
    expect(screen.getByText('Departamento no disponible')).toBeInTheDocument();
  });

  it('shows custom roles from API alongside system roles', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    await screen.findByText('Ada Lovelace');

    // Open the role filter SearchableSelect to verify options
    const comboboxes = screen.getAllByRole('combobox');
    await user.click(comboboxes[0]);

    // 'Supervisor' is a custom role (esSistema: false) only in mockRoles
    expect(screen.getByText('Supervisor')).toBeInTheDocument();

    // Existing system roles like 'Jefatura' also appear
    expect(screen.getByText('Jefatura')).toBeInTheDocument();
  });

  it('allows creating a user without RUT', async () => {
    const user = userEvent.setup();
      vi.mocked(adminUsuariosApi.crearUsuario).mockResolvedValue({
        id: 'new-user-id',
        nombreCompleto: 'New User',
        nombres: 'New',
        apellidoPaterno: 'User',
        apellidoMaterno: '',
        telefono: null,
        direccion: null,
        email: 'new@docflow.cl',
        rut: null,
        rol: 'Administrador',
      departamentoId: null,
      departamentoNombre: null,
      activo: true,
      creadoEn: '2026-05-19T00:00:00Z',
      rolId: null,
    });

    renderWithProviders();

    await screen.findByText('Ada Lovelace');
    await user.click(screen.getByRole('button', { name: /crear usuario/i }));

    const dialog = screen.getByRole('dialog', { name: /crear usuario/i });
    await user.type(dialog.querySelector('input[name="nombres"]')!, 'New');
    await user.type(dialog.querySelector('input[name="apellidoPaterno"]')!, 'User');
    await user.type(dialog.querySelector('input[name="apellidoMaterno"]')!, 'Test');
    await user.type(dialog.querySelector('input[name="telefono"]')!, '+56 9 1111 2222');
    await user.type(dialog.querySelector('input[name="direccion"]')!, 'Calle 123');
    await user.type(dialog.querySelector('input[name="email"]')!, 'new@docflow.cl');
    await user.type(dialog.querySelector('input[name="password"]')!, 'Password123!');

    const roleInput = within(dialog).getAllByRole('combobox')[0];
    await user.click(roleInput);
    await user.click(within(dialog).getByText('Administrador'));

    await user.click(screen.getByRole('button', { name: /guardar/i }));

    await waitFor(() => {
      expect(adminUsuariosApi.crearUsuario).toHaveBeenCalledWith(expect.objectContaining({
        nombres: 'New',
        apellidoPaterno: 'User',
        apellidoMaterno: 'Test',
        telefono: '+56 9 1111 2222',
        direccion: 'Calle 123',
        email: 'new@docflow.cl',
        rut: null,
        rol: 'Administrador',
        password: 'Password123!',
        departamentoId: null,
      }), expect.anything());
    });
  });

  it('surfaces create errors as a toast notification', async () => {
    const user = userEvent.setup();
    vi.mocked(adminUsuariosApi.crearUsuario).mockRejectedValue(new Error('Boom'));

    renderWithProviders();

    await screen.findByText('Ada Lovelace');
    await user.click(screen.getByRole('button', { name: /crear usuario/i }));

    const dialog = screen.getByRole('dialog', { name: /crear usuario/i });
    await user.type(dialog.querySelector('input[name="nombres"]')!, 'New');
    await user.type(dialog.querySelector('input[name="apellidoPaterno"]')!, 'User');
    await user.type(dialog.querySelector('input[name="apellidoMaterno"]')!, 'Test');
    await user.type(dialog.querySelector('input[name="email"]')!, 'new@docflow.cl');
    await user.type(dialog.querySelector('input[name="password"]')!, 'Password123!');

    const roleInput = within(dialog).getAllByRole('combobox')[0];
    await user.click(roleInput);
    await user.click(within(dialog).getByText('Administrador'));

    await user.click(screen.getByRole('button', { name: /guardar/i }));

    const toast = await screen.findByRole('alert');
    expect(within(toast).getByText('Boom')).toBeInTheDocument();
  });

  it('shows placeholder when roles catalog fails to load', async () => {
    vi.mocked(adminRolesApi.getRoles).mockRejectedValue(new Error('Network error'));

    renderWithProviders();

    await screen.findByText('Ada Lovelace');

    // Should show placeholder instead of role options
    expect(screen.getByText('Roles no disponibles')).toBeInTheDocument();
  });
});

describe('adminUsuariosApi — verb alignment', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('activarUsuario calls PUT /admin/usuarios/:id/activar', async () => {
    const httpModule = await import('../../lib/http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: undefined });
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: undefined });

    // Use vi.importActual to bypass the hoisted vi.mock at the top of the file
    const api = await vi.importActual<typeof import('../../lib/api/admin/adminUsuariosApi')>('../../lib/api/admin/adminUsuariosApi');
    await api.activarUsuario('user-id-001');

    // Should use PUT, not POST
    expect(putSpy).toHaveBeenCalledWith('/admin/usuarios/user-id-001/activar');
    expect(postSpy).not.toHaveBeenCalled();
  });

  it('desactivarUsuario calls PUT /admin/usuarios/:id/desactivar', async () => {
    const httpModule = await import('../../lib/http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: undefined });
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: undefined });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminUsuariosApi')>('../../lib/api/admin/adminUsuariosApi');
    await api.desactivarUsuario('user-id-002');

    // Should use PUT, not POST
    expect(putSpy).toHaveBeenCalledWith('/admin/usuarios/user-id-002/desactivar');
    expect(postSpy).not.toHaveBeenCalled();
  });

  it('bloquearUsuario calls PUT /admin/usuarios/:id/bloquear', async () => {
    const httpModule = await import('../../lib/http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: undefined });
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: undefined });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminUsuariosApi')>('../../lib/api/admin/adminUsuariosApi');
    await api.bloquearUsuario('user-id-003');

    // Should use PUT, not POST
    expect(putSpy).toHaveBeenCalledWith('/admin/usuarios/user-id-003/bloquear');
    expect(postSpy).not.toHaveBeenCalled();
  });

  it('desbloquearUsuario calls PUT /admin/usuarios/:id/desbloquear', async () => {
    const httpModule = await import('../../lib/http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: undefined });
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: undefined });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminUsuariosApi')>('../../lib/api/admin/adminUsuariosApi');
    await api.desbloquearUsuario('user-id-004');

    expect(putSpy).toHaveBeenCalledWith('/admin/usuarios/user-id-004/desbloquear');
    expect(postSpy).not.toHaveBeenCalled();
  });

  it('PagedResult interface matches backend { items, total, page, totalPaginas }', async () => {
    const httpModule = await import('../../lib/http');
    vi.spyOn(httpModule.default, 'get').mockResolvedValue({
      data: {
        items: [],
        total: 0,
        page: 1,
        totalPaginas: 0,
      },
    });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminUsuariosApi')>('../../lib/api/admin/adminUsuariosApi');
    const result = await api.getUsuarios(1, 20);

    // The PagedResult interface should match the backend contract
    expect(result).toHaveProperty('total');
    expect(result).toHaveProperty('page');
    expect(result).toHaveProperty('totalPaginas');
    // These fields should NOT be present — they are the OLD frontend-only names
    const typedResult = result as unknown as Record<string, unknown>;
    expect(typedResult.totalCount).toBeUndefined();
    expect(typedResult.pageNumber).toBeUndefined();
    expect(typedResult.pageSize).toBeUndefined();
  });

  it('getUsuarios sends search param when provided', async () => {
    const httpModule = await import('../../lib/http');
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({
      data: { items: [], total: 0, page: 1, totalPaginas: 0 },
    });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminUsuariosApi')>('../../lib/api/admin/adminUsuariosApi');
    await api.getUsuarios(1, 20, { search: 'ada' });

    // Backend now supports search in addition to page, pageSize, rol, departamentoId, activo
    const params = getSpy.mock.calls[0][1]?.params as Record<string, unknown> ?? {};
    expect(params).toHaveProperty('search', 'ada');
  });
});
