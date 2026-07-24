import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminRespaldosPage from './AdminRespaldosPage';
import * as adminRespaldosApi from '../../lib/api/admin/adminRespaldosApi';
import { useHasPermission } from '../../hooks/usePermissions';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';

// ── Mocks ────────────────────────────────────────────────────────────────────

vi.mock('../../lib/api/admin/adminRespaldosApi', () => ({
  getRespaldos: vi.fn(),
  triggerRespaldo: vi.fn(),
  downloadRespaldo: vi.fn(),
  getRespaldoConfig: vi.fn(),
  updateRespaldoConfig: vi.fn(),
  restoreRespaldo: vi.fn(),
  getRestoreLogs: vi.fn(),
}));

vi.mock('../../hooks/usePermissions', () => ({
  useHasPermission: vi.fn(),
}));

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}

function renderWithProviders() {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <AdminRespaldosPage />
    </QueryClientProvider>,
  );
}

// ── Fixtures ─────────────────────────────────────────────────────────────────

const mockRespaldos = [
  {
    id: '70000000-0000-0000-0000-000000000001',
    nombre: 'Respaldo-20260516-120000',
    fechaCreacion: '2026-05-16T12:00:00Z',
    tamanioBytes: 2048,
    estado: 'Completado',
    ruta: '/respaldos/backup-001.zip',
  },
  {
    id: '70000000-0000-0000-0000-000000000002',
    nombre: 'Respaldo-20260516-110000',
    fechaCreacion: '2026-05-16T11:00:00Z',
    tamanioBytes: 1048576,
    estado: 'Fallido',
    ruta: '/respaldos/backup-002.zip',
  },
  {
    id: '70000000-0000-0000-0000-000000000003',
    nombre: 'Respaldo-20260516-100000',
    fechaCreacion: '2026-05-16T10:00:00Z',
    tamanioBytes: 0,
    estado: 'EnProceso',
    ruta: '',
  },
];

const mockConfig = {
  id: '80000000-0000-0000-0000-000000000001',
  intervaloMinutos: 60,
  habilitado: true,
  maxBackupCount: 5,
  retentionDays: 7,
  outputPath: './Respaldos',
  timeoutMinutos: 30,
  actualizadoEn: '2026-05-17T00:00:00Z',
};

// ── Tests ────────────────────────────────────────────────────────────────────

describe('AdminRespaldosPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(adminRespaldosApi.getRespaldoConfig).mockResolvedValue(mockConfig);
    vi.mocked(useHasPermission).mockReturnValue(true);
  });

  describe('Respaldos tab', () => {
    it('renders the backup list from API data', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);

      renderWithProviders();

      expect(await screen.findByText('Respaldo-20260516-120000')).toBeInTheDocument();
      expect(screen.getByText('Respaldo-20260516-110000')).toBeInTheDocument();
      expect(screen.getByText('Respaldo-20260516-100000')).toBeInTheDocument();

      // Estado badges
      expect(screen.getByText('Completado')).toBeInTheDocument();
      expect(screen.getByText('Fallido')).toBeInTheDocument();
      expect(screen.getByText('En Proceso')).toBeInTheDocument();

      // Tamaño formatted (2048 = 2.0 KB)
      expect(screen.getByText('2.0 KB')).toBeInTheDocument();
      // 1048576 = 1.0 MB
      expect(screen.getByText('1.0 MB')).toBeInTheDocument();

      // La ruta del servidor NO debe mostrarse al usuario (info interna).
      expect(screen.queryByText('/respaldos/backup-001.zip')).not.toBeInTheDocument();
      expect(screen.queryByText('/respaldos/backup-002.zip')).not.toBeInTheDocument();
    });

    it('triggers a backup when button is clicked', async () => {
      const user = userEvent.setup();
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([]);
      vi.mocked(adminRespaldosApi.triggerRespaldo).mockResolvedValue({
        id: '70000000-0000-0000-0000-000000000003',
        nombre: 'Respaldo-20260516-130000',
        fechaCreacion: '2026-05-16T13:00:00Z',
        tamanioBytes: 0,
        estado: 'Completado',
        ruta: '/respaldos/stub',
      });

      renderWithProviders();

      // Wait for empty state
      await screen.findByText('No hay respaldos registrados.');

      const triggerButton = screen.getByRole('button', { name: /generar respaldo/i });
      await user.click(triggerButton);

      expect(vi.mocked(adminRespaldosApi.triggerRespaldo)).toHaveBeenCalledTimes(1);
    });

    it('polls until an in-progress backup becomes completed', async () => {
      const pendingBackup = {
        id: '70000000-0000-0000-0000-000000000099',
        nombre: 'Respaldo-20260516-150000',
        fechaCreacion: '2026-05-16T15:00:00Z',
        tamanioBytes: 0,
        estado: 'EnProceso',
        ruta: '/respaldos/backup-099.sql.gz',
      };

      const completedBackup = {
        ...pendingBackup,
        tamanioBytes: 4096,
        estado: 'Completado',
      };

      vi.mocked(adminRespaldosApi.getRespaldos)
        .mockResolvedValueOnce([pendingBackup])
        .mockResolvedValueOnce([completedBackup]);

      renderWithProviders();

      expect(await screen.findByText('Respaldo-20260516-150000')).toBeInTheDocument();
      expect(screen.getByText('En Proceso')).toBeInTheDocument();

      await waitFor(() => expect(adminRespaldosApi.getRespaldos).toHaveBeenCalledTimes(2), { timeout: 4000 });

      await waitFor(() => expect(screen.getByText('Completado')).toBeInTheDocument());
      expect(screen.queryByText('En Proceso')).not.toBeInTheDocument();
    });

    it('shows empty state when no backups exist', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([]);

      renderWithProviders();

      expect(await screen.findByText('No hay respaldos registrados.')).toBeInTheDocument();
    });

    it('shows download button only for Completado backups', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');

      // Only one download button for the Completado entry
      const downloadButtons = screen.getAllByRole('button', { name: /descargar/i });
      expect(downloadButtons).toHaveLength(1);
    });

    it('hides download button without dedicated download permission', async () => {
      vi.mocked(useHasPermission).mockImplementation((permission) =>
        permission !== PERMISSIONS.ADMIN_RESPALDOS_DESCARGAR,
      );
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');

      expect(screen.queryByRole('button', { name: /descargar/i })).not.toBeInTheDocument();
    });

    it('does not show download button for Pendiente or Fallido backups', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([
        {
          id: '70000000-0000-0000-0000-000000000010',
          nombre: 'Respaldo-Pendiente',
          fechaCreacion: '2026-05-16T10:00:00Z',
          tamanioBytes: 0,
          estado: 'Pendiente',
          ruta: '',
        },
        {
          id: '70000000-0000-0000-0000-000000000011',
          nombre: 'Respaldo-Fallido-2',
          fechaCreacion: '2026-05-16T09:00:00Z',
          tamanioBytes: 0,
          estado: 'Fallido',
          ruta: '/respaldos/failed.sql.gz',
        },
      ]);

      renderWithProviders();

      await screen.findByText('Respaldo-Pendiente');

      expect(screen.queryByRole('button', { name: /descargar/i })).not.toBeInTheDocument();
    });
  });

  describe('Configuración tab', () => {
    it('renders both tab buttons', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);

      renderWithProviders();

      // Wait for content to load
      await screen.findByText('Respaldo-20260516-120000');

      expect(screen.getByRole('tab', { name: /respaldos/i })).toBeInTheDocument();
      expect(screen.getByRole('tab', { name: /configuración/i })).toBeInTheDocument();
    });

    it('switches to Configuración tab and shows config form', async () => {
      const user = userEvent.setup();
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);

      renderWithProviders();

      // Wait for data load
      await screen.findByText('Respaldo-20260516-120000');

      // Click Configuración tab
      const configTab = screen.getByRole('tab', { name: /configuración/i });
      await user.click(configTab);

      // Should show the config form with fields
      expect(await screen.findByLabelText(/intervalo/i)).toBeInTheDocument();
    });

    it('shows config form with config data from API', async () => {
      const user = userEvent.setup();
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);

      renderWithProviders();

      // Wait for data load
      await screen.findByText('Respaldo-20260516-120000');

      // Click Configuración tab
      const configTab = screen.getByRole('tab', { name: /configuración/i });
      await user.click(configTab);

      // Should show the config data
      const intervalInput = await screen.findByLabelText(/intervalo/i) as HTMLInputElement;
      expect(intervalInput.value).toBe('60');
    });

    it('submits config changes (errors surface via toast)', async () => {
      const user = userEvent.setup();
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);
      vi.mocked(adminRespaldosApi.getRestoreLogs).mockResolvedValue([]);
      vi.mocked(adminRespaldosApi.updateRespaldoConfig).mockRejectedValue(new Error('boom'));

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');
      await user.click(screen.getByRole('tab', { name: /configuración/i }));

      const intervalInput = await screen.findByLabelText(/intervalo/i);
      await user.clear(intervalInput);
      await user.type(intervalInput, '120');
      await user.click(screen.getByRole('button', { name: /guardar configuración/i }));

      // Action error feedback is delivered via the global toast (no-op without provider in tests).
      // Here we assert the save action was actually submitted with the new value.
      await waitFor(() =>
        expect(vi.mocked(adminRespaldosApi.updateRespaldoConfig)).toHaveBeenCalledWith(
          expect.objectContaining({ intervaloMinutos: 120 }),
        ),
      );
    });

    it('requires dedicated configure permission to edit and save config', async () => {
      const user = userEvent.setup();
      vi.mocked(useHasPermission).mockImplementation((permission) =>
        permission !== PERMISSIONS.ADMIN_RESPALDOS_CONFIGURAR,
      );
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');
      await user.click(screen.getByRole('tab', { name: /configuración/i }));

      expect(await screen.findByLabelText(/intervalo/i)).toBeDisabled();
      expect(screen.queryByRole('button', { name: /guardar configuración/i })).not.toBeInTheDocument();
      expect(vi.mocked(adminRespaldosApi.updateRespaldoConfig)).not.toHaveBeenCalled();
    });

    it('does not allow generic edit permission to save config without configure permission', async () => {
      const user = userEvent.setup();
      vi.mocked(useHasPermission).mockImplementation((permission) =>
        permission === PERMISSIONS.ADMIN_RESPALDOS_EDITAR,
      );
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue(mockRespaldos);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');
      await user.click(screen.getByRole('tab', { name: /configuración/i }));

      expect(await screen.findByLabelText(/ruta de salida/i)).toBeDisabled();
      expect(screen.queryByRole('button', { name: /guardar configuración/i })).not.toBeInTheDocument();
    });
  });

  describe('Restore', () => {
    const completedFixture = {
      id: '70000000-0000-0000-0000-000000000001',
      nombre: 'Respaldo-20260516-120000',
      fechaCreacion: '2026-05-16T12:00:00Z',
      tamanioBytes: 2048,
      estado: 'Completado',
      ruta: '/respaldos/backup-001.zip',
    };

    it('shows Restaurar button for completed backups', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([completedFixture]);
      vi.mocked(adminRespaldosApi.getRestoreLogs).mockResolvedValue([]);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');

      expect(screen.getByRole('button', { name: /restaurar/i })).toBeInTheDocument();
    });

    it('hides Restaurar button without dedicated restore permission', async () => {
      vi.mocked(useHasPermission).mockImplementation((permission) =>
        permission !== PERMISSIONS.ADMIN_RESPALDOS_RESTAURAR,
      );
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([completedFixture]);
      vi.mocked(adminRespaldosApi.getRestoreLogs).mockResolvedValue([]);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');

      expect(screen.queryByRole('button', { name: /restaurar/i })).not.toBeInTheDocument();
    });

    it('does not show Restaurar button for non-completed backups', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([
        { ...completedFixture, estado: 'Fallido' },
      ]);
      vi.mocked(adminRespaldosApi.getRestoreLogs).mockResolvedValue([]);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');

      expect(screen.queryByRole('button', { name: /restaurar/i })).not.toBeInTheDocument();
    });

    it('disables confirm button until exact backup name is typed', async () => {
      const user = userEvent.setup();
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([completedFixture]);
      vi.mocked(adminRespaldosApi.getRestoreLogs).mockResolvedValue([]);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');

      // Click Restaurar
      const restoreButton = screen.getByRole('button', { name: /restaurar/i });
      await user.click(restoreButton);

      // Dialog opened — confirm button disabled
      const confirmButton = screen.getByRole('button', { name: /confirmar restauración/i });
      expect(confirmButton).toBeDisabled();

      // Type partial name — still disabled
      const input = screen.getByPlaceholderText(/escribe el nombre/i);
      await user.type(input, 'Respaldo-');
      expect(confirmButton).toBeDisabled();

      // Type exact name — enabled
      await user.clear(input);
      await user.type(input, 'Respaldo-20260516-120000');
      expect(confirmButton).toBeEnabled();
    });

    it('restore history section renders with logs', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([completedFixture]);
      vi.mocked(adminRespaldosApi.getRestoreLogs).mockResolvedValue([
        {
          id: '80000000-0000-0000-0000-000000000001',
          respaldoId: completedFixture.id,
          fechaInicio: '2026-05-16T13:00:00Z',
          fechaFin: '2026-05-16T13:05:00Z',
          estado: 'Completado',
          mensajeError: null,
        },
      ]);

      renderWithProviders();

      await screen.findByText('Respaldo-20260516-120000');

      // "Historial de Restauraciones" section visible
      expect(await screen.findByText('Historial de Restauraciones')).toBeInTheDocument();
    });

    it('refreshes restore history after a successful restore', async () => {
      const user = userEvent.setup();
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([completedFixture]);
      vi.mocked(adminRespaldosApi.getRestoreLogs).mockResolvedValue([]);
      vi.mocked(adminRespaldosApi.getRestoreLogs)
        .mockResolvedValueOnce([
          {
            id: '80000000-0000-0000-0000-000000000001',
            respaldoId: completedFixture.id,
            fechaInicio: '2026-05-16T13:00:00Z',
            fechaFin: '2026-05-16T13:05:00Z',
            estado: 'Completado',
            mensajeError: null,
          },
        ])
        .mockResolvedValueOnce([
          {
            id: '80000000-0000-0000-0000-000000000001',
            respaldoId: completedFixture.id,
            fechaInicio: '2026-05-16T13:00:00Z',
            fechaFin: '2026-05-16T13:05:00Z',
            estado: 'Completado',
            mensajeError: null,
          },
          {
            id: '80000000-0000-0000-0000-000000000002',
            respaldoId: completedFixture.id,
            fechaInicio: '2026-05-16T14:00:00Z',
            fechaFin: '2026-05-16T14:02:00Z',
            estado: 'Completado',
            mensajeError: null,
          },
        ]);

      let resolveRestore!: (value: adminRespaldosApi.RestoreLogDto) => void;
      const restorePromise = new Promise<adminRespaldosApi.RestoreLogDto>((resolve) => {
        resolveRestore = resolve;
      });
      vi.mocked(adminRespaldosApi.restoreRespaldo).mockReturnValue(restorePromise);

      renderWithProviders();

      await screen.findByText('Historial de Restauraciones');
      await user.click(screen.getByRole('button', { name: /restaurar/i }));
      await user.type(screen.getByPlaceholderText(/escribe el nombre/i), completedFixture.nombre);
      await user.click(screen.getByRole('button', { name: /confirmar restauración/i }));

      expect(await screen.findByText('La restauración ya fue iniciada y se está procesando en segundo plano.')).toBeInTheDocument();

      resolveRestore({
        id: '90000000-0000-0000-0000-000000000001',
        respaldoId: completedFixture.id,
        fechaInicio: '2026-05-16T14:00:00Z',
        fechaFin: '2026-05-16T14:02:00Z',
        estado: 'Completado',
        mensajeError: null,
      });

      await waitFor(() => expect(vi.mocked(adminRespaldosApi.getRestoreLogs).mock.calls.length).toBeGreaterThanOrEqual(2));
    });

    it('polls restore history until the log becomes completed', async () => {
      vi.mocked(adminRespaldosApi.getRespaldos).mockResolvedValue([completedFixture]);
      vi.mocked(adminRespaldosApi.getRestoreLogs)
        .mockResolvedValueOnce([
          {
            id: '80000000-0000-0000-0000-000000000010',
            respaldoId: completedFixture.id,
            fechaInicio: '2026-05-16T15:00:00Z',
            fechaFin: null,
            estado: 'EnProceso',
            mensajeError: null,
          },
        ])
        .mockResolvedValueOnce([
          {
            id: '80000000-0000-0000-0000-000000000010',
            respaldoId: completedFixture.id,
            fechaInicio: '2026-05-16T15:00:00Z',
            fechaFin: '2026-05-16T15:05:00Z',
            estado: 'Completado',
            mensajeError: null,
          },
        ]);

      renderWithProviders();

      expect(await screen.findByText('Historial de Restauraciones')).toBeInTheDocument();
      expect(await screen.findByText('En Proceso')).toBeInTheDocument();

      await waitFor(() => expect(adminRespaldosApi.getRestoreLogs).toHaveBeenCalledTimes(2), { timeout: 4000 });

      const completedLabels = await screen.findAllByText('Completado');
      expect(completedLabels.length).toBeGreaterThanOrEqual(2);
      expect(screen.queryByText('En Proceso')).not.toBeInTheDocument();
    });
  });
});
