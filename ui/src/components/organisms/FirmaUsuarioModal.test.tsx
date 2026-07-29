import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import FirmaUsuarioModal, { type FirmaOperations } from './FirmaUsuarioModal';
import { ToastProvider } from '../../contexts/ToastContext';
import type { FirmaUsuarioMetadata } from '../../lib/api/admin/firmaUsuarioApi';

const USER_ID = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001';

const emptyMeta: FirmaUsuarioMetadata = {
  usuarioId: USER_ID,
  tieneFirma: false,
  tieneClave: false,
  sigla: null,
  contentType: null,
  tamano: 0,
  creadoEn: null,
  actualizadoEn: null,
};

const withFirmaMeta: FirmaUsuarioMetadata = {
  usuarioId: USER_ID,
  tieneFirma: true,
  tieneClave: true,
  sigla: 'JPG',
  contentType: 'image/png',
  tamano: 1234,
  creadoEn: '2026-01-01T00:00:00Z',
  actualizadoEn: '2026-01-02T00:00:00Z',
};

/** Mocked signature operations injected into the modal. */
function createOps(overrides: Partial<FirmaOperations> = {}) {
  const getMetadata = vi.fn<FirmaOperations['getMetadata']>().mockResolvedValue(emptyMeta);
  const getImagen = vi
    .fn<FirmaOperations['getImagen']>()
    .mockResolvedValue(new Blob(['img'], { type: 'image/png' }));
  const guardar = vi
    .fn<FirmaOperations['guardar']>()
    .mockResolvedValue({ ...emptyMeta, tieneFirma: true });
  const eliminar = vi.fn<FirmaOperations['eliminar']>().mockResolvedValue(undefined);
  return {
    getMetadata,
    getImagen,
    guardar,
    eliminar,
    cacheKey: ['test', 'firma', USER_ID] as const,
    ...overrides,
  } satisfies FirmaOperations;
}

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
}

function renderModal(
  overrides: Partial<React.ComponentProps<typeof FirmaUsuarioModal>> = {},
  ops: FirmaOperations = createOps(),
) {
  const onClose = vi.fn();
  const queryClient = createTestQueryClient();
  render(
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <FirmaUsuarioModal
          open
          operations={ops}
          usuarioNombre="Ada Lovelace"
          canEdit
          onClose={onClose}
          {...overrides}
        />
      </ToastProvider>
    </QueryClientProvider>,
  );
  return { onClose, ops };
}

beforeEach(() => {
  vi.clearAllMocks();
  // jsdom has no object-URL support.
  URL.createObjectURL = vi.fn(() => 'blob:mock-url');
  URL.revokeObjectURL = vi.fn();
});

describe('FirmaUsuarioModal', () => {
  it('shows the empty state when the user has no signature', async () => {
    renderModal();
    expect(await screen.findByText(/no tiene una firma configurada/i)).toBeInTheDocument();
    // No delete button when there is no signature.
    expect(screen.queryByRole('button', { name: /eliminar firma/i })).not.toBeInTheDocument();
  });

  it('uploads an image and saves via guardar without clave when the field is empty', async () => {
    const user = userEvent.setup();
    const { onClose, ops } = renderModal();

    await screen.findByText(/no tiene una firma configurada/i);

    const file = new File(['hello'], 'firma.png', { type: 'image/png' });
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    await user.upload(input, file);

    await user.click(screen.getByRole('button', { name: /^Guardar$/i }));

    await waitFor(() => {
      expect(ops.guardar).toHaveBeenCalledTimes(1);
    });
    const [body] = vi.mocked(ops.guardar).mock.calls[0];
    expect(body.contentType).toBe('image/png');
    expect(body.imagenBase64).toBeTruthy();
    expect(body.clave).toBeUndefined();
    await waitFor(() => expect(onClose).toHaveBeenCalled());
  });

  it('includes clave in the body only when the user types one', async () => {
    const user = userEvent.setup();
    const { ops } = renderModal();

    await screen.findByText(/no tiene una firma configurada/i);

    const file = new File(['hello'], 'firma.png', { type: 'image/png' });
    await user.upload(document.querySelector('input[type="file"]') as HTMLInputElement, file);

    await user.type(screen.getByPlaceholderText(/dejar vacío para omitir/i), 'secret1');
    await user.click(screen.getByRole('button', { name: /^Guardar$/i }));

    await waitFor(() => expect(ops.guardar).toHaveBeenCalled());
    const body = vi.mocked(ops.guardar).mock.calls[0][0];
    expect(body.clave).toBe('secret1');
  });

  it('rejects a file larger than 2 MB', async () => {
    const user = userEvent.setup();
    const { ops } = renderModal();

    await screen.findByText(/no tiene una firma configurada/i);

    const big = new File([new Uint8Array(2 * 1024 * 1024 + 1)], 'big.png', { type: 'image/png' });
    await user.upload(document.querySelector('input[type="file"]') as HTMLInputElement, big);

    expect(await screen.findByText(/no puede superar los 2 mb/i)).toBeInTheDocument();
    expect(ops.guardar).not.toHaveBeenCalled();
  });

  it('editing only the sigla preserves image and clave (no warning, partial body)', async () => {
    const user = userEvent.setup();
    const ops = createOps({
      getMetadata: vi.fn<FirmaOperations['getMetadata']>().mockResolvedValue(withFirmaMeta),
    });
    renderModal({}, ops);

    // Wait for the loaded form (metadata resolved), then confirm the old
    // destructive-clave warning is gone (backend preserves on omit now).
    const siglaInput = await screen.findByPlaceholderText(/ej: jpg/i);
    expect(screen.queryByText(/si no volvés a ingresar la clave/i)).not.toBeInTheDocument();

    // Change ONLY the sigla and save: image and clave must not be resent.
    await user.clear(siglaInput);
    await user.type(siglaInput, 'ABC');
    await user.click(screen.getByRole('button', { name: /^Guardar$/i }));

    await waitFor(() => expect(ops.guardar).toHaveBeenCalled());
    const body = vi.mocked(ops.guardar).mock.calls[0][0];
    expect(body.sigla).toBe('ABC');
    expect(body.imagenBase64).toBeUndefined();
    expect(body.clave).toBeUndefined();
  });

  it('deletes the signature after confirmation', async () => {
    const user = userEvent.setup();
    const ops = createOps({
      getMetadata: vi.fn<FirmaOperations['getMetadata']>().mockResolvedValue(withFirmaMeta),
    });
    const { onClose } = renderModal({}, ops);

    const deleteBtn = await screen.findByRole('button', { name: /eliminar firma/i });
    await user.click(deleteBtn);

    // ConfirmDialog appears
    const dialog = await screen.findByRole('dialog', { name: /eliminar firma/i });
    await user.click(within(dialog).getByRole('button', { name: /^Eliminar$/i }));

    await waitFor(() => expect(ops.eliminar).toHaveBeenCalledTimes(1));
    await waitFor(() => expect(onClose).toHaveBeenCalled());
  });

  it('hides save/delete controls when the user cannot edit', async () => {
    const ops = createOps({
      getMetadata: vi.fn<FirmaOperations['getMetadata']>().mockResolvedValue(withFirmaMeta),
    });
    renderModal({ canEdit: false }, ops);

    await waitFor(() => expect(ops.getMetadata).toHaveBeenCalled());
    expect(screen.queryByRole('button', { name: /^Guardar$/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /eliminar firma/i })).not.toBeInTheDocument();
  });
});
