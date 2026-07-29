import axios from 'axios';
import http from '../../http';

/**
 * Metadata of a user's signature. Mirrors the backend `FirmaUsuarioMetadataDto`.
 * When the user has no signature the endpoint returns `tieneFirma: false` (never 404).
 */
export interface FirmaUsuarioMetadata {
  usuarioId: string;
  tieneFirma: boolean;
  tieneClave: boolean;
  sigla: string | null;
  contentType: string | null;
  tamano: number;
  creadoEn: string | null;
  actualizadoEn: string | null;
}

export type FirmaContentType = 'image/png' | 'image/jpeg';

/**
 * Body for the upsert (PUT). Partial-update semantics — the backend PRESERVES what
 * is not resent:
 * - `imagenBase64`+`contentType`: optional. Omit to keep the existing image (required
 *   only when creating the first signature). Sent as base64 WITHOUT the `data:` prefix.
 * - `clave`: optional. Send only to set/replace the PIN; omitting keeps the stored one.
 * - `sigla`: applied as received (the form loads the current value, so no data loss).
 */
export interface GuardarFirmaUsuarioRequest {
  imagenBase64?: string;
  contentType?: FirmaContentType;
  clave?: string;
  sigla?: string;
}

const firmaBase = (usuarioId: string) => `/admin/usuarios/${usuarioId}/firma`;

/** GET signature metadata. Returns `tieneFirma:false` (200) when there is none. */
export async function getFirmaMetadata(usuarioId: string): Promise<FirmaUsuarioMetadata> {
  const { data } = await http.get<FirmaUsuarioMetadata>(firmaBase(usuarioId));
  return data;
}

/**
 * GET the signature image as a Blob. Resolves to `null` when there is no image (404).
 * This is a GET (cookie auth, CSRF-exempt).
 */
export async function getFirmaImagen(usuarioId: string): Promise<Blob | null> {
  try {
    const { data } = await http.get<Blob>(`${firmaBase(usuarioId)}/imagen`, {
      responseType: 'blob',
    });
    return data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 404) {
      return null;
    }
    throw error;
  }
}

/** PUT upsert the signature (create or full replace). Returns fresh metadata. */
export async function guardarFirma(
  usuarioId: string,
  body: GuardarFirmaUsuarioRequest,
): Promise<FirmaUsuarioMetadata> {
  const { data } = await http.put<FirmaUsuarioMetadata>(firmaBase(usuarioId), body);
  return data;
}

/** DELETE the signature. */
export async function eliminarFirma(usuarioId: string): Promise<void> {
  await http.delete(firmaBase(usuarioId));
}
