import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '@/test/utils';
import OrdenesCompraPage from './OrdenesCompraPage';
import {
  ADJUNTO_ID,
  OC_ID,
  adjunto,
  detalle,
  listItem,
  page,
  proveedoresPage,
} from './__tests__/ordenesCompra.fixtures';

vi.mock('@/lib/api/ordenesCompra', () => ({
  listOrdenesCompra: vi.fn(),
  getOrdenCompra: vi.fn(),
  getOrdenCompraPdf: vi.fn(),
  downloadAdjuntoOrdenCompra: vi.fn(),
  createOrdenCompra: vi.fn(),
  updateOrdenCompra: vi.fn(),
  enviarAprobacionOrdenCompra: vi.fn(),
  aprobarOrdenCompra: vi.fn(),
  rechazarOrdenCompra: vi.fn(),
  marcarEnviadaOrdenCompra: vi.fn(),
  anularOrdenCompra: vi.fn(),
  agregarAdjuntoOrdenCompra: vi.fn(),
  eliminarAdjuntoOrdenCompra: vi.fn(),
  buscarOrdenMercadoPublico: vi.fn(),
  vincularMercadoPublicoOrdenCompra: vi.fn(),
  desvincularMercadoPublicoOrdenCompra: vi.fn(),
}));

vi.mock('@/lib/api/proveedores', () => ({
  listProveedores: vi.fn(),
}));

vi.mock('@/hooks/usePermissions', () => ({
  useHasPermission: () => true,
}));

import {
  agregarAdjuntoOrdenCompra as mockAgregarAdjunto,
  downloadAdjuntoOrdenCompra as mockDownloadAdjunto,
  eliminarAdjuntoOrdenCompra as mockEliminarAdjunto,
  getOrdenCompra as mockGetOrdenCompra,
  listOrdenesCompra as mockListOrdenesCompra,
} from '@/lib/api/ordenesCompra';
import { listProveedores as mockListProveedores } from '@/lib/api/proveedores';

const abrirDetalle = async (user: ReturnType<typeof userEvent.setup>) => {
  renderWithProviders(<OrdenesCompraPage />);
  await screen.findAllByText('OC-2026-0001');
  await user.click(screen.getAllByRole('button', { name: 'Ver' })[0]);
  expect((await screen.findAllByText('Adjuntos')).length).toBeGreaterThan(0);
};

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(mockListProveedores).mockResolvedValue(proveedoresPage as never);
  vi.mocked(mockListOrdenesCompra).mockResolvedValue(page([listItem()]));
});

// ── D. Attachments in the detail modal ───────────────────────────────────────

describe('OrdenesCompraPage — attachments', () => {
  it('lists the existing attachments by name', async () => {
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(
      detalle({
        adjuntos: [
          adjunto(),
          adjunto({ id: '00000000-0000-0000-0000-0000000000ce', nombreArchivo: 'cotizacion.xlsx' }),
        ],
      }),
    );
    const user = userEvent.setup();

    await abrirDetalle(user);

    expect(await screen.findByText('factura.pdf')).toBeInTheDocument();
    expect(screen.getByText('cotizacion.xlsx')).toBeInTheDocument();
    expect(screen.queryByText('Sin adjuntos.')).not.toBeInTheDocument();
  });

  it('downloads an attachment through downloadAdjuntoOrdenCompra and an anchor', async () => {
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(detalle({ adjuntos: [adjunto()] }));
    vi.mocked(mockDownloadAdjunto).mockResolvedValue(
      new Blob(['%PDF-1.7'], { type: 'application/pdf' }),
    );

    const originalCreateObjectURL = URL.createObjectURL;
    const originalRevokeObjectURL = URL.revokeObjectURL;
    URL.createObjectURL = vi.fn(() => 'blob:mock-url');
    URL.revokeObjectURL = vi.fn();
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => {});

    try {
      const user = userEvent.setup();
      await abrirDetalle(user);

      await user.click(await screen.findByRole('button', { name: 'Descargar' }));

      await waitFor(() => {
        expect(vi.mocked(mockDownloadAdjunto)).toHaveBeenCalledWith(OC_ID, ADJUNTO_ID);
      });
      await waitFor(() => expect(clickSpy).toHaveBeenCalledTimes(1));
      const anchor = clickSpy.mock.contexts[0] as HTMLAnchorElement;
      expect(anchor.download).toBe('factura.pdf');
    } finally {
      clickSpy.mockRestore();
      URL.createObjectURL = originalCreateObjectURL;
      URL.revokeObjectURL = originalRevokeObjectURL;
    }
  });

  it('does not delete the attachment when the confirm dialog is cancelled', async () => {
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(detalle({ adjuntos: [adjunto()] }));
    const user = userEvent.setup();

    await abrirDetalle(user);

    await user.click(await screen.findByRole('button', { name: 'Eliminar' }));
    expect(await screen.findByText('¿Eliminar el adjunto factura.pdf?')).toBeInTheDocument();

    const confirmDialog = screen.getAllByRole('dialog').at(-1)!;
    await user.click(within(confirmDialog).getByRole('button', { name: 'Cancelar' }));

    expect(vi.mocked(mockEliminarAdjunto)).not.toHaveBeenCalled();
  });

  it('uploads a file sending nombre, contentType and base64 content', async () => {
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(detalle());
    vi.mocked(mockAgregarAdjunto).mockResolvedValue(adjunto({ nombreArchivo: 'nota.txt' }));
    const user = userEvent.setup();

    await abrirDetalle(user);

    const fileInput = screen.getByLabelText('Archivo adjunto');
    // 'hola' → base64 aG9sYQ==
    await user.upload(fileInput, new File(['hola'], 'nota.txt', { type: 'text/plain' }));

    await waitFor(() => {
      expect(vi.mocked(mockAgregarAdjunto)).toHaveBeenCalledWith(OC_ID, {
        nombreArchivo: 'nota.txt',
        contentType: 'text/plain',
        contenidoBase64: 'aG9sYQ==',
      });
    });
    expect(await screen.findByText('Adjunto agregado correctamente.')).toBeInTheDocument();
  });

  it('rejects files over 10 MB client-side without calling the API', async () => {
    vi.mocked(mockGetOrdenCompra).mockResolvedValue(detalle());
    const user = userEvent.setup();

    await abrirDetalle(user);

    const grande = new File([], 'grande.pdf', { type: 'application/pdf' });
    Object.defineProperty(grande, 'size', { value: 10 * 1024 * 1024 + 1 });

    await user.upload(screen.getByLabelText('Archivo adjunto'), grande);

    expect(
      await screen.findByText('El adjunto no puede superar los 10 MB.'),
    ).toBeInTheDocument();
    expect(vi.mocked(mockAgregarAdjunto)).not.toHaveBeenCalled();
  });
});
