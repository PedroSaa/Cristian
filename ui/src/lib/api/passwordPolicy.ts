import http from '../http';
import type { PasswordPolicy } from '../validations/auth';

/** Effective password policy enforced by the backend, so the UI can validate in sync. */
export async function getPasswordPolicy(): Promise<PasswordPolicy> {
  const { data } = await http.get<PasswordPolicy>('/auth/password-policy');
  return data;
}
