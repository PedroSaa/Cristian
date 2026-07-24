import { describe, it, expect, vi, beforeEach } from 'vitest';

describe('adminCatalogosApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('lists categorías from the legacy catalogs endpoint', async () => {
    const httpModule = await import('../../http');
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({ data: [] });

    const api = await import('./adminCatalogosApi');
    await api.listCatalogoCategorias();

    expect(getSpy).toHaveBeenCalledWith('/admin/catalogos/categorias');
  });

  it('creates an acción de tarea using POST', async () => {
    const httpModule = await import('../../http');
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: {} });

    const api = await import('./adminCatalogosApi');
    await api.createSeTiptar({ dftaccion: 'DERIVAR', dftacdesc: 'Derivar documento' });

    expect(postSpy).toHaveBeenCalledWith('/admin/catalogos/acciones-tarea', {
      dftaccion: 'DERIVAR',
      dftacdesc: 'Derivar documento',
    });
  });

  it('deletes correlativos through the expected REST path', async () => {
    const httpModule = await import('../../http');
    const deleteSpy = vi.spyOn(httpModule.default, 'delete').mockResolvedValue({ data: undefined });

    const api = await import('./adminCatalogosApi');
    await api.deleteSeCorfor('TIPO-01');

    expect(deleteSpy).toHaveBeenCalledWith('/admin/catalogos/correlativos/TIPO-01');
  });
});
