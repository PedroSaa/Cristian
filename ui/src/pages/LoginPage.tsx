import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { login, loginMfa } from '../lib/api/auth';
import BrandingIdentity from '../components/molecules/BrandingIdentity';
import { useBranding } from '../hooks/useBranding';
import { getLoginBackgroundPreset } from '../lib/branding/loginBackgroundPalette';
import {
  getLoginSurfaceToneClasses,
  getLoginSplitBrandPanelStyle,
  normalizeLoginSurfaceTone,
  normalizeLoginTemplateKey,
  type LoginSurfaceTone,
  type LoginTemplateKey,
} from '../lib/branding/loginDesign';
import {
  DEFAULT_LOGIN_BRAND_TAGLINE,
  DEFAULT_LOGIN_BRAND_FOOTER_NOTE,
  type BrandingDisplay,
} from '../lib/api/brandingApi';

function LoginBackgroundShell({ branding, backgroundImageFailed, onBackgroundImageError }: {
  branding: BrandingDisplay;
  backgroundImageFailed: boolean;
  onBackgroundImageError: () => void;
}) {
  const loginBackgroundPreset = getLoginBackgroundPreset(branding.loginBackgroundPresetKey);
  const loginBackgroundImage = branding.loginBackgroundMode === 'image' ? branding.loginBackgroundUrl : null;
  const showLoginBackgroundImage = Boolean(loginBackgroundImage) && !backgroundImageFailed;

  return (
    <div
      data-testid="login-background-shell"
      className="absolute inset-0 overflow-hidden"
      style={loginBackgroundPreset.previewStyle}
    >
      <div className="absolute -left-20 top-[-5rem] h-72 w-72 rounded-full bg-white/10 blur-3xl" />
      <div className="absolute bottom-[-7rem] right-[-5rem] h-80 w-80 rounded-full bg-cyan-400/12 blur-3xl" />
      <div className="absolute inset-0 bg-[linear-gradient(135deg,rgba(15,23,42,0.32),rgba(2,6,23,0.52))]" />
      {showLoginBackgroundImage && (
        <img
          src={loginBackgroundImage!}
          alt={`Fondo de acceso de ${branding.nombreInstitucion}`}
          className="absolute inset-0 h-full w-full object-cover opacity-95"
          onError={onBackgroundImageError}
        />
      )}
      <div className="absolute inset-0 bg-slate-950/30" />
    </div>
  );
}

function LoginSurface({ tone, className = '', children, regionLabel }: {
  tone: LoginSurfaceTone;
  className?: string;
  children: ReactNode;
  regionLabel: string;
}) {
  const surfaceClasses = getLoginSurfaceToneClasses(tone);

  return (
    <section
      aria-label={regionLabel}
      className={[
        'w-full rounded-3xl border p-6 sm:p-8',
        surfaceClasses.panelBorder,
        surfaceClasses.panel,
        className,
      ].join(' ')}
    >
      {children}
    </section>
  );
}

function LoginBrandPanel({
  branding,
  welcomeMessageBlock,
  tone,
}: {
  branding: BrandingDisplay;
  welcomeMessageBlock: ReactNode;
  tone: LoginSurfaceTone;
}) {
  return (
    <aside
      aria-label="Identidad institucional"
      className="relative isolate flex min-h-[32rem] flex-col overflow-hidden rounded-[2rem] px-8 py-10 text-center sm:px-10 lg:px-12 lg:py-14"
      style={{
        ...getLoginSplitBrandPanelStyle(tone),
        borderWidth: 0,
        boxShadow: 'none',
        textAlign: 'center',
      }}
    >
      <div className="relative z-10 flex flex-1 flex-col items-center justify-center gap-8">
        <BrandingIdentity branding={branding} align="center" size="lg" tone={tone} />
        <div className="mx-auto flex max-w-md flex-col items-center gap-4">
          <p className="text-sm font-semibold uppercase tracking-[0.24em]">{branding.loginBrandTagline ?? DEFAULT_LOGIN_BRAND_TAGLINE}</p>
          <div className="space-y-2 text-center">{welcomeMessageBlock}</div>
        </div>
      </div>

      <p className="relative z-10 hidden px-8 pt-2 text-center text-[0.7rem] font-medium uppercase tracking-[0.24em] text-current/45 lg:block lg:px-0 lg:pb-1">
        {branding.loginBrandFooterNote ?? DEFAULT_LOGIN_BRAND_FOOTER_NOTE}
      </p>
    </aside>
  );
}

function LoginAuthHeader({
  title,
  subtitle,
  surfaceTone,
  centered = false,
}: {
  title: string;
  subtitle?: string;
  surfaceTone: LoginSurfaceTone;
  centered?: boolean;
}) {
  const surfaceClasses = getLoginSurfaceToneClasses(surfaceTone);

  return (
    <div className={['space-y-2', centered ? 'text-center' : ''].join(' ')} style={centered ? { textAlign: 'center' } : undefined}>
      <h2 className={['text-2xl font-semibold tracking-tight sm:text-3xl', surfaceClasses.title].join(' ')}>{title}</h2>
      {subtitle && <p className={['text-sm leading-relaxed', surfaceClasses.mutedText].join(' ')}>{subtitle}</p>}
    </div>
  );
}

function LoginCredentialsForm({
  tone,
  loading,
  error,
  identifier,
  password,
  onIdentifierChange,
  onPasswordChange,
  onSubmit,
}: {
  tone: LoginSurfaceTone;
  loading: boolean;
  error: string;
  identifier: string;
  password: string;
  onIdentifierChange: (value: string) => void;
  onPasswordChange: (value: string) => void;
  onSubmit: (e: FormEvent) => void;
}) {
  const surfaceClasses = getLoginSurfaceToneClasses(tone);

  return (
    <form onSubmit={onSubmit} aria-label="Formulario de acceso" className="mt-8 space-y-4">
      <div>
        <label htmlFor="login-identifier" className={['mb-1 block text-xs font-medium', surfaceClasses.text].join(' ')}>
          Email o usuario
        </label>
        <input
          id="login-identifier"
          type="text"
          autoComplete="username"
          value={identifier}
          onChange={(e) => onIdentifierChange(e.target.value)}
          placeholder="correo@docflow.cl o jperez"
          required
          className={['block w-full rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2', surfaceClasses.input].join(' ')}
        />
      </div>
      <div>
        <label htmlFor="login-password" className={['mb-1 block text-xs font-medium', surfaceClasses.text].join(' ')}>Contraseña</label>
        <input
          id="login-password"
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => onPasswordChange(e.target.value)}
          placeholder="••••••••"
          required
          className={['block w-full rounded border px-3 py-2 text-sm focus:outline-none focus:ring-2', surfaceClasses.input].join(' ')}
        />
      </div>

      {error && <p role="alert" className="text-sm text-red-600">{error}</p>}

      <button
        type="submit"
        disabled={loading}
        className={['w-full rounded px-4 py-2 text-sm font-medium transition-colors disabled:opacity-50', surfaceClasses.primaryButton].join(' ')}
      >
        {loading ? 'Ingresando...' : 'Ingresar'}
      </button>
    </form>
  );
}

function LoginMfaForm({
  tone,
  loading,
  error,
  code,
  onCodeChange,
  onSubmit,
  onCancel,
}: {
  tone: LoginSurfaceTone;
  loading: boolean;
  error: string;
  code: string;
  onCodeChange: (value: string) => void;
  onSubmit: (e: FormEvent) => void;
  onCancel: () => void;
}) {
  const surfaceClasses = getLoginSurfaceToneClasses(tone);

  return (
    <form onSubmit={onSubmit} aria-label="Verificación MFA" className="mt-8 space-y-4">
      <div>
        <label htmlFor="login-mfa-code" className={['mb-1 block text-xs font-medium', surfaceClasses.text].join(' ')}>Código</label>
        <input
          id="login-mfa-code"
          type="text"
          inputMode="numeric"
          maxLength={6}
          value={code}
          onChange={(e) => onCodeChange(e.target.value.replace(/\D/g, '').slice(0, 6))}
          placeholder="000000"
          required
          className={['block w-full rounded border px-3 py-2 text-center text-lg tracking-widest focus:outline-none focus:ring-2', surfaceClasses.input].join(' ')}
        />
      </div>

      {error && <p role="alert" className="text-sm text-red-600">{error}</p>}

      <button
        type="submit"
        disabled={loading || code.length !== 6}
        className={['w-full rounded px-4 py-2 text-sm font-medium transition-colors disabled:opacity-50', surfaceClasses.primaryButton].join(' ')}
      >
        {loading ? 'Verificando...' : 'Verificar'}
      </button>

      <button
        type="button"
        onClick={onCancel}
        className={['w-full rounded border px-4 py-2 text-sm font-medium transition-colors', surfaceClasses.secondaryButton].join(' ')}
      >
        Cancelar
      </button>
    </form>
  );
}

function LoginCenteredTemplate({
  branding,
  surfaceTone,
  loading,
  error,
  identifier,
  password,
  code,
  requiresMfa,
  onIdentifierChange,
  onPasswordChange,
  onCodeChange,
  onSubmit,
  onMfaSubmit,
  onCancelMfa,
  welcomeMessageBlock,
  credentialSubtitle,
}: {
  branding: BrandingDisplay;
  surfaceTone: LoginSurfaceTone;
  loading: boolean;
  error: string;
  identifier: string;
  password: string;
  code: string;
  requiresMfa: boolean;
  onIdentifierChange: (value: string) => void;
  onPasswordChange: (value: string) => void;
  onCodeChange: (value: string) => void;
  onSubmit: (e: FormEvent) => void;
  onMfaSubmit: (e: FormEvent) => void;
  onCancelMfa: () => void;
  welcomeMessageBlock: ReactNode;
  credentialSubtitle?: string;
}) {
  return (
    <div className="relative z-10 flex min-h-screen items-center justify-center px-4 py-10">
      <div className="w-full max-w-md">
        <LoginSurface tone={surfaceTone} regionLabel={`Superficie de acceso ${surfaceTone === 'dark' ? 'oscura' : 'clara'}`}>
          <section aria-label="Información de acceso" className="space-y-5">
            <div className="space-y-5">
              <BrandingIdentity
                branding={branding}
                align="center"
                size="lg"
                tone={surfaceTone}
                subtitle={requiresMfa ? 'Ingresá el código de 6 dígitos de tu aplicación autenticadora' : undefined}
              />
              {!requiresMfa && (
                <div className={['space-y-1 text-center', surfaceTone === 'dark' ? 'text-white/92' : 'text-slate-700'].join(' ')}>
                  {credentialSubtitle && <p className="text-sm">{credentialSubtitle}</p>}
                  {welcomeMessageBlock}
                </div>
              )}
            </div>
          </section>

          {requiresMfa ? (
            <LoginMfaForm
              tone={surfaceTone}
              loading={loading}
              error={error}
              code={code}
              onCodeChange={onCodeChange}
              onSubmit={onMfaSubmit}
              onCancel={onCancelMfa}
            />
          ) : (
            <LoginCredentialsForm
              tone={surfaceTone}
              loading={loading}
              error={error}
              identifier={identifier}
              password={password}
              onIdentifierChange={onIdentifierChange}
              onPasswordChange={onPasswordChange}
              onSubmit={onSubmit}
            />
          )}

          {import.meta.env.DEV && !requiresMfa && (
            <div className="mt-4 rounded bg-gray-50 p-3 text-xs text-gray-500">
              <p className="font-medium">Usuarios de prueba (solo desarrollo):</p>
              <p>jperez@docflow.cl o jperez / admin123 (Usuario)</p>
              <p>admin@docflow.cl o admin / admin123 (Admin)</p>
              <p>rrhh@docflow.cl o rrhh / admin123 (RRHH)</p>
              <p>jefatura@docflow.cl o jefatura / admin123 (Jefatura)</p>
            </div>
          )}
        </LoginSurface>
      </div>
    </div>
  );
}

function LoginSplitTemplate({
  branding,
  surfaceTone,
  loading,
  error,
  identifier,
  password,
  code,
  requiresMfa,
  onIdentifierChange,
  onPasswordChange,
  onCodeChange,
  onSubmit,
  onMfaSubmit,
  onCancelMfa,
  welcomeMessageBlock,
}: {
  branding: BrandingDisplay;
  surfaceTone: LoginSurfaceTone;
  loading: boolean;
  error: string;
  identifier: string;
  password: string;
  code: string;
  requiresMfa: boolean;
  onIdentifierChange: (value: string) => void;
  onPasswordChange: (value: string) => void;
  onCodeChange: (value: string) => void;
  onSubmit: (e: FormEvent) => void;
  onMfaSubmit: (e: FormEvent) => void;
  onCancelMfa: () => void;
  welcomeMessageBlock: ReactNode;
}) {
  const surfaceLabel = `Superficie de acceso ${surfaceTone === 'dark' ? 'oscura' : 'clara'}`;

  return (
    <div className="relative z-10 flex min-h-screen items-center px-4 py-10 lg:items-stretch">
      <div className="mx-auto grid w-full max-w-6xl gap-6 lg:grid-cols-[minmax(0,1.1fr)_minmax(22rem,0.9fr)]">
        <LoginBrandPanel branding={branding} welcomeMessageBlock={welcomeMessageBlock} tone={surfaceTone} />

        <LoginSurface tone={surfaceTone} regionLabel={surfaceLabel} className="self-center lg:self-stretch">
          <div className="flex h-full flex-col justify-center">
            <LoginAuthHeader
              centered
              title={requiresMfa ? 'Verificación MFA' : 'Inicio de sesión'}
              subtitle={requiresMfa ? 'Ingresá el código de 6 dígitos para continuar.' : undefined}
              surfaceTone={surfaceTone}
            />

            {requiresMfa ? (
              <LoginMfaForm
                tone={surfaceTone}
                loading={loading}
                error={error}
                code={code}
                onCodeChange={onCodeChange}
                onSubmit={onMfaSubmit}
                onCancel={onCancelMfa}
              />
            ) : (
              <LoginCredentialsForm
                tone={surfaceTone}
                loading={loading}
                error={error}
                identifier={identifier}
                password={password}
                onIdentifierChange={onIdentifierChange}
                onPasswordChange={onPasswordChange}
                onSubmit={onSubmit}
              />
            )}

            {import.meta.env.DEV && !requiresMfa && (
              <div className={['mt-4 rounded p-3 text-xs', getLoginSurfaceToneClasses(surfaceTone).badge].join(' ')}>
                <p className="font-medium">Usuarios de prueba (solo desarrollo):</p>
                <p>jperez@docflow.cl o jperez / admin123 (Usuario)</p>
                <p>admin@docflow.cl o admin / admin123 (Admin)</p>
                <p>rrhh@docflow.cl o rrhh / admin123 (RRHH)</p>
                <p>jefatura@docflow.cl o jefatura / admin123 (Jefatura)</p>
              </div>
            )}
          </div>
        </LoginSurface>
      </div>
    </div>
  );
}

export default function LoginPage() {
  const { login: authLogin, state: authState, requireMfa, cancelMfa } = useAuth();
  const navigate = useNavigate();
  const { branding, isPending: isBrandingPending } = useBranding();
  const [identifier, setIdentifier] = useState('');
  const [password, setPassword] = useState('');
  const [mfaCode, setMfaCode] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [backgroundImageFailed, setBackgroundImageFailed] = useState(false);

  useEffect(() => {
    setBackgroundImageFailed(false);
  }, [branding.loginBackgroundMode, branding.loginBackgroundPresetKey, branding.loginBackgroundUrl]);

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const response = await login(identifier, password);

      if (response.authState === 'mfa_setup_required') {
        if (!response.user) {
          throw new Error('Respuesta de login inválida');
        }

        authLogin({
          ...response.user,
          authState: response.authState,
          setupToken: response.setupToken ?? response.user.setupToken,
          canLogout: response.canLogout ?? response.user.canLogout ?? true,
          permissions: response.user.permissions ?? [],
        });
        navigate('/perfil?mfaRequired=true');
        return;
      }

      if (response.requiresMfa && response.mfaToken) {
        requireMfa(response.mfaToken);
        return;
      }

      if (!response.user) {
        throw new Error('Respuesta de login inválida');
      }

      authLogin({ ...response.user, permissions: response.user.permissions ?? [] });
      navigate('/inbox');
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { mensaje?: string } } };
        setError(axiosErr.response?.data?.mensaje ?? 'Identificador o contraseña incorrectos.');
      } else {
        setError('Identificador o contraseña incorrectos.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleMfaSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const response = await loginMfa(authState.mfaToken!, mfaCode);

      if (!response.user) {
        throw new Error('Respuesta de login MFA inválida');
      }

      authLogin({ ...response.user, permissions: response.user.permissions ?? [] });
      navigate('/inbox');
    } catch (err: unknown) {
      if (err && typeof err === 'object' && 'response' in err) {
        const axiosErr = err as { response?: { data?: { mensaje?: string } } };
        setError(axiosErr.response?.data?.mensaje ?? 'Código inválido. Intentá de nuevo.');
      } else {
        setError('Código inválido. Intentá de nuevo.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleCancelMfa = () => {
    cancelMfa();
    setMfaCode('');
    setError('');
    setIdentifier('');
    setPassword('');
    navigate('/login', { replace: true });
  };

  const templateKey: LoginTemplateKey = normalizeLoginTemplateKey(branding.loginTemplateKey);
  const surfaceTone: LoginSurfaceTone = normalizeLoginSurfaceTone(branding.loginSurfaceTone);
  const hasWelcomeMessage = Boolean(
    branding.loginWelcomeTitle || branding.loginWelcomeSubtitle || branding.loginWelcomeHelpText,
  );
  const credentialSubtitle = hasWelcomeMessage ? undefined : 'Ingresá tus credenciales para continuar';
  const centeredBrandWelcomeMessageBlock = hasWelcomeMessage ? (
    <div className="space-y-1 text-center">
      {branding.loginWelcomeTitle && <p className="text-sm font-semibold">{branding.loginWelcomeTitle}</p>}
      {branding.loginWelcomeSubtitle && <p className="text-sm">{branding.loginWelcomeSubtitle}</p>}
      {branding.loginWelcomeHelpText && <p className="text-xs leading-relaxed">{branding.loginWelcomeHelpText}</p>}
    </div>
  ) : null;

  const splitBrandWelcomeMessageBlock = hasWelcomeMessage ? (
    <section
      aria-label="Mensaje de bienvenida"
      className="space-y-3 text-center"
      style={{ color: surfaceTone === 'dark' ? 'rgba(255, 255, 255, 0.92)' : 'rgb(15, 23, 42)' }}
    >
      {branding.loginWelcomeTitle && (
        <h2
          className="text-3xl font-semibold tracking-tight sm:text-4xl"
          style={{ color: surfaceTone === 'dark' ? 'rgba(255, 255, 255, 0.92)' : 'rgb(15, 23, 42)' }}
        >
          {branding.loginWelcomeTitle}
        </h2>
      )}
      {branding.loginWelcomeSubtitle && (
        <p
          className="text-sm font-medium leading-relaxed"
          style={{ color: surfaceTone === 'dark' ? 'rgba(255, 255, 255, 0.72)' : 'rgba(15, 23, 42, 0.72)' }}
        >
          {branding.loginWelcomeSubtitle}
        </p>
      )}
      {branding.loginWelcomeHelpText && (
        <p
          className="text-xs leading-relaxed"
          style={{ color: surfaceTone === 'dark' ? 'rgba(255, 255, 255, 0.58)' : 'rgba(15, 23, 42, 0.58)' }}
        >
          {branding.loginWelcomeHelpText}
        </p>
      )}
    </section>
  ) : null;

  const loadingShell = (
    <div className="relative z-10 flex min-h-screen items-center justify-center px-4 py-10">
      <div className="w-full max-w-md rounded-lg border border-gray-200 bg-white p-8 shadow-sm sm:p-10">
        <div role="status" aria-label="Cargando acceso" className="space-y-5 text-center">
          <div className="mx-auto h-16 w-16 animate-pulse rounded-2xl border border-gray-200 bg-gray-100" />
          <div className="space-y-2">
            <p className="text-sm font-medium text-gray-900">Cargando acceso…</p>
            <p className="text-sm text-gray-500">Preparando la identidad institucional.</p>
          </div>
        </div>
      </div>
    </div>
  );

  if (isBrandingPending) {
    return (
      <div className="relative min-h-screen overflow-hidden bg-slate-950 text-text-base">
        <LoginBackgroundShell
          branding={branding}
          backgroundImageFailed={backgroundImageFailed}
          onBackgroundImageError={() => setBackgroundImageFailed(true)}
        />
        {loadingShell}
      </div>
    );
  }

  const templateProps = {
    branding,
    surfaceTone,
    loading,
    error,
    identifier,
    password,
    code: mfaCode,
    requiresMfa: authState.requiresMfa,
    onIdentifierChange: setIdentifier,
    onPasswordChange: setPassword,
    onCodeChange: setMfaCode,
    onSubmit: handleSubmit,
    onMfaSubmit: handleMfaSubmit,
    onCancelMfa: handleCancelMfa,
    welcomeMessageBlock: templateKey === 'split-brand' ? splitBrandWelcomeMessageBlock : centeredBrandWelcomeMessageBlock,
  };

  return (
    <div className="relative min-h-screen overflow-hidden bg-slate-950 text-text-base">
      <LoginBackgroundShell
        branding={branding}
        backgroundImageFailed={backgroundImageFailed}
        onBackgroundImageError={() => setBackgroundImageFailed(true)}
      />

      {templateKey === 'split-brand' ? (
        <LoginSplitTemplate {...templateProps} />
      ) : (
        <LoginCenteredTemplate
          {...templateProps}
          credentialSubtitle={credentialSubtitle}
        />
      )}
    </div>
  );
}
