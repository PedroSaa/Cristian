import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import AdminContadoresNumeracionPage from './AdminContadoresNumeracionPage';
import * as numeracionApi from '../../lib/api/admin/numeracionApi';
import { useHasPermission } from '../../hooks/usePermissions';

vi.mock('../../lib/api/admin/numeracionApi', () => ({
  listCounters: vi.fn(),
  createCounter: vi.fn(),
  setCounterValue: vi.fn(),
  incrementCounter: vi.fn(),
  deactivateCounter: vi.fn(),
  reactivateCounter: vi.fn(),
}));

vi.mock('../../hooks/usePermissions', () => ({
  useHasPermission: vi.fn(),
}));

vi.mock('../../lib/api/admin/adminCatalogosApi', () => ({
  listSeFordoc: vi.fn(),
}));

import * as adminCatalogosApi from '../../lib/api/admin/adminCatalogosApi';

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
      <AdminContadoresNumeracionPage />
    </QueryClientProvider>,
  );
}

describe('AdminContadoresNumeracionPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(useHasPermission).mockReturnValue(true);
    vi.mocked(numeracionApi.listCounters).mockResolvedValue({
      items: [],
      total: 0,
      page: 1,
      totalPaginas: 1,
    });
    vi.mocked(adminCatalogosApi.listSeFordoc).mockResolvedValue([
      { tipoCod: 3, tipoDesc: 'Carta' },
      { tipoCod: 12, tipoDesc: 'Memo' },
    ] as never);
  });

  it('offers fixed DF-type options and document formats as selects in the create form', async () => {
    const user = userEvent.setup();
    renderWithProviders();

    await screen.findByText('No hay contadores registrados.');
    await user.click(screen.getByRole('button', { name: /nuevo contador/i }));

    // Tipo DF: closed select with the 4 legacy flow types + empty option
    const dfSelect = await screen.findByLabelText(/tipo df/i);
    expect(dfSelect.tagName).toBe('SELECT');
    const dfOptions = within(dfSelect as HTMLElement).getAllByRole('option').map((o) => o.textContent);
    expect(dfOptions).toEqual(
      expect.arrayContaining([expect.stringMatching(/Interno/), expect.stringMatching(/Recibido/), expect.stringMatching(/Enviado/), expect.stringMatching(/Tareas/)]),
    );

    // Tipo: select fed by the document-format catalog + "all formats" option
    const tipoSelect = screen.getByLabelText(/^tipo$/i);
    expect(tipoSelect.tagName).toBe('SELECT');
    const tipoOptions = within(tipoSelect as HTMLElement).getAllByRole('option').map((o) => o.textContent);
    expect(tipoOptions).toEqual(
      expect.arrayContaining([expect.stringMatching(/Carta/), expect.stringMatching(/Memo/)]),
    );
  });

  it('submits the selected format code and DF type in the create payload', async () => {
    const user = userEvent.setup();
    vi.mocked(numeracionApi.createCounter).mockResolvedValue({
      id: 'counter-id', codigoContador: 'DOCS', orgDepCod: 'GLOBAL', nivelCod: null,
      tipoCod: 12, dfTipo: 'DOCINTER', periodicidad: 'ANUAL', periodoRef: '2026',
      ultimoValor: 0, activo: true, createdAt: '2026-07-10T00:00:00Z', updatedAt: '2026-07-10T00:00:00Z',
    });
    renderWithProviders();

    await screen.findByText('No hay contadores registrados.');
    await user.click(screen.getByRole('button', { name: /nuevo contador/i }));

    await user.type(screen.getByLabelText(/código del contador/i), 'DOCS');
    await user.type(screen.getByLabelText(/código organización/i), 'GLOBAL');
    await user.selectOptions(await screen.findByLabelText(/tipo df/i), 'DOCINTER');
    await user.selectOptions(screen.getByLabelText(/^tipo$/i), '12');
    await user.selectOptions(screen.getByLabelText(/periodicidad/i), 'ANUAL');
    await user.click(screen.getByRole('button', { name: /^crear$/i }));

    expect(numeracionApi.createCounter).toHaveBeenCalledWith({
      codigoContador: 'DOCS',
      orgDepCod: 'GLOBAL',
      tipoCod: 12,
      dfTipo: 'DOCINTER',
      nivelCod: undefined,
      periodicidad: 'ANUAL',
      valorInicial: 0,
    });
  });

  it('opens the create flow and submits the counter payload', async () => {
    const user = userEvent.setup();
    vi.mocked(numeracionApi.createCounter).mockResolvedValue({
      id: 'counter-id',
      codigoContador: 'DOC-001',
      orgDepCod: 'ORG-01',
      nivelCod: null,
      tipoCod: 0,
      dfTipo: null,
      periodicidad: 'CONTINUO',
      periodoRef: null,
      ultimoValor: 0,
      activo: true,
      createdAt: '2026-05-16T00:00:00Z',
      updatedAt: '2026-05-16T00:00:00Z',
    });

    renderWithProviders();

    await screen.findByText('No hay contadores registrados.');
    await user.click(screen.getByRole('button', { name: /nuevo contador/i }));

    await user.type(screen.getByLabelText(/código del contador/i), 'DOC-001');
    await user.type(screen.getByLabelText(/código organización/i), 'ORG-01');
    await user.selectOptions(screen.getByLabelText(/periodicidad/i), 'ANUAL');
    await user.click(screen.getByRole('button', { name: /^crear$/i }));

    expect(numeracionApi.createCounter).toHaveBeenCalledWith({
      codigoContador: 'DOC-001',
      orgDepCod: 'ORG-01',
      tipoCod: 0,
      dfTipo: undefined,
      nivelCod: undefined,
      periodicidad: 'ANUAL',
      valorInicial: 0,
    });
    expect(useHasPermission).toHaveBeenCalledWith('admin.numeracion.editar');
  });

  it('hides edit actions when the user only has catalog edit permission', async () => {
    vi.mocked(useHasPermission).mockImplementation((permission) => permission === 'admin.catalogos.editar');
    vi.mocked(numeracionApi.listCounters).mockResolvedValue({
      items: [{
        id: 'counter-id',
        codigoContador: 'DOC-001',
        orgDepCod: 'ORG-01',
        nivelCod: null,
        tipoCod: 0,
        dfTipo: null,
        periodicidad: 'CONTINUO',
        periodoRef: null,
        ultimoValor: 10,
        activo: true,
      }],
      total: 1,
      page: 1,
      totalPaginas: 1,
    });

    renderWithProviders();

    expect(await screen.findByText('DOC-001')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /nuevo contador/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: '+1' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Valor' })).not.toBeInTheDocument();
    expect(useHasPermission).toHaveBeenCalledWith('admin.numeracion.editar');
  });

  it('keeps row actions available by accessible label and calls their handlers', async () => {
    const user = userEvent.setup();
    vi.mocked(numeracionApi.listCounters).mockResolvedValue({
      items: [{
        id: 'counter-id',
        codigoContador: 'DOC-001',
        orgDepCod: 'ORG-01',
        nivelCod: null,
        tipoCod: 0,
        dfTipo: null,
        periodicidad: 'CONTINUO',
        periodoRef: null,
        ultimoValor: 7,
        activo: true,
      }],
      total: 1,
      page: 1,
      totalPaginas: 1,
    });
    vi.mocked(numeracionApi.incrementCounter).mockResolvedValue({ valor: 11 });
    vi.mocked(numeracionApi.deactivateCounter).mockResolvedValue(undefined);

    renderWithProviders();

    await screen.findByText('DOC-001');
    expect(screen.getByRole('button', { name: '+1' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Valor' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Desactivar' })).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: '+1' }));
    expect(vi.mocked(numeracionApi.incrementCounter).mock.calls[0][0]).toBe('counter-id');

    await user.click(screen.getByRole('button', { name: 'Valor' }));
    expect(await screen.findByText('Contador:')).toBeInTheDocument();
    expect(screen.getByDisplayValue('7')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Cancelar' }));

    // Desactivar ahora pide confirmación antes de llamar al handler.
    await user.click(screen.getByRole('button', { name: 'Desactivar' }));
    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: 'Desactivar' }));
    await waitFor(() => expect(numeracionApi.deactivateCounter).toHaveBeenCalledWith('counter-id'));
  });
});
