import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import AdminRolesPage from './AdminRolesPage';
import * as adminRolesApi from '../../lib/api/admin/adminRolesApi';
import * as adminPermisosApi from '../../lib/api/admin/adminPermisosApi';
import { useHasPermission } from '../../hooks/usePermissions';

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock('../../lib/api/admin/adminRolesApi', () => ({
  getRoles: vi.fn(),
  crearRol: vi.fn(),
  actualizarRol: vi.fn(),
  eliminarRol: vi.fn(),
  getPermisosRol: vi.fn(),
}));

vi.mock('../../lib/api/admin/adminPermisosApi', () => ({
  getPermisos: vi.fn(),
  assignPermisosRol: vi.fn(),
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
      <BrowserRouter>
        <AdminRolesPage />
      </BrowserRouter>
    </QueryClientProvider>,
  );
}

// ── Fixtures ─────────────────────────────────────────────────────────────────

const mockRoles = [
  {
    id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001',
    nombre: 'Administrador',
    descripcion: 'Acceso total al sistema',
    esSistema: true,
    permisos: [
      { id: 'p1', nombre: 'ver', descripcion: 'Ver usuarios', grupo: 'admin.usuarios' },
      { id: 'p2', nombre: 'crear', descripcion: 'Crear usuarios', grupo: 'admin.usuarios' },
    ],
  },
  {
    id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002',
    nombre: 'Supervisor',
    descripcion: null,
    esSistema: false,
    permisos: [
      { id: 'p3', nombre: 'ver', descripcion: 'Ver docs', grupo: 'documentos' },
    ],
  },
  {
    id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0003',
    nombre: 'Usuario',
    descripcion: 'Usuario estándar',
    esSistema: false,
  },
];

const mockPermisos = [
  { id: 'p1', nombre: 'ver', descripcion: 'Ver usuarios', grupo: 'admin.usuarios' },
  { id: 'p2', nombre: 'crear', descripcion: 'Crear usuarios', grupo: 'admin.usuarios' },
];

// ── Tests ────────────────────────────────────────────────────────────────────

describe('AdminRolesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(adminRolesApi.getRoles).mockResolvedValue(mockRoles);
    vi.mocked(adminRolesApi.getPermisosRol).mockResolvedValue([]);
    vi.mocked(adminPermisosApi.getPermisos).mockResolvedValue(mockPermisos);
    vi.mocked(adminPermisosApi.assignPermisosRol).mockResolvedValue(undefined);
    vi.mocked(useHasPermission).mockReturnValue(true);
  });

  it('renders roles grouped into system and custom sections', async () => {
    renderWithProviders();

    expect(await screen.findByText('Roles del Sistema')).toBeInTheDocument();
    expect(screen.getByText('Roles Personalizados')).toBeInTheDocument();
  });

  it('renders role names from API data', async () => {
    renderWithProviders();

    expect(await screen.findByText('Administrador')).toBeInTheDocument();
    expect(screen.getByText('Supervisor')).toBeInTheDocument();
    expect(screen.getByText('Usuario')).toBeInTheDocument();
  });

  it('renders type badges for system and custom roles', async () => {
    renderWithProviders();

    await screen.findByText('Administrador');

    expect(screen.getByText('Sistema')).toBeInTheDocument();
    const customBadges = screen.getAllByText('Personalizado');
    expect(customBadges).toHaveLength(2);
  });

  it('shows permissions for roles that have them', async () => {
    renderWithProviders();

    await screen.findByText('Administrador');

    // Administrador has 2 permisos
    expect(screen.getByText('2 permisos')).toBeInTheDocument();
    // Supervisor has 1 permiso
    expect(screen.getByText('1 permiso')).toBeInTheDocument();
  });

  it('shows permission descriptions instead of raw keys in the role rows', async () => {
    renderWithProviders();

    await screen.findByText('Administrador');

    // Human description as visible text; the raw key stays only in the tooltip.
    expect(screen.getByText('Ver usuarios')).toBeInTheDocument();
    expect(screen.getByText('Crear usuarios')).toBeInTheDocument();
  });

  it('shows friendly group titles and descriptions in the permission tree', async () => {
    vi.mocked(adminPermisosApi.getPermisos).mockResolvedValue([
      ...mockPermisos,
      { id: 'p9', nombre: 'ordenescompra.ver', descripcion: 'Ver órdenes de compra', grupo: 'ordenescompra' },
    ]);
    const user = userEvent.setup();
    renderWithProviders();

    await screen.findByText('Usuario');
    // Open edit on the last (custom) role to reach the permission tree
    const editButtons = screen.getAllByRole('button', { name: /^editar$/i });
    await user.click(editButtons[editButtons.length - 1]);

    // Friendly group title, not the raw capitalized key
    const grupoHeader = await screen.findByText('Órdenes de Compra');
    expect(grupoHeader).toBeInTheDocument();

    // Expand the group: the permission shows its description as the main label
    await user.click(grupoHeader);
    expect(await screen.findByText('Ver órdenes de compra')).toBeInTheDocument();
  });

  it('shows placeholder text for roles without permissions', async () => {
    renderWithProviders();

    await screen.findByText('Usuario');

    expect(screen.getByText('Seleccionar al editar')).toBeInTheDocument();
  });

  it('renders delete button for each role row', async () => {
    renderWithProviders();

    await screen.findByText('Administrador');

    const deleteButtons = screen.getAllByRole('button', { name: /eliminar/i });
    expect(deleteButtons).toHaveLength(3);
  });

  it('disables delete button for system roles with tooltip', async () => {
    renderWithProviders();

    await screen.findByText('Administrador');

    const allDeleteButtons = screen.getAllByRole('button', { name: /eliminar/i });

    // The first button (Administrador — sistema) should be disabled
    const sistemaDeleteBtn = allDeleteButtons[0];
    expect(sistemaDeleteBtn).toBeDisabled();

    // The second button (Supervisor — not sistema) should NOT be disabled
    const customDeleteBtn = allDeleteButtons[1];
    expect(customDeleteBtn).not.toBeDisabled();
  });

  it('opens create modal on "Nuevo Rol" click', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    await screen.findByText('Administrador');

    await user.click(screen.getByRole('button', { name: /nuevo rol/i }));

    expect(screen.getByRole('dialog', { name: 'Nuevo Rol' })).toBeInTheDocument();
    expect(screen.getByText('Nuevo Rol')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Nombre del rol')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Descripción opcional')).toBeInTheDocument();
  });

  it('master checkbox selects and clears all permisos across all modules', async () => {
    const user = userEvent.setup();
    vi.mocked(adminPermisosApi.getPermisos).mockResolvedValue([
      { id: 'p1', nombre: 'ver', descripcion: 'Ver usuarios', grupo: 'admin.usuarios' },
      { id: 'p2', nombre: 'crear', descripcion: 'Crear usuarios', grupo: 'admin.usuarios' },
      { id: 'p3', nombre: 'ver', descripcion: 'Ver docs', grupo: 'documentos' },
    ]);
    vi.mocked(adminRolesApi.getPermisosRol).mockResolvedValue([]);

    renderWithProviders();
    await screen.findByText('Administrador');
    await user.click(screen.getAllByRole('button', { name: /editar/i })[1]);

    const master = await screen.findByRole('checkbox', { name: 'Seleccionar todos los permisos de todos los módulos' });
    const grupoUsuarios = screen.getByRole('checkbox', { name: 'Seleccionar todos los permisos de Admin.usuarios' });
    const grupoDocs = screen.getByRole('checkbox', { name: 'Seleccionar todos los permisos de Documentos' });

    expect(master).not.toBeChecked();

    // Marca todos los módulos a la vez
    await user.click(master);
    expect(master).toBeChecked();
    expect(grupoUsuarios).toBeChecked();
    expect(grupoDocs).toBeChecked();

    // Desmarca todos a la vez
    await user.click(master);
    expect(master).not.toBeChecked();
    expect(grupoUsuarios).not.toBeChecked();
    expect(grupoDocs).not.toBeChecked();
  });

  it('opens edit modal as an accessible dialog with current role values', async () => {
    const user = userEvent.setup();
    vi.mocked(adminRolesApi.getPermisosRol).mockResolvedValue(mockPermisos.slice(0, 1));

    renderWithProviders();

    await screen.findByText('Administrador');

    const editButtons = screen.getAllByRole('button', { name: /editar/i });
    await user.click(editButtons[1]);

    expect(screen.getByRole('dialog', { name: 'Editar Rol: Supervisor' })).toBeInTheDocument();
    expect(screen.getByDisplayValue('Supervisor')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Descripción opcional')).toBeInTheDocument();
  });

  it('shows server error inline below nombre field on create failure', async () => {
    const user = userEvent.setup();
    const serverError = new Error('Ya existe un rol con el nombre Admin.');
    (serverError as any).userMessage = 'Ya existe un rol con el nombre Admin.';
    vi.mocked(adminRolesApi.crearRol).mockRejectedValue(serverError);

    renderWithProviders();

    await screen.findByText('Administrador');

    // Open modal
    await user.click(screen.getByRole('button', { name: /nuevo rol/i }));
    expect(screen.getByText('Nuevo Rol')).toBeInTheDocument();

    // Fill form
    await user.type(screen.getByPlaceholderText('Nombre del rol'), 'Admin');

    // Submit
    await user.click(screen.getByRole('button', { name: /guardar/i }));

    // Wait for inline error below nombre field
    await waitFor(() => {
      expect(screen.getByText('Ya existe un rol con el nombre Admin.')).toBeInTheDocument();
    });
  });

  it('deletes role after confirmation', async () => {
    const user = userEvent.setup();
    vi.mocked(adminRolesApi.eliminarRol).mockResolvedValue(undefined);

    renderWithProviders();

    await screen.findByText('Administrador');

    // Click delete on the first non-sistema role (Supervisor)
    const allDeleteButtons = screen.getAllByRole('button', { name: /eliminar/i });
    const supervisorDeleteBtn = allDeleteButtons[1]; // Supervisor, esSistema=false
    await user.click(supervisorDeleteBtn);

    // Modal confirmation: click "Eliminar rol"
    await user.click(screen.getByRole('button', { name: /^eliminar rol$/i }));

    await waitFor(() => {
      expect(adminRolesApi.eliminarRol).toHaveBeenCalledWith(
        'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0002',
      );
    });
  });
});

describe('adminRolesApi — verb alignment', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('getRoles calls GET /admin/roles', async () => {
    const httpModule = await import('../../lib/http');
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({ data: [] });
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: undefined });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminRolesApi')>(
      '../../lib/api/admin/adminRolesApi',
    );
    await api.getRoles();

    expect(getSpy).toHaveBeenCalledWith('/admin/roles');
    expect(postSpy).not.toHaveBeenCalled();
  });

  it('crearRol calls POST /admin/roles with body', async () => {
    const httpModule = await import('../../lib/http');
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: { id: 'new-id', nombre: 'Test', descripcion: null, esSistema: false } });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminRolesApi')>(
      '../../lib/api/admin/adminRolesApi',
    );
    await api.crearRol({ nombre: 'Test', descripcion: 'Test role' });

    expect(postSpy).toHaveBeenCalledWith('/admin/roles', { nombre: 'Test', descripcion: 'Test role' });
  });

  it('actualizarRol calls PUT /admin/roles/:id with body including id', async () => {
    const httpModule = await import('../../lib/http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: { id: 'rol-id', nombre: 'Updated', descripcion: null, esSistema: false } });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminRolesApi')>(
      '../../lib/api/admin/adminRolesApi',
    );
    await api.actualizarRol('rol-id', { nombre: 'Updated' });

    expect(putSpy).toHaveBeenCalledWith('/admin/roles/rol-id', { id: 'rol-id', nombre: 'Updated' });
  });

  it('eliminarRol calls DELETE /admin/roles/:id', async () => {
    const httpModule = await import('../../lib/http');
    const deleteSpy = vi.spyOn(httpModule.default, 'delete').mockResolvedValue({ data: undefined });

    const api = await vi.importActual<typeof import('../../lib/api/admin/adminRolesApi')>(
      '../../lib/api/admin/adminRolesApi',
    );
    await api.eliminarRol('rol-id-001');

    expect(deleteSpy).toHaveBeenCalledWith('/admin/roles/rol-id-001');
  });
});
