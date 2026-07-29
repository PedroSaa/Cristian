import http from '../../http';

export type TipoAccionFlujo = 'Autorizar' | 'Firmar' | 'Revisar' | 'Visar';
export type ResponsableFlujoTipo = 'Usuario' | 'Rol' | 'Departamento';

/** Ordered step as returned by the backend (GET / PUT response). */
export interface PlantillaFlujoPaso {
  id: string;
  orden: number;
  tipoAccion: TipoAccionFlujo;
  responsableTipo: ResponsableFlujoTipo;
  responsableId: string;
  responsableNombre: string | null;
  obligatorio: boolean;
}

/** Single step of the save payload (server assigns id and resolves nombre). */
export interface GuardarFlujoPaso {
  orden: number;
  tipoAccion: TipoAccionFlujo;
  responsableTipo: ResponsableFlujoTipo;
  responsableId: string;
  obligatorio: boolean;
}

export interface GuardarFlujoRequest {
  pasos: GuardarFlujoPaso[];
}

/** GET the ordered workflow of a template. Returns `[]` when none is configured. */
export async function getPlantillaFlujo(codForm: string): Promise<PlantillaFlujoPaso[]> {
  const { data } = await http.get<PlantillaFlujoPaso[]>(
    `/admin/catalogos/plantillas/${encodeURIComponent(codForm)}/flujo`,
  );
  return data;
}

/**
 * PUT the full workflow (replaces everything). Returns the resolved ordered array
 * (same shape as GET), with server-assigned ids and responsable names.
 */
export async function guardarPlantillaFlujo(
  codForm: string,
  pasos: GuardarFlujoPaso[],
): Promise<PlantillaFlujoPaso[]> {
  const body: GuardarFlujoRequest = { pasos };
  const { data } = await http.put<PlantillaFlujoPaso[]>(
    `/admin/catalogos/plantillas/${encodeURIComponent(codForm)}/flujo`,
    body,
  );
  return data;
}
