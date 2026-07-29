import { describe, it, expect, vi, beforeEach } from 'vitest';

describe('perfilFirmaApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('getMiFirmaMetadata calls GET on the /perfil/firma endpoint', async () => {
    const httpModule = await import('../../http');
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({
      data: { usuarioId: 'me', tieneFirma: false, tieneClave: false, sigla: null, contentType: null, tamano: 0, creadoEn: null, actualizadoEn: null },
    });

    const api = await import('./perfilFirmaApi');
    const result = await api.getMiFirmaMetadata();

    expect(getSpy).toHaveBeenCalledWith('/perfil/firma');
    expect(result.tieneFirma).toBe(false);
  });

  it('getMiFirmaImagen requests the image as a blob', async () => {
    const httpModule = await import('../../http');
    const blob = new Blob(['x'], { type: 'image/png' });
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({ data: blob });

    const api = await import('./perfilFirmaApi');
    const result = await api.getMiFirmaImagen();

    expect(getSpy).toHaveBeenCalledWith('/perfil/firma/imagen', { responseType: 'blob' });
    expect(result).toBe(blob);
  });

  it('getMiFirmaImagen resolves to null on a 404', async () => {
    const httpModule = await import('../../http');
    vi.spyOn(httpModule.default, 'get').mockRejectedValue({
      isAxiosError: true,
      response: { status: 404 },
    });

    const api = await import('./perfilFirmaApi');
    const result = await api.getMiFirmaImagen();

    expect(result).toBeNull();
  });

  it('getMiFirmaImagen rethrows non-404 errors', async () => {
    const httpModule = await import('../../http');
    vi.spyOn(httpModule.default, 'get').mockRejectedValue({
      isAxiosError: true,
      response: { status: 500 },
    });

    const api = await import('./perfilFirmaApi');
    await expect(api.getMiFirmaImagen()).rejects.toBeTruthy();
  });

  it('guardarMiFirma sends a PUT with the upsert body', async () => {
    const httpModule = await import('../../http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: {} });

    const api = await import('./perfilFirmaApi');
    await api.guardarMiFirma({
      imagenBase64: 'QUJD',
      contentType: 'image/png',
      clave: '1234',
      sigla: 'JPG',
    });

    expect(putSpy).toHaveBeenCalledWith('/perfil/firma', {
      imagenBase64: 'QUJD',
      contentType: 'image/png',
      clave: '1234',
      sigla: 'JPG',
    });
  });

  it('eliminarMiFirma sends a DELETE', async () => {
    const httpModule = await import('../../http');
    const deleteSpy = vi.spyOn(httpModule.default, 'delete').mockResolvedValue({ data: undefined });

    const api = await import('./perfilFirmaApi');
    await api.eliminarMiFirma();

    expect(deleteSpy).toHaveBeenCalledWith('/perfil/firma');
  });
});
