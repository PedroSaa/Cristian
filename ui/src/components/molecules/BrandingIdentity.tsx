import { useState } from 'react';
import { DEFAULT_BRANDING_NAME, type BrandingDisplay } from '@/lib/api/brandingApi';

interface BrandingIdentityProps {
  branding?: BrandingDisplay;
  subtitle?: string;
  align?: 'left' | 'center';
  size?: 'sm' | 'md' | 'lg';
  tone?: 'light' | 'dark';
}

const SIZE_STYLES = {
  sm: {
    logo: 'h-10 w-10 rounded-xl',
    brand: 'text-[11px] font-semibold uppercase tracking-[0.24em]',
    name: 'text-sm font-semibold',
    subtitle: 'text-xs',
    fallback: 'text-[0.65rem] font-semibold tracking-[0.24em]',
  },
  md: {
    logo: 'h-12 w-12 rounded-xl',
    brand: 'text-[11px] font-semibold uppercase tracking-[0.26em]',
    name: 'text-lg font-semibold tracking-tight',
    subtitle: 'text-sm',
    fallback: 'text-[0.7rem] font-semibold tracking-[0.26em]',
  },
  lg: {
    logo: 'h-24 w-24 rounded-2xl',
    brand: 'text-[11px] font-semibold uppercase tracking-[0.28em]',
    name: 'text-2xl font-bold tracking-tight sm:text-3xl',
    subtitle: 'text-sm',
    fallback: 'text-sm font-semibold tracking-[0.28em]',
  },
} as const;

export default function BrandingIdentity({ branding, subtitle, align = 'left', size = 'sm', tone = 'light' }: BrandingIdentityProps) {
  const [logoError, setLogoError] = useState(false);
  const displayName = branding?.nombreInstitucion?.trim() || DEFAULT_BRANDING_NAME;
  const logoUrl = branding?.logoUrl ?? null;
  const showLogo = Boolean(logoUrl) && !logoError;
  const styles = SIZE_STYLES[size];
  const centered = align === 'center';
  const darkTone = tone === 'dark';

  return (
    <div className={[
      'flex min-w-0 gap-3',
      centered ? 'flex-col items-center text-center' : 'items-center',
    ].join(' ')}>
      <div className={[
        'flex shrink-0 items-center justify-center overflow-hidden border shadow-sm',
        darkTone ? 'border-white/10 bg-white/5' : 'border-border-base bg-surface',
        styles.logo,
      ].join(' ')}>
        {showLogo ? (
          <img
            src={logoUrl!}
            alt={`Logo de ${displayName}`}
            className="h-full w-full object-contain p-1.5"
            onError={() => setLogoError(true)}
          />
        ) : (
          <span className={[darkTone ? 'text-white' : 'text-primary-700', styles.fallback].join(' ')}>DF</span>
        )}
      </div>

      <div className={centered ? 'space-y-1' : 'min-w-0'}>
        <p className={[styles.brand, darkTone ? 'text-white/80' : 'text-primary-700'].join(' ')}>DocFlow Infinity</p>
        <h1 className={[
          styles.name,
          centered ? (darkTone ? 'text-white' : 'text-text-base') : darkTone ? 'truncate text-white' : 'truncate text-text-base',
        ].join(' ')}>
          {displayName}
        </h1>
        {subtitle && (
          <p className={[
            styles.subtitle,
            centered ? (darkTone ? 'max-w-sm text-white/70' : 'max-w-sm text-text-base/60') : darkTone ? 'truncate text-white/70' : 'truncate text-text-base/55',
          ].join(' ')}>
            {subtitle}
          </p>
        )}
      </div>
    </div>
  );
}
