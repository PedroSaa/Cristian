import { z } from 'zod';

export const loginSchema = z.object({
  identifier: z
    .string({ error: 'El identificador es requerido' })
    .min(1, 'El identificador es requerido')
    .max(100, 'El identificador no puede superar los 100 caracteres'),
  password: z
    .string({ error: 'La contraseña es requerida' })
    .min(1, 'La contraseña es requerida'),
});

export type LoginFormData = z.infer<typeof loginSchema>;

export const updateProfileSchema = z.object({
  nombreCompleto: z
    .string()
    .max(200, 'El nombre no puede superar los 200 caracteres')
    .optional()
    .or(z.literal('')),
  email: z
    .string()
    .email('Formato de email inválido')
    .max(200, 'El email no puede superar los 200 caracteres')
    .optional()
    .or(z.literal('')),
});

export type UpdateProfileFormData = z.infer<typeof updateProfileSchema>;

// ── Password policy ────────────────────────────────────────────────────────
// The effective policy comes from the backend (GET /auth/password-policy) so the
// UI validates live in sync with what the server enforces. The static exports
// below use the strict default as a fallback (while loading / if the fetch fails).

export interface PasswordPolicy {
  minLength: number;
  requireUppercase: boolean;
  requireLowercase: boolean;
  requireDigit: boolean;
  requireSpecial: boolean;
}

/** Strict fallback — matches the server's hard floor when no policy is loaded yet. */
export const DEFAULT_PASSWORD_POLICY: PasswordPolicy = {
  minLength: 8,
  requireUppercase: true,
  requireLowercase: true,
  requireDigit: true,
  requireSpecial: true,
};

const SPECIAL_CHAR_REGEX = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/;

/** Builds a password field schema from the effective policy. */
export function buildPasswordPolicySchema(policy: PasswordPolicy = DEFAULT_PASSWORD_POLICY) {
  let schema = z
    .string({ error: 'La nueva contraseña es requerida' })
    .min(policy.minLength, `La contraseña debe tener al menos ${policy.minLength} caracteres`);
  if (policy.requireUppercase) schema = schema.regex(/[A-Z]/, 'La contraseña debe contener al menos una mayúscula');
  if (policy.requireLowercase) schema = schema.regex(/[a-z]/, 'La contraseña debe contener al menos una minúscula');
  if (policy.requireDigit) schema = schema.regex(/[0-9]/, 'La contraseña debe contener al menos un dígito');
  if (policy.requireSpecial) schema = schema.regex(SPECIAL_CHAR_REGEX, 'La contraseña debe contener al menos un carácter especial');
  return schema;
}

/** Static strict schema (fallback / non-dynamic callers). */
export const passwordPolicy = buildPasswordPolicySchema();
export const createUserPasswordPolicy = passwordPolicy;

export function buildChangePasswordSchema(policy: PasswordPolicy = DEFAULT_PASSWORD_POLICY) {
  return z
    .object({
      currentPassword: z
        .string({ error: 'La contraseña actual es requerida' })
        .min(1, 'La contraseña actual es requerida'),
      newPassword: buildPasswordPolicySchema(policy),
      confirmPassword: z
        .string({ error: 'Debe confirmar la nueva contraseña' })
        .min(1, 'Debe confirmar la nueva contraseña'),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
      message: 'Las contraseñas no coinciden',
      path: ['confirmPassword'],
    });
}

export const changePasswordSchema = buildChangePasswordSchema();

export type ChangePasswordFormData = z.infer<ReturnType<typeof buildChangePasswordSchema>>;

export function buildResetPasswordSchema(policy: PasswordPolicy = DEFAULT_PASSWORD_POLICY) {
  return z
    .object({
      newPassword: buildPasswordPolicySchema(policy),
      confirmPassword: z
        .string({ error: 'Debe confirmar la nueva contraseña' })
        .min(1, 'Debe confirmar la nueva contraseña'),
    })
    .refine((data) => data.newPassword === data.confirmPassword, {
      message: 'Las contraseñas no coinciden',
      path: ['confirmPassword'],
    });
}

export const resetPasswordSchema = buildResetPasswordSchema();

export type ResetPasswordFormData = z.infer<ReturnType<typeof buildResetPasswordSchema>>;
