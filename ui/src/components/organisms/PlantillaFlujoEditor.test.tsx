import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import PlantillaFlujoEditor from './PlantillaFlujoEditor';
import { ToastProvider } from '../../contexts/ToastContext';
import * as flujoApi from '../../lib/api/admin/plantillaFlujoApi';
import * as usuariosApi from '../../lib/api/admin/adminUsuariosApi';
import * as rolesApi from '../../lib/api/admin/adminRolesApi';
import * as catalogosApi from '../../lib/api/catalogos';
import type { PlantillaFlujoPaso } from '../../lib/api/admin/plantillaFlujoApi';

vi.mock('../../lib/api/admin/plantillaFlujoApi', () => ({
  getPlantillaFlujo: vi.fn(),
  guardarPlantillaFlujo: vi.fn(),
}));
vi.mock('../../lib/api/admin/adminUsuariosApi', () => ({ getUsuarios: vi.fn() }));
vi.mock('../../lib/api/admin/adminRolesApi', () => ({ getRoles: vi.fn() }));
vi.mock('../../lib/api/catalogos', () => ({ getDepartamentosCatalogo: vi.fn() }));

const COD_FORM = '{"tipo":"T","nt":5,"nc":0,"ns":0}';

function existingPaso(overrides: Partial<PlantillaFlujoPaso> = {}): PlantillaFlujoPaso {
  return {
    id: 'paso-guid-1',
    orden: 1,
    tipoAccion: 'Autorizar',
    responsableTipo: 'Usuario',
    responsableId: 'u1',
    responsableNombre: 'Ada Lovelace',
    obligatorio: true,
    ...overrides,
  };
}

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
}

function renderEditor(overrides: Partial<React.ComponentProps<typeof PlantillaFlujoEditor>> = {}) {
  const onClose = vi.fn();
  render(
    <QueryClientProvider client={createTestQueryClient()}>
      <ToastProvider>
        <PlantillaFlujoEditor
          open
          codForm={COD_FORM}
          nomForm="Plantilla de Prueba"
          canEdit
          onClose={onClose}
          {...overrides}
        />
      </ToastProvider>
    </QueryClientProvider>,
  );
  return { onClose };
}

beforeEach(() => {
  vi.clearAllMocks();
  vi.mocked(flujoApi.getPlantillaFlujo).mockResolvedValue([]);
  vi.mocked(flujoApi.guardarPlantillaFlujo).mockResolvedValue([]);
  vi.mocked(usuariosApi.getUsuarios).mockResolvedValue({
    items: [
      { id: 'u1', nombreCompleto: 'Ada Lovelace', email: 'ada@x.io', rut: null, rol: 'Admin', departamentoId: null, departamentoNombre: null, activo: true, creadoEn: '', rolId: null },
      { id: 'u2', nombreCompleto: 'Alan Turing', email: 'alan@x.io', rut: null, rol: 'Admin', departamentoId: null, departamentoNombre: null, activo: true, creadoEn: '', rolId: null },
    ],
    total: 2,
    page: 1,
    totalPaginas: 1,
  });
  vi.mocked(rolesApi.getRoles).mockResolvedValue([
    { id: 'rol-1', nombre: 'Aprobador', descripcion: null, esSistema: false },
  ]);
  vi.mocked(catalogosApi.getDepartamentosCatalogo).mockResolvedValue([
    { id: 'dept-1', nombre: 'Finanzas', codigo: 'FIN' },
  ]);
});

describe('PlantillaFlujoEditor', () => {
  it('preloads and shows the existing workflow steps', async () => {
    vi.mocked(flujoApi.getPlantillaFlujo).mockResolvedValue([
      existingPaso(),
      existingPaso({ id: 'paso-guid-2', orden: 2, tipoAccion: 'Firmar', responsableTipo: 'Departamento', responsableId: 'dept-1', responsableNombre: 'Finanzas' }),
    ]);

    renderEditor();

    // Two step rows loaded.
    expect(await screen.findByTestId('flujo-paso-0')).toBeInTheDocument();
    expect(screen.getByTestId('flujo-paso-1')).toBeInTheDocument();

    // Action selects reflect the loaded values.
    expect((screen.getByLabelText('Acción del paso 1') as HTMLSelectElement).value).toBe('Autorizar');
    expect((screen.getByLabelText('Acción del paso 2') as HTMLSelectElement).value).toBe('Firmar');
    expect((screen.getByLabelText('Tipo de responsable del paso 2') as HTMLSelectElement).value).toBe('Departamento');

    // The responsible searchable combobox displays the resolved name once catalogs arrive.
    await waitFor(() => expect((screen.getByLabelText('Responsable del paso 1') as HTMLInputElement).value).toBe('Ada Lovelace'));
    await waitFor(() => expect((screen.getByLabelText('Responsable del paso 2') as HTMLInputElement).value).toBe('Finanzas'));
  });

  it('adds a step, sets action + type + responsible, and saves the correct payload', async () => {
    const user = userEvent.setup();
    renderEditor();

    await screen.findByText(/no hay pasos configurados/i);
    await user.click(screen.getByRole('button', { name: /agregar paso/i }));

    // New step defaults to Autorizar / Usuario. Switch it to a Department responsible.
    await user.selectOptions(screen.getByLabelText('Tipo de responsable del paso 1'), 'Departamento');
    // Responsable is a searchable combobox: focus to open, then pick the option.
    await user.click(screen.getByLabelText('Responsable del paso 1'));
    await user.click(await screen.findByRole('option', { name: 'Finanzas' }));
    await user.selectOptions(screen.getByLabelText('Acción del paso 1'), 'Firmar');

    await user.click(screen.getByRole('button', { name: /^guardar$/i }));

    await waitFor(() => expect(flujoApi.guardarPlantillaFlujo).toHaveBeenCalledTimes(1));
    const [codForm, pasos] = vi.mocked(flujoApi.guardarPlantillaFlujo).mock.calls[0];
    expect(codForm).toBe(COD_FORM);
    expect(pasos).toEqual([
      { orden: 1, tipoAccion: 'Firmar', responsableTipo: 'Departamento', responsableId: 'dept-1', obligatorio: true },
    ]);
  });

  it('reordering changes the order sent on save', async () => {
    const user = userEvent.setup();
    vi.mocked(flujoApi.getPlantillaFlujo).mockResolvedValue([
      existingPaso({ id: 'p1', orden: 1, tipoAccion: 'Autorizar', responsableTipo: 'Rol', responsableId: 'rol-1' }),
      existingPaso({ id: 'p2', orden: 2, tipoAccion: 'Firmar', responsableTipo: 'Departamento', responsableId: 'dept-1' }),
    ]);

    renderEditor();

    const firstRow = await screen.findByTestId('flujo-paso-0');
    // Move the first step down so the department step becomes step 1.
    await user.click(within(firstRow).getByRole('button', { name: 'Bajar' }));

    await user.click(screen.getByRole('button', { name: /^guardar$/i }));

    await waitFor(() => expect(flujoApi.guardarPlantillaFlujo).toHaveBeenCalledTimes(1));
    const [, pasos] = vi.mocked(flujoApi.guardarPlantillaFlujo).mock.calls[0];
    expect(pasos).toEqual([
      { orden: 1, tipoAccion: 'Firmar', responsableTipo: 'Departamento', responsableId: 'dept-1', obligatorio: true },
      { orden: 2, tipoAccion: 'Autorizar', responsableTipo: 'Rol', responsableId: 'rol-1', obligatorio: true },
    ]);
  });

  it('does not save a step that has no responsible selected', async () => {
    const user = userEvent.setup();
    renderEditor();

    await screen.findByText(/no hay pasos configurados/i);
    await user.click(screen.getByRole('button', { name: /agregar paso/i }));

    // Leave the responsible empty and try to save.
    await user.click(screen.getByRole('button', { name: /^guardar$/i }));

    expect(await screen.findByText(/seleccione un responsable para este paso/i)).toBeInTheDocument();
    expect(flujoApi.guardarPlantillaFlujo).not.toHaveBeenCalled();
  });
});
