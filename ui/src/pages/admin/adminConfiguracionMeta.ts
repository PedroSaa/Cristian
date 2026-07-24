/**
 * Metadata map for security configuration entries.
 * Transforms raw technical keys into human-friendly form fields
 * so non-technical administrators can understand and manage security settings.
 */

export interface SecurityConfigMeta {
  /** Human-readable label for the form field */
  label: string;
  /** Helper text explaining what this setting does */
  helpText: string;
  /** Optional unit label (e.g. "minutos", "caracteres") */
  unit?: string;
  /** Display ordering within the security section */
  displayOrder: number;
  /** Which UI control to render */
  inputType: 'toggle' | 'number';
  /** Optional minimum allowed value enforced in the UI */
  minValue?: number;
}

const securityKeys = [
  'JwtExpirationMinutos',
  'RefreshTokenExpirationDias',
  'LockoutMaxIntentos',
  'LockoutDuracionMinutos',
  'RateLimitLoginPermitLimit',
  'RateLimitLoginWindowSegundos',
  'PasswordMinLength',
  'PasswordRequireUpper',
  'PasswordRequireSpecial',
  'RequireMfaAdministradores',
  'RequireMfaOtrosUsuarios',
  'TotpWindowSegundos',
] as const;

export const SECURITY_CONFIGURATION_KEYS = new Set<string>(securityKeys);
const SECURITY_CONFIGURATION_KEYS_LOWER = new Set<string>(securityKeys.map((key) => key.toLowerCase()));
export const SECURITY_TOGGLE_KEYS = new Set<string>([
  'PasswordRequireUpper',
  'PasswordRequireSpecial',
  'RequireMfaAdministradores',
  'RequireMfaOtrosUsuarios',
]);
const SECURITY_TOGGLE_KEYS_LOWER = new Set<string>(Array.from(SECURITY_TOGGLE_KEYS).map((key) => key.toLowerCase()));

const securityMetaMap: Record<string, SecurityConfigMeta> = {
  // ── Autenticación / Bloqueo ──────────────────────────────────────────────

  JwtExpirationMinutos: {
    label: 'Vigencia del token de acceso',
    helpText: 'Cuánto dura la credencial de acceso antes de renovarse sola (sin pedir login de nuevo). Bajarlo acorta la ventana en que un token robado seguiría sirviendo. No acelera los bloqueos ni los cambios de permisos: esos se aplican al instante.',
    unit: 'minutos',
    displayOrder: 0,
    inputType: 'number',
    minValue: 15,
  },
  RefreshTokenExpirationDias: {
    label: 'Sesión recordada',
    helpText: 'Días que el usuario puede estar sin actividad antes de tener que iniciar sesión otra vez. El plazo se reinicia con cada uso; superado, el sistema lo redirige al login.',
    unit: 'días',
    displayOrder: 0.5,
    inputType: 'number',
    minValue: 1,
  },

  LockoutMaxIntentos: {
    label: 'Intentos fallidos antes de bloqueo',
    helpText: 'Cantidad de intentos incorrectos permitidos antes de bloquear temporalmente la cuenta.',
    unit: 'intentos',
    displayOrder: 1,
    inputType: 'number',
  },
  LockoutDuracionMinutos: {
    label: 'Tiempo de bloqueo de la cuenta',
    helpText: 'Indica cuántos minutos permanecerá bloqueada una cuenta después de exceder los intentos permitidos.',
    unit: 'minutos',
    displayOrder: 2,
    inputType: 'number',
  },
  RateLimitLoginPermitLimit: {
    label: 'Intentos de login por IP',
    helpText: 'Máximo de intentos de inicio de sesión desde una misma dirección IP dentro de la ventana de tiempo. Frena ataques automatizados de fuerza bruta sobre varias cuentas.',
    unit: 'intentos',
    displayOrder: 2.7,
    inputType: 'number',
    minValue: 1,
  },
  RateLimitLoginWindowSegundos: {
    label: 'Ventana del límite de login',
    helpText: 'Período (en segundos) sobre el que se cuentan los intentos de login por IP antes de empezar a rechazarlos.',
    unit: 'segundos',
    displayOrder: 2.8,
    inputType: 'number',
    minValue: 1,
  },

  // ── Política de contraseñas ──────────────────────────────────────────────

  PasswordMinLength: {
    label: 'Longitud mínima de contraseña',
    helpText: 'Cantidad mínima de caracteres que debe tener una contraseña.',
    unit: 'caracteres',
    displayOrder: 3,
    inputType: 'number',
    minValue: 8,
  },
  PasswordRequireUpper: {
    label: 'Requerir mayúsculas',
    helpText: 'Las contraseñas deben contener al menos una letra mayúscula (A-Z).',
    displayOrder: 4,
    inputType: 'toggle',
  },
  PasswordRequireSpecial: {
    label: 'Exigir símbolos especiales',
    helpText: 'Las contraseñas deberán incluir símbolos como @, # o !.',
    displayOrder: 7,
    inputType: 'toggle',
  },
  RequireMfaAdministradores: {
    label: 'Requerir MFA para administradores',
    helpText: 'Obliga a que las cuentas con rol de administración completen MFA antes de navegar por el sistema.',
    displayOrder: 9,
    inputType: 'toggle',
  },
  RequireMfaOtrosUsuarios: {
    label: 'Requerir MFA para el resto de usuarios',
    helpText: 'Obliga a que los usuarios que no son administradores completen MFA cuando la política esté activa.',
    displayOrder: 10,
    inputType: 'toggle',
  },

  // ── Sesión ───────────────────────────────────────────────────────────────

  TotpWindowSegundos: {
    label: 'Margen de tiempo para validar el código de autenticación en dos pasos',
    helpText: 'Define cuántos segundos de tolerancia acepta el sistema al validar el código de verificación.',
    unit: 'segundos',
    displayOrder: 11,
    inputType: 'number',
    minValue: 90,
  },
};

const securityMetaMapLower = Object.fromEntries(
  Object.entries(securityMetaMap).map(([key, value]) => [key.toLowerCase(), value]),
) as Record<string, SecurityConfigMeta>;

/**
 * Returns the metadata for a given security clave, or undefined if unknown.
 */
export function getSecurityMeta(clave: string): SecurityConfigMeta | undefined {
  const normalized = clave.trim();
  return securityMetaMap[normalized] ?? securityMetaMapLower[normalized.toLowerCase()];
}

export function isSecurityConfigurationKey(clave: string): boolean {
  const normalized = clave.trim();
  return SECURITY_CONFIGURATION_KEYS.has(normalized) || SECURITY_CONFIGURATION_KEYS_LOWER.has(normalized.toLowerCase());
}

export function isSecurityToggleKey(clave: string): boolean {
  const normalized = clave.trim();
  return SECURITY_TOGGLE_KEYS.has(normalized) || SECURITY_TOGGLE_KEYS_LOWER.has(normalized.toLowerCase());
}

/**
 * Generates a human-readable range hint from min/max values.
 *
 * Example: `getRangeLabel(1, 10, 'minutos')` → "Rango válido: 1 — 10 minutos"
 */
export function getRangeLabel(min: number, max: number, unit?: string): string {
  const suffix = unit ? ` ${unit}` : '';
  return `Rango válido: ${min} — ${max}${suffix}`;
}
