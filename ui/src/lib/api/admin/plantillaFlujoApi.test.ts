import { describe, it, expect, vi, beforeEach } from 'vitest';
import type { GuardarFlujoPaso, PlantillaFlujoPaso } from './plantillaFlujoApi';

// codForm with characters that must be URL-encoded (JSON association codes contain
// braces, quotes and commas), to prove the endpoint encodes it.
const COD_FORM = '{"tipo":"T","nt":5,"nc":0,"ns":0}';
const ENCODED = encodeURIComponent(COD_FORM);

const samplePaso: PlantillaFlujoPaso = {
  id: 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001',
  orden: 1,
  tipoAccion: 'Autorizar',
  responsableTipo: 'Departamento',
  responsableId: 'dept-guid-1',
  responsableNombre: 'Finanzas',
  obligatorio: true,
};

describe('plantillaFlujoApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('getPlantillaFlujo GETs the encoded flujo endpoint and returns the array', async () => {
    const httpModule = await import('../../http');
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({ data: [samplePaso] });

    const api = await import('./plantillaFlujoApi');
    const result = await api.getPlantillaFlujo(COD_FORM);

    expect(getSpy).toHaveBeenCalledWith(`/admin/catalogos/plantillas/${ENCODED}/flujo`);
    expect(result).toEqual([samplePaso]);
    expect(result[0].responsableNombre).toBe('Finanzas');
  });

  it('guardarPlantillaFlujo PUTs { pasos } to the encoded endpoint and returns the resolved array', async () => {
    const httpModule = await import('../../http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: [samplePaso] });

    const pasos: GuardarFlujoPaso[] = [
      {
        orden: 1,
        tipoAccion: 'Autorizar',
        responsableTipo: 'Departamento',
        responsableId: 'dept-guid-1',
        obligatorio: true,
      },
    ];

    const api = await import('./plantillaFlujoApi');
    const result = await api.guardarPlantillaFlujo(COD_FORM, pasos);

    expect(putSpy).toHaveBeenCalledWith(`/admin/catalogos/plantillas/${ENCODED}/flujo`, { pasos });
    expect(result).toEqual([samplePaso]);
  });
});
