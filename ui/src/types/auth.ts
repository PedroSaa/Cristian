import {
  ADMIN_NAV_ITEMS as GENERATED_ADMIN_NAV_ITEMS,
  ADMIN_PERMISSION_PREFIX as GENERATED_ADMIN_PERMISSION_PREFIX,
} from '../lib/generated/permissionCatalog';

export const ADMIN_PERMISSION_PREFIX = GENERATED_ADMIN_PERMISSION_PREFIX;

export interface AdminNavItem {
  to: string;
  label: string;
  requiredPermission: string;
  accessPermissions?: readonly string[];
}

export interface AdminNavGroup {
  label: string;
  items: readonly AdminNavItem[];
}

export type AdminNavEntry = AdminNavItem | AdminNavGroup;

export const ADMIN_NAV_ITEMS: readonly AdminNavEntry[] = GENERATED_ADMIN_NAV_ITEMS as readonly AdminNavEntry[];

export type AuthState = 'authenticated' | 'mfa_setup_required';

function isAdminNavGroup(entry: AdminNavEntry): entry is AdminNavGroup {
  return 'items' in entry;
}

function canAccessAdminNavItem(permisos: readonly string[] | undefined | null, item: AdminNavItem): boolean {
  return hasAnyPermission(permisos, item.accessPermissions ?? [item.requiredPermission]);
}

export function getVisibleAdminNavEntries(permisos: readonly string[] | undefined | null): AdminNavEntry[] {
  const visible: AdminNavEntry[] = [];

  for (const entry of ADMIN_NAV_ITEMS) {
    if (isAdminNavGroup(entry)) {
      const items = entry.items.filter((item) => canAccessAdminNavItem(permisos, item));
      if (items.length > 0) visible.push({ ...entry, items });
      continue;
    }

    if (canAccessAdminNavItem(permisos, entry)) visible.push(entry);
  }

  return visible;
}

export function getFirstAllowedAdminRoute(permisos: readonly string[] | undefined | null): string | null {
  for (const entry of ADMIN_NAV_ITEMS) {
    if ('items' in entry) {
      const firstVisible = entry.items.find((item) => canAccessAdminNavItem(permisos, item));
      if (firstVisible) return firstVisible.to;
      continue;
    }

    if (canAccessAdminNavItem(permisos, entry)) return entry.to;
  }

  return null;
}

export function hasPermission(permisos: readonly string[] | undefined | null, permiso: string): boolean {
  return Array.isArray(permisos) && permisos.includes(permiso);
}

export function hasAnyPermission(permisos: readonly string[] | undefined | null, required: readonly string[]): boolean {
  return Array.isArray(permisos) && required.some((permiso) => permisos.includes(permiso));
}

export function hasAllPermissions(permisos: readonly string[] | undefined | null, required: readonly string[]): boolean {
  return Array.isArray(permisos) && required.every((permiso) => permisos.includes(permiso));
}

export function hasAnyAdminPermission(permisos: readonly string[] | undefined | null): boolean {
  return Array.isArray(permisos) && permisos.some((permiso) => permiso.startsWith(ADMIN_PERMISSION_PREFIX));
}

export interface DepartamentoRef {
  id: string;
  nombre: string;
}

export interface AuthUserApiDto {
  id: string;
  nombre: string;
  nombreCompleto?: string;
  email: string;
  rut?: string | null;
  rol: string;
  rolId?: string | null;
  departamento?: DepartamentoRef | null;
  departamentoId?: string;
  departamentoNombre?: string;
  activo: boolean;
  intentosRestantes?: number;
  authState?: AuthState;
  setupToken?: string | null;
  canLogout?: boolean;
  permissions?: readonly string[] | null;
  permisos?: readonly string[] | null;
  mfaEnabled?: boolean;
  usucod?: string | null;
}

export interface User {
  id: string;
  nombre: string;
  nombreCompleto?: string;
  email: string;
  rut?: string | null;
  rol: string;
  rolId?: string;
  departamento?: DepartamentoRef | null;
  departamentoId?: string;
  departamentoNombre?: string;
  activo?: boolean;
  intentosRestantes?: number;
  authState?: AuthState;
  setupToken?: string | null;
  canLogout?: boolean;
  mfaEnabled?: boolean;
  permissions?: string[];
  permisos?: string[];
  usucod?: string | null;
}

export interface AuthSession {
  token: string;
  refreshToken: string;
  user: User;
  expiresIn?: number;
  intentosRestantes?: number;
  authState?: AuthState;
  setupToken?: string | null;
  canLogout?: boolean;
  permissions?: string[];
  requiresMfa?: boolean;
  mfaToken?: string;
}

export function normalizePermissions(source: Pick<User, 'permissions' | 'permisos'> | Pick<AuthUserApiDto, 'permissions' | 'permisos'>): string[] {
  if (Array.isArray(source.permissions)) {
    return [...source.permissions];
  }

  if (Array.isArray(source.permisos)) {
    return [...source.permisos];
  }

  return [];
}

export function mapAuthUser(source: AuthUserApiDto | User): User {
  const permisos = normalizePermissions(source);
  const nombreCompleto = source.nombreCompleto ?? source.nombre;

  return {
    id: source.id,
    nombre: source.nombre,
    nombreCompleto,
    email: source.email,
    rut: source.rut ?? null,
    rol: source.rol,
    rolId: source.rolId ?? undefined,
    departamento: source.departamento ?? undefined,
    departamentoId: source.departamento?.id ?? source.departamentoId,
    departamentoNombre: source.departamento?.nombre ?? source.departamentoNombre,
    activo: source.activo ?? true,
    intentosRestantes: source.intentosRestantes,
    authState: source.authState,
    setupToken: source.setupToken ?? undefined,
    canLogout: source.canLogout,
    mfaEnabled: source.mfaEnabled,
    permissions: permisos,
    usucod: source.usucod ?? undefined,
  };
}

export function mapAuthUserToStorage(source: AuthUserApiDto | User): User {
  const user = mapAuthUser(source);

  return {
    ...user,
    permissions: user.permissions ?? [],
  };
}
