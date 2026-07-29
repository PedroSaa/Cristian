import { useMemo, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { QRCodeSVG } from 'qrcode.react';
import { useSearchParams } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { changePassword, enableMfa, verifyMfa, disableMfa } from '@/lib/api/auth';
import {
  buildChangePasswordSchema,
  type ChangePasswordFormData,
} from '@/lib/validations/auth';
import { usePasswordPolicy } from '@/hooks/usePasswordPolicy';
import FormField from '@/components/molecules/FormField';
import { Button, Input, Badge, Spinner, Divider } from '@/components/atoms';
import ModalDialog from '@/components/organisms/ModalDialog';
import FirmaUsuarioModal, { type FirmaOperations } from '@/components/organisms/FirmaUsuarioModal';
import { ToastProvider } from '@/contexts/ToastContext';
import {
  getMiFirmaMetadata,
  getMiFirmaImagen,
  guardarMiFirma,
  eliminarMiFirma,
} from '@/lib/api/admin/perfilFirmaApi';

function roleVariant(rol: string): 'default' | 'success' | 'warning' | 'danger' | 'info' | 'neutral' {
  switch (rol) {
    case 'Administrador':
      return 'danger';
    case 'Operador':
      return 'warning';
    default:
      return 'info';
  }
}

export default function ProfilePage() {
  const { state, login: authLogin, logout } = useAuth();
  const user = state.user;
  const [searchParams] = useSearchParams();

  const [passwordMessage, setPasswordMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  // ── Signature (self-service) ─────────────────────────────────────────────
  const [showFirmaModal, setShowFirmaModal] = useState(false);
  // Operations bound to the self-service `/perfil/firma` API, injected into the
  // shared FirmaUsuarioModal (same modal used by admin, different transport).
  const firmaOperations = useMemo<FirmaOperations>(() => ({
    getMetadata: getMiFirmaMetadata,
    getImagen: getMiFirmaImagen,
    guardar: guardarMiFirma,
    eliminar: eliminarMiFirma,
    cacheKey: ['perfil', 'firma'] as const,
  }), []);

  // ── MFA state ──────────────────────────────────────────────────────────────
  const [mfaStatus, setMfaStatus] = useState<'idle' | 'enabling' | 'verify' | 'disabling'>('idle');
  const [qrData, setQrData] = useState<{ provisioningUri: string; secretKey: string } | null>(null);
  const [verifyCodeInput, setVerifyCodeInput] = useState('');
  const [mfaMessage, setMfaMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [disablePasswordInput, setDisablePasswordInput] = useState('');
  const [showDisableModal, setShowDisableModal] = useState(false);

  // ── MFA handlers ──────────────────────────────────────────────────────────────

  const handleEnableMfa = async () => {
    setMfaMessage(null);
    setMfaStatus('enabling');
    try {
      const result = await enableMfa();
      setQrData(result);
      setMfaStatus('verify');
    } catch {
      setMfaMessage({ type: 'error', text: 'Error al iniciar la activación de MFA.' });
      setMfaStatus('idle');
    }
  };

  const handleVerifyMfa = async () => {
    setMfaMessage(null);
    if (verifyCodeInput.length !== 6) {
      setMfaMessage({ type: 'error', text: 'El código debe tener 6 dígitos.' });
      return;
    }
    try {
      const result = await verifyMfa(verifyCodeInput);
      if (result.success) {
        setMfaMessage({ type: 'success', text: 'Autenticación en dos pasos activada correctamente. Cierre sesión y vuelva a ingresar para acceder a Administración.' });
        setMfaStatus('idle');
        setQrData(null);
        setVerifyCodeInput('');
        // Update user context to reflect MFA enabled
        if (user) {
          authLogin({ ...user, mfaEnabled: true });
        }
      } else {
        setMfaMessage({ type: 'error', text: 'Código inválido. Intentá de nuevo.' });
      }
    } catch {
      setMfaMessage({ type: 'error', text: 'Error al verificar el código.' });
    }
  };

  const handleDisableMfa = async () => {
    setMfaMessage(null);
    if (!disablePasswordInput) {
      setMfaMessage({ type: 'error', text: 'Debes ingresar tu contraseña actual.' });
      return;
    }
    try {
      await disableMfa(disablePasswordInput);
      setMfaMessage({ type: 'success', text: 'Autenticación en dos pasos desactivada correctamente.' });
      setShowDisableModal(false);
      setDisablePasswordInput('');
        // Update user context to reflect MFA disabled
        if (user) {
          authLogin({ ...user, mfaEnabled: false });
        }
    } catch {
      setMfaMessage({ type: 'error', text: 'La contraseña actual ingresada no es correcta.' });
    }
  };

  // ── Password form ─────────────────────────────────────────────────────────
  const passwordPolicy = usePasswordPolicy();
  const changeSchema = useMemo(() => buildChangePasswordSchema(passwordPolicy), [passwordPolicy]);
  const passwordForm = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changeSchema),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
  });

  const onPasswordSubmit = async (data: ChangePasswordFormData) => {
    setPasswordMessage(null);
    try {
      await changePassword({ currentPassword: data.currentPassword, newPassword: data.newPassword });
      // Password change invalidates active refresh sessions per backend spec.
      // Log out immediately so the user re-authenticates with the new password.
      logout();
    } catch {
      setPasswordMessage({ type: 'error', text: 'Error al cambiar la contraseña. Verifique que la contraseña actual sea correcta.' });
    }
  };

  if (!user) {
    return (
      <div className="flex items-center justify-center py-12">
        <Spinner size="lg" />
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl space-y-8">
      <h2 className="text-xl font-semibold text-gray-900">Mi Perfil</h2>

      {searchParams.get('mfaRequired') === 'true' && (
        <div className="rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
          {user.mfaEnabled
            ? 'Su cuenta requiere completar MFA en esta sesión para acceder a Administración. Cierre sesión e ingrese nuevamente para completar la verificación.'
            : 'Debes activar MFA desde esta pantalla para acceder a Administración.'}
        </div>
      )}

      {/* ── User Info ──────────────────────────────────────────────────────── */}
      <section className="rounded-lg border border-gray-200 bg-white p-6">
        <h3 className="mb-4 text-sm font-medium text-gray-500 uppercase tracking-wide">Información del Usuario</h3>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <div>
            <p className="text-xs text-gray-500">Nombre</p>
          <p className="text-sm font-medium text-gray-900">{user.nombreCompleto ?? user.nombre}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500">Email</p>
            <p className="text-sm font-medium text-gray-900">{user.email}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500">RUT</p>
            <p className="text-sm font-medium text-gray-900">{user.rut || '—'}</p>
          </div>
          <div>
            <p className="text-xs text-gray-500 mb-1">Rol</p>
            <Badge variant={roleVariant(user.rol)}>{user.rol}</Badge>
          </div>
        </div>
      </section>

      {/* ── MFA Section ───────────────────────────────────────────────────── */}
      <section className="rounded-lg border border-gray-200 bg-white p-6">
        <h3 className="mb-4 text-sm font-medium text-gray-500 uppercase tracking-wide">
          Autenticación de Dos Factores (MFA)
        </h3>

        <div className="flex items-center gap-3 mb-4">
          <span className="text-sm text-gray-700">Estado:</span>
          <Badge variant={user.mfaEnabled ? 'success' : 'neutral'}>
            {user.mfaEnabled ? 'Activado' : 'Desactivado'}
          </Badge>
        </div>

        {mfaMessage && (
          <p
            className={`mb-4 text-sm ${mfaMessage.type === 'success' ? 'text-green-600' : 'text-red-600'}`}
            role="alert"
          >
            {mfaMessage.text}
          </p>
        )}

        {/* Enable flow — initial */}
        {!user.mfaEnabled && mfaStatus === 'idle' && (
          <Button onClick={handleEnableMfa}>Activar MFA</Button>
        )}

        {/* Enable flow — QR + verify */}
        {!user.mfaEnabled && mfaStatus === 'enabling' && (
          <p className="text-sm text-gray-500">Generando código QR…</p>
        )}

        {!user.mfaEnabled && mfaStatus === 'verify' && qrData && (
          <div className="space-y-4">
            <p className="text-sm text-gray-600">
              Escaneá este código QR con tu aplicación de autenticación (Google Authenticator, Authy, etc.):
            </p>
            <div className="flex justify-center">
              <div className="inline-block rounded-lg border border-gray-200 p-3 bg-white">
                <QRCodeSVG value={qrData.provisioningUri} size={180} />
              </div>
            </div>
            <p className="text-xs text-gray-400 break-all select-all font-mono">
              {qrData.secretKey}
            </p>
            <div className="flex items-end gap-2">
              <div className="flex-1">
                <FormField label="Código de 6 dígitos" error={verifyCodeInput.length > 0 && verifyCodeInput.length !== 6 ? 'Debe tener 6 dígitos' : undefined}>
                  <Input
                    type="text"
                    maxLength={6}
                    placeholder="123456"
                    value={verifyCodeInput}
                    onChange={(e) => setVerifyCodeInput(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  />
                </FormField>
              </div>
              <Button onClick={handleVerifyMfa} disabled={verifyCodeInput.length !== 6}>
                Verificar
              </Button>
            </div>
            <Button variant="ghost" size="sm" onClick={() => { setMfaStatus('idle'); setQrData(null); setVerifyCodeInput(''); setMfaMessage(null); }}>
              Cancelar
            </Button>
          </div>
        )}

        {/* Disable flow */}
        {user.mfaEnabled && (
          <Button variant="danger" onClick={() => setShowDisableModal(true)}>
            Desactivar MFA
          </Button>
        )}

        {/* Disable MFA confirmation modal */}
        <ModalDialog
          open={showDisableModal}
          title="Desactivar MFA"
          onClose={() => { setShowDisableModal(false); setDisablePasswordInput(''); setMfaMessage(null); }}
          size="sm"
          footer={
            <>
              <Button variant="ghost" onClick={() => { setShowDisableModal(false); setDisablePasswordInput(''); setMfaMessage(null); }}>
                Cancelar
              </Button>
              <Button variant="danger" onClick={handleDisableMfa} loading={mfaStatus === 'disabling'}>
                Desactivar
              </Button>
            </>
          }
        >
          <p className="text-sm text-gray-600 mb-4">
            Para desactivar la autenticación de dos factores, ingrese su contraseña actual.
          </p>
          <Input
            type="password"
            placeholder="Contraseña actual"
            value={disablePasswordInput}
            onChange={(e) => setDisablePasswordInput(e.target.value)}
            autoFocus
          />
        </ModalDialog>
      </section>

      {/* ── Signature Section ──────────────────────────────────────────────── */}
      <section className="rounded-lg border border-gray-200 bg-white p-6">
        <h3 className="mb-4 text-sm font-medium text-gray-500 uppercase tracking-wide">Firma</h3>
        <p className="mb-4 text-sm text-gray-600">
          Configurá tu firma personal (imagen, clave y sigla) para usarla en los documentos.
        </p>
        <Button onClick={() => setShowFirmaModal(true)}>Configurar firma</Button>
      </section>

      {/* The signature modal surfaces its own toasts; AppLayout has no ToastProvider,
          so we mount a local one to keep save/delete feedback visible in Mi Perfil. */}
      <ToastProvider>
        <FirmaUsuarioModal
          open={showFirmaModal}
          operations={firmaOperations}
          usuarioNombre={user.nombreCompleto ?? user.nombre}
          canEdit
          onClose={() => setShowFirmaModal(false)}
        />
      </ToastProvider>

      <Divider />

      {/* ── Change Password ────────────────────────────────────────────────── */}
      <section className="rounded-lg border border-gray-200 bg-white p-6">
        <h3 className="mb-4 text-sm font-medium text-gray-500 uppercase tracking-wide">Cambiar Contraseña</h3>
        <form onSubmit={passwordForm.handleSubmit(onPasswordSubmit)} className="space-y-4">
          <FormField
            label="Contraseña Actual"
            error={passwordForm.formState.errors.currentPassword?.message}
            required
          >
            <Input
              type="password"
              placeholder="••••••••"
              error={!!passwordForm.formState.errors.currentPassword}
              {...passwordForm.register('currentPassword')}
            />
          </FormField>

          <FormField
            label="Nueva Contraseña"
            error={passwordForm.formState.errors.newPassword?.message}
            required
          >
            <Input
              type="password"
              placeholder="••••••••"
              error={!!passwordForm.formState.errors.newPassword}
              {...passwordForm.register('newPassword')}
            />
          </FormField>

          <FormField
            label="Confirmar Nueva Contraseña"
            error={passwordForm.formState.errors.confirmPassword?.message}
            required
          >
            <Input
              type="password"
              placeholder="••••••••"
              error={!!passwordForm.formState.errors.confirmPassword}
              {...passwordForm.register('confirmPassword')}
            />
          </FormField>

          {passwordMessage && (
            <p
              className={`text-sm ${passwordMessage.type === 'success' ? 'text-green-600' : 'text-red-600'}`}
              role="alert"
            >
              {passwordMessage.text}
            </p>
          )}

          <Button type="submit" loading={passwordForm.formState.isSubmitting}>
            Cambiar Contraseña
          </Button>
        </form>
      </section>
    </div>
  );
}
