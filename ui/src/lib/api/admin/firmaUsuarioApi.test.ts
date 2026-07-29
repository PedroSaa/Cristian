import { describe, it, expect, vi, beforeEach } from 'vitest';

const USER_ID = 'aaaaaaaa-bbbb-cccc-dddd-eeeeeeee0001';

describe('firmaUsuarioApi', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it('getFirmaMetadata calls GET on the firma endpoint', async () => {
    const httpModule = await import('../../http');
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({
      data: { usuarioId: USER_ID, tieneFirma: false, tieneClave: false, sigla: null, contentType: null, tamano: 0, creadoEn: null, actualizadoEn: null },
    });

    const api = await import('./firmaUsuarioApi');
    const result = await api.getFirmaMetadata(USER_ID);

    expect(getSpy).toHaveBeenCalledWith(`/admin/usuarios/${USER_ID}/firma`);
    expect(result.tieneFirma).toBe(false);
  });

  it('getFirmaImagen requests the image as a blob', async () => {
    const httpModule = await import('../../http');
    const blob = new Blob(['x'], { type: 'image/png' });
    const getSpy = vi.spyOn(httpModule.default, 'get').mockResolvedValue({ data: blob });

    const api = await import('./firmaUsuarioApi');
    const result = await api.getFirmaImagen(USER_ID);

    expect(getSpy).toHaveBeenCalledWith(
      `/admin/usuarios/${USER_ID}/firma/imagen`,
      { responseType: 'blob' },
    );
    expect(result).toBe(blob);
  });

  it('getFirmaImagen resolves to null on a 404', async () => {
    const httpModule = await import('../../http');
    vi.spyOn(httpModule.default, 'get').mockRejectedValue({
      isAxiosError: true,
      response: { status: 404 },
    });

    const api = await import('./firmaUsuarioApi');
    const result = await api.getFirmaImagen(USER_ID);

    expect(result).toBeNull();
  });

  it('getFirmaImagen rethrows non-404 errors', async () => {
    const httpModule = await import('../../http');
    vi.spyOn(httpModule.default, 'get').mockRejectedValue({
      isAxiosError: true,
      response: { status: 500 },
    });

    const api = await import('./firmaUsuarioApi');
    await expect(api.getFirmaImagen(USER_ID)).rejects.toBeTruthy();
  });

  it('guardarFirma sends a PUT with the upsert body', async () => {
    const httpModule = await import('../../http');
    const putSpy = vi.spyOn(httpModule.default, 'put').mockResolvedValue({ data: {} });

    const api = await import('./firmaUsuarioApi');
    await api.guardarFirma(USER_ID, {
      imagenBase64: 'QUJD',
      contentType: 'image/png',
      clave: '1234',
      sigla: 'JPG',
    });

    expect(putSpy).toHaveBeenCalledWith(`/admin/usuarios/${USER_ID}/firma`, {
      imagenBase64: 'QUJD',
      contentType: 'image/png',
      clave: '1234',
      sigla: 'JPG',
    });
  });

  it('eliminarFirma sends a DELETE', async () => {
    const httpModule = await import('../../http');
    const deleteSpy = vi.spyOn(httpModule.default, 'delete').mockResolvedValue({ data: undefined });

    const api = await import('./firmaUsuarioApi');
    await api.eliminarFirma(USER_ID);

    expect(deleteSpy).toHaveBeenCalledWith(`/admin/usuarios/${USER_ID}/firma`);
  });
});
