import { beforeEach, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminPlantillasSection from './AdminPlantillasSection';
import * as adminCatalogosApi from '../../lib/api/admin/adminCatalogosApi';
import type { SeForplaDto } from '../../lib/api/admin/adminCatalogosApi';
import { useHasPermission } from '../../hooks/usePermissions';

vi.mock('../../lib/api/admin/adminCatalogosApi', () => ({
  listSeForpla: vi.fn(),
  createSeForpla: vi.fn(),
  updateSeForpla: vi.fn(),
  deleteSeForpla: vi.fn(),
  listSeFordoc: vi.fn(),
  listCatalogoCategorias: vi.fn(),
  listCatalogoSubcategorias: vi.fn(),
  getPlantillaEditorConfig: vi.fn(),
  forcePlantillaSave: vi.fn(),
  getPlantillaMedidas: vi.fn(),
  getPlantillaPdf: vi.fn(),
  updatePlantillaMedidas: vi.fn(),
}));

// pdf.js no puede renderizar en jsdom: mockeamos el módulo y el worker (?url) para que
// PlantillaMedidasEditor monte sin intentar un render real.
vi.mock('pdfjs-dist', () => ({
  GlobalWorkerOptions: { workerSrc: '' },
  getDocument: vi.fn(() => ({
    promise: Promise.resolve({
      numPages: 1,
      getPage: vi.fn(() =>
        Promise.resolve({
          getViewport: vi.fn(({ scale }: { scale: number }) => ({ width: 612 * scale, height: 792 * scale })),
          render: vi.fn(() => ({ promise: Promise.resolve(), cancel: vi.fn() })),
        }),
      ),
    }),
  })),
}));
vi.mock('pdfjs-dist/build/pdf.worker.min.mjs?url', () => ({ default: 'test-worker-url' }));

vi.mock('../../hooks/usePermissions', () => ({
  useHasPermission: vi.fn(),
}));

function plantilla(overrides: Partial<SeForplaDto> = {}): SeForplaDto {
  return {
    codForm: '{"tipo":"T","nt":5,"nc":0,"ns":0}',
    usucod: 'admin',
    tipoCod: 5,
    nomForm: 'Plantilla de Prueba',
    blobForm: btoa('contenido de prueba'),
    sisForm: '1',
    obsForm: null,
    extForm: 'docx',
    alto: null,
    ancho: null,
    ...overrides,
  };
}

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
      <AdminPlantillasSection />
    </QueryClientProvider>,
  );
}

describe('AdminPlantillasSection', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useHasPermission).mockReturnValue(true);
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([]);
    vi.mocked(adminCatalogosApi.listSeFordoc).mockResolvedValue([
      {
        tipoCod: 5,
        tipoRec: 0,
        tipoInt: 0,
        tipoDesc: 'Oficio Ordinario',
        corrN: 1,
        corrFecha: '2026-01-01',
        tipoEnv: null,
        seFordocVistaI: 0,
        seFordocVistaE: 0,
        seFordocVistaR: 0,
        seFordocFormatoNum: null,
      },
    ]);
    vi.mocked(adminCatalogosApi.listCatalogoCategorias).mockResolvedValue([
      { catCod: 12, catDesc: 'Documentos Internos', totalSubcategorias: 1 },
    ]);
    vi.mocked(adminCatalogosApi.listCatalogoSubcategorias).mockResolvedValue([
      {
        catCod: 12,
        categoriaDesc: 'Documentos Internos',
        idSubcategoria: 3,
        subcatNombre: 'Actas',
        subcatDescripcion: null,
      },
    ]);
    // Por defecto el PDF de fondo carga bien; los tests que ejercitan el fallback lo sobreescriben.
    vi.mocked(adminCatalogosApi.getPlantillaPdf).mockResolvedValue(new Blob(['%PDF-1.4 test']));
  });

  it('renders view and download actions for each plantilla', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([plantilla()]);

    renderWithProviders();

    expect(await screen.findByText('Plantilla de Prueba')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^ver$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /descargar/i })).toBeInTheDocument();
  });

  it('hides edit and delete actions when the user cannot edit plantillas', async () => {
    vi.mocked(useHasPermission).mockReturnValue(false);
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([
      plantilla({ codForm: '{"tipo":"T","nt":5,"nc":0,"ns":0}', nomForm: 'Plantilla Solo Lectura' }),
    ]);

    renderWithProviders();

    expect(await screen.findByText('Plantilla Solo Lectura')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^ver$/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /descargar/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /editar/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /eliminar/i })).not.toBeInTheDocument();
  });

  it('opens the PDF preview endpoint when viewing a plantilla', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([
      plantilla({ codForm: 'FOR-002', nomForm: 'Plantilla Vista', extForm: 'doc' }),
    ]);

    const openSpy = vi.spyOn(window, 'open').mockReturnValue(null);
    renderWithProviders();

    await screen.findByText('Plantilla Vista');
    screen.getByRole('button', { name: /^ver$/i }).click();

    expect(openSpy).toHaveBeenCalledWith('/api/admin/catalogos/plantillas/FOR-002/pdf', '_blank', 'noopener,noreferrer');
  });

  it('downloads a plantilla with a normalized file name', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([
      plantilla({ codForm: 'FOR-003', nomForm: 'Plantilla Descarga', extForm: '.docx' }),
    ]);

    const createObjectUrlSpy = vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:descarga');
    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined);

    renderWithProviders();

    await screen.findByText('Plantilla Descarga');
    screen.getByRole('button', { name: /descargar/i }).click();

    expect(createObjectUrlSpy).toHaveBeenCalledTimes(1);
    expect(clickSpy).toHaveBeenCalledTimes(1);
  });

  it('derives the Formato Documento column from the codForm JSON', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([
      plantilla({ codForm: '{"tipo":"T","nt":5,"nc":0,"ns":0}', nomForm: 'Por Formato' }),
      plantilla({ codForm: '{"tipo":"C","nt":0,"nc":12,"ns":0}', nomForm: 'Por Categoria', tipoCod: 12 }),
      plantilla({ codForm: '{"tipo":"S","nt":0,"nc":12,"ns":3}', nomForm: 'Por Subcategoria', tipoCod: 3 }),
      plantilla({ codForm: 'PLA-VIEJA', nomForm: 'Fila Legada', tipoCod: null }),
    ]);

    renderWithProviders();

    expect(await screen.findByText('Oficio Ordinario')).toBeInTheDocument();
    expect(screen.getByText('Categoría: Documentos Internos')).toBeInTheDocument();
    expect(screen.getByText('Subcategoría: Actas')).toBeInTheDocument();
    // Rows created before the redesign keep a plain codForm: shown as-is.
    expect(screen.getByText('PLA-VIEJA')).toBeInTheDocument();
    // Legacy WW columns.
    expect(screen.getByText('Usuario')).toBeInTheDocument();
    expect(screen.getByText('Nombre')).toBeInTheDocument();
    expect(screen.getByText('Observación')).toBeInTheDocument();
    expect(screen.getByText('Formato Documento')).toBeInTheDocument();
  });

  it('opens the create form with only association, file and observation fields', async () => {
    renderWithProviders();

    fireEvent.click(await screen.findByRole('button', { name: /crear plantilla/i }));

    expect(screen.getByLabelText('Asociar a')).toBeInTheDocument();
    expect(screen.getByLabelText('Formato de documento')).toBeInTheDocument();
    expect(screen.getByText('Archivo Word')).toBeInTheDocument();
    expect(screen.getByLabelText('Observación')).toBeInTheDocument();
    // Manual fields from the old 10-field form must be gone.
    expect(screen.queryByLabelText('Código')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Usuario')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Sistema')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Extensión')).not.toBeInTheDocument();
    expect(screen.queryByLabelText('Alto')).not.toBeInTheDocument();
  });

  it('cascades the association selects when choosing subcategoria', async () => {
    renderWithProviders();

    fireEvent.click(await screen.findByRole('button', { name: /crear plantilla/i }));
    fireEvent.change(screen.getByLabelText('Asociar a'), { target: { value: 'S' } });

    expect(screen.getByLabelText('Categoría')).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText('Categoría'), { target: { value: '12' } });

    const subSelect = await screen.findByLabelText('Subcategoría');
    expect(subSelect).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Actas' })).toBeInTheDocument();
    expect(screen.queryByLabelText('Formato de documento')).not.toBeInTheDocument();
  });

  it('requires the Word file when creating', async () => {
    renderWithProviders();

    fireEvent.click(await screen.findByRole('button', { name: /crear plantilla/i }));
    fireEvent.change(screen.getByLabelText('Formato de documento'), { target: { value: '5' } });
    fireEvent.click(screen.getByRole('button', { name: /guardar/i }));

    expect(await screen.findByText('El archivo Word es obligatorio')).toBeInTheDocument();
    expect(adminCatalogosApi.createSeForpla).not.toHaveBeenCalled();
  });

  const medidasFixture = () => [
    { idForplaMed: 1, objeto: 'AUTORIZACION', x: 100, y: 170, ancho: 1, alto: 1 },
    { idForplaMed: 2, objeto: 'NUMERO', x: 100, y: 130, ancho: 0, alto: 0 },
    { idForplaMed: 3, objeto: 'FIRMA', x: 50, y: 20, ancho: 450, alto: 50 },
    { idForplaMed: 4, objeto: 'QR', x: 0, y: 0, ancho: 0, alto: 0 },
    { idForplaMed: 5, objeto: 'FOTOFIRMA', x: 50, y: 50, ancho: 0, alto: 0 },
    { idForplaMed: 6, objeto: 'QRFIRMA', x: 1, y: 1, ancho: 0, alto: 0 },
    { idForplaMed: 7, objeto: 'FIRMAGOBx', x: 0, y: 0, ancho: 0, alto: 0 },
  ];

  it('opens the medidas editor with a draggable box + numeric row per object, hiding QR; QRFIRMA is editable', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([plantilla({ nomForm: 'Con Medidas' })]);
    vi.mocked(adminCatalogosApi.getPlantillaMedidas).mockResolvedValue(medidasFixture());

    renderWithProviders();

    await screen.findByText('Con Medidas');
    fireEvent.click(screen.getByRole('button', { name: /medidas/i }));

    expect(await screen.findByText('Medidas de la plantilla')).toBeInTheDocument();
    // Numeric panel column headers.
    expect(screen.getByText('Descripción')).toBeInTheDocument();
    expect(screen.getByText('Alto')).toBeInTheDocument();
    expect(screen.getByText('Ancho')).toBeInTheDocument();
    expect(screen.getByText('X')).toBeInTheDocument();
    expect(screen.getByText('Y')).toBeInTheDocument();
    // One draggable box per visible object; the QR row stays in the DB but is never shown.
    expect(screen.getByTestId('medida-box-AUTORIZACION')).toBeInTheDocument();
    expect(screen.getByTestId('medida-box-NUMERO')).toBeInTheDocument();
    expect(screen.getByTestId('medida-box-FIRMA')).toBeInTheDocument();
    expect(screen.getByTestId('medida-box-FOTOFIRMA')).toBeInTheDocument();
    expect(screen.getByTestId('medida-box-QRFIRMA')).toBeInTheDocument();
    expect(screen.getByTestId('medida-box-FIRMAGOBx')).toBeInTheDocument();
    expect(screen.queryByTestId('medida-box-QR')).not.toBeInTheDocument();
    // Object name appears both inside the box and in the numeric row.
    expect(screen.getAllByText('FIRMA')).toHaveLength(2);
    // QRFIRMA is now fully editable like any other object: size inputs are editable and it
    // has its own resize handle (no longer locked to 200x200).
    expect(screen.getByLabelText('Alto QRFIRMA')).not.toHaveAttribute('readonly');
    expect(screen.getByLabelText('Ancho QRFIRMA')).not.toHaveAttribute('readonly');
    expect(screen.getByTestId('medida-resize-QRFIRMA')).toBeInTheDocument();
    expect(screen.getByTestId('medida-resize-FIRMA')).toBeInTheDocument();
  });

  it('moves the box when a coordinate input is typed (two-way sync)', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([plantilla({ nomForm: 'Con Medidas' })]);
    vi.mocked(adminCatalogosApi.getPlantillaMedidas).mockResolvedValue(medidasFixture());

    renderWithProviders();

    await screen.findByText('Con Medidas');
    fireEvent.click(screen.getByRole('button', { name: /medidas/i }));
    await screen.findByText('Medidas de la plantilla');

    const box = screen.getByTestId('medida-box-FIRMA');
    const escala = 600 / 612;
    // FIRMA starts at x=50 → left = 50 * escala.
    expect(parseFloat(box.style.left)).toBeCloseTo(50 * escala, 1);

    fireEvent.change(screen.getByLabelText('X FIRMA'), { target: { value: '120' } });

    // Typing a number repositions the box: left now reflects x=120.
    expect(parseFloat(box.style.left)).toBeCloseTo(120 * escala, 1);
  });

  it('shows a resilient fallback notice when the background PDF fails to load', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([plantilla({ nomForm: 'Con Medidas' })]);
    vi.mocked(adminCatalogosApi.getPlantillaMedidas).mockResolvedValue(medidasFixture());
    vi.mocked(adminCatalogosApi.getPlantillaPdf).mockRejectedValue(new Error('503 OnlyOffice down'));

    renderWithProviders();

    await screen.findByText('Con Medidas');
    fireEvent.click(screen.getByRole('button', { name: /medidas/i }));
    await screen.findByText('Medidas de la plantilla');

    // The notice appears and the editor stays usable (boxes still rendered).
    expect(await screen.findByText(/no se pudo cargar la vista del documento/i)).toBeInTheDocument();
    expect(screen.getByTestId('medida-box-FIRMA')).toBeInTheDocument();
    expect(screen.getByLabelText('X FIRMA')).toBeInTheDocument();
  });

  it('saves the six visible medidas, sending QRFIRMA with its edited size', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([plantilla({ nomForm: 'Con Medidas' })]);
    vi.mocked(adminCatalogosApi.getPlantillaMedidas).mockResolvedValue([
      { idForplaMed: 1, objeto: 'AUTORIZACION', x: 100, y: 170, ancho: 1, alto: 1 },
      { idForplaMed: 2, objeto: 'NUMERO', x: 100, y: 130, ancho: 0, alto: 0 },
      { idForplaMed: 3, objeto: 'FIRMA', x: 50, y: 20, ancho: 450, alto: 50 },
      { idForplaMed: 4, objeto: 'QR', x: 0, y: 0, ancho: 0, alto: 0 },
      { idForplaMed: 5, objeto: 'FOTOFIRMA', x: 50, y: 50, ancho: 0, alto: 0 },
      { idForplaMed: 6, objeto: 'QRFIRMA', x: 1, y: 1, ancho: 0, alto: 0 },
      { idForplaMed: 7, objeto: 'FIRMAGOBx', x: 0, y: 0, ancho: 0, alto: 0 },
    ]);
    vi.mocked(adminCatalogosApi.updatePlantillaMedidas).mockResolvedValue(undefined);

    renderWithProviders();

    await screen.findByText('Con Medidas');
    fireEvent.click(screen.getByRole('button', { name: /medidas/i }));
    await screen.findByText('Medidas de la plantilla');

    fireEvent.change(screen.getByLabelText('X FIRMA'), { target: { value: '60' } });
    // QRFIRMA is editable but square-locked: typing only Ancho mirrors Alto (stays square).
    fireEvent.change(screen.getByLabelText('Ancho QRFIRMA'), { target: { value: '150' } });
    expect(screen.getByLabelText('Alto QRFIRMA')).toHaveValue(150);
    fireEvent.click(screen.getByRole('button', { name: /guardar/i }));

    await vi.waitFor(() => expect(adminCatalogosApi.updatePlantillaMedidas).toHaveBeenCalledTimes(1));
    const [codForm, items] = vi.mocked(adminCatalogosApi.updatePlantillaMedidas).mock.calls[0];
    expect(codForm).toBe('{"tipo":"T","nt":5,"nc":0,"ns":0}');
    // Only the six visible rows travel; the backend touches only what it receives (QR untouched).
    expect(items).toHaveLength(6);
    expect(items.find((i) => i.idForplaMed === 4)).toBeUndefined();
    expect(items.find((i) => i.idForplaMed === 3)).toEqual({ idForplaMed: 3, x: 60, y: 20, ancho: 450, alto: 50 });
    expect(items.find((i) => i.idForplaMed === 6)).toEqual({ idForplaMed: 6, x: 1, y: 1, ancho: 150, alto: 150 });
  });

  it('shows the association as read-only when editing', async () => {
    vi.mocked(adminCatalogosApi.listSeForpla).mockResolvedValue([
      plantilla({ codForm: '{"tipo":"C","nt":0,"nc":12,"ns":0}', nomForm: 'Editable', tipoCod: 12, obsForm: 'obs previa' }),
    ]);

    renderWithProviders();

    await screen.findByText('Editable');
    fireEvent.click(screen.getByRole('button', { name: /^editar$/i }));

    // Association is informative only: no selects rendered in edit mode.
    expect(screen.queryByLabelText('Asociar a')).not.toBeInTheDocument();
    expect(screen.getAllByText('Categoría: Documentos Internos').length).toBeGreaterThan(0);
    expect(screen.getByText(/reemplazar/i)).toBeInTheDocument();
    expect(screen.getByLabelText('Observación')).toHaveValue('obs previa');
  });
});
