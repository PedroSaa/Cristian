import http from '../../http';

export type Periodicidad = 'CONTINUO' | 'ANUAL' | 'MENSUAL';
export type MomentoGeneracion = 'AL_INGRESAR' | 'AL_FIRMAR' | 'AMBOS' | 'MANUAL';

/** Política de conteo de la plantilla (ejes + reinicio + momento + formato del número). */
export interface PlantillaPolicy {
  porOrganismo: boolean;
  porTipoDocumento: boolean;     // Recibido / Enviado / Interno
  porFormatoDocumento: boolean;  // Memo / Informe / Contrato / …
  periodicidad: Periodicidad;
  momentoGeneracion: MomentoGeneracion;
  rellenoCeros: number;
  valorInicial: number;
}

export interface PlantillaNumeracionDto extends PlantillaPolicy {
  id: number;
  descripcion: string;
  patron: string | null;
  activo: boolean;
}

// El Id lo autogenera el servidor (máx + 1); el formulario ya no lo pide.
export interface CreatePlantillaData extends PlantillaPolicy {
  descripcion: string;
  patron: string;
}

export interface UpdatePlantillaData extends PlantillaPolicy {
  descripcion: string;
  patron: string;
}

export async function listPlantillasNumeracion(soloActivos?: boolean): Promise<PlantillaNumeracionDto[]> {
  const params = soloActivos === undefined ? '' : `?soloActivos=${soloActivos}`;
  const { data } = await http.get<PlantillaNumeracionDto[]>(`/admin/numeracion/plantillas${params}`);
  return data;
}

export async function createPlantillaNumeracion(body: CreatePlantillaData): Promise<PlantillaNumeracionDto> {
  const { data } = await http.post<PlantillaNumeracionDto>('/admin/numeracion/plantillas', body);
  return data;
}

export async function updatePlantillaNumeracion(id: number, body: UpdatePlantillaData): Promise<PlantillaNumeracionDto> {
  const { data } = await http.put<PlantillaNumeracionDto>(`/admin/numeracion/plantillas/${id}`, body);
  return data;
}

export async function togglePlantillaNumeracion(id: number): Promise<void> {
  await http.put(`/admin/numeracion/plantillas/${id}/toggle`);
}

/** Define esta plantilla como la activa del sistema (única activa). */
export async function setPlantillaActiva(id: number): Promise<void> {
  await http.put(`/admin/numeracion/plantillas/${id}/activar`);
}

export async function deletePlantillaNumeracion(id: number): Promise<void> {
  await http.delete(`/admin/numeracion/plantillas/${id}`);
}

export interface TokenNumeracion {
  token: string;
  descripcion: string;
  ejemplo: string;
}

/** Catálogo de tokens válidos para construir patrones. */
export async function getTokensNumeracion(): Promise<TokenNumeracion[]> {
  const { data } = await http.get<TokenNumeracion[]>('/admin/numeracion/plantillas/tokens');
  return data;
}
