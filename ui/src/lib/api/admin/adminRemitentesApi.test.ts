import { describe, it, expect, vi, beforeEach } from 'vitest';

describe('adminRemitentesApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('lists tipos from the remitentes legacy endpoint', async () => {
    const httpModule = await import('../../http');
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({ data: [] });

    const api = await import('./adminRemitentesApi');
    await api.listSeremTipos();

    expect(getSpy).toHaveBeenCalledWith('/admin/remitentes-legado/tipos');
  });

  it('creates remitentes using POST with the legacy route', async () => {
    const httpModule = await import('../../http');
    const postSpy = vi.spyOn(httpModule.default, 'post').mockResolvedValue({ data: {} });

    const api = await import('./adminRemitentesApi');
    await api.createSerem({ remCod: 'REM-01', remTipo: 'MUNI', remNomb: 'Municipalidad' });

    expect(postSpy).toHaveBeenCalledWith('/admin/remitentes-legado', {
      remCod: 'REM-01',
      remTipo: 'MUNI',
      remNomb: 'Municipalidad',
    });
  });

  it('deletes remitentes using DELETE', async () => {
    const httpModule = await import('../../http');
    const deleteSpy = vi.spyOn(httpModule.default, 'delete').mockResolvedValue({ data: undefined });

    const api = await import('./adminRemitentesApi');
    await api.deleteSerem('REM-01');

    expect(deleteSpy).toHaveBeenCalledWith('/admin/remitentes-legado/REM-01');
  });
});
