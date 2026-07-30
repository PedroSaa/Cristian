import http from '../http';
import type { ConfiguracionDto } from './admin/adminConfiguracionApi';
import {
  DEFAULT_LOGIN_BACKGROUND_MODE,
  DEFAULT_LOGIN_BACKGROUND_PRESET_KEY,
  getDefaultPresetKeyForMode,
  getLoginBackgroundPreset,
  isLoginBackgroundMode,
  isLoginBackgroundPresetKey,
  type LoginBackgroundMode,
  type LoginBackgroundPresetKey,
} from '../branding/loginBackgroundPalette';
import {
  DEFAULT_LOGIN_SURFACE_TONE,
  DEFAULT_LOGIN_TEMPLATE_KEY,
  normalizeLoginSurfaceTone,
  normalizeLoginTemplateKey,
  type LoginSurfaceTone,
  type LoginTemplateKey,
} from '../branding/loginDesign';

export const DEFAULT_BRANDING_NAME = 'DocFlow Infinity';
export const DEFAULT_LOGIN_BRAND_TAGLINE = 'Acceso institucional';
export const DEFAULT_LOGIN_BRAND_FOOTER_NOTE = 'Acceso seguro a la gestión documental';

export interface BrandingDto {
  nombreInstitucion: string;
  logoUrl?: string | null;
  loginTemplateKey?: string | null;
  loginSurfaceTone?: string | null;
  loginBackgroundMode?: LoginBackgroundMode | null;
  loginBackgroundPresetKey?: string | null;
  loginBackgroundUrl?: string | null;
  loginWelcomeTitle?: string | null;
  loginWelcomeSubtitle?: string | null;
  loginWelcomeHelpText?: string | null;
  loginBrandTagline?: string | null;
  loginBrandFooterNote?: string | null;
}

export interface BrandingDisplay {
  nombreInstitucion: string;
  logoUrl: string | null;
  loginTemplateKey: LoginTemplateKey;
  loginSurfaceTone: LoginSurfaceTone;
  loginBackgroundMode: LoginBackgroundMode;
  loginBackgroundPresetKey: LoginBackgroundPresetKey;
  loginBackgroundUrl: string | null;
  loginWelcomeTitle: string | null;
  loginWelcomeSubtitle: string | null;
  loginWelcomeHelpText: string | null;
  loginBrandTagline: string | null;
  loginBrandFooterNote: string | null;
}

export async function getBranding(): Promise<BrandingDto> {
  const { data } = await http.get<BrandingDto>('/configuracion/branding');
  return data;
}

export async function uploadBrandingLogo(logo: File): Promise<ConfiguracionDto> {
  const formData = new FormData();
  formData.append('logo', logo);

  const { data } = await http.post<ConfiguracionDto>('/admin/configuracion/logo', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  return data;
}

export async function uploadBrandingLoginBackground(loginBackground: File): Promise<ConfiguracionDto> {
  const formData = new FormData();
  formData.append('loginBackground', loginBackground);

  const { data } = await http.post<ConfiguracionDto>('/admin/configuracion/login-background', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  return data;
}

function resolveAssetUrl(url?: string | null): string | null {
  if (!url || !url.trim()) return null;
  if (/^(https?:|data:)/i.test(url)) return url;
  return url.startsWith('/') ? url : `/${url}`;
}

function normalizeOptionalText(value?: string | null): string | null {
  if (!value || !value.trim()) return null;
  return value.trim();
}

function resolveLoginBackgroundMode(data?: BrandingDto | null): LoginBackgroundMode {
  const mode = data?.loginBackgroundMode ?? null;
  if (isLoginBackgroundMode(mode)) return mode;

  if (resolveAssetUrl(data?.loginBackgroundUrl ?? null)) return 'image';

  if (isLoginBackgroundPresetKey(data?.loginBackgroundPresetKey ?? null)) {
    const preset = getLoginBackgroundPreset(data?.loginBackgroundPresetKey ?? null);
    return preset.mode;
  }

  return DEFAULT_LOGIN_BACKGROUND_MODE;
}

export function normalizeBranding(data?: BrandingDto | null): BrandingDisplay {
  const loginBackgroundMode = resolveLoginBackgroundMode(data);
  const presetKeyFromApi = data?.loginBackgroundPresetKey ?? null;
  const resolvedPresetKey = loginBackgroundMode === 'image'
    ? DEFAULT_LOGIN_BACKGROUND_PRESET_KEY
    : (isLoginBackgroundPresetKey(presetKeyFromApi) && getLoginBackgroundPreset(presetKeyFromApi).mode === loginBackgroundMode)
      ? presetKeyFromApi
      : getDefaultPresetKeyForMode(loginBackgroundMode);

  return {
    nombreInstitucion: data?.nombreInstitucion?.trim() || DEFAULT_BRANDING_NAME,
    logoUrl: resolveAssetUrl(data?.logoUrl ?? null),
    loginTemplateKey: normalizeLoginTemplateKey(data?.loginTemplateKey ?? DEFAULT_LOGIN_TEMPLATE_KEY),
    loginSurfaceTone: normalizeLoginSurfaceTone(data?.loginSurfaceTone ?? DEFAULT_LOGIN_SURFACE_TONE),
    loginBackgroundMode,
    loginBackgroundPresetKey: resolvedPresetKey,
    loginBackgroundUrl: resolveAssetUrl(data?.loginBackgroundUrl ?? null),
    loginWelcomeTitle: normalizeOptionalText(data?.loginWelcomeTitle ?? null),
    loginWelcomeSubtitle: normalizeOptionalText(data?.loginWelcomeSubtitle ?? null),
    loginWelcomeHelpText: normalizeOptionalText(data?.loginWelcomeHelpText ?? null),
    loginBrandTagline: normalizeOptionalText(data?.loginBrandTagline ?? null),
    loginBrandFooterNote: normalizeOptionalText(data?.loginBrandFooterNote ?? null),
  };
}
