import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders, setupAuthInStorage, clearAuthFromStorage, mockUser } from '@/test/utils';
import ProfilePage from './ProfilePage';

// Mock the API module
vi.mock('@/lib/api/auth', () => ({
  getProfile: vi.fn().mockReturnValue(new Promise(() => {})),
  updateProfile: vi.fn(),
  changePassword: vi.fn(),
  logout: vi.fn(),
  enableMfa: vi.fn(),
  verifyMfa: vi.fn(),
  disableMfa: vi.fn(),
}));

vi.mock('@/lib/api/passwordPolicy', () => ({
  getPasswordPolicy: vi.fn().mockResolvedValue({
    minLength: 8,
    requireUppercase: true,
    requireLowercase: true,
    requireDigit: true,
    requireSpecial: true,
  }),
}));

vi.mock('@/lib/api/admin/perfilFirmaApi', () => ({
  getMiFirmaMetadata: vi.fn().mockResolvedValue({
    usuarioId: 'me', tieneFirma: false, tieneClave: false, sigla: null,
    contentType: null, tamano: 0, creadoEn: null, actualizadoEn: null,
  }),
  getMiFirmaImagen: vi.fn().mockResolvedValue(null),
  guardarMiFirma: vi.fn(),
  eliminarMiFirma: vi.fn(),
}));

import { changePassword as mockChangePassword, enableMfa as mockEnableMfa, verifyMfa as mockVerifyMfa, disableMfa as mockDisableMfa } from '@/lib/api/auth';
import { useAuth } from '@/contexts/AuthContext';

beforeEach(() => {
  clearAuthFromStorage();
  vi.clearAllMocks();
});

/**
 * Helper component to read AuthContext state in parallel with ProfilePage.
 */
function AuthStateReader() {
  const { state } = useAuth();
  return (
    <div aria-live="polite" data-testid="auth-state-reader">
      <span data-testid="reader-isAuthenticated">{String(state.isAuthenticated)}</span>
      <span data-testid="reader-user">{state.user?.nombre ?? 'null'}</span>
    </div>
  );
}

/**
 * Render ProfilePage alongside AuthStateReader so we can check auth state
 * after interactions.
 */
function renderProfileWithAuthReader() {
  return renderWithProviders(
    <>
      <ProfilePage />
      <AuthStateReader />
    </>,
  );
}

describe('ProfilePage', () => {
  it('shows loading spinner when user is not available', () => {
    renderWithProviders(<ProfilePage />);

    expect(screen.getByRole('status', { name: /cargando/i })).toBeInTheDocument();
  });

  it('renders user info when authenticated', async () => {
    setupAuthInStorage(mockUser);

    renderWithProviders(<ProfilePage />);

    expect(await screen.findByText('Mi Perfil')).toBeInTheDocument();
    expect(screen.getByText('Juan Pérez')).toBeInTheDocument();
    expect(screen.getByText('jperez@docflow.cl')).toBeInTheDocument();
    expect(screen.getByText('11111111-1')).toBeInTheDocument();
    expect(screen.getByText('Usuario')).toBeInTheDocument();
  });

  it('renders the change password section with form fields', async () => {
    setupAuthInStorage(mockUser);

    renderWithProviders(<ProfilePage />);

    const sectionHeadings = screen.getAllByText('Cambiar Contraseña');
    expect(sectionHeadings.length).toBeGreaterThanOrEqual(1);

    // Labels have a required * indicator via <span>, so they render as "Nueva Contraseña*" etc.
    // "Nueva contraseña" matches both "Nueva Contraseña" and "Confirmar Nueva Contraseña"
    await screen.findByText(/contraseña actual/i);
    const nuevaMatches = screen.getAllByText(/nueva contraseña/i);
    expect(nuevaMatches.length).toBeGreaterThanOrEqual(1);
    expect(screen.getByText(/confirmar nueva contraseña/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /cambiar contraseña/i })).toBeInTheDocument();
  });

  it('changes password on valid submission', async () => {
    const user = userEvent.setup();
    setupAuthInStorage(mockUser);

    vi.mocked(mockChangePassword).mockResolvedValueOnce(undefined);

    renderWithProviders(<ProfilePage />);

    // Find inputs by their placeholder since labels include required *
    const passwordInputs = screen.getAllByPlaceholderText('••••••••');
    await user.type(passwordInputs[0], 'current-pass');
    await user.type(passwordInputs[1], 'NewValid1!');
    await user.type(passwordInputs[2], 'NewValid1!');

    await user.click(screen.getByRole('button', { name: /cambiar contraseña/i }));

    // API call was made
    await waitFor(() => {
      expect(mockChangePassword).toHaveBeenCalledWith({
        currentPassword: 'current-pass',
        newPassword: 'NewValid1!',
      });
    });

    // Session is invalidated → page reverts to loading state
    await waitFor(() => {
      expect(screen.getByRole('status', { name: /cargando/i })).toBeInTheDocument();
    });
  });

  it('shows error message when password change fails', async () => {
    const user = userEvent.setup();
    setupAuthInStorage(mockUser);

    vi.mocked(mockChangePassword).mockRejectedValueOnce(new Error('Wrong password'));

    renderWithProviders(<ProfilePage />);

    const passwordInputs = screen.getAllByPlaceholderText('••••••••');
    await user.type(passwordInputs[0], 'wrong-current');
    await user.type(passwordInputs[1], 'NewValid1!');
    await user.type(passwordInputs[2], 'NewValid1!');

    await user.click(screen.getByRole('button', { name: /cambiar contraseña/i }));

    await waitFor(() => {
      expect(screen.getByText(/Error al cambiar la contraseña/)).toBeInTheDocument();
    });
  });

  // ── 2.3 RED: Password change invalidates session ────────────────────────

  it('logs out and invalidates refresh session after password change', async () => {
    const user = userEvent.setup();
    setupAuthInStorage(mockUser);

    vi.mocked(mockChangePassword).mockResolvedValueOnce(undefined);

    renderProfileWithAuthReader();

    const passwordInputs = screen.getAllByPlaceholderText('••••••••');
    await user.type(passwordInputs[0], 'current-pass');
    await user.type(passwordInputs[1], 'NewValid1!');
    await user.type(passwordInputs[2], 'NewValid1!');

    await user.click(screen.getByRole('button', { name: /cambiar contraseña/i }));

    // API call was made
    await waitFor(() => {
      expect(mockChangePassword).toHaveBeenCalledWith({
        currentPassword: 'current-pass',
        newPassword: 'NewValid1!',
      });
    });

    // Auth session should be cleared (logout) — page now shows loading spinner
    await waitFor(() => {
      expect(screen.getByTestId('reader-isAuthenticated')).toHaveTextContent('false');
      expect(screen.getByTestId('reader-user')).toHaveTextContent('null');
    });

    // localStorage should be cleared
    expect(localStorage.getItem('refreshToken')).toBeNull();
    expect(localStorage.getItem('user')).toBeNull();
  });

  // ── 2.3 RED: Cancelled (error) password change preserves session ────────

  it('preserves auth session when password change fails', async () => {
    const user = userEvent.setup();
    setupAuthInStorage(mockUser);

    vi.mocked(mockChangePassword).mockRejectedValueOnce(new Error('Wrong password'));

    renderProfileWithAuthReader();

    const passwordInputs = screen.getAllByPlaceholderText('••••••••');
    await user.type(passwordInputs[0], 'wrong-current');
    await user.type(passwordInputs[1], 'NewValid1!');
    await user.type(passwordInputs[2], 'NewValid1!');

    await user.click(screen.getByRole('button', { name: /cambiar contraseña/i }));

    await waitFor(() => {
      expect(screen.getByText(/Error al cambiar la contraseña/)).toBeInTheDocument();
    });

    // Auth session should remain intact
    expect(screen.getByTestId('reader-isAuthenticated')).toHaveTextContent('true');
    expect(screen.getByTestId('reader-user')).toHaveTextContent('Juan Pérez');

    // localStorage should still have user
    expect(localStorage.getItem('user')).toContain('Juan Pérez');
  });

  // ── 3.3 MFA enable flow ─────────────────────────────────────────────────

  it('shows MFA section with "Desactivado" badge when MFA is off', async () => {
    setupAuthInStorage(mockUser);

    renderWithProviders(<ProfilePage />);

    expect(await screen.findByText('Autenticación de Dos Factores (MFA)')).toBeInTheDocument();
    expect(screen.getByText('Desactivado')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /activar mfa/i })).toBeInTheDocument();
  });

  it('shows QR after clicking Activar MFA', async () => {
    const user = userEvent.setup();
    setupAuthInStorage(mockUser);

    vi.mocked(mockEnableMfa).mockResolvedValueOnce({
      provisioningUri: 'otpauth://totp/DocFlow:test?secret=TEST',
      secretKey: 'TEST',
    });

    renderWithProviders(<ProfilePage />);

    await user.click(await screen.findByRole('button', { name: /activar mfa/i }));

    await waitFor(() => {
      expect(mockEnableMfa).toHaveBeenCalledTimes(1);
    });

    // QR code should be rendered (QRCodeSVG renders an <svg>)
    expect(document.querySelector('svg')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('123456')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /verificar/i })).toBeInTheDocument();
  });

  it('shows error on invalid verification code', async () => {
    const user = userEvent.setup();
    setupAuthInStorage(mockUser);

    vi.mocked(mockEnableMfa).mockResolvedValueOnce({
      provisioningUri: 'otpauth://totp/DocFlow:test?secret=TEST',
      secretKey: 'TEST',
    });
    vi.mocked(mockVerifyMfa).mockResolvedValueOnce({ success: false, error: 'Código de verificación inválido.' });

    renderWithProviders(<ProfilePage />);

    await user.click(await screen.findByRole('button', { name: /activar mfa/i }));
    await screen.findByPlaceholderText('123456');

    const codeInput = screen.getByPlaceholderText('123456');
    await user.type(codeInput, '000000');
    await user.click(screen.getByRole('button', { name: /verificar/i }));

    await waitFor(() => {
      expect(mockVerifyMfa).toHaveBeenCalledWith('000000');
    });

    expect(await screen.findByText(/código inválido/i)).toBeInTheDocument();
  });

  it('accepts valid verification code and updates user context', async () => {
    const user = userEvent.setup();
    setupAuthInStorage(mockUser);

    vi.mocked(mockEnableMfa).mockResolvedValueOnce({
      provisioningUri: 'otpauth://totp/DocFlow:test?secret=TEST',
      secretKey: 'TEST',
    });
    vi.mocked(mockVerifyMfa).mockResolvedValueOnce({ success: true, error: undefined });

    renderProfileWithAuthReader();

    await user.click(await screen.findByRole('button', { name: /activar mfa/i }));
    await screen.findByPlaceholderText('123456');

    const codeInput = screen.getByPlaceholderText('123456');
    await user.type(codeInput, '123456');
    await user.click(screen.getByRole('button', { name: /verificar/i }));

    await waitFor(() => {
      expect(mockVerifyMfa).toHaveBeenCalledWith('123456');
    });

    await waitFor(() => {
      expect(screen.getByText(/autenticación en dos pasos activada correctamente/i)).toBeInTheDocument();
    });
  });

  // ── 3.4 MFA disable flow ────────────────────────────────────────────────

  it('shows disable modal when clicking Desactivar MFA with MFA enabled', async () => {
    const user = userEvent.setup();
    const mfaUser = { ...mockUser, mfaEnabled: true };
    setupAuthInStorage(mfaUser);

    renderWithProviders(<ProfilePage />);

    expect(await screen.findByText('Activado')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /desactivar mfa/i }));

    // Modal should be visible
    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByPlaceholderText('Contraseña actual')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^desactivar$/i })).toBeInTheDocument();
  });

  it('calls disableMfa with password and shows success', async () => {
    const user = userEvent.setup();
    const mfaUser = { ...mockUser, mfaEnabled: true };
    setupAuthInStorage(mfaUser);

    vi.mocked(mockDisableMfa).mockResolvedValueOnce(undefined);

    renderProfileWithAuthReader();

    await user.click(await screen.findByRole('button', { name: /desactivar mfa/i }));

    const passwordInput = screen.getByPlaceholderText('Contraseña actual');
    await user.type(passwordInput, 'correct-password');

    await user.click(screen.getByRole('button', { name: /^desactivar$/i }));

    await waitFor(() => {
      expect(mockDisableMfa).toHaveBeenCalledWith('correct-password');
    });

    await waitFor(() => {
      expect(screen.getByText(/autenticación en dos pasos desactivada correctamente/i)).toBeInTheDocument();
    });
  });

  it('shows error when disableMfa fails with wrong password', async () => {
    const user = userEvent.setup();
    const mfaUser = { ...mockUser, mfaEnabled: true };
    setupAuthInStorage(mfaUser);

    vi.mocked(mockDisableMfa).mockRejectedValueOnce(new Error('La contraseña actual ingresada no es correcta.'));

    renderWithProviders(<ProfilePage />);

    await user.click(await screen.findByRole('button', { name: /desactivar mfa/i }));

    const passwordInput = screen.getByPlaceholderText('Contraseña actual');
    await user.type(passwordInput, 'wrong-password');

    await user.click(screen.getByRole('button', { name: /^desactivar$/i }));

    await waitFor(() => {
      expect(screen.getByText(/La contraseña actual ingresada no es correcta/i)).toBeInTheDocument();
    });
  });

  // ── 3.5 MFA badge ───────────────────────────────────────────────────────

  it('shows "Activado" badge when user has mfaEnabled', async () => {
    const mfaUser = { ...mockUser, mfaEnabled: true };
    setupAuthInStorage(mfaUser);

    renderWithProviders(<ProfilePage />);

    expect(await screen.findByText('Activado')).toBeInTheDocument();
    expect(screen.getByText('Desactivar MFA')).toBeInTheDocument();
    expect(screen.queryByText('Activar MFA')).not.toBeInTheDocument();
  });

  it('shows "Desactivado" badge when user has mfaEnabled false', async () => {
    const noMfaUser = { ...mockUser, mfaEnabled: false };
    setupAuthInStorage(noMfaUser);

    renderWithProviders(<ProfilePage />);

    expect(await screen.findByText('Desactivado')).toBeInTheDocument();
    expect(screen.getByText('Activar MFA')).toBeInTheDocument();
    expect(screen.queryByText('Desactivar MFA')).not.toBeInTheDocument();
  });

  it('shows "Desactivado" badge when mfaEnabled is undefined', async () => {
    setupAuthInStorage(mockUser); // mockUser has no mfaEnabled

    renderWithProviders(<ProfilePage />);

    expect(await screen.findByText('Desactivado')).toBeInTheDocument();
    expect(screen.getByText('Activar MFA')).toBeInTheDocument();
  });

  // ── Signature (self-service) section ────────────────────────────────────

  it('renders the Firma section with a "Configurar firma" button', async () => {
    setupAuthInStorage(mockUser);

    renderWithProviders(<ProfilePage />);

    expect(await screen.findByText('Firma')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /configurar firma/i })).toBeInTheDocument();
  });

  it('opens the signature modal when clicking "Configurar firma"', async () => {
    const user = userEvent.setup();
    setupAuthInStorage(mockUser);

    renderWithProviders(<ProfilePage />);

    await user.click(await screen.findByRole('button', { name: /configurar firma/i }));

    // Modal renders its title and the self-service metadata is loaded (empty state).
    expect(await screen.findByRole('dialog', { name: /configurar firma/i })).toBeInTheDocument();
    expect(await screen.findByText(/no tiene una firma configurada/i)).toBeInTheDocument();
  });
});
