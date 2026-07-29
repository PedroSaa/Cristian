import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import Button from '../../components/atoms/Button';
import IconButton from '../../components/atoms/IconButton';
import FormField from '../../components/molecules/FormField';
import ModalDialog from '../../components/organisms/ModalDialog';
import PlantillaEditor from '../../components/organisms/PlantillaEditor';
import PlantillaMedidasEditor, {
  toCoordinate,
  type MedidaRow,
} from '../../components/organisms/PlantillaMedidasEditor';
import PlantillaFlujoEditor from '../../components/organisms/PlantillaFlujoEditor';
import LegacyCrudSection from './LegacyCrudSection';
import { useHasPermission } from '../../hooks/usePermissions';
import { useToast } from '../../contexts/ToastContext';
import { PERMISSIONS } from '../../lib/generated/permissionCatalog';
import {
  createSeForpla,
  deleteSeForpla,
  getPlantillaEditorConfig,
  getPlantillaMedidas,
  getPlantillaPdf,
  forcePlantillaSave,
  listCatalogoCategorias,
  listCatalogoSubcategorias,
  listSeFordoc,
  listSeForpla,
  updatePlantillaMedidas,
  updateSeForpla,
  type ActualizarSeForplaData,
  type ActualizarSeForplaMedidaItem,
  type CatalogoCategoriaDto,
  type CatalogoSubcategoriaDto,
  type SeFordocDto,
  type SeForplaDto,
  type SeForplaTipoSeleccion,
} from '../../lib/api/admin/adminCatalogosApi';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
}

function emptyToNull(value: string) {
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

function matchesSearch(search: string, values: Array<string | number | null | undefined>) {
  const q = search.trim().toLowerCase();
  if (!q) return true;
  return values.some((value) => String(value ?? '').toLowerCase().includes(q));
}

function getTemplateFileName(item: SeForplaDto) {
  const extension = item.extForm.startsWith('.') ? item.extForm.slice(1) : item.extForm;
  return `${item.nomForm}.${extension}`;
}

function getTemplatePdfUrl(codForm: string) {
  return `/api/admin/catalogos/plantillas/${encodeURIComponent(codForm)}/pdf`;
}

function getTemplateMimeType(extension: string) {
  const normalized = extension.toLowerCase().replace(/^\./, '');
  if (normalized === 'doc') return 'application/msword';
  if (normalized === 'docx') return 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
  return 'application/octet-stream';
}

function decodeBase64(base64: string) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) bytes[i] = binary.charCodeAt(i);
  return bytes;
}

function openTemplatePdf(item: SeForplaDto) {
  window.open(getTemplatePdfUrl(item.codForm), '_blank', 'noopener,noreferrer');
}

function downloadTemplateFile(item: SeForplaDto) {
  const fileName = getTemplateFileName(item);
  const fileBytes = decodeBase64(item.blobForm);
  const mimeType = getTemplateMimeType(item.extForm);
  const url = URL.createObjectURL(new Blob([fileBytes], { type: mimeType }));

  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = fileName;
  anchor.rel = 'noopener noreferrer';
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();

  window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

function readFileAsBase64(file: File): Promise<string> {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      const result = String(reader.result ?? '');
      resolve(result.includes(',') ? result.split(',')[1] : result);
    };
    reader.onerror = () => reject(reader.error ?? new Error('No se pudo leer el archivo.'));
    reader.readAsDataURL(file);
  });
}

interface CodFormAsociacion {
  tipo: string;
  nt: number;
  nc: number;
  ns: number;
}

/**
 * codForm guarda la asociación de la plantilla como JSON: {"tipo":"T","nt":1,"nc":0,"ns":0}.
 * Filas anteriores al rediseño pueden tener un código plano: se devuelven como null.
 */
function parseCodForm(codForm: string): CodFormAsociacion | null {
  try {
    const parsed: unknown = JSON.parse(codForm);
    if (parsed && typeof parsed === 'object' && typeof (parsed as CodFormAsociacion).tipo === 'string') {
      return parsed as CodFormAsociacion;
    }
  } catch {
    // No es JSON: fila legada con codForm plano.
  }
  return null;
}

function formatoDocumentoLabel(
  item: SeForplaDto,
  formatos: SeFordocDto[] | undefined,
  categorias: CatalogoCategoriaDto[] | undefined,
  subcategorias: CatalogoSubcategoriaDto[] | undefined,
): string {
  const asociacion = parseCodForm(item.codForm);
  if (!asociacion) return item.codForm;
  if (asociacion.tipo === 'T') {
    return formatos?.find((f) => f.tipoCod === asociacion.nt)?.tipoDesc ?? `Formato ${asociacion.nt}`;
  }
  if (asociacion.tipo === 'C') {
    return `Categoría: ${categorias?.find((c) => c.catCod === asociacion.nc)?.catDesc ?? asociacion.nc}`;
  }
  if (asociacion.tipo === 'S') {
    const sub = subcategorias?.find((s) => s.catCod === asociacion.nc && s.idSubcategoria === asociacion.ns);
    return `Subcategoría: ${sub?.subcatNombre ?? asociacion.ns}`;
  }
  return item.codForm;
}

const schema = z
  .object({
    mode: z.enum(['crear', 'editar']),
    tipoSeleccion: z.enum(['T', 'C', 'S']),
    tipoCod: z.string().optional(),
    catCod: z.string().optional(),
    idSubcategoria: z.string().optional(),
    fileName: z.string().optional(),
    blobForm: z.string().optional(),
    obsForm: z.string().max(255, 'Máximo 255 caracteres').optional(),
  })
  .superRefine((values, ctx) => {
    if (values.mode !== 'crear') return;
    if (values.tipoSeleccion === 'T' && !values.tipoCod) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['tipoCod'], message: 'Seleccione un formato de documento' });
    }
    if ((values.tipoSeleccion === 'C' || values.tipoSeleccion === 'S') && !values.catCod) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['catCod'], message: 'Seleccione una categoría' });
    }
    if (values.tipoSeleccion === 'S' && !values.idSubcategoria) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['idSubcategoria'], message: 'Seleccione una subcategoría' });
    }
    if (!values.blobForm || !values.fileName) {
      ctx.addIssue({ code: z.ZodIssueCode.custom, path: ['blobForm'], message: 'El archivo Word es obligatorio' });
    }
  });

type FormData = z.infer<typeof schema>;
type Mode = 'crear' | 'editar' | null;

export default function AdminPlantillasSection() {
  const canEdit = useHasPermission(PERMISSIONS.ADMIN_CATALOGOS_EDITAR);
  const qc = useQueryClient();
  const toast = useToast();
  const [modal, setModal] = useState<Mode>(null);
  const [selected, setSelected] = useState<SeForplaDto | null>(null);
  const [deleting, setDeleting] = useState<SeForplaDto | null>(null);
  const [blobFileName, setBlobFileName] = useState('');
  const [search, setSearch] = useState('');
  const [editorData, setEditorData] = useState<{ editorUrl: string; config: Record<string, unknown>; title: string; codForm: string; docKey: string } | null>(null);
  const [closingEditor, setClosingEditor] = useState(false);
  const [medidasItem, setMedidasItem] = useState<SeForplaDto | null>(null);
  const [medidasRows, setMedidasRows] = useState<MedidaRow[]>([]);
  const [flujoItem, setFlujoItem] = useState<SeForplaDto | null>(null);

  const form = useForm<FormData>({ resolver: zodResolver(schema) as any });
  const { data, isLoading, isError } = useQuery({ queryKey: ['admin-catalogos', 'plantillas'], queryFn: listSeForpla });
  const { data: formatos } = useQuery({ queryKey: ['admin-catalogos', 'formatos-documento'], queryFn: listSeFordoc });
  const { data: categorias } = useQuery({ queryKey: ['admin-catalogos', 'categorias'], queryFn: listCatalogoCategorias });
  const { data: subcategorias } = useQuery({ queryKey: ['admin-catalogos', 'subcategorias', 'all'], queryFn: () => listCatalogoSubcategorias() });

  const tipoSeleccion = form.watch('tipoSeleccion');
  const catCodValue = form.watch('catCod');
  const subcategoriasDeCategoria = (subcategorias ?? []).filter((s) => String(s.catCod) === catCodValue);

  const filteredData = (data ?? []).filter((item) => matchesSearch(search, [
    item.usucod,
    item.nomForm,
    item.obsForm,
    formatoDocumentoLabel(item, formatos, categorias, subcategorias),
  ]));

  const createMut = useMutation({
    mutationFn: createSeForpla,
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'plantillas'] }); setModal(null); toast.success('Plantilla creada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo crear la plantilla.')),
  });

  const updateMut = useMutation({
    mutationFn: ({ codForm, body }: { codForm: string; body: ActualizarSeForplaData }) => updateSeForpla(codForm, body),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'plantillas'] }); setModal(null); toast.success('Plantilla actualizada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo actualizar la plantilla.')),
  });

  const medidasMut = useMutation({
    mutationFn: ({ codForm, items }: { codForm: string; items: ActualizarSeForplaMedidaItem[] }) => updatePlantillaMedidas(codForm, items),
    onSuccess: () => { setMedidasItem(null); toast.success('Medidas actualizadas correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudieron actualizar las medidas.')),
  });

  const deleteMut = useMutation({
    mutationFn: (codForm: string) => deleteSeForpla(codForm),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-catalogos', 'plantillas'] }); setDeleting(null); toast.success('Plantilla eliminada correctamente.'); },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo eliminar la plantilla.')),
  });

  function openCreate() {
    form.reset({ mode: 'crear', tipoSeleccion: 'T', tipoCod: '', catCod: '', idSubcategoria: '', fileName: '', blobForm: '', obsForm: '' });
    setBlobFileName('');
    setSelected(null);
    setModal('crear');
  }

  function openEdit(item: SeForplaDto) {
    form.reset({ mode: 'editar', tipoSeleccion: 'T', tipoCod: '', catCod: '', idSubcategoria: '', fileName: '', blobForm: '', obsForm: item.obsForm ?? '' });
    setBlobFileName('');
    setSelected(item);
    setModal('editar');
  }

  function submit(values: FormData) {
    if (modal === 'crear') {
      createMut.mutate({
        tipoSeleccion: values.tipoSeleccion as SeForplaTipoSeleccion,
        tipoCod: values.tipoSeleccion === 'T' ? Number(values.tipoCod) : null,
        catCod: values.tipoSeleccion !== 'T' ? Number(values.catCod) : null,
        idSubcategoria: values.tipoSeleccion === 'S' ? Number(values.idSubcategoria) : null,
        fileName: values.fileName ?? '',
        blobForm: values.blobForm ?? '',
        obsForm: emptyToNull(values.obsForm ?? ''),
      });
    }
    if (modal === 'editar' && selected) {
      const replacesFile = Boolean(values.fileName && values.blobForm);
      updateMut.mutate({
        codForm: selected.codForm,
        body: {
          fileName: replacesFile ? values.fileName : null,
          blobForm: replacesFile ? values.blobForm : null,
          obsForm: emptyToNull(values.obsForm ?? ''),
        },
      });
    }
  }

  async function handleFileChange(file: File | undefined) {
    if (!file) return;
    const base64 = await readFileAsBase64(file);
    form.setValue('blobForm', base64, { shouldDirty: true, shouldValidate: true });
    form.setValue('fileName', file.name, { shouldDirty: true });
    setBlobFileName(file.name);
  }

  async function openEditorContent(item: SeForplaDto) {
    try {
      const cfg = await getPlantillaEditorConfig(item.codForm);
      const ext = item.extForm.startsWith('.') ? item.extForm.slice(1) : item.extForm;
      const docKey = (cfg.config as { document?: { key?: string } }).document?.key ?? '';
      setEditorData({ editorUrl: cfg.editorUrl, config: cfg.config, title: `${item.nomForm}.${ext}`, codForm: item.codForm, docKey });
    } catch (err) {
      toast.error(getErrorMessage(err, 'No se pudo abrir el editor de la plantilla.'));
    }
  }

  async function openMedidas(item: SeForplaDto) {
    try {
      const rows = await getPlantillaMedidas(item.codForm);
      // La fila QR (id 4) queda en BD pero nunca se muestra, igual que en el legacy.
      setMedidasRows(rows
        .filter((row) => row.objeto !== 'QR')
        .map((row) => ({
          idForplaMed: row.idForplaMed,
          objeto: row.objeto,
          x: String(row.x),
          y: String(row.y),
          ancho: String(row.ancho),
          alto: String(row.alto),
        })));
      setMedidasItem(item);
    } catch (err) {
      toast.error(getErrorMessage(err, 'No se pudieron cargar las medidas de la plantilla.'));
    }
  }

  function updateMedidaRow(idForplaMed: number, field: 'x' | 'y' | 'ancho' | 'alto', value: string) {
    setMedidasRows((rows) => rows.map((row) => (row.idForplaMed === idForplaMed ? { ...row, [field]: value } : row)));
  }

  function saveMedidas() {
    if (!medidasItem) return;
    // Solo viajan las filas visibles: el backend actualiza únicamente lo recibido,
    // así la fila QR oculta conserva sus valores.
    medidasMut.mutate({
      codForm: medidasItem.codForm,
      items: medidasRows.map((row) => ({
        idForplaMed: row.idForplaMed,
        x: toCoordinate(row.x),
        y: toCoordinate(row.y),
        ancho: toCoordinate(row.ancho),
        alto: toCoordinate(row.alto),
      })),
    });
  }

  async function closeEditor() {
    const current = editorData;
    if (current?.docKey) {
      setClosingEditor(true);
      try {
        // Forzar el guardado YA en vez de esperar el guardado diferido. Si el Document
        // Server ya autoguardó (saved=false), igual damos un margen breve para que el
        // contenido recién persistido esté disponible al refrescar la lista.
        const { saved } = await forcePlantillaSave(current.codForm, current.docKey);
        await new Promise((resolve) => setTimeout(resolve, saved ? 1500 : 700));
      } catch {
        // Si el forcesave falla, cerramos igual; OnlyOffice guardará de forma diferida.
      } finally {
        setClosingEditor(false);
      }
    }
    setEditorData(null);
    qc.invalidateQueries({ queryKey: ['admin-catalogos', 'plantillas'] });
  }

  const selectClass = 'w-full rounded border border-gray-300 px-3 py-2 text-sm';

  return (
    <div className="space-y-4">
      <LegacyCrudSection
        title="Plantillas"
        description="Catálogo SEFORPLA. Se asocia la plantilla a un formato, categoría o subcategoría y se sube el archivo Word."
        items={filteredData}
        columns={[
          { header: 'Usuario', render: (item) => item.usucod },
          { header: 'Nombre', render: (item) => item.nomForm },
          { header: 'Observación', render: (item) => item.obsForm ?? '—' },
          { header: 'Formato Documento', render: (item) => formatoDocumentoLabel(item, formatos, categorias, subcategorias) },
        ]}
        getRowKey={(item) => item.codForm}
        isLoading={isLoading}
        isError={isError}
        errorMessage="No se pudieron cargar las plantillas."
        emptyMessage="No hay plantillas cargadas."
        canEdit={canEdit}
        onCreate={openCreate}
        onEdit={openEdit}
        onEditContent={openEditorContent}
        extraActions={(item) => canEdit && (
          <>
            <IconButton name="ruler" tooltip="Medidas" appearance="admin" onClick={() => openMedidas(item)} />
            <IconButton name="workflow" tooltip="Flujo" appearance="admin" onClick={() => setFlujoItem(item)} />
          </>
        )}
        onDelete={(item) => setDeleting(item)}
        onView={openTemplatePdf}
        onDownload={downloadTemplateFile}
        actionLabel="Crear plantilla"
        searchValue={search}
        searchPlaceholder="Buscar plantillas..."
        onSearchChange={setSearch}
      />

      {modal && (
        <ModalDialog
          open
          title={modal === 'crear' ? 'Crear plantilla' : 'Editar plantilla'}
          onClose={() => setModal(null)}
          size="lg"
          footer={(
            <>
              <Button variant="secondary" onClick={() => setModal(null)}>Cancelar</Button>
              <Button type="submit" form="forpla-form" loading={createMut.isPending || updateMut.isPending}>Guardar</Button>
            </>
          )}
        >
          <form id="forpla-form" onSubmit={form.handleSubmit(submit)} className="grid gap-3">
            {modal === 'crear' ? (
              <>
                <FormField label="Asociar a" error={form.formState.errors.tipoSeleccion?.message}>
                  <select
                    {...form.register('tipoSeleccion', {
                      onChange: () => {
                        form.setValue('tipoCod', '');
                        form.setValue('catCod', '');
                        form.setValue('idSubcategoria', '');
                      },
                    })}
                    className={selectClass}
                  >
                    <option value="T">Formato</option>
                    <option value="C">Categoría</option>
                    <option value="S">Subcategoría</option>
                  </select>
                </FormField>

                {tipoSeleccion === 'T' && (
                  <FormField label="Formato de documento" error={form.formState.errors.tipoCod?.message}>
                    <select {...form.register('tipoCod')} className={selectClass}>
                      <option value="">Seleccione un formato…</option>
                      {(formatos ?? []).map((f) => (
                        <option key={f.tipoCod} value={String(f.tipoCod)}>{f.tipoDesc}</option>
                      ))}
                    </select>
                  </FormField>
                )}

                {(tipoSeleccion === 'C' || tipoSeleccion === 'S') && (
                  <FormField label="Categoría" error={form.formState.errors.catCod?.message}>
                    <select
                      {...form.register('catCod', { onChange: () => form.setValue('idSubcategoria', '') })}
                      className={selectClass}
                    >
                      <option value="">Seleccione una categoría…</option>
                      {(categorias ?? []).map((c) => (
                        <option key={c.catCod} value={String(c.catCod)}>{c.catDesc}</option>
                      ))}
                    </select>
                  </FormField>
                )}

                {tipoSeleccion === 'S' && (
                  <FormField label="Subcategoría" error={form.formState.errors.idSubcategoria?.message}>
                    <select {...form.register('idSubcategoria')} className={selectClass} disabled={!catCodValue}>
                      <option value="">{catCodValue ? 'Seleccione una subcategoría…' : 'Seleccione primero una categoría'}</option>
                      {subcategoriasDeCategoria.map((s) => (
                        <option key={`${s.catCod}-${s.idSubcategoria}`} value={String(s.idSubcategoria)}>{s.subcatNombre}</option>
                      ))}
                    </select>
                  </FormField>
                )}
              </>
            ) : (
              selected && (
                <FormField label="Asociación">
                  <input
                    value={formatoDocumentoLabel(selected, formatos, categorias, subcategorias)}
                    disabled
                    readOnly
                    className="w-full rounded border border-gray-300 bg-gray-100 px-3 py-2 text-sm text-gray-600"
                  />
                </FormField>
              )
            )}

            <FormField label="Archivo Word" error={form.formState.errors.blobForm?.message}>
              <div className="space-y-2 rounded border border-gray-300 bg-gray-50 px-3 py-3">
                <input
                  type="file"
                  accept=".doc,.docx,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                  onChange={(e) => handleFileChange(e.target.files?.[0])}
                  className="block w-full text-sm text-slate-700 file:mr-4 file:rounded-md file:border-0 file:bg-slate-900 file:px-3 file:py-2 file:text-sm file:font-medium file:text-white hover:file:bg-slate-700"
                />
                <div className="text-xs text-slate-500">
                  {modal === 'crear'
                    ? 'Suba un archivo Word (.doc o .docx). El nombre y la extensión se toman del archivo.'
                    : 'Suba un archivo Word solo si quiere reemplazar el actual; si no, se conserva.'}
                </div>
                <div className="text-xs font-medium text-slate-600">{blobFileName || 'Sin archivo seleccionado'}</div>
              </div>
            </FormField>

            <FormField label="Observación" error={form.formState.errors.obsForm?.message}>
              <textarea {...form.register('obsForm')} maxLength={255} rows={3} className="w-full rounded border border-gray-300 px-3 py-2 text-sm" placeholder="Opcional" />
            </FormField>
          </form>
        </ModalDialog>
      )}

      {medidasItem && (
        <ModalDialog
          open
          title="Medidas de la plantilla"
          onClose={() => setMedidasItem(null)}
          size="xl"
          footer={(
            <>
              <Button variant="secondary" onClick={() => setMedidasItem(null)}>Cancelar</Button>
              <Button loading={medidasMut.isPending} onClick={saveMedidas}>Guardar</Button>
            </>
          )}
        >
          <p className="mb-3 text-sm text-gray-600">
            Arrastrá cada objeto sobre la vista del documento o editá las coordenadas (en puntos del PDF) para posicionar
            dónde se estampa al firmar documentos generados con la plantilla <strong>{medidasItem.nomForm}</strong>.
          </p>
          <PlantillaMedidasEditor
            codForm={medidasItem.codForm}
            rows={medidasRows}
            onChange={updateMedidaRow}
            fetchPdf={getPlantillaPdf}
          />
        </ModalDialog>
      )}

      {flujoItem && (
        <PlantillaFlujoEditor
          open
          codForm={flujoItem.codForm}
          nomForm={flujoItem.nomForm}
          canEdit={canEdit}
          onClose={() => setFlujoItem(null)}
        />
      )}

      {deleting && (
        <ModalDialog
          open
          title="Eliminar plantilla"
          onClose={() => setDeleting(null)}
          footer={(
            <>
              <Button variant="secondary" onClick={() => setDeleting(null)}>Cancelar</Button>
              <Button variant="danger" loading={deleteMut.isPending} onClick={() => deleteMut.mutate(deleting.codForm)}>Eliminar plantilla</Button>
            </>
          )}
        >
          <p className="text-sm text-gray-600">Está por eliminarse la plantilla <strong>{deleting.nomForm}</strong>.</p>
        </ModalDialog>
      )}

      {editorData && (
        <PlantillaEditor
          editorUrl={editorData.editorUrl}
          config={editorData.config}
          title={editorData.title}
          busy={closingEditor}
          onClose={closeEditor}
        />
      )}
    </div>
  );
}
