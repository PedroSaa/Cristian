import axios from 'axios';
import http from '../../http';
import type {
  FirmaUsuarioMetadata,
  GuardarFirmaUsuarioRequest,
} from './firmaUsuarioApi';

/**
 * Self-service signature API. These endpoints resolve the user from the auth
 * token ([Authorize] only, no admin permission) and operate on the CURRENT
 * user's signature. They mirror the admin API shape (same DTOs, same
 * 404 → null contract for the image) but hit `/perfil/firma`.
 */
const PERFIL_FIRMA = '/perfil/firma';

/** GET the current user's signature metadata. Returns `tieneFirma:false` (200) when none. */
export async function getMiFirmaMetadata(): Promise<FirmaUsuarioMetadata> {
  const { data } = await http.get<FirmaUsuarioMetadata>(PERFIL_FIRMA);
  return data;
}

/**
 * GET the current user's signature image as a Blob. Resolves to `null` when
 * there is no image (404).
 */
export async function getMiFirmaImagen(): Promise<Blob | null> {
  try {
    const { data } = await http.get<Blob>(`${PERFIL_FIRMA}/imagen`, {
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

/** PUT upsert the current user's signature (create or partial replace). Returns fresh metadata. */
export async function guardarMiFirma(
  body: GuardarFirmaUsuarioRequest,
): Promise<FirmaUsuarioMetadata> {
  const { data } = await http.put<FirmaUsuarioMetadata>(PERFIL_FIRMA, body);
  return data;
}

/** DELETE the current user's signature. */
export async function eliminarMiFirma(): Promise<void> {
  await http.delete(PERFIL_FIRMA);
}
