import React, { type ReactNode } from 'react';
import { render, type RenderOptions } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from '@/contexts/AuthContext';
import type { User } from '@/types/auth';
import { PERMISSIONS } from '@/lib/generated/permissionCatalog';

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}

interface WrapperOptions {
  initialRoute?: string;
}

export function renderWithProviders(
  ui: React.ReactElement,
  options?: RenderOptions & WrapperOptions,
) {
  const queryClient = createTestQueryClient();

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <AuthProvider>
            {children}
          </AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
    );
  }

  return render(ui, { wrapper: Wrapper, ...options });
}

export const mockUser: User = {
  id: '00000000-0000-0000-0000-000000000001',
  nombre: 'Juan Pérez',
  email: 'jperez@docflow.cl',
  rut: '11111111-1',
  rol: 'Usuario',
  departamentoId: '00000000-0000-0000-0000-000000000010',
  permissions: [PERMISSIONS.BANDEJA_VER],
};

export const mockAdminUser: User = {
  id: '00000000-0000-0000-0000-000000000002',
  nombre: 'Admin User',
  email: 'admin@docflow.cl',
  rut: '22222222-2',
  rol: 'Administrador',
  departamentoId: undefined,
  permissions: [
    PERMISSIONS.ADMIN_USUARIOS_VER,
    PERMISSIONS.ADMIN_USUARIOS_CREAR,
    PERMISSIONS.ADMIN_USUARIOS_EDITAR,
    PERMISSIONS.ADMIN_USUARIOS_ACTIVAR,
    PERMISSIONS.ADMIN_USUARIOS_DESACTIVAR,
    PERMISSIONS.ADMIN_USUARIOS_RESET_PASSWORD,
    PERMISSIONS.ADMIN_USUARIOS_BLOQUEAR,
    PERMISSIONS.ADMIN_DEPARTAMENTOS_VER,
    PERMISSIONS.ADMIN_ROLES_VER,
  ],
};

export const mockRrhhUser: User = {
  id: '00000000-0000-0000-0000-000000000003',
  nombre: 'RRHH User',
  email: 'rrhh@docflow.cl',
  rut: '33333333-3',
  rol: 'RRHH',
  departamentoId: '00000000-0000-0000-0000-000000000011',
  permissions: [PERMISSIONS.BANDEJA_VER, PERMISSIONS.REPORTES_GENERAR, PERMISSIONS.RRHH_GESTIONAR],
};

export const mockJefaturaUser: User = {
  id: '00000000-0000-0000-0000-000000000004',
  nombre: 'Jefatura User',
  email: 'jefatura@docflow.cl',
  rut: '44444444-4',
  rol: 'Jefatura',
  departamentoId: '00000000-0000-0000-0000-000000000011',
  permissions: [
    PERMISSIONS.ADMIN_USUARIOS_VER,
    PERMISSIONS.ADMIN_USUARIOS_CREAR,
    PERMISSIONS.ADMIN_USUARIOS_EDITAR,
    PERMISSIONS.ADMIN_USUARIOS_ACTIVAR,
    PERMISSIONS.ADMIN_USUARIOS_DESACTIVAR,
    PERMISSIONS.ADMIN_USUARIOS_RESET_PASSWORD,
    PERMISSIONS.ADMIN_USUARIOS_BLOQUEAR,
    PERMISSIONS.ADMIN_DEPARTAMENTOS_VER,
    PERMISSIONS.ADMIN_ROLES_VER,
    PERMISSIONS.ADMIN_CATALOGOS_VER,
    PERMISSIONS.ADMIN_CONFIG_VER,
    PERMISSIONS.ADMIN_INTEGRACIONES_VER,
    PERMISSIONS.ADMIN_AUDITORIA_VER,
    PERMISSIONS.ADMIN_RESPALDOS_VER,
  ],
};

/**
 * Set up localStorage with user data to simulate an authenticated state.
 * Only 'user' is persisted — tokens are HttpOnly cookies managed by the backend.
 */
export function setupAuthInStorage(user: User = mockUser) {
  localStorage.setItem('user', JSON.stringify(user));
}

export function clearAuthFromStorage() {
  localStorage.removeItem('user');
}
