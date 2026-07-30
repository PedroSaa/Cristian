import { useQuery } from '@tanstack/react-query';
import { useEffect } from 'react';
import { getBranding, normalizeBranding, type BrandingDisplay, type BrandingDto } from '@/lib/api/brandingApi';

export const BRANDING_CACHE_KEY = 'docflow-infinity:branding-cache';

function readBrandingCache() {
  if (typeof window === 'undefined') return null;

  try {
    const cached = window.localStorage.getItem(BRANDING_CACHE_KEY);
    return cached ? (JSON.parse(cached) as BrandingDto) : null;
  } catch {
    return null;
  }
}

function writeBrandingCache(branding: NonNullable<Awaited<ReturnType<typeof getBranding>>>) {
  if (typeof window === 'undefined') return;

  try {
    window.localStorage.setItem(BRANDING_CACHE_KEY, JSON.stringify(branding));
  } catch {
    // Ignore storage failures.
  }
}

export function useBranding(): { branding: BrandingDisplay } & ReturnType<typeof useQuery> {
  const query = useQuery({
    queryKey: ['branding'],
    queryFn: getBranding,
    retry: 2,
    staleTime: 0,
    initialData: readBrandingCache() ?? undefined,
  });

  useEffect(() => {
    if (query.data) {
      writeBrandingCache(query.data);
    }
  }, [query.data]);

  return {
    ...query,
    branding: normalizeBranding(query.data),
  };
}
