import http from '../../http';

export interface CatalogoCategoriaDto {
  catCod: number;
  catDesc: string;
  totalSubcategorias: number;
}

export interface CatalogoSubcategoriaDto {
  catCod: number;
  categoriaDesc: string;
  idSubcategoria: number;
  subcatNombre: string;
  subcatDescripcion: string | null;
}

export interface SeClasegDto {
  dfClasif: number;
  dfnClasif: string;
  dfdClasif: string;
}

export interface SeFormaEnvioDto {
  idFormaEnvio: number;
  formaEnvio: string;
}

export interface SeTiptarDto {
  dftaccion: string;
  dftacobsv: string | null;
  dftacdesc: string | null;
}

export interface SeFordocDto {
  tipoCod: number;
  tipoRec: number;
  tipoInt: number;
  tipoDesc: string;
  corrN: number;
  corrFecha: string;
  tipoEnv: number | null;
  seFordocVistaI: number;
  seFordocVistaE: number;
  seFordocVistaR: number;
  seFordocFormatoNum: string | null;
}

export interface SeForplaDto {
  codForm: string;
  usucod: string;
  tipoCod: number | null;
  nomForm: string;
  blobForm: string;
  sisForm: string;
  obsForm: string | null;
  extForm: string;
  alto: number | null;
  ancho: number | null;
}

export interface SeCorforDto {
  corrTip: string;
  corrNro: number;
  corrDes: string;
  corrFch: string;
}

export interface CrearCatalogoCategoriaData {
  catDesc: string;
}

export interface ActualizarCatalogoCategoriaData {
  catDesc: string;
}

export interface CrearCatalogoSubcategoriaData {
  catCod: number;
  subcatNombre: string;
  subcatDescripcion?: string | null;
}

export interface ActualizarCatalogoSubcategoriaData {
  subcatNombre: string;
  subcatDescripcion?: string | null;
}

export interface CrearSeClasegData {
  dfnClasif: string;
  dfdClasif: string;
}

export interface ActualizarSeClasegData {
  dfnClasif: string;
  dfdClasif: string;
}

export interface CrearSeFormaEnvioData {
  formaEnvio: string;
}

export interface ActualizarSeFormaEnvioData {
  formaEnvio: string;
}

export interface CrearSeTiptarData {
  dftaccion: string;
  dftacobsv?: string | null;
  dftacdesc?: string | null;
}

export interface ActualizarSeTiptarData {
  dftacobsv?: string | null;
  dftacdesc?: string | null;
}

export interface CrearSeFordocData {
  tipoRec: number;
  tipoInt: number;
  tipoDesc: string;
  corrN: number;
  tipoEnv?: number | null;
  seFordocVistaI?: number;
  seFordocVistaE?: number;
  seFordocVistaR?: number;
  seFordocFormatoNum?: string | null;
}

export interface ActualizarSeFordocData {
  tipoRec: number;
  tipoInt: number;
  tipoDesc: string;
  corrN: number;
  tipoEnv?: number | null;
  seFordocVistaI?: number;
  seFordocVistaE?: number;
  seFordocVistaR?: number;
  seFordocFormatoNum?: string | null;
}

export type SeForplaTipoSeleccion = 'T' | 'C' | 'S';

export interface CrearSeForplaData {
  tipoSeleccion: SeForplaTipoSeleccion;
  tipoCod?: number | null;
  catCod?: number | null;
  idSubcategoria?: number | null;
  fileName: string;
  blobForm: string;
  obsForm?: string | null;
}

export interface ActualizarSeForplaData {
  fileName?: string | null;
  blobForm?: string | null;
  obsForm?: string | null;
}

export interface CrearSeCorforData {
  corrTip: string;
  corrNro: number;
  corrDes: string;
  corrFch: string;
}

export interface ActualizarSeCorforData {
  corrNro: number;
  corrDes: string;
  corrFch: string;
}

export async function listCatalogoCategorias(): Promise<CatalogoCategoriaDto[]> {
  const { data } = await http.get<CatalogoCategoriaDto[]>('/admin/catalogos/categorias');
  return data;
}

export async function createCatalogoCategoria(body: CrearCatalogoCategoriaData): Promise<CatalogoCategoriaDto> {
  const { data } = await http.post<CatalogoCategoriaDto>('/admin/catalogos/categorias', body);
  return data;
}

export async function updateCatalogoCategoria(catCod: number, body: ActualizarCatalogoCategoriaData): Promise<void> {
  await http.put(`/admin/catalogos/categorias/${catCod}`, body);
}

export async function deleteCatalogoCategoria(catCod: number): Promise<void> {
  await http.delete(`/admin/catalogos/categorias/${catCod}`);
}

export async function listCatalogoSubcategorias(catCod?: number): Promise<CatalogoSubcategoriaDto[]> {
  const params: Record<string, unknown> = {};
  if (catCod !== undefined) params.catCod = catCod;
  const { data } = await http.get<CatalogoSubcategoriaDto[]>('/admin/catalogos/subcategorias', { params });
  return data;
}

export async function createCatalogoSubcategoria(body: CrearCatalogoSubcategoriaData): Promise<CatalogoSubcategoriaDto> {
  const { data } = await http.post<CatalogoSubcategoriaDto>('/admin/catalogos/subcategorias', body);
  return data;
}

export async function updateCatalogoSubcategoria(catCod: number, idSubcategoria: number, body: ActualizarCatalogoSubcategoriaData): Promise<void> {
  await http.put(`/admin/catalogos/subcategorias/${catCod}/${idSubcategoria}`, body);
}

export async function deleteCatalogoSubcategoria(catCod: number, idSubcategoria: number): Promise<void> {
  await http.delete(`/admin/catalogos/subcategorias/${catCod}/${idSubcategoria}`);
}

export async function listSeClaseg(): Promise<SeClasegDto[]> {
  const { data } = await http.get<SeClasegDto[]>('/admin/catalogos/clasificaciones');
  return data;
}

export async function createSeClaseg(body: CrearSeClasegData): Promise<SeClasegDto> {
  const { data } = await http.post<SeClasegDto>('/admin/catalogos/clasificaciones', body);
  return data;
}

export async function updateSeClaseg(dfClasif: number, body: ActualizarSeClasegData): Promise<void> {
  await http.put(`/admin/catalogos/clasificaciones/${dfClasif}`, body);
}

export async function deleteSeClaseg(dfClasif: number): Promise<void> {
  await http.delete(`/admin/catalogos/clasificaciones/${dfClasif}`);
}

export async function listSeFormaEnvio(): Promise<SeFormaEnvioDto[]> {
  const { data } = await http.get<SeFormaEnvioDto[]>('/admin/catalogos/formas-envio');
  return data;
}

export async function createSeFormaEnvio(body: CrearSeFormaEnvioData): Promise<SeFormaEnvioDto> {
  const { data } = await http.post<SeFormaEnvioDto>('/admin/catalogos/formas-envio', body);
  return data;
}

export async function updateSeFormaEnvio(idFormaEnvio: number, body: ActualizarSeFormaEnvioData): Promise<void> {
  await http.put(`/admin/catalogos/formas-envio/${idFormaEnvio}`, body);
}

export async function deleteSeFormaEnvio(idFormaEnvio: number): Promise<void> {
  await http.delete(`/admin/catalogos/formas-envio/${idFormaEnvio}`);
}

export async function listSeTiptar(): Promise<SeTiptarDto[]> {
  const { data } = await http.get<SeTiptarDto[]>('/admin/catalogos/acciones-tarea');
  return data;
}

export async function createSeTiptar(body: CrearSeTiptarData): Promise<SeTiptarDto> {
  const { data } = await http.post<SeTiptarDto>('/admin/catalogos/acciones-tarea', body);
  return data;
}

export async function updateSeTiptar(dftaccion: string, body: ActualizarSeTiptarData): Promise<void> {
  await http.put(`/admin/catalogos/acciones-tarea/${encodeURIComponent(dftaccion)}`, body);
}

export async function deleteSeTiptar(dftaccion: string): Promise<void> {
  await http.delete(`/admin/catalogos/acciones-tarea/${encodeURIComponent(dftaccion)}`);
}

export async function listSeFordoc(): Promise<SeFordocDto[]> {
  const { data } = await http.get<SeFordocDto[]>('/admin/catalogos/formatos-documento');
  return data;
}

export async function createSeFordoc(body: CrearSeFordocData): Promise<SeFordocDto> {
  const { data } = await http.post<SeFordocDto>('/admin/catalogos/formatos-documento', body);
  return data;
}

export async function updateSeFordoc(tipoCod: number, body: ActualizarSeFordocData): Promise<void> {
  await http.put(`/admin/catalogos/formatos-documento/${tipoCod}`, body);
}

export async function deleteSeFordoc(tipoCod: number): Promise<void> {
  await http.delete(`/admin/catalogos/formatos-documento/${tipoCod}`);
}

export async function listSeForpla(): Promise<SeForplaDto[]> {
  const { data } = await http.get<SeForplaDto[]>('/admin/catalogos/plantillas');
  return data;
}

export async function createSeForpla(body: CrearSeForplaData): Promise<SeForplaDto> {
  const { data } = await http.post<SeForplaDto>('/admin/catalogos/plantillas', body);
  return data;
}

export async function updateSeForpla(codForm: string, body: ActualizarSeForplaData): Promise<void> {
  await http.put(`/admin/catalogos/plantillas/${encodeURIComponent(codForm)}`, body);
}

export async function deleteSeForpla(codForm: string): Promise<void> {
  await http.delete(`/admin/catalogos/plantillas/${encodeURIComponent(codForm)}`);
}

export interface SeForplaMedidaDto {
  idForplaMed: number;
  objeto: string;
  x: number;
  y: number;
  ancho: number;
  alto: number;
}

export interface ActualizarSeForplaMedidaItem {
  idForplaMed: number;
  x: number;
  y: number;
  ancho: number;
  alto: number;
}

export async function getPlantillaMedidas(codForm: string): Promise<SeForplaMedidaDto[]> {
  const { data } = await http.get<SeForplaMedidaDto[]>(
    `/admin/catalogos/plantillas/${encodeURIComponent(codForm)}/medidas`,
  );
  return data;
}

export async function updatePlantillaMedidas(
  codForm: string,
  items: ActualizarSeForplaMedidaItem[],
): Promise<void> {
  await http.put(`/admin/catalogos/plantillas/${encodeURIComponent(codForm)}/medidas`, { items });
}

/**
 * Descarga la plantilla renderizada a PDF (via OnlyOffice) para usarla como fondo del
 * editor visual de medidas. Devuelve el Blob crudo; puede fallar con 500/503 si el
 * Document Server no está disponible, en cuyo caso el editor cae al fondo en blanco.
 */
export async function getPlantillaPdf(codForm: string): Promise<Blob> {
  const { data } = await http.get<Blob>(
    `/admin/catalogos/plantillas/${encodeURIComponent(codForm)}/pdf`,
    { responseType: 'blob' },
  );
  return data;
}

export async function listSeCorfor(): Promise<SeCorforDto[]> {
  const { data } = await http.get<SeCorforDto[]>('/admin/catalogos/correlativos');
  return data;
}

export async function createSeCorfor(body: CrearSeCorforData): Promise<SeCorforDto> {
  const { data } = await http.post<SeCorforDto>('/admin/catalogos/correlativos', body);
  return data;
}

export async function updateSeCorfor(corrTip: string, body: ActualizarSeCorforData): Promise<void> {
  await http.put(`/admin/catalogos/correlativos/${encodeURIComponent(corrTip)}`, body);
}

export async function deleteSeCorfor(corrTip: string): Promise<void> {
  await http.delete(`/admin/catalogos/correlativos/${encodeURIComponent(corrTip)}`);
}

export interface PlantillaEditorConfig {
  editorUrl: string;
  config: Record<string, unknown>;
}

export async function getPlantillaEditorConfig(codForm: string): Promise<PlantillaEditorConfig> {
  const { data } = await http.get<PlantillaEditorConfig>(
    `/admin/catalogos/plantillas/${encodeURIComponent(codForm)}/editor-config`,
  );
  return data;
}

/**
 * Fuerza el guardado inmediato del documento abierto en OnlyOffice (en vez de esperar
 * el guardado diferido al cerrar). `saved=false` indica que no había cambios que guardar.
 */
export async function forcePlantillaSave(codForm: string, key: string): Promise<{ saved: boolean }> {
  const { data } = await http.post<{ saved: boolean }>(
    `/admin/catalogos/plantillas/${encodeURIComponent(codForm)}/forcesave`,
    null,
    { params: { key } },
  );
  return data;
}
