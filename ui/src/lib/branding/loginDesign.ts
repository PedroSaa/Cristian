import type { CSSProperties } from 'react';

export type LoginTemplateKey = 'centered-brand' | 'split-brand';
export type LoginSurfaceTone = 'light' | 'dark';

export const DEFAULT_LOGIN_TEMPLATE_KEY: LoginTemplateKey = 'centered-brand';
export const DEFAULT_LOGIN_SURFACE_TONE: LoginSurfaceTone = 'light';

export interface LoginTemplateOption {
  key: LoginTemplateKey;
  label: string;
  description: string;
}

export interface LoginSurfaceToneOption {
  key: LoginSurfaceTone;
  label: string;
  description: string;
}

export const LOGIN_TEMPLATE_OPTIONS: readonly LoginTemplateOption[] = [
  {
    key: 'centered-brand',
    label: 'Centrada institucional',
    description: 'Mantiene la composición actual, sobria y enfocada en el acceso.',
  },
  {
    key: 'split-brand',
    label: 'Marca dividida',
    description: 'Presenta la identidad institucional a la izquierda y el acceso a la derecha.',
  },
] as const;

export const LOGIN_SURFACE_TONE_OPTIONS: readonly LoginSurfaceToneOption[] = [
  {
    key: 'light',
    label: 'Claro',
    description: 'Superficies claras con lectura limpia y formal.',
  },
  {
    key: 'dark',
    label: 'Oscuro',
    description: 'Superficies oscuras con mayor contraste y presencia.',
  },
] as const;

export interface LoginSurfaceToneClasses {
  shell: string;
  panel: string;
  panelBorder: string;
  title: string;
  text: string;
  mutedText: string;
  input: string;
  primaryButton: string;
  secondaryButton: string;
  badge: string;
  supportPanel: string;
}

export function getLoginSplitBrandPanelStyle(tone: LoginSurfaceTone): CSSProperties {
  return tone === 'dark'
    ? {
        backgroundColor: 'rgba(2, 6, 23, 0.84)',
        backgroundImage: 'linear-gradient(180deg, rgba(2, 6, 23, 0.88) 0%, rgba(15, 23, 42, 0.76) 100%)',
        color: '#ffffff',
        backdropFilter: 'blur(16px)',
      }
    : {
        backgroundColor: 'rgba(255, 255, 255, 0.82)',
        backgroundImage: 'linear-gradient(180deg, rgba(255, 255, 255, 0.92) 0%, rgba(248, 250, 252, 0.72) 100%)',
        color: '#0f172a',
        backdropFilter: 'blur(16px)',
      };
}

const LIGHT_SURFACE_CLASSES: LoginSurfaceToneClasses = {
  shell: 'bg-slate-50 text-slate-900',
  panel: 'bg-white text-slate-900 shadow-sm',
  panelBorder: 'border-slate-200',
  title: 'text-slate-900',
  text: 'text-slate-800',
  mutedText: 'text-slate-600',
  input: 'border-slate-300 bg-white text-slate-900 placeholder:text-slate-400 focus:ring-blue-500',
  primaryButton: 'bg-slate-900 text-white hover:bg-slate-800',
  secondaryButton: 'border-slate-300 bg-white text-slate-700 hover:bg-slate-50',
  badge: 'border-slate-200 bg-slate-50 text-slate-600',
  supportPanel: 'border-slate-200 bg-slate-50',
};

const DARK_SURFACE_CLASSES: LoginSurfaceToneClasses = {
  shell: 'bg-slate-950 text-white',
  panel: 'bg-slate-950 text-white shadow-[0_24px_60px_rgba(2,6,23,0.45)]',
  panelBorder: 'border-white/10',
  title: 'text-white',
  text: 'text-white/92',
  mutedText: 'text-white/68',
  input: 'border-white/10 bg-white/5 text-white placeholder:text-white/45 focus:ring-cyan-400',
  primaryButton: 'bg-white text-slate-950 hover:bg-white/90',
  secondaryButton: 'border-white/15 bg-white/5 text-white hover:bg-white/10',
  badge: 'border-white/10 bg-slate-900 text-white/80',
  supportPanel: 'border-white/10 bg-slate-900',
};

export function isLoginTemplateKey(value: unknown): value is LoginTemplateKey {
  return value === 'centered-brand' || value === 'split-brand';
}

export function isLoginSurfaceTone(value: unknown): value is LoginSurfaceTone {
  return value === 'light' || value === 'dark';
}

export function normalizeLoginTemplateKey(value?: string | null): LoginTemplateKey {
  const trimmed = value?.trim();
  return isLoginTemplateKey(trimmed) ? trimmed : DEFAULT_LOGIN_TEMPLATE_KEY;
}

export function normalizeLoginSurfaceTone(value?: string | null): LoginSurfaceTone {
  const trimmed = value?.trim();
  return isLoginSurfaceTone(trimmed) ? trimmed : DEFAULT_LOGIN_SURFACE_TONE;
}

export function getLoginTemplateOption(key: LoginTemplateKey): LoginTemplateOption {
  return LOGIN_TEMPLATE_OPTIONS.find((option) => option.key === key) ?? LOGIN_TEMPLATE_OPTIONS[0];
}

export function getLoginSurfaceToneOption(key: LoginSurfaceTone): LoginSurfaceToneOption {
  return LOGIN_SURFACE_TONE_OPTIONS.find((option) => option.key === key) ?? LOGIN_SURFACE_TONE_OPTIONS[0];
}

export function getLoginSurfaceToneClasses(tone: LoginSurfaceTone): LoginSurfaceToneClasses {
  return tone === 'dark' ? DARK_SURFACE_CLASSES : LIGHT_SURFACE_CLASSES;
}
