import http from '../http';
import { mapAuthUser, type AuthState, type AuthUserApiDto, type User } from '@/types/auth';

// ── Response types matching backend cookie-only auth contract ─────────────

interface AuthEnvelope<TUser = AuthUserApiDto> {
  expiresIn?: number;
  authState?: AuthState;
  setupToken?: string | null;
  canLogout?: boolean;
  user?: TUser;
  intentosRestantes?: number;
  permissions?: string[];
  permisos?: string[];
  requiresMfa?: boolean;
  mfaToken?: string;
}

export interface LoginResponse {
  expiresIn?: number;
  authState?: AuthState;
  setupToken?: string | null;
  canLogout?: boolean;
  user?: User;
  intentosRestantes?: number;
  permissions?: string[];
  requiresMfa?: boolean;
  mfaToken?: string;
}

export interface EnableMfaResult {
  provisioningUri: string;
  secretKey: string;
}

export interface MfaVerificationResult {
  success: boolean;
  error?: string;
}

// ── Public API functions ──────────────────────────────────────────────────

function mapAuthEnvelope<TUser extends AuthUserApiDto | User>(data: AuthEnvelope<TUser>): LoginResponse {
  const user = data.user ? mapAuthUser(data.user) : undefined;
  const permissions = data.permissions ?? data.permisos ?? user?.permissions ?? [];

  return {
    expiresIn: data.expiresIn,
    authState: data.authState,
    setupToken: data.setupToken ?? undefined,
    canLogout: data.canLogout,
    user,
    intentosRestantes: data.intentosRestantes ?? user?.intentosRestantes,
    permissions: permissions.length > 0 ? permissions : undefined,
    requiresMfa: data.requiresMfa,
    mfaToken: data.mfaToken,
  };
}

export async function login(identifier: string, password: string): Promise<LoginResponse> {
  const { data } = await http.post<AuthEnvelope>('/auth/login', { identifier, password });
  return mapAuthEnvelope(data);
}

export async function refreshToken(): Promise<LoginResponse> {
  const { data } = await http.post<AuthEnvelope>('/auth/refresh', {}, { withCredentials: true });
  return mapAuthEnvelope(data);
}

export async function getProfile(): Promise<User> {
  const { data } = await http.get<AuthUserApiDto>('/auth/me');
  return mapAuthUser(data);
}

export async function updateProfile(payload: { nombreCompleto?: string; email?: string }): Promise<User> {
  const request = {
    ...(payload.nombreCompleto ? { nombre: payload.nombreCompleto } : {}),
    ...(payload.email ? { email: payload.email } : {}),
  };

  const { data } = await http.put<AuthUserApiDto>('/auth/profile', request);
  return mapAuthUser(data);
}

export async function changePassword(payload: { currentPassword: string; newPassword: string }): Promise<void> {
  await http.put('/auth/profile/password', payload);
}

export async function logout(): Promise<void> {
  try {
    await http.post('/auth/logout');
  } catch {
    // Even if the server call fails, client-side logout still proceeds
  }
}

// ── MFA API functions ─────────────────────────────────────────────────────

export async function enableMfa(): Promise<EnableMfaResult> {
  const { data } = await http.post<EnableMfaResult>('/auth/mfa/enable');
  return data;
}

export async function verifyMfa(code: string): Promise<MfaVerificationResult> {
  const { data } = await http.post<MfaVerificationResult>('/auth/mfa/verify', { code });
  return data;
}

export async function disableMfa(currentPassword: string): Promise<void> {
  await http.post('/auth/mfa/disable', { currentPassword });
}

export async function loginMfa(mfaToken: string, code: string): Promise<LoginResponse> {
  const { data } = await http.post<AuthEnvelope>('/auth/login/mfa', { mfaToken, code });
  return mapAuthEnvelope(data);
}
