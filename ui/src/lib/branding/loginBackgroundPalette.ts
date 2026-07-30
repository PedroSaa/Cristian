import type { CSSProperties } from 'react';

export type LoginBackgroundMode = 'image' | 'color' | 'gradient';

export type LoginBackgroundPresetKey =
  | 'midnight'
  | 'indigo'
  | 'cobalt'
  | 'emerald'
  | 'rose'
  | 'amber'
  | 'slate'
  | 'graphite'
  | 'teal'
  | 'ruby'
  | 'sand'
  | 'plum'
  | 'midnight-indigo'
  | 'aurora'
  | 'dawn'
  | 'sunset'
  | 'ocean'
  | 'forest'
  | 'lavender'
  | 'ember'
  | 'lagoon'
  | 'galaxy'
  | 'rose-gold'
  | 'arctic'
  | 'sunset-ember';

export interface LoginBackgroundPreset {
  key: LoginBackgroundPresetKey;
  label: string;
  mode: Exclude<LoginBackgroundMode, 'image'>;
  description: string;
  previewStyle: CSSProperties;
}

export const LOGIN_BACKGROUND_PRESETS: readonly LoginBackgroundPreset[] = [
  {
    key: 'midnight',
    label: 'Medianoche',
    mode: 'color',
    description: 'Azul profundo, sobrio y institucional.',
    previewStyle: {
      backgroundColor: '#020617',
      backgroundImage: 'linear-gradient(180deg, #020617 0%, #0f172a 100%)',
    },
  },
  {
    key: 'indigo',
    label: 'Índigo',
    mode: 'color',
    description: 'Más presencia visual, sin perder sobriedad.',
    previewStyle: {
      backgroundColor: '#312e81',
      backgroundImage: 'linear-gradient(180deg, #312e81 0%, #4338ca 100%)',
    },
  },
  {
    key: 'cobalt',
    label: 'Cobalto',
    mode: 'color',
    description: 'Azul vivo para interfaces más energéticas.',
    previewStyle: {
      backgroundColor: '#1d4ed8',
      backgroundImage: 'linear-gradient(180deg, #1d4ed8 0%, #2563eb 100%)',
    },
  },
  {
    key: 'emerald',
    label: 'Esmeralda',
    mode: 'color',
    description: 'Verde institucional con un toque fresco.',
    previewStyle: {
      backgroundColor: '#047857',
      backgroundImage: 'linear-gradient(180deg, #047857 0%, #0f766e 100%)',
    },
  },
  {
    key: 'rose',
    label: 'Rosa',
    mode: 'color',
    description: 'Un acento cálido, limpio y moderno.',
    previewStyle: {
      backgroundColor: '#be185d',
      backgroundImage: 'linear-gradient(180deg, #be185d 0%, #db2777 100%)',
    },
  },
  {
    key: 'amber',
    label: 'Ámbar',
    mode: 'color',
    description: 'Calidez suave para fondos más luminosos.',
    previewStyle: {
      backgroundColor: '#b45309',
      backgroundImage: 'linear-gradient(180deg, #b45309 0%, #f59e0b 100%)',
    },
  },
  {
    key: 'slate',
    label: 'Pizarra',
    mode: 'color',
    description: 'Neutro y profesional, con bastante contraste.',
    previewStyle: {
      backgroundColor: '#334155',
      backgroundImage: 'linear-gradient(180deg, #334155 0%, #475569 100%)',
    },
  },
  {
    key: 'graphite',
    label: 'Grafito',
    mode: 'color',
    description: 'Oscuro elegante para un look más sobrio.',
    previewStyle: {
      backgroundColor: '#111827',
      backgroundImage: 'linear-gradient(180deg, #111827 0%, #1f2937 100%)',
    },
  },
  {
    key: 'teal',
    label: 'Verde agua',
    mode: 'color',
    description: 'Un verde azulado limpio y contemporáneo.',
    previewStyle: {
      backgroundColor: '#0f766e',
      backgroundImage: 'linear-gradient(180deg, #0f766e 0%, #14b8a6 100%)',
    },
  },
  {
    key: 'ruby',
    label: 'Rubí',
    mode: 'color',
    description: 'Intenso y con carácter, sin perder claridad.',
    previewStyle: {
      backgroundColor: '#991b1b',
      backgroundImage: 'linear-gradient(180deg, #991b1b 0%, #dc2626 100%)',
    },
  },
  {
    key: 'sand',
    label: 'Arena',
    mode: 'color',
    description: 'Neutro cálido para interfaces más suaves.',
    previewStyle: {
      backgroundColor: '#a16207',
      backgroundImage: 'linear-gradient(180deg, #a16207 0%, #fbbf24 100%)',
    },
  },
  {
    key: 'plum',
    label: 'Ciruela',
    mode: 'color',
    description: 'Profundo, elegante y con personalidad.',
    previewStyle: {
      backgroundColor: '#5b21b6',
      backgroundImage: 'linear-gradient(180deg, #5b21b6 0%, #7c3aed 100%)',
    },
  },
  {
    key: 'midnight-indigo',
    label: 'Medianoche índigo',
    mode: 'gradient',
    description: 'El fondo base por defecto, con transición profunda.',
    previewStyle: {
      backgroundColor: '#0f172a',
      backgroundImage: 'linear-gradient(135deg, #0f172a 0%, #1d4ed8 68%, #38bdf8 140%)',
    },
  },
  {
    key: 'aurora',
    label: 'Aurora',
    mode: 'gradient',
    description: 'Más brillo y contraste, con una sensación etérea.',
    previewStyle: {
      backgroundColor: '#042f2e',
      backgroundImage: 'radial-gradient(circle at 20% 20%, rgba(45, 212, 191, 0.8) 0, rgba(45, 212, 191, 0) 32%), linear-gradient(135deg, #052e2b 0%, #0f766e 50%, #0ea5e9 100%)',
    },
  },
  {
    key: 'dawn',
    label: 'Amanecer',
    mode: 'gradient',
    description: 'Un degradado suave, claro y optimista.',
    previewStyle: {
      backgroundColor: '#7c2d12',
      backgroundImage: 'linear-gradient(135deg, #7c2d12 0%, #f97316 55%, #fde68a 100%)',
    },
  },
  {
    key: 'sunset',
    label: 'Atardecer',
    mode: 'gradient',
    description: 'Cálido y muy legible en composiciones oscuras.',
    previewStyle: {
      backgroundColor: '#7f1d1d',
      backgroundImage: 'linear-gradient(135deg, #7f1d1d 0%, #c2410c 50%, #f97316 100%)',
    },
  },
  {
    key: 'ocean',
    label: 'Océano',
    mode: 'gradient',
    description: 'Fresco, profundo y con un look institucional moderno.',
    previewStyle: {
      backgroundColor: '#082f49',
      backgroundImage: 'linear-gradient(135deg, #082f49 0%, #0f766e 50%, #38bdf8 100%)',
    },
  },
  {
    key: 'forest',
    label: 'Bosque',
    mode: 'gradient',
    description: 'Verde oscuro con bastante contraste visual.',
    previewStyle: {
      backgroundColor: '#14532d',
      backgroundImage: 'linear-gradient(135deg, #14532d 0%, #15803d 55%, #84cc16 100%)',
    },
  },
  {
    key: 'lavender',
    label: 'Lavanda',
    mode: 'gradient',
    description: 'Más suave, ideal para un fondo elegante.',
    previewStyle: {
      backgroundColor: '#4c1d95',
      backgroundImage: 'linear-gradient(135deg, #4c1d95 0%, #7c3aed 52%, #c084fc 100%)',
    },
  },
  {
    key: 'ember',
    label: 'Brasa',
    mode: 'gradient',
    description: 'Naranjas profundos con un acento encendido.',
    previewStyle: {
      backgroundColor: '#7c2d12',
      backgroundImage: 'linear-gradient(135deg, #7c2d12 0%, #ea580c 50%, #fb7185 100%)',
    },
  },
  {
    key: 'lagoon',
    label: 'Laguna',
    mode: 'gradient',
    description: 'Turquesas y azules que respiran más luz.',
    previewStyle: {
      backgroundColor: '#155e75',
      backgroundImage: 'linear-gradient(135deg, #155e75 0%, #06b6d4 52%, #93c5fd 100%)',
    },
  },
  {
    key: 'galaxy',
    label: 'Galaxia',
    mode: 'gradient',
    description: 'Oscuro con brillo, para un look más premium.',
    previewStyle: {
      backgroundColor: '#111827',
      backgroundImage: 'radial-gradient(circle at 20% 20%, rgba(129, 140, 248, 0.55) 0, rgba(129, 140, 248, 0) 30%), linear-gradient(135deg, #111827 0%, #312e81 55%, #0f172a 100%)',
    },
  },
  {
    key: 'rose-gold',
    label: 'Rosa dorado',
    mode: 'gradient',
    description: 'Cálido, luminoso y más editorial.',
    previewStyle: {
      backgroundColor: '#881337',
      backgroundImage: 'linear-gradient(135deg, #881337 0%, #db2777 52%, #f9a8d4 100%)',
    },
  },
  {
    key: 'arctic',
    label: 'Ártico',
    mode: 'gradient',
    description: 'Muy limpio, frío y con mucho aire.',
    previewStyle: {
      backgroundColor: '#0f172a',
      backgroundImage: 'linear-gradient(135deg, #0f172a 0%, #0369a1 52%, #bae6fd 100%)',
    },
  },
  {
    key: 'sunset-ember',
    label: 'Brasa al atardecer',
    mode: 'gradient',
    description: 'La versión más cálida y dramática del catálogo.',
    previewStyle: {
      backgroundColor: '#431407',
      backgroundImage: 'linear-gradient(135deg, #431407 0%, #c2410c 40%, #fb7185 100%)',
    },
  },
] as const;

const PRESET_MAP = Object.fromEntries(LOGIN_BACKGROUND_PRESETS.map((preset) => [preset.key, preset])) as Record<LoginBackgroundPresetKey, LoginBackgroundPreset>;

export const DEFAULT_LOGIN_BACKGROUND_PRESET_KEY: LoginBackgroundPresetKey = 'midnight-indigo';

export const DEFAULT_LOGIN_BACKGROUND_MODE: LoginBackgroundMode = 'gradient';

export function isLoginBackgroundMode(value: unknown): value is LoginBackgroundMode {
  return value === 'image' || value === 'color' || value === 'gradient';
}

export function isLoginBackgroundPresetKey(value: unknown): value is LoginBackgroundPresetKey {
  return LOGIN_BACKGROUND_PRESETS.some((preset) => preset.key === value);
}

export function getLoginBackgroundPreset(key?: string | null): LoginBackgroundPreset {
  if (key && isLoginBackgroundPresetKey(key)) {
    return PRESET_MAP[key];
  }

  return PRESET_MAP[DEFAULT_LOGIN_BACKGROUND_PRESET_KEY];
}

export function getDefaultPresetKeyForMode(mode: LoginBackgroundMode): LoginBackgroundPresetKey {
  switch (mode) {
    case 'color':
      return 'midnight';
    case 'image':
      return DEFAULT_LOGIN_BACKGROUND_PRESET_KEY;
    case 'gradient':
    default:
      return DEFAULT_LOGIN_BACKGROUND_PRESET_KEY;
  }
}

export function getLoginBackgroundPresetOptions(mode: Exclude<LoginBackgroundMode, 'image'>): readonly LoginBackgroundPreset[] {
  return LOGIN_BACKGROUND_PRESETS.filter((preset) => preset.mode === mode);
}
