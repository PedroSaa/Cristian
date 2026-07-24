import { useState, useEffect, useMemo, type CSSProperties, type ReactNode } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  getConfiguraciones,
  upsertConfiguracion,
  type ConfiguracionDto,
} from '../../lib/api/admin/adminConfiguracionApi';
import {
  listPlantillasNumeracion,
  setPlantillaActiva,
} from '../../lib/api/admin/plantillasNumeracionApi';
import {
  DEFAULT_BRANDING_NAME,
  DEFAULT_LOGIN_BRAND_TAGLINE,
  DEFAULT_LOGIN_BRAND_FOOTER_NOTE,
  getBranding,
  normalizeBranding,
  uploadBrandingLogo,
  uploadBrandingLoginBackground,
  type BrandingDisplay,
} from '../../lib/api/brandingApi';
import BrandingIdentity from '../../components/molecules/BrandingIdentity';
import {
  DEFAULT_LOGIN_BACKGROUND_PRESET_KEY,
  getDefaultPresetKeyForMode,
  getLoginBackgroundPreset,
  getLoginBackgroundPresetOptions,
  type LoginBackgroundMode,
  type LoginBackgroundPresetKey,
} from '../../lib/branding/loginBackgroundPalette';
import {
  LOGIN_SURFACE_TONE_OPTIONS,
  LOGIN_TEMPLATE_OPTIONS,
  getLoginSurfaceToneClasses,
  getLoginSplitBrandPanelStyle,
  normalizeLoginSurfaceTone,
  normalizeLoginTemplateKey,
  type LoginSurfaceTone,
  type LoginTemplateKey,
} from '../../lib/branding/loginDesign';
import Spinner from '../../components/atoms/Spinner';
import IconButton from '../../components/atoms/IconButton';
import ModalDialog from '../../components/organisms/ModalDialog';
import Button from '../../components/atoms/Button';
import Toggle from '../../components/atoms/Toggle';
import TabPanel from '../../components/organisms/TabPanel';
import { useHasPermission } from '../../hooks/usePermissions';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import {
  getSecurityMeta,
  isSecurityConfigurationKey,
  isSecurityToggleKey,
  getRangeLabel,
} from './adminConfiguracionMeta';

type ModalMode = 'crear' | 'editar' | null;
type AdminConfiguracionTab = 'seguridad' | 'general' | 'acceso' | 'logo' | 'bienvenida';

const BRANDING_LOGO_KEY = 'LogoUrl';
const BRANDING_LOGIN_BACKGROUND_KEY = 'LoginBackgroundUrl';
const BRANDING_LOGIN_BACKGROUND_MODE_KEY = 'LoginBackgroundMode';
const BRANDING_LOGIN_BACKGROUND_PRESET_KEY = 'LoginBackgroundPresetKey';
const BRANDING_LOGIN_TEMPLATE_KEY = 'LoginTemplateKey';
const BRANDING_LOGIN_SURFACE_TONE_KEY = 'LoginSurfaceTone';
const BRANDING_LOGIN_WELCOME_TITLE_KEY = 'LoginWelcomeTitle';
const BRANDING_LOGIN_WELCOME_SUBTITLE_KEY = 'LoginWelcomeSubtitle';
const BRANDING_LOGIN_WELCOME_HELP_TEXT_KEY = 'LoginWelcomeHelpText';
const BRANDING_LOGIN_BRAND_TAGLINE_KEY = 'LoginBrandTagline';
const BRANDING_LOGIN_BRAND_FOOTER_NOTE_KEY = 'LoginBrandFooterNote';
const BRANDING_IMAGE_ACCEPT = 'image/png,image/jpeg,image/gif,image/webp';
const BRANDING_IMAGE_HELP_TEXT = 'PNG, JPG, GIF o WEBP. Máximo 5 MB.';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }

  return fallback;
}

function getPreviewBackgroundStyle(baseStyle: CSSProperties, backgroundSrc: string | null): CSSProperties {
  if (!backgroundSrc) return baseStyle;

  return {
    ...baseStyle,
    backgroundImage: `url("${backgroundSrc}")`,
    backgroundPosition: 'center',
    backgroundRepeat: 'no-repeat',
    backgroundSize: 'cover',
  };
}

function isSecurityConfiguration(item: ConfiguracionDto): boolean {
  return item.grupo === 'seguridad' || isSecurityConfigurationKey(item.clave);
}

function inferInputType(clave: string, valor: string): 'text' | 'url' | 'number' {
  const lowerClave = clave.toLowerCase();
  if (lowerClave.includes('url') || valor.startsWith('http://') || valor.startsWith('https://')) return 'url';
  if (/^-?\d+(\.\d+)?$/.test(valor)) return 'number';
  return 'text';
}

// ── Security card — one per security entry ───────────────────────────────────

interface SecurityCardProps {
  item: ConfiguracionDto;
  draft: string;
  error?: string | null;
  onDraftChange: (item: ConfiguracionDto, valor: string) => void;
  saving?: boolean;
  disabled?: boolean;
}

function validateSecurityDraft(item: ConfiguracionDto, draft: string): string | null {
  const meta = getSecurityMeta(item.clave);
  const inputType = meta?.inputType ?? (isSecurityToggleKey(item.clave) ? 'toggle' : 'text');
  const minValue = meta?.minValue ?? item.minValue ?? null;
  const maxValue = item.maxValue ?? null;

  if (inputType === 'toggle') {
    return draft === 'true' || draft === 'false' ? null : 'Ingresa un valor booleano válido.';
  }
  const parsed = Number(draft);
  if (!Number.isFinite(parsed)) return 'Ingresa un número válido.';
  if (minValue != null && parsed < minValue) return `El valor mínimo es ${minValue}.`;
  if (maxValue != null && parsed > maxValue) return `El valor máximo es ${maxValue}.`;
  return null;
}

function SecurityCard({ item, draft, error, onDraftChange, saving, disabled }: SecurityCardProps) {
  const meta = getSecurityMeta(item.clave);
  const label = meta?.label ?? item.clave;
  const helpText = meta?.helpText ?? item.descripcion;
  const inputType = meta?.inputType ?? (isSecurityToggleKey(item.clave) ? 'toggle' : 'text');
  const minValue = meta?.minValue ?? item.minValue ?? null;
  const maxValue = item.maxValue ?? null;
  const toggleId = `security-toggle-${item.clave.replace(/[^a-zA-Z0-9_-]/g, '-')}`;

  return (
    <div className="rounded-lg border border-gray-200 bg-white px-4 py-3.5 shadow-sm transition-shadow hover:shadow-md">
      <div className="mb-1 flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <span className="block text-sm font-medium text-gray-800">{label}</span>
          <span className="mt-0.5 block text-xs leading-relaxed text-gray-500">{helpText}</span>
        </div>
      </div>

      <div className="mt-2 space-y-2">
        {inputType === 'toggle' && (
          <div className="flex items-center justify-between gap-3 rounded border border-gray-300 px-3 py-2.5 text-sm text-gray-700">
            <label htmlFor={toggleId} className="cursor-pointer select-none">
              {draft === 'true' ? 'Activado' : draft === 'false' ? 'Desactivado' : draft}
            </label>
            <Toggle
              id={toggleId}
              checked={draft === 'true'}
              onChange={(e) => onDraftChange(item, e.target.checked ? 'true' : 'false')}
              label=""
              disabled={saving || disabled}
            />
          </div>
        )}

        {inputType === 'number' && (
          <div className="flex items-center gap-2">
            <input
              type="number"
              value={draft}
              min={minValue ?? undefined}
              max={maxValue ?? undefined}
              disabled={saving || disabled}
              onChange={(e) => onDraftChange(item, e.target.value)}
              className="block w-24 rounded border border-gray-300 px-2.5 py-1.5 text-sm text-gray-900 transition-colors focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500/20 disabled:cursor-not-allowed disabled:opacity-50"
            />
            {meta?.unit && <span className="text-sm text-gray-500">{meta.unit}</span>}
          </div>
        )}

        {inputType === 'text' && <span className="text-sm text-gray-700">{item.valor}</span>}

        {error && <p className="text-xs text-red-600">{error}</p>}
      </div>

      {/* Human-readable range hint */}
      {minValue != null && maxValue != null && meta?.unit && (
        <p className="mt-1.5 text-xs text-gray-400">
          {getRangeLabel(minValue, maxValue, meta.unit)}
        </p>
      )}
      {/* Fallback range for entries without metadata unit */}
      {minValue != null && maxValue != null && !meta?.unit && (
        <p className="mt-1.5 text-xs text-gray-400">
          {getRangeLabel(minValue, maxValue)}
        </p>
      )}

    </div>
  );
}

interface ConfigCardProps {
  title: string;
  description?: string | null;
  value: string;
  rangeLabel?: string;
  updatedAt?: string;
  canEdit?: boolean;
  onEdit: () => void;
}

function ConfigCard({ title, description, value, rangeLabel, updatedAt, canEdit, onEdit }: ConfigCardProps) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white px-4 py-3.5 shadow-sm transition-shadow hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <span className="block text-sm font-medium text-gray-800">{title}</span>
          {description && <span className="mt-0.5 block text-xs leading-relaxed text-gray-500">{description}</span>}
        </div>
        {canEdit && <IconButton name="edit" tooltip="Editar" appearance="admin" onClick={onEdit} />}
      </div>

      <div className="mt-3 space-y-1">
        <div className="text-sm text-gray-700">{value || '—'}</div>
        {rangeLabel && <p className="text-xs text-gray-400">{rangeLabel}</p>}
        {updatedAt && <p className="text-xs text-gray-400">Actualizado: {new Date(updatedAt).toLocaleDateString()}</p>}
      </div>
    </div>
  );
}

interface DesignChoiceCardProps {
  title: string;
  description: string;
  selected: boolean;
  onClick: () => void;
  preview: ReactNode;
  disabled?: boolean;
}

function DesignChoiceCard({ title, description, selected, onClick, preview, disabled }: DesignChoiceCardProps) {
  return (
    <button
      type="button"
      aria-label={title}
      aria-pressed={selected}
      disabled={disabled}
      onClick={onClick}
      className={[
        'overflow-hidden rounded-3xl border bg-white text-left shadow-sm transition-all hover:shadow-md disabled:cursor-not-allowed disabled:opacity-60',
        selected ? 'border-primary-300 ring-2 ring-primary-200' : 'border-gray-200 hover:border-primary-200',
      ].join(' ')}
    >
      <div className="border-b border-gray-100 bg-gray-50/80 p-3">{preview}</div>
      <div className="space-y-1 p-4">
        <p className="text-sm font-semibold text-gray-800">{title}</p>
        <p className="text-xs leading-relaxed text-gray-500">{description}</p>
      </div>
    </button>
  );
}

interface LoginDesignPreviewProps {
  branding: BrandingDisplay;
  templateKey: LoginTemplateKey;
  surfaceTone: LoginSurfaceTone;
  backgroundMode: LoginBackgroundMode;
  backgroundPresetKey: LoginBackgroundPresetKey;
  backgroundSrc: string | null;
  backgroundPresetStyle: CSSProperties;
  welcomeTitle: string | null;
  welcomeSubtitle: string | null;
  welcomeHelpText: string | null;
  brandTagline: string | null;
  brandFooterNote: string | null;
}

function LoginDesignPreview({
  branding,
  templateKey,
  surfaceTone,
  backgroundMode,
  backgroundPresetKey,
  backgroundSrc,
  backgroundPresetStyle,
  welcomeTitle,
  welcomeSubtitle,
  welcomeHelpText,
  brandTagline,
  brandFooterNote,
}: LoginDesignPreviewProps) {
  const surfaceClasses = getLoginSurfaceToneClasses(surfaceTone);
  const previewPreset = getLoginBackgroundPreset(backgroundPresetKey);
  const templateLabel = LOGIN_TEMPLATE_OPTIONS.find((option) => option.key === templateKey)?.label ?? 'Vista previa';
  const surfaceLabel = `Superficie de acceso ${surfaceTone === 'dark' ? 'oscura' : 'clara'}`;
  const showBackgroundImage = backgroundMode === 'image' && Boolean(backgroundSrc);
  const mainBackgroundStyle = getPreviewBackgroundStyle(backgroundPresetStyle, showBackgroundImage ? backgroundSrc : null);
  // El panel de marca es una superficie de color plano según el tono (claro/oscuro),
  // igual que el LoginBrandPanel real. La imagen de fondo es el telón de la página, no de la card.
  const splitBrandPanelStyle = getLoginSplitBrandPanelStyle(surfaceTone);

  const welcomeBlock = welcomeTitle || welcomeSubtitle || welcomeHelpText ? (
    <div className="space-y-1 text-center">
      {welcomeTitle && <p className="text-sm font-semibold">{welcomeTitle}</p>}
      {welcomeSubtitle && <p className="text-sm">{welcomeSubtitle}</p>}
      {welcomeHelpText && <p className="text-xs leading-relaxed">{welcomeHelpText}</p>}
    </div>
  ) : (
    <div className="space-y-1 text-center">
      <p className="text-xs leading-relaxed">Bienvenida sobria para la pantalla de acceso.</p>
    </div>
  );

  // Réplica visual (decorativa) del LoginCredentialsForm real, sin inputs funcionales.
  const formPreview = (
    <div aria-hidden className="mt-8 space-y-4 text-left">
      <div>
        <span className={['mb-1 block text-xs font-medium', surfaceClasses.text].join(' ')}>Email o usuario</span>
        <div className={['flex h-9 items-center rounded border px-3 text-sm', surfaceClasses.input].join(' ')}>
          <span className="truncate opacity-50">correo@docflow.cl o jperez</span>
        </div>
      </div>
      <div>
        <span className={['mb-1 block text-xs font-medium', surfaceClasses.text].join(' ')}>Contraseña</span>
        <div className={['flex h-9 items-center rounded border px-3 text-sm', surfaceClasses.input].join(' ')}>
          <span className="opacity-50">••••••••</span>
        </div>
      </div>
      <div className={['w-full rounded px-4 py-2 text-center text-sm font-medium', surfaceClasses.primaryButton].join(' ')}>
        Ingresar
      </div>
    </div>
  );

  const centeredPreview = (
    <section aria-label="Información de acceso" className="space-y-5">
      <div className="space-y-5">
        <BrandingIdentity branding={branding} align="center" size="lg" tone={surfaceTone} />
        <div className={['space-y-1 text-center', surfaceClasses.text].join(' ')}>{welcomeBlock}</div>
      </div>
      {formPreview}
    </section>
  );

  const splitPreview = (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1.1fr)_minmax(22rem,0.9fr)]">
      <aside
        aria-label="Identidad institucional"
        className={['relative isolate flex min-h-[26rem] flex-col overflow-hidden rounded-[2rem] px-8 py-10 text-center shadow-sm', surfaceTone === 'dark' ? 'border border-white/10' : 'border border-gray-200'].join(' ')}
        style={splitBrandPanelStyle}
      >
        <div className="relative z-10 flex flex-1 flex-col items-center justify-center gap-8 text-center">
          <BrandingIdentity branding={branding} align="center" size="lg" tone={surfaceTone} />
          <div className="mx-auto flex max-w-md flex-col items-center gap-4 text-center">
            <p className="text-sm font-semibold uppercase tracking-[0.24em]">{brandTagline || DEFAULT_LOGIN_BRAND_TAGLINE}</p>
            <div className="space-y-2">{welcomeBlock}</div>
          </div>
        </div>
        <p className="relative z-10 hidden px-2 pt-2 text-center text-[0.7rem] font-medium uppercase tracking-[0.24em] text-current/45 lg:block">
          {brandFooterNote || DEFAULT_LOGIN_BRAND_FOOTER_NOTE}
        </p>
      </aside>

      <section aria-label={surfaceLabel} className={['self-stretch rounded-3xl border p-6 shadow-sm sm:p-8', surfaceClasses.panelBorder, surfaceClasses.panel].join(' ')}>
        <div className="flex h-full flex-col justify-center">
          <div className="text-center">
            <h2 className={['text-2xl font-semibold tracking-tight sm:text-3xl', surfaceClasses.title].join(' ')}>Inicio de sesión</h2>
          </div>
          {formPreview}
        </div>
      </section>
    </div>
  );

  return (
    <section aria-label="Vista previa del acceso" className="rounded-3xl border border-gray-200 bg-white p-4 shadow-sm">
      <div className="relative isolate overflow-hidden rounded-2xl" style={mainBackgroundStyle} data-testid="login-design-preview-frame">
        {showBackgroundImage && (
          <img src={backgroundSrc!} alt="Vista previa del fondo de acceso" className="absolute inset-0 h-full w-full object-cover" />
        )}
        <div className="absolute inset-0 bg-slate-950/25" />

        <div className="relative space-y-5 p-4 sm:p-5">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-sm font-medium text-white/92">Vista previa del acceso</p>
              <p className="text-xs text-white/70">{templateLabel}</p>
            </div>

            <div className="flex h-14 w-24 items-center justify-center overflow-hidden rounded-2xl border border-white/20 bg-black/20" style={backgroundPresetStyle}>
              {showBackgroundImage ? (
                <img src={backgroundSrc!} alt="Vista previa del fondo de acceso" className="h-full w-full object-cover" />
              ) : (
                <span className="text-[0.65rem] font-semibold tracking-[0.24em] text-white/80">{previewPreset.label}</span>
              )}
            </div>
          </div>

          <div className={['rounded-[1.75rem] border p-4 text-slate-900 shadow-sm backdrop-blur-sm', showBackgroundImage ? 'border-white/10 bg-white/72' : 'border-white/10 bg-white/92'].join(' ')}>
            {templateKey === 'split-brand' ? (
              splitPreview
            ) : (
              <section aria-label={`Superficie de acceso ${surfaceTone === 'dark' ? 'oscura' : 'clara'}`} className={['mx-auto w-full max-w-md space-y-5 rounded-3xl border p-6 shadow-sm sm:p-8', surfaceClasses.panelBorder, surfaceClasses.panel].join(' ')}>
                {centeredPreview}
              </section>
            )}
          </div>
        </div>
      </div>
    </section>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export default function AdminConfiguracionPage() {
  const qc = useQueryClient();
  const toast = useToast();
  const canEditConfiguracion = useHasPermission(PERMISSIONS.ADMIN_CONFIG_EDITAR);
  const [modal, setModal] = useState<ModalMode>(null);
  const [editing, setEditing] = useState<ConfiguracionDto | null>(null);
  const [formData, setFormData] = useState({ clave: '', valor: '', descripcion: '' });
  const [formError, setFormError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<AdminConfiguracionTab>('seguridad');
  const [securityDrafts, setSecurityDrafts] = useState<Record<string, string>>({});
  const [securityNotice, setSecurityNotice] = useState<string | null>(null);
  const [selectedLogoFile, setSelectedLogoFile] = useState<File | null>(null);
  const [selectedLogoPreview, setSelectedLogoPreview] = useState<string | null>(null);
  const [selectedLoginBackgroundFile, setSelectedLoginBackgroundFile] = useState<File | null>(null);
  const [selectedLoginBackgroundPreview, setSelectedLoginBackgroundPreview] = useState<string | null>(null);
  const [loginBackgroundModeDraft, setLoginBackgroundModeDraft] = useState<LoginBackgroundMode>('gradient');
  const [loginBackgroundPresetKeyDraft, setLoginBackgroundPresetKeyDraft] = useState<LoginBackgroundPresetKey>(DEFAULT_LOGIN_BACKGROUND_PRESET_KEY);
  const [loginTemplateKeyDraft, setLoginTemplateKeyDraft] = useState<LoginTemplateKey>('centered-brand');
  const [loginSurfaceToneDraft, setLoginSurfaceToneDraft] = useState<LoginSurfaceTone>('light');
  const [loginWelcomeTitleDraft, setLoginWelcomeTitleDraft] = useState('');
  const [loginWelcomeSubtitleDraft, setLoginWelcomeSubtitleDraft] = useState('');
  const [loginWelcomeHelpTextDraft, setLoginWelcomeHelpTextDraft] = useState('');
  const [loginBrandTaglineDraft, setLoginBrandTaglineDraft] = useState('');
  const [loginBrandFooterNoteDraft, setLoginBrandFooterNoteDraft] = useState('');
  const [loginPreviewOpen, setLoginPreviewOpen] = useState(false);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-configuracion'],
    queryFn: getConfiguraciones,
  });

  const { data: brandingData } = useQuery({
    queryKey: ['branding'],
    queryFn: getBranding,
    retry: false,
  });

  const branding: BrandingDisplay = useMemo(() => normalizeBranding(brandingData), [brandingData]);

  // Plantilla de numeración del sistema (General): la activa es la única con activo=true.
  const { data: plantillasNum = [] } = useQuery({
    queryKey: ['admin-numeracion', 'plantillas'],
    queryFn: () => listPlantillasNumeracion(),
  });
  const plantillaActivaId = plantillasNum.find((p) => p.activo)?.id ?? null;

  const setPlantillaActivaMut = useMutation({
    mutationFn: setPlantillaActiva,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-numeracion', 'plantillas'] });
      toast.success('Plantilla de numeración del sistema actualizada.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo cambiar la plantilla de numeración.')),
  });

  const upsertMut = useMutation({
    mutationFn: async (items: Array<{ item: ConfiguracionDto; valor: string }>) => Promise.all(
      items.map(({ item, valor }) => upsertConfiguracion({ clave: item.clave, valor, descripcion: item.descripcion })),
    ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-configuracion'] });
      qc.invalidateQueries({ queryKey: ['branding'] });
      setEditing(null);
      setModal(null);
      setFormError(null);
      setSecurityNotice(null);
      toast.success('Configuración guardada correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo guardar la configuración.')),
  });

  const uploadLogoMut = useMutation({
    mutationFn: uploadBrandingLogo,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['branding'] });
      qc.invalidateQueries({ queryKey: ['admin-configuracion'] });
      setSelectedLogoFile(null);
      setSelectedLogoPreview(null);
      toast.success('Logo institucional actualizado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo subir el logo institucional.')),
  });

  const uploadLoginBackgroundMut = useMutation({
    mutationFn: uploadBrandingLoginBackground,
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['branding'] });
      qc.invalidateQueries({ queryKey: ['admin-configuracion'] });
      setSelectedLoginBackgroundFile(null);
      setSelectedLoginBackgroundPreview(null);
      toast.success('Fondo de acceso actualizado correctamente.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo subir el fondo de acceso.')),
  });

  const saveLoginDesignMut = useMutation({
    mutationFn: async (items: Array<{ item: ConfiguracionDto; valor: string }>) => Promise.all(
      items.map(({ item, valor }) => upsertConfiguracion({ clave: item.clave, valor, descripcion: item.descripcion })),
    ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['branding'] });
      qc.invalidateQueries({ queryKey: ['admin-configuracion'] });
      toast.success('Diseño de acceso guardado correctamente.');
      setFormError(null);
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo guardar el diseño de acceso.')),
  });

  useEffect(() => {
    return () => {
      if (selectedLogoPreview) {
        URL.revokeObjectURL(selectedLogoPreview);
      }
    };
  }, [selectedLogoPreview]);

  useEffect(() => {
    return () => {
      if (selectedLoginBackgroundPreview) {
        URL.revokeObjectURL(selectedLoginBackgroundPreview);
      }
    };
  }, [selectedLoginBackgroundPreview]);

  useEffect(() => {
    setLoginBackgroundModeDraft(branding.loginBackgroundMode);
    setLoginBackgroundPresetKeyDraft(branding.loginBackgroundPresetKey);
  }, [branding.loginBackgroundMode, branding.loginBackgroundPresetKey]);

  useEffect(() => {
    setLoginTemplateKeyDraft(normalizeLoginTemplateKey(branding.loginTemplateKey));
    setLoginSurfaceToneDraft(normalizeLoginSurfaceTone(branding.loginSurfaceTone));
  }, [branding.loginTemplateKey, branding.loginSurfaceTone]);

  useEffect(() => {
    setLoginWelcomeTitleDraft(branding.loginWelcomeTitle ?? '');
    setLoginWelcomeSubtitleDraft(branding.loginWelcomeSubtitle ?? '');
    setLoginWelcomeHelpTextDraft(branding.loginWelcomeHelpText ?? '');
    setLoginBrandTaglineDraft(branding.loginBrandTagline ?? '');
    setLoginBrandFooterNoteDraft(branding.loginBrandFooterNote ?? '');
  }, [branding.loginWelcomeTitle, branding.loginWelcomeSubtitle, branding.loginWelcomeHelpText, branding.loginBrandTagline, branding.loginBrandFooterNote]);

  function openCreate() {
    if (!canEditConfiguracion) return;
    setEditing(null);
    setFormData({ clave: '', valor: '', descripcion: '' });
    setFormError(null);
    setModal('crear');
  }

  function startEdit(item: ConfiguracionDto) {
    if (!canEditConfiguracion) return;
    setEditing(item);
    setFormData({
      clave: item.clave,
      valor: item.valor,
      descripcion: item.descripcion ?? '',
    });
    setFormError(null);
    setModal('editar');
  }

  function closeModal() {
    setEditing(null);
    setModal(null);
    setFormError(null);
  }

  function saveEdit() {
    if (!canEditConfiguracion) return;
    if (!formData.clave.trim()) {
      setFormError('La clave es obligatoria.');
      return;
    }

    if (!formData.valor.trim()) {
      setFormError('El valor es obligatorio.');
      return;
    }

    setFormError(null);
    const item: ConfiguracionDto = {
      id: editing?.id ?? '00000000-0000-0000-0000-000000000000',
      clave: formData.clave.trim(),
      valor: formData.valor.trim(),
      descripcion: formData.descripcion.trim(),
      actualizadoEn: editing?.actualizadoEn ?? new Date().toISOString(),
    };
    upsertMut.mutate([{ item, valor: item.valor }]);
  }

  useEffect(() => {
    if (!data) return;

    const nextDrafts: Record<string, string> = {};
    for (const item of data.filter(isSecurityConfiguration)) {
      nextDrafts[item.clave] = item.valor;
    }

    setSecurityDrafts(nextDrafts);
    setSecurityNotice(null);
  }, [data]);

  // ── Group data ──────────────────────────────────────────────────────────

  const securityEntries: ConfiguracionDto[] = [];
  const generalEntries: ConfiguracionDto[] = [];

  if (data) {
    for (const item of data) {
      if (isSecurityConfiguration(item)) {
        securityEntries.push(item);
      } else {
        generalEntries.push(item);
      }
    }
  }

  // Sort security entries by metadata displayOrder (known keys first), then by clave
  securityEntries.sort((a, b) => {
    const metaA = getSecurityMeta(a.clave);
    const metaB = getSecurityMeta(b.clave);
    const orderA = metaA?.displayOrder ?? 999;
    const orderB = metaB?.displayOrder ?? 999;
    if (orderA !== orderB) return orderA - orderB;
    return a.clave.localeCompare(b.clave);
  });

  const securityErrors = useMemo(() => {
    const next: Record<string, string | null> = {};
    for (const item of securityEntries) {
      const draft = securityDrafts[item.clave] ?? item.valor;
      next[item.clave] = validateSecurityDraft(item, draft);
    }
    return next;
  }, [securityDrafts, securityEntries]);

  const hasSecurityChanges = securityEntries.some((item) => (securityDrafts[item.clave] ?? item.valor) !== item.valor);
  function saveAllSecurityChanges() {
    if (!canEditConfiguracion) return;
    if (securityEntries.length === 0) return;

    const invalidLabels = securityEntries
      .filter((item) => securityErrors[item.clave])
      .map((item) => getSecurityMeta(item.clave)?.label ?? item.clave);

    if (invalidLabels.length > 0) {
      setSecurityNotice(`Corrige estos campos antes de guardar: ${invalidLabels.join(', ')}.`);
      return;
    }

    const changes = securityEntries
      .filter((item) => (securityDrafts[item.clave] ?? item.valor) !== item.valor)
      .map((item) => ({ item, valor: securityDrafts[item.clave] ?? item.valor }));

    if (changes.length === 0) {
      setSecurityNotice('No hay cambios para guardar.');
      return;
    }

    setFormError(null);
    setSecurityNotice(null);
    upsertMut.mutate(changes);
  }

  function handleLogoSelection(file: File | null) {
    if (selectedLogoPreview) {
      URL.revokeObjectURL(selectedLogoPreview);
      setSelectedLogoPreview(null);
    }

    setSelectedLogoFile(file);

    if (!file) return;

    const previewUrl = URL.createObjectURL(file);
    setSelectedLogoPreview(previewUrl);
    setFormError(null);
  }

  function handleLoginBackgroundSelection(file: File | null) {
    if (selectedLoginBackgroundPreview) {
      URL.revokeObjectURL(selectedLoginBackgroundPreview);
      setSelectedLoginBackgroundPreview(null);
    }

    setSelectedLoginBackgroundFile(file);

    if (!file) return;

    const previewUrl = URL.createObjectURL(file);
    setSelectedLoginBackgroundPreview(previewUrl);
    setFormError(null);
  }

  function handleLogoUpload() {
    if (!selectedLogoFile) {
      setFormError('Selecciona un archivo de logo antes de subirlo.');
      return;
    }

    setFormError(null);
    uploadLogoMut.mutate(selectedLogoFile);
  }

  function handleLoginBackgroundUpload() {
    if (!selectedLoginBackgroundFile) {
      setFormError('Selecciona un archivo de fondo de acceso antes de subirlo.');
      return;
    }

    setFormError(null);
    uploadLoginBackgroundMut.mutate(selectedLoginBackgroundFile);
  }

  function handleLoginDesignSave() {
    if (!canEditConfiguracion) return;

    setFormError(null);
    saveLoginDesignMut.mutate([
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_TEMPLATE_KEY,
          valor: loginTemplateKeyDraft,
          descripcion: 'Plantilla de la pantalla de acceso',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginTemplateKeyDraft,
      },
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_SURFACE_TONE_KEY,
          valor: loginSurfaceToneDraft,
          descripcion: 'Tono de la superficie de acceso',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginSurfaceToneDraft,
      },
    ]);
  }

  function handleLoginBackgroundModeChange(nextMode: LoginBackgroundMode) {
    if (!canEditConfiguracion) return;

    setLoginBackgroundModeDraft(nextMode);
    setLoginBackgroundPresetKeyDraft((current) => {
      if (nextMode === 'color' && getLoginBackgroundPreset(current).mode === 'color') {
        return current;
      }

      if (nextMode === 'gradient' && getLoginBackgroundPreset(current).mode === 'gradient') {
        return current;
      }

      return getDefaultPresetKeyForMode(nextMode);
    });

    if (nextMode !== 'image' && selectedLoginBackgroundFile) {
      handleLoginBackgroundSelection(null);
    }

    setFormError(null);
  }

  function handleLoginBackgroundPresetSelect(presetKey: LoginBackgroundPresetKey) {
    if (!canEditConfiguracion) return;

    setLoginBackgroundPresetKeyDraft(presetKey);
    setFormError(null);
  }

  function handleLoginBackgroundSave() {
    if (!canEditConfiguracion) return;

    if (loginBackgroundModeDraft === 'image') {
      setFormError(null);
      upsertMut.mutate([
        {
          item: {
            id: '00000000-0000-0000-0000-000000000000',
            clave: BRANDING_LOGIN_BACKGROUND_MODE_KEY,
            valor: 'image',
            descripcion: 'Modo del fondo de acceso',
            actualizadoEn: new Date().toISOString(),
          },
          valor: 'image',
        },
      ]);
      return;
    }

    const preset = getLoginBackgroundPreset(loginBackgroundPresetKeyDraft);
    if (preset.mode !== loginBackgroundModeDraft) {
      setLoginBackgroundPresetKeyDraft(getDefaultPresetKeyForMode(loginBackgroundModeDraft));
      setFormError('El preset elegido no corresponde al modo seleccionado.');
      return;
    }

    setFormError(null);
    upsertMut.mutate([
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_BACKGROUND_MODE_KEY,
          valor: loginBackgroundModeDraft,
          descripcion: 'Modo del fondo de acceso',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginBackgroundModeDraft,
      },
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_BACKGROUND_PRESET_KEY,
          valor: loginBackgroundPresetKeyDraft,
          descripcion: 'Preset del fondo de acceso',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginBackgroundPresetKeyDraft,
      },
    ]);
  }

  function handleLoginWelcomeSave() {
    if (!canEditConfiguracion) return;

    setFormError(null);
    upsertMut.mutate([
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_WELCOME_TITLE_KEY,
          valor: loginWelcomeTitleDraft.trim(),
          descripcion: 'Título del mensaje de bienvenida',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginWelcomeTitleDraft.trim(),
      },
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_WELCOME_SUBTITLE_KEY,
          valor: loginWelcomeSubtitleDraft.trim(),
          descripcion: 'Subtítulo del mensaje de bienvenida',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginWelcomeSubtitleDraft.trim(),
      },
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_WELCOME_HELP_TEXT_KEY,
          valor: loginWelcomeHelpTextDraft.trim(),
          descripcion: 'Texto de ayuda del mensaje de bienvenida',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginWelcomeHelpTextDraft.trim(),
      },
    ]);
  }

  function handleLoginBrandTextsSave() {
    if (!canEditConfiguracion) return;

    setFormError(null);
    upsertMut.mutate([
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_BRAND_TAGLINE_KEY,
          valor: loginBrandTaglineDraft.trim(),
          descripcion: 'Texto sobre el nombre en la plantilla de marca dividida',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginBrandTaglineDraft.trim(),
      },
      {
        item: {
          id: '00000000-0000-0000-0000-000000000000',
          clave: BRANDING_LOGIN_BRAND_FOOTER_NOTE_KEY,
          valor: loginBrandFooterNoteDraft.trim(),
          descripcion: 'Nota al pie en la plantilla de marca dividida',
          actualizadoEn: new Date().toISOString(),
        },
        valor: loginBrandFooterNoteDraft.trim(),
      },
    ]);
  }

  const currentLogoSrc = selectedLogoPreview ?? branding.logoUrl;
  const currentLoginBackgroundSrc = selectedLoginBackgroundPreview ?? branding.loginBackgroundUrl;
  const currentLoginBackgroundPreset = getLoginBackgroundPreset(loginBackgroundPresetKeyDraft);
  const currentLoginBackgroundStyle = currentLoginBackgroundPreset.previewStyle;
  const hasLoginDesignChanges = loginTemplateKeyDraft !== branding.loginTemplateKey || loginSurfaceToneDraft !== branding.loginSurfaceTone;
  const hasLoginWelcomeChanges = [
    loginWelcomeTitleDraft.trim() !== (branding.loginWelcomeTitle ?? ''),
    loginWelcomeSubtitleDraft.trim() !== (branding.loginWelcomeSubtitle ?? ''),
    loginWelcomeHelpTextDraft.trim() !== (branding.loginWelcomeHelpText ?? ''),
  ].some(Boolean);
  const hasLoginBrandTextChanges = [
    loginBrandTaglineDraft.trim() !== (branding.loginBrandTagline ?? ''),
    loginBrandFooterNoteDraft.trim() !== (branding.loginBrandFooterNote ?? ''),
  ].some(Boolean);
  const generalConfigEntries = useMemo(
    () => generalEntries.filter((item) => ![
      BRANDING_LOGO_KEY,
      BRANDING_LOGIN_BACKGROUND_KEY,
      BRANDING_LOGIN_BACKGROUND_MODE_KEY,
      BRANDING_LOGIN_BACKGROUND_PRESET_KEY,
      BRANDING_LOGIN_TEMPLATE_KEY,
      BRANDING_LOGIN_SURFACE_TONE_KEY,
      BRANDING_LOGIN_WELCOME_TITLE_KEY,
      BRANDING_LOGIN_WELCOME_SUBTITLE_KEY,
      BRANDING_LOGIN_WELCOME_HELP_TEXT_KEY,
      BRANDING_LOGIN_BRAND_TAGLINE_KEY,
      BRANDING_LOGIN_BRAND_FOOTER_NOTE_KEY,
      'PlantillaNumeracionActiva', // se gestiona con el desplegable dedicado, no como card editable
    ].includes(item.clave)),
    [generalEntries],
  );

  const generalTabContent = (
    <section>
      <p className="mb-4 text-sm text-gray-500">
        Parámetros generales del sistema. Edita las tarjetas para mantener una lectura más clara.
      </p>

      <div className="mb-6 rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
        <h3 className="text-sm font-semibold text-gray-800">Plantilla de numeración del sistema</h3>
        <p className="mt-1 text-xs text-gray-500">
          Define qué plantilla usa el sistema para numerar documentos. Solo una puede estar activa a la vez.
        </p>
        <div className="mt-3 flex flex-col gap-2 sm:flex-row sm:items-center">
          <select
            value={plantillaActivaId ?? ''}
            disabled={!canEditConfiguracion || setPlantillaActivaMut.isPending || plantillasNum.length === 0}
            onChange={(e) => { const id = Number(e.target.value); if (!Number.isNaN(id)) setPlantillaActivaMut.mutate(id); }}
            className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none sm:max-w-md"
          >
            {plantillaActivaId == null && <option value="">— Seleccionar plantilla —</option>}
            {plantillasNum.map((p) => (
              <option key={p.id} value={p.id}>
                {p.descripcion} {p.patron ? `(${p.patron})` : ''}
              </option>
            ))}
          </select>
        </div>
      </div>

      {generalConfigEntries.length > 0 ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {generalConfigEntries.map((item) => {
            const rangeLabel = item.minValue != null && item.maxValue != null
              ? getRangeLabel(item.minValue, item.maxValue)
              : undefined;

            return (
              <ConfigCard
                key={item.id}
                title={item.clave}
                description={item.descripcion}
                value={item.valor}
                rangeLabel={rangeLabel}
                updatedAt={item.actualizadoEn}
                canEdit={canEditConfiguracion}
                onEdit={() => startEdit(item)}
              />
            );
          })}
        </div>
      ) : (
        <p className="py-8 text-center text-sm text-gray-500">No hay configuraciones generales registradas todavía.</p>
      )}
    </section>
  );

  const accesoTabContent = (
    <section className="space-y-6">
      <p className="text-sm text-gray-500">
        Define la plantilla, el tono y el fondo de la pantalla de acceso.
      </p>

      {formError && (
        <div role="alert" className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
          {formError}
        </div>
      )}

      <div className="rounded-lg border border-gray-200 bg-white px-4 py-4 shadow-sm">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0 flex-1 space-y-5">
            <div>
              <p className="text-sm font-medium text-gray-800">Diseño de acceso</p>
              <p className="mt-0.5 text-xs leading-relaxed text-gray-500">
                Define la plantilla y el tono de la pantalla de acceso. El panel de vista previa combina el fondo, el logo y el mensaje de bienvenida actuales.
              </p>
            </div>

            <div className="space-y-3">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-gray-500">Plantilla</p>
              <div className="grid gap-3 md:grid-cols-2">
                {LOGIN_TEMPLATE_OPTIONS.map((option) => (
                  <DesignChoiceCard
                    key={option.key}
                    title={option.label}
                    description={option.description}
                    selected={loginTemplateKeyDraft === option.key}
                    disabled={!canEditConfiguracion || saveLoginDesignMut.isPending}
                    onClick={() => {
                      if (!canEditConfiguracion) return;
                      setLoginTemplateKeyDraft(option.key);
                      setFormError(null);
                    }}
                    preview={option.key === 'centered-brand' ? (
                      <div className="space-y-3">
                        <div className="h-24 rounded-2xl border border-dashed border-gray-200 bg-gradient-to-br from-slate-50 to-slate-100 p-3">
                          <div className="mx-auto flex h-full max-w-[10rem] flex-col items-center justify-center gap-2 rounded-2xl border border-gray-200 bg-white px-3 text-center shadow-sm">
                            <div className="h-8 w-8 rounded-lg bg-primary-600/10" />
                            <div className="h-2 w-20 rounded-full bg-slate-200" />
                            <div className="h-2 w-28 rounded-full bg-slate-200/80" />
                          </div>
                        </div>
                      </div>
                    ) : (
                      <div className="grid grid-cols-[1.2fr_0.8fr] gap-2">
                        <div className="h-24 rounded-2xl border border-gray-200 bg-slate-950 p-3 text-white shadow-sm">
                          <div className="space-y-2 rounded-2xl bg-white/5 p-3 backdrop-blur-sm">
                            <div className="h-3 w-16 rounded-full bg-white/25" />
                            <div className="h-2 w-24 rounded-full bg-white/20" />
                            <div className="h-2 w-20 rounded-full bg-white/10" />
                          </div>
                        </div>
                        <div className="h-24 rounded-2xl border border-dashed border-gray-200 bg-gray-50 p-3 shadow-sm">
                          <div className="space-y-2 rounded-2xl border border-gray-200 bg-white p-3 shadow-sm">
                            <div className="h-2 w-20 rounded-full bg-slate-200" />
                            <div className="h-2 w-14 rounded-full bg-slate-200/80" />
                            <div className="mt-4 h-8 rounded-xl bg-slate-900" />
                          </div>
                        </div>
                      </div>
                    )}
                  />
                ))}
              </div>
            </div>

            <div className="space-y-3">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-gray-500">Tono</p>
              <div className="grid gap-3 md:grid-cols-2">
                {LOGIN_SURFACE_TONE_OPTIONS.map((option) => (
                  <DesignChoiceCard
                    key={option.key}
                    title={option.label}
                    description={option.description}
                    selected={loginSurfaceToneDraft === option.key}
                    disabled={!canEditConfiguracion || saveLoginDesignMut.isPending}
                    onClick={() => {
                      if (!canEditConfiguracion) return;
                      setLoginSurfaceToneDraft(option.key);
                      setFormError(null);
                    }}
                    preview={option.key === 'light' ? (
                      <div className="rounded-2xl border border-slate-200 bg-white p-3 shadow-sm">
                        <div className="space-y-2 rounded-xl border border-slate-200 bg-slate-50 p-3">
                          <div className="h-2 w-16 rounded-full bg-slate-200" />
                          <div className="h-2 w-24 rounded-full bg-slate-200/80" />
                          <div className="h-8 rounded-lg bg-white" />
                        </div>
                      </div>
                    ) : (
                      <div className="rounded-2xl border border-slate-800 bg-slate-950 p-3 shadow-sm">
                        <div className="space-y-2 rounded-xl border border-white/10 bg-white/5 p-3 text-white">
                          <div className="h-2 w-16 rounded-full bg-white/25" />
                          <div className="h-2 w-24 rounded-full bg-white/15" />
                          <div className="h-8 rounded-lg bg-white/10" />
                        </div>
                      </div>
                    )}
                  />
                ))}
              </div>
            </div>

            <div className="flex flex-wrap gap-3">
              <Button
                onClick={handleLoginDesignSave}
                loading={saveLoginDesignMut.isPending}
                disabled={!canEditConfiguracion || !hasLoginDesignChanges}
              >
                Guardar diseño de acceso
              </Button>
              <Button variant="secondary" onClick={() => setLoginPreviewOpen(true)}>
                Ver vista previa
              </Button>
            </div>
          </div>

          <ModalDialog
            open={loginPreviewOpen}
            onClose={() => setLoginPreviewOpen(false)}
            title="Vista previa del acceso"
            size="xl"
          >
            <LoginDesignPreview
              branding={branding}
              templateKey={loginTemplateKeyDraft}
              surfaceTone={loginSurfaceToneDraft}
              backgroundMode={loginBackgroundModeDraft}
              backgroundPresetKey={loginBackgroundPresetKeyDraft}
              backgroundSrc={currentLoginBackgroundSrc}
              backgroundPresetStyle={currentLoginBackgroundStyle}
              welcomeTitle={branding.loginWelcomeTitle}
              welcomeSubtitle={branding.loginWelcomeSubtitle}
              welcomeHelpText={branding.loginWelcomeHelpText}
              brandTagline={branding.loginBrandTagline}
              brandFooterNote={branding.loginBrandFooterNote}
            />
          </ModalDialog>
        </div>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white px-4 py-4 shadow-sm">
        <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0 flex-1 space-y-4">
            <div>
              <p className="text-sm font-medium text-gray-800">Fondo de acceso</p>
              <p className="mt-0.5 text-xs leading-relaxed text-gray-500">
                Elige un modo y una paleta. En imagen, el preset queda de base y la imagen se superpone cuando carga.
              </p>
            </div>

            <div className="grid gap-3 sm:grid-cols-3">
              {([
                { mode: 'image', label: 'Imagen', hint: 'Sube un archivo y conserva un preset base.' },
                { mode: 'color', label: 'Color', hint: 'Un solo tono para una lectura sobria.' },
                { mode: 'gradient', label: 'Gradiente', hint: 'Más profundidad visual sin perder sobriedad.' },
              ] as const).map((option) => (
                <button
                  key={option.mode}
                  type="button"
                  aria-pressed={loginBackgroundModeDraft === option.mode}
                  onClick={() => handleLoginBackgroundModeChange(option.mode)}
                  className={[
                    'rounded-2xl border px-4 py-4 text-left transition-all',
                    loginBackgroundModeDraft === option.mode
                      ? 'border-primary-300 bg-primary-50 shadow-sm ring-2 ring-primary-200'
                      : 'border-gray-200 bg-white hover:border-primary-200 hover:bg-gray-50',
                  ].join(' ')}
                >
                  <span className="block text-sm font-semibold text-gray-800">Modo {option.label.toLowerCase()}</span>
                  <span className="mt-1 block text-xs leading-relaxed text-gray-500">{option.hint}</span>
                </button>
              ))}
            </div>

            {loginBackgroundModeDraft === 'image' ? (
              <div className="rounded-2xl border border-dashed border-gray-200 bg-gray-50/80 p-4">
                <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-gray-800">Imagen de fondo</p>
                    <p className="mt-1 text-xs text-gray-500">
                      Sube una imagen para la pantalla de acceso. {BRANDING_IMAGE_HELP_TEXT} El preset base seguirá visible si el archivo falla.
                    </p>
                  </div>

                  <input
                    type="file"
                    accept={BRANDING_IMAGE_ACCEPT}
                    aria-label="Archivo de fondo de acceso"
                    onChange={(e) => handleLoginBackgroundSelection(e.target.files?.[0] ?? null)}
                    className="block w-full text-sm text-gray-600 file:mr-3 file:rounded-md file:border-0 file:bg-primary-600 file:px-3 file:py-2 file:text-sm file:font-medium file:text-white hover:file:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50 sm:max-w-xs"
                    disabled={!canEditConfiguracion || uploadLoginBackgroundMut.isPending}
                  />
                </div>

                <div className="mt-4 flex items-center gap-4">
                  <div
                    className="flex h-16 w-24 shrink-0 items-center justify-center overflow-hidden rounded-2xl border border-gray-200 shadow-sm"
                    style={currentLoginBackgroundStyle}
                    data-testid="login-background-current-preview"
                  >
                    {loginBackgroundModeDraft === 'image' && currentLoginBackgroundSrc ? (
                      <img
                        src={currentLoginBackgroundSrc}
                        alt="Vista previa del fondo de acceso"
                        className="h-full w-full object-cover"
                      />
                    ) : (
                      <span className="text-[0.65rem] font-semibold tracking-[0.24em] text-white/80">
                        {currentLoginBackgroundPreset.label}
                      </span>
                    )}
                  </div>
                  <div className="min-w-0">
                    <p className="truncate text-sm font-semibold text-gray-800">
                      {selectedLoginBackgroundFile
                        ? selectedLoginBackgroundFile.name
                        : branding.loginBackgroundUrl
                          ? 'Fondo cargado actualmente'
                          : 'Sin imagen cargada'}
                    </p>
                    <p className="truncate text-xs text-gray-500">Base visual: {currentLoginBackgroundPreset.label}</p>
                  </div>
                </div>
              </div>
            ) : (
              <div className="space-y-3">
                <p className="text-xs font-semibold uppercase tracking-[0.24em] text-gray-500">Paleta</p>
                <div className="grid gap-3 sm:grid-cols-2">
                  {getLoginBackgroundPresetOptions(loginBackgroundModeDraft).map((preset) => (
                    <button
                      key={preset.key}
                      type="button"
                      aria-pressed={loginBackgroundPresetKeyDraft === preset.key}
                      onClick={() => handleLoginBackgroundPresetSelect(preset.key)}
                      className={[
                        'overflow-hidden rounded-2xl border text-left transition-all',
                        loginBackgroundPresetKeyDraft === preset.key
                          ? 'border-primary-300 ring-2 ring-primary-200'
                          : 'border-gray-200 hover:border-primary-200',
                      ].join(' ')}
                    >
                      <div className="h-24" style={preset.previewStyle} />
                      <div className="space-y-1 p-3">
                        <p className="text-sm font-semibold text-gray-800">{preset.label}</p>
                        <p className="text-xs leading-relaxed text-gray-500">{preset.description}</p>
                      </div>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {formError && (
              <div role="alert" className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                {formError}
              </div>
            )}

            <div className="flex flex-wrap gap-3">
              <Button onClick={handleLoginBackgroundSave} loading={upsertMut.isPending} disabled={!canEditConfiguracion}>
                Guardar fondo de acceso
              </Button>

              {loginBackgroundModeDraft === 'image' && (
                <Button
                  variant="secondary"
                  onClick={handleLoginBackgroundUpload}
                  loading={uploadLoginBackgroundMut.isPending}
                  disabled={!canEditConfiguracion || !selectedLoginBackgroundFile}
                >
                  Subir fondo
                </Button>
              )}
            </div>
          </div>

          <div className="min-w-[240px] rounded-3xl border border-gray-200 bg-slate-950 p-3 shadow-sm">
            <div className="relative h-56 overflow-hidden rounded-2xl" style={currentLoginBackgroundStyle}>
              {loginBackgroundModeDraft === 'image' && currentLoginBackgroundSrc && (
                <img
                  src={currentLoginBackgroundSrc}
                  alt="Vista previa del fondo de acceso"
                  className="absolute inset-0 h-full w-full object-cover"
                />
              )}
              <div className="absolute inset-0 bg-slate-950/25" />
              <div className="absolute left-4 top-4 rounded-full border border-white/20 bg-white/10 px-3 py-1 text-[0.65rem] font-semibold uppercase tracking-[0.28em] text-white/80">
                {loginBackgroundModeDraft}
              </div>
              <div className="absolute bottom-4 left-4 right-4 rounded-2xl border border-white/15 bg-slate-950/35 p-3 backdrop-blur-sm">
                <p className="text-sm font-semibold text-white">{currentLoginBackgroundPreset.label}</p>
                <p className="mt-1 text-xs leading-relaxed text-white/70">
                  {loginBackgroundModeDraft === 'image'
                    ? 'La imagen siempre se monta sobre la paleta base para que el acceso siga siendo legible.'
                    : currentLoginBackgroundPreset.description}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );

  const logoTabContent = (
    <section className="space-y-6">
      <p className="text-sm text-gray-500">
        Sube y revisa el logo institucional que se usará en la cabecera y en la pantalla de acceso.
      </p>

      <div className="rounded-lg border border-gray-200 bg-white px-4 py-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0 flex-1">
            <p className="text-sm font-medium text-gray-800">Logo institucional</p>
            <p className="mt-0.5 text-xs leading-relaxed text-gray-500">
              {BRANDING_IMAGE_HELP_TEXT} Este logo se usará en la cabecera y en la pantalla de acceso.
            </p>

            {formError && (
              <div className="mt-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                {formError}
              </div>
            )}

            <div className="mt-4 flex items-center gap-4">
              <div className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-2xl border border-gray-200 bg-gray-50 shadow-sm">
                {currentLogoSrc ? (
                  <img
                    src={currentLogoSrc}
                    alt="Vista previa del logo institucional"
                    className="h-full w-full object-contain p-1.5"
                  />
                ) : (
                  <span className="text-[0.7rem] font-semibold tracking-[0.24em] text-primary-700">DF</span>
                )}
              </div>

              <div className="min-w-0">
                <p className="truncate text-sm font-semibold text-gray-800">{branding.nombreInstitucion || DEFAULT_BRANDING_NAME}</p>
                <p className="text-xs text-gray-500">
                  {selectedLogoFile ? selectedLogoFile.name : branding.logoUrl ? 'Logo cargado actualmente' : 'Sin logo cargado'}
                </p>
              </div>
            </div>
          </div>

          <div className="flex min-w-[240px] flex-col gap-3 lg:items-end">
            <input
              type="file"
              accept={BRANDING_IMAGE_ACCEPT}
              aria-label="Archivo de logo institucional"
              onChange={(e) => handleLogoSelection(e.target.files?.[0] ?? null)}
              className="block w-full text-sm text-gray-600 file:mr-3 file:rounded-md file:border-0 file:bg-primary-600 file:px-3 file:py-2 file:text-sm file:font-medium file:text-white hover:file:bg-primary-700 disabled:cursor-not-allowed disabled:opacity-50"
              disabled={!canEditConfiguracion || uploadLogoMut.isPending}
            />

            <Button
              onClick={handleLogoUpload}
              loading={uploadLogoMut.isPending}
              disabled={!canEditConfiguracion || !selectedLogoFile}
            >
              Subir logo
            </Button>
          </div>
        </div>
      </div>
    </section>
  );

  const bienvenidaTabContent = (
    <section className="space-y-6">
      <p className="text-sm text-gray-500">
        Configura el mensaje de bienvenida que acompaña a la pantalla de acceso.
      </p>

      <div className="rounded-lg border border-gray-200 bg-white px-4 py-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0 flex-1">
            <p className="text-sm font-medium text-gray-800">Mensaje de bienvenida</p>
            <p className="mt-0.5 text-xs leading-relaxed text-gray-500">
              Texto breve y sobrio para mostrar en la pantalla de acceso.
            </p>

            <div className="mt-4 grid gap-4">
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-700" htmlFor="login-welcome-title">
                  Título
                </label>
                <input
                  id="login-welcome-title"
                  type="text"
                  value={loginWelcomeTitleDraft}
                  onChange={(e) => setLoginWelcomeTitleDraft(e.target.value)}
                  maxLength={60}
                  disabled={!canEditConfiguracion || upsertMut.isPending}
                  className="block w-full rounded border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:cursor-not-allowed disabled:opacity-50"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-medium text-gray-700" htmlFor="login-welcome-subtitle">
                  Subtítulo
                </label>
                <input
                  id="login-welcome-subtitle"
                  type="text"
                  value={loginWelcomeSubtitleDraft}
                  onChange={(e) => setLoginWelcomeSubtitleDraft(e.target.value)}
                  maxLength={100}
                  disabled={!canEditConfiguracion || upsertMut.isPending}
                  className="block w-full rounded border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:cursor-not-allowed disabled:opacity-50"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-medium text-gray-700" htmlFor="login-welcome-help-text">
                  Ayuda (opcional)
                </label>
                <textarea
                  id="login-welcome-help-text"
                  value={loginWelcomeHelpTextDraft}
                  onChange={(e) => setLoginWelcomeHelpTextDraft(e.target.value)}
                  maxLength={120}
                  rows={3}
                  disabled={!canEditConfiguracion || upsertMut.isPending}
                  className="block w-full rounded border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:cursor-not-allowed disabled:opacity-50"
                />
              </div>
            </div>
          </div>

          <div className="flex min-w-[240px] flex-col gap-3 lg:items-end">
            <Button
              onClick={handleLoginWelcomeSave}
              loading={upsertMut.isPending}
              disabled={!canEditConfiguracion || !hasLoginWelcomeChanges}
            >
              Guardar mensaje de bienvenida
            </Button>
          </div>
        </div>
      </div>

      <div className="rounded-lg border border-gray-200 bg-white px-4 py-4 shadow-sm">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div className="min-w-0 flex-1">
            <p className="text-sm font-medium text-gray-800">Textos del panel de marca dividida</p>
            <p className="mt-0.5 text-xs leading-relaxed text-gray-500">
              Solo se muestran con la plantilla "Marca dividida". Si los dejas vacíos, se usan los textos por defecto.
            </p>

            <div className="mt-4 grid gap-4">
              <div>
                <label className="mb-1 block text-xs font-medium text-gray-700" htmlFor="login-brand-tagline">
                  Texto sobre el nombre
                </label>
                <input
                  id="login-brand-tagline"
                  type="text"
                  value={loginBrandTaglineDraft}
                  onChange={(e) => setLoginBrandTaglineDraft(e.target.value)}
                  maxLength={40}
                  placeholder={DEFAULT_LOGIN_BRAND_TAGLINE}
                  disabled={!canEditConfiguracion || upsertMut.isPending}
                  className="block w-full rounded border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:cursor-not-allowed disabled:opacity-50"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs font-medium text-gray-700" htmlFor="login-brand-footer-note">
                  Nota al pie
                </label>
                <input
                  id="login-brand-footer-note"
                  type="text"
                  value={loginBrandFooterNoteDraft}
                  onChange={(e) => setLoginBrandFooterNoteDraft(e.target.value)}
                  maxLength={80}
                  placeholder={DEFAULT_LOGIN_BRAND_FOOTER_NOTE}
                  disabled={!canEditConfiguracion || upsertMut.isPending}
                  className="block w-full rounded border border-gray-300 px-3 py-2 text-sm text-gray-900 focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:cursor-not-allowed disabled:opacity-50"
                />
              </div>
            </div>
          </div>

          <div className="flex min-w-[240px] flex-col gap-3 lg:items-end">
            <Button
              onClick={handleLoginBrandTextsSave}
              loading={upsertMut.isPending}
              disabled={!canEditConfiguracion || !hasLoginBrandTextChanges}
            >
              Guardar textos del panel
            </Button>
          </div>
        </div>
      </div>
    </section>
  );

  const tabs = [
    {
      id: 'seguridad',
      label: 'Seguridad',
      content: (
        <section>
          <p className="mb-4 text-sm text-gray-500">
            Configura las políticas de seguridad del sistema. Guarda los cambios desde el botón superior.
          </p>

          {securityNotice && (
            <div role="status" className="mb-4 rounded border border-amber-200 bg-amber-50 px-3 py-2 text-sm text-amber-800">
              {securityNotice}
            </div>
          )}

          {formError && (
            <div role="alert" className="mb-4 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
              {formError}
            </div>
          )}

          {securityEntries.length > 0 ? (
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {securityEntries.map((item) => (
                <SecurityCard
                  key={item.id}
                  item={item}
                  draft={securityDrafts[item.clave] ?? item.valor}
                  error={securityErrors[item.clave]}
                  onDraftChange={(changedItem, value) => {
                    setSecurityDrafts((prev) => ({ ...prev, [changedItem.clave]: value }));
                    setSecurityNotice(null);
                  }}
                  saving={upsertMut.isPending}
                  disabled={!canEditConfiguracion}
                />
              ))}
            </div>
          ) : (
            <p className="py-8 text-center text-sm text-gray-500">No hay configuraciones de seguridad registradas todavía.</p>
          )}
        </section>
      ),
    },
    { id: 'general', label: 'General', content: generalTabContent },
    { id: 'acceso', label: 'Acceso', content: accesoTabContent },
    { id: 'logo', label: 'Logo', content: logoTabContent },
    { id: 'bienvenida', label: 'Bienvenida', content: bienvenidaTabContent },
  ];

  useEffect(() => {
    if (activeTab === 'seguridad' && data && securityEntries.length === 0) {
      setActiveTab('general');
    }
  }, [activeTab, data, securityEntries.length]);

  return (
    <div>
      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="mb-6 flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-gray-800">Configuración del Sistema</h2>
        <div className="flex items-center gap-3">
          {canEditConfiguracion && (
            <Button variant="secondary" onClick={openCreate}>
              Nueva configuración
            </Button>
          )}
          {canEditConfiguracion && (
            <Button onClick={saveAllSecurityChanges} loading={upsertMut.isPending} disabled={!hasSecurityChanges}>
              Guardar cambios
            </Button>
          )}
        </div>
      </div>

      {/* ── Loading ─────────────────────────────────────────────────────── */}
      {isLoading && (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      )}

      {/* ── Error ───────────────────────────────────────────────────────── */}
      {isError && (
        <p className="text-red-600 text-sm">No se pudo cargar la configuración.</p>
      )}

      {/* ── Content ─────────────────────────────────────────────────────── */}
      {data && (
        <div className="space-y-8">
          <TabPanel
            tabs={tabs}
            activeTab={activeTab}
            onTabChange={(tabId) => setActiveTab(tabId as AdminConfiguracionTab)}
          />

          {securityEntries.length === 0 && generalEntries.length === 0 && (
            <p className="py-8 text-center text-sm text-gray-500">
              No hay configuraciones registradas todavía.
            </p>
          )}
        </div>
      )}

      {/* ── Modal (general create/edit only) ──────────────────────────────── */}
        <ModalDialog
        open={modal !== null && canEditConfiguracion}
        title={modal === 'crear' ? 'Nueva configuración' : 'Editar configuración'}
        onClose={closeModal}
        footer={(
          <>
            <Button variant="secondary" onClick={closeModal}>Cancelar</Button>
            <Button onClick={saveEdit} loading={upsertMut.isPending}>Guardar</Button>
          </>
        )}
      >
        {formError && (
          <div role="alert" className="mb-4 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
            {formError}
          </div>
        )}

        <div className="space-y-3">
          <div>
            <label htmlFor="config-clave" className="mb-1 block text-xs text-gray-600">Clave</label>
            <input
              id="config-clave"
              type="text"
              value={formData.clave}
              onChange={(e) => setFormData((f) => ({ ...f, clave: e.target.value }))}
              disabled={modal === 'editar'}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm disabled:bg-gray-50 disabled:text-gray-500"
              placeholder="ej: auth.session.timeout"
            />
          </div>

          <div>
            <label htmlFor="config-valor" className="mb-1 block text-xs text-gray-600">Valor</label>
            <input
              id="config-valor"
              type={inferInputType(formData.clave, formData.valor)}
              value={formData.valor}
              onChange={(e) => setFormData((f) => ({ ...f, valor: e.target.value }))}
              className="w-full rounded border border-gray-300 px-3 py-2 text-sm"
              placeholder="Valor de configuración"
            />
          </div>

          <div>
            <label htmlFor="config-descripcion" className="mb-1 block text-xs text-gray-600">Descripción</label>
            <textarea
              id="config-descripcion"
              value={formData.descripcion}
              onChange={(e) => setFormData((f) => ({ ...f, descripcion: e.target.value }))}
              className="min-h-24 w-full rounded border border-gray-300 px-3 py-2 text-sm"
              placeholder="Contexto para el equipo sobre cuándo y por qué se usa esta configuración"
            />
          </div>
        </div>
      </ModalDialog>
    </div>
  );
}
