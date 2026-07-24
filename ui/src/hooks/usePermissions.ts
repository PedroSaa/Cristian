import { useAuth } from '../contexts/AuthContext';

/**
 * Checks whether the current authenticated user has a specific permission.
 *
 * @param permiso — The permission name to check (e.g. "admin.usuarios.ver").
 * @returns `true` if the user's `permisos[]` includes the given value.
 *
 * @example
 * ```tsx
 * const canViewUsers = useHasPermission('admin.usuarios.ver');
 * if (!canViewUsers) return null;
 * ```
 */
export function useHasPermission(permiso: string): boolean {
  const { state } = useAuth();

  return state.user?.permissions?.includes(permiso) ?? false;
}
