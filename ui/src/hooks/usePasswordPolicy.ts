import { useQuery } from '@tanstack/react-query';
import { getPasswordPolicy } from '../lib/api/passwordPolicy';
import { DEFAULT_PASSWORD_POLICY, type PasswordPolicy } from '../lib/validations/auth';

/**
 * Returns the effective password policy from the backend so forms validate live in
 * sync with what the server enforces. While loading (or if the request fails) it
 * falls back to the strict default, so validation never gets laxer by accident.
 */
export function usePasswordPolicy(): PasswordPolicy {
  const { data } = useQuery({
    queryKey: ['auth', 'password-policy'],
    queryFn: getPasswordPolicy,
    staleTime: 5 * 60 * 1000,
  });
  return data ?? DEFAULT_PASSWORD_POLICY;
}
