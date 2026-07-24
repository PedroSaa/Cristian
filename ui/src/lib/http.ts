import axios from 'axios';
import { mapAuthUser } from '@/types/auth';

const http = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// ── Cookie reader utility ──────────────────────────────────────────────────

function getCookie(name: string): string | null {
  const match = document.cookie.match(new RegExp(`(?:^|;\\s*)${name}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
}

// ── Request interceptor: CSRF headers on mutating requests ─────────────────
// The access_token is now HttpOnly — the browser auto-sends it.
// We do NOT inject Bearer headers anymore, but we DO send:
//   1. X-Requested-With (backward-compat marker)
//   2. X-CSRF-TOKEN    (Double Submit Cookie — actual token validation)

http.interceptors.request.use((config) => {
  const method = (config.method ?? 'get').toUpperCase();
  if (method !== 'GET' && method !== 'HEAD' && method !== 'OPTIONS' && method !== 'TRACE') {
    config.headers = config.headers ?? {};
    config.headers['X-Requested-With'] = 'XMLHttpRequest';

    // Double Submit Cookie: send the CSRF token from the cookie as a header
    const csrfToken = getCookie('XSRF-TOKEN');
    if (csrfToken) {
      config.headers['X-CSRF-TOKEN'] = csrfToken;
    }
  }
  return config;
});

// ── Response interceptor: 401 → refresh or logout (cookie-aware) ──────────

let isRefreshing = false;
let pendingQueue: Array<{
  resolve: () => void;
  reject: (err: unknown) => void;
}> = [];

function processQueue(error: unknown) {
  pendingQueue.forEach((p) => {
    if (error) {
      p.reject(error);
    } else {
      p.resolve();
    }
  });
  pendingQueue = [];
}

http.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    const status = error.response?.status;
    const requestUrl = String(originalRequest?.url ?? '');
    const isInteractiveAuthStep =
      requestUrl.includes('/auth/login') ||
      requestUrl.includes('/auth/login/mfa');

    // Auth endpoints: always reject — user needs to retry
    if (status === 401 && isInteractiveAuthStep) {
      return Promise.reject(error);
    }

    // ── 401 handling with refresh retry ───────────────────────────────────
    if (status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        // Queue this request until the refresh resolves, then retry
        // (cookie is auto-sent by the browser after refresh sets it)
        return new Promise<void>((resolve, reject) => {
          pendingQueue.push({ resolve, reject });
        }).then(() => http(originalRequest));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const { data } = await axios.post('/api/auth/refresh', {}, {
          headers: {
            'Content-Type': 'application/json',
            'X-Requested-With': 'XMLHttpRequest',
          },
          withCredentials: true,
        });

        if (data.user) {
          const canonicalUser = mapAuthUser(data.user);
          localStorage.setItem('user', JSON.stringify({ ...canonicalUser, permissions: canonicalUser.permissions ?? [] }));
        }

        processQueue(null);

        // Retry the original request — cookie is now fresh
        return http(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError);
        localStorage.removeItem('user');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    // ── 400 CSRF: token desincronizado/viejo → regenerar cookie y reintentar ──
    // Pasa cuando la cookie XSRF-TOKEN del navegador quedó vieja o corrupta
    // (p. ej. de antes del fix base64url). Matcheamos el código estable del
    // backend (no el texto traducido). Borramos la cookie, pedimos un GET que
    // la regenera (el middleware la setea cuando falta) y reintentamos UNA vez.
    const csrfCode = error.response?.data?.codigo;
    if (
      status === 400 &&
      typeof csrfCode === 'string' &&
      csrfCode.startsWith('CSRF_') &&
      originalRequest &&
      !originalRequest._csrfRetry
    ) {
      originalRequest._csrfRetry = true;

      // Descartar la cookie vieja para que el servidor emita una nueva
      document.cookie = 'XSRF-TOKEN=; Path=/; Max-Age=0; SameSite=Strict';

      try {
        // Cualquier GET regenera la cookie (SetCsrfCookieIfMissing); usamos axios
        // crudo para no pasar por estos interceptores. El Set-Cookie se aplica
        // aunque la respuesta no sea 2xx.
        await axios.get('/api/auth/me', { withCredentials: true });
      } catch {
        // Ignorado: solo nos interesa el Set-Cookie de la respuesta.
      }

      // Reintentar: el request interceptor leerá la cookie nueva
      return http(originalRequest);
    }

    // ── Error message extraction (400/403/422/500) ────────────────────────
    if (status >= 400) {
      const data = error.response?.data;

      if (status === 403 && data?.error === 'MFARequired') {
        window.location.href = '/perfil?mfaRequired=true';
      }

      // ProblemDetails/ModelState: { errors: { campo: ["msg", ...] } }. Extraemos los
      // mensajes de campo para no mostrar el genérico "One or more validation errors occurred".
      const fieldErrors =
        data && typeof data === 'object' && data.errors && typeof data.errors === 'object'
          ? Object.values(data.errors as Record<string, unknown>)
              .flat()
              .filter((m): m is string => typeof m === 'string')
          : [];

      error.userMessage =
        data?.mensaje ||
        (fieldErrors.length ? fieldErrors.join(' ') : null) ||
        data?.message ||
        data?.title ||
        data?.error ||
        (typeof data === 'string' ? data : null) ||
        `Error ${status}: no se pudo completar la operación.`;
    }

    return Promise.reject(error);
  },
);

export default http;
