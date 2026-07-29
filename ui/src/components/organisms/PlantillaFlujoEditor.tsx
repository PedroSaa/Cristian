import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import ModalDialog from './ModalDialog';
import Button from '../atoms/Button';
import IconButton from '../atoms/IconButton';
import Spinner from '../atoms/Spinner';
import SearchableSelect from '../molecules/SearchableSelect';
import { useToast } from '../../contexts/ToastContext';
import { getUsuarios } from '../../lib/api/admin/adminUsuariosApi';
import { getRoles } from '../../lib/api/admin/adminRolesApi';
import { getDepartamentosCatalogo } from '../../lib/api/catalogos';
import {
  getPlantillaFlujo,
  guardarPlantillaFlujo,
  type GuardarFlujoPaso,
  type PlantillaFlujoPaso,
  type ResponsableFlujoTipo,
  type TipoAccionFlujo,
} from '../../lib/api/admin/plantillaFlujoApi';

const ACCIONES: readonly TipoAccionFlujo[] = ['Autorizar', 'Firmar', 'Revisar', 'Visar'];
const TIPOS_RESPONSABLE: readonly ResponsableFlujoTipo[] = ['Usuario', 'Rol', 'Departamento'];
// Catalogs rarely change during an editing session; cache them for the whole session.
const CATALOG_STALE_TIME = 5 * 60 * 1000;
// A single fetch large enough to list every user in the responsible-user selector.
const USUARIOS_PAGE_SIZE = 1000;

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
}

/** Working copy of a step. `key` is a stable local id used only for React lists. */
interface PasoDraft {
  key: string;
  tipoAccion: TipoAccionFlujo;
  responsableTipo: ResponsableFlujoTipo;
  responsableId: string;
  obligatorio: boolean;
}

interface ResponsableOption {
  id: string;
  nombre: string;
}

let draftKeySeq = 0;
function nextKey(): string {
  draftKeySeq += 1;
  return `paso-${draftKeySeq}`;
}

function toDraft(paso: PlantillaFlujoPaso): PasoDraft {
  return {
    key: nextKey(),
    tipoAccion: paso.tipoAccion,
    responsableTipo: paso.responsableTipo,
    responsableId: paso.responsableId,
    obligatorio: paso.obligatorio,
  };
}

function emptyDraft(): PasoDraft {
  return { key: nextKey(), tipoAccion: 'Autorizar', responsableTipo: 'Usuario', responsableId: '', obligatorio: true };
}

interface PlantillaFlujoEditorProps {
  open: boolean;
  codForm: string;
  nomForm: string;
  /** When false the editor is read-only (no save / structural edits). */
  canEdit: boolean;
  onClose: () => void;
}

/**
 * Editor of a template's mandatory workflow: an ORDERED list of steps, each one an
 * action + a responsible party (user / role / department). We only configure and
 * persist it here; another team executes it. Saving replaces the whole workflow.
 */
export default function PlantillaFlujoEditor({ open, codForm, nomForm, canEdit, onClose }: PlantillaFlujoEditorProps) {
  const toast = useToast();
  const qc = useQueryClient();

  const flujoQuery = useQuery({
    queryKey: ['admin-catalogos', 'plantillas', codForm, 'flujo'],
    queryFn: () => getPlantillaFlujo(codForm),
    enabled: open,
    staleTime: 0,
  });

  const [pasos, setPasos] = useState<PasoDraft[]>([]);
  const [invalidKeys, setInvalidKeys] = useState<Set<string>>(new Set());

  // Seed the working copy from the server workflow whenever it (re)loads.
  useEffect(() => {
    if (flujoQuery.data) {
      setPasos(flujoQuery.data.map(toDraft));
      setInvalidKeys(new Set());
    }
  }, [flujoQuery.data]);

  // Only fetch each catalog when at least one step actually uses that responsible type.
  const needsUsuarios = pasos.some((p) => p.responsableTipo === 'Usuario');
  const needsRoles = pasos.some((p) => p.responsableTipo === 'Rol');
  const needsDepartamentos = pasos.some((p) => p.responsableTipo === 'Departamento');

  const usuariosQuery = useQuery({
    queryKey: ['admin-usuarios', 'flujo-selector'],
    queryFn: () => getUsuarios(1, USUARIOS_PAGE_SIZE),
    enabled: open && needsUsuarios,
    staleTime: CATALOG_STALE_TIME,
  });
  const rolesQuery = useQuery({
    queryKey: ['admin-roles', 'flujo-selector'],
    queryFn: () => getRoles(),
    enabled: open && needsRoles,
    staleTime: CATALOG_STALE_TIME,
  });
  const departamentosQuery = useQuery({
    queryKey: ['catalogos', 'departamentos', 'flujo-selector'],
    queryFn: () => getDepartamentosCatalogo(),
    enabled: open && needsDepartamentos,
    staleTime: CATALOG_STALE_TIME,
  });

  const opcionesUsuarios = useMemo<ResponsableOption[]>(
    () => (usuariosQuery.data?.items ?? []).map((u) => ({ id: u.id, nombre: u.nombreCompleto })),
    [usuariosQuery.data],
  );
  const opcionesRoles = useMemo<ResponsableOption[]>(
    () => (rolesQuery.data ?? []).map((r) => ({ id: r.id, nombre: r.nombre })),
    [rolesQuery.data],
  );
  const opcionesDepartamentos = useMemo<ResponsableOption[]>(
    () => (departamentosQuery.data ?? []).map((d) => ({ id: d.id, nombre: d.nombre })),
    [departamentosQuery.data],
  );

  function optionsForTipo(tipo: ResponsableFlujoTipo): { options: ResponsableOption[]; loading: boolean } {
    if (tipo === 'Usuario') return { options: opcionesUsuarios, loading: usuariosQuery.isLoading && needsUsuarios };
    if (tipo === 'Rol') return { options: opcionesRoles, loading: rolesQuery.isLoading && needsRoles };
    return { options: opcionesDepartamentos, loading: departamentosQuery.isLoading && needsDepartamentos };
  }

  const guardarMut = useMutation({
    mutationFn: (payload: GuardarFlujoPaso[]) => guardarPlantillaFlujo(codForm, payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['admin-catalogos', 'plantillas', codForm, 'flujo'] });
      toast.success('Flujo actualizado correctamente.');
      onClose();
    },
    onError: (err) => toast.error(getErrorMessage(err, 'No se pudo guardar el flujo.')),
  });

  function updatePaso(key: string, changes: Partial<PasoDraft>) {
    setPasos((prev) => prev.map((p) => (p.key === key ? { ...p, ...changes } : p)));
  }

  function handleTipoChange(key: string, responsableTipo: ResponsableFlujoTipo) {
    // Switching the responsible type invalidates the previously chosen id.
    updatePaso(key, { responsableTipo, responsableId: '' });
  }

  function addPaso() {
    setPasos((prev) => [...prev, emptyDraft()]);
  }

  function removePaso(key: string) {
    setPasos((prev) => prev.filter((p) => p.key !== key));
  }

  function movePaso(index: number, direction: -1 | 1) {
    setPasos((prev) => {
      const target = index + direction;
      if (target < 0 || target >= prev.length) return prev;
      const next = [...prev];
      [next[index], next[target]] = [next[target], next[index]];
      return next;
    });
  }

  function handleGuardar() {
    const missing = new Set(pasos.filter((p) => !p.responsableId).map((p) => p.key));
    if (missing.size > 0) {
      setInvalidKeys(missing);
      toast.error('Cada paso debe tener un responsable seleccionado.');
      return;
    }
    setInvalidKeys(new Set());
    const payload: GuardarFlujoPaso[] = pasos.map((p, index) => ({
      orden: index + 1,
      tipoAccion: p.tipoAccion,
      responsableTipo: p.responsableTipo,
      responsableId: p.responsableId,
      obligatorio: p.obligatorio,
    }));
    guardarMut.mutate(payload);
  }

  const selectClass = 'w-full rounded border border-gray-300 px-2 py-1.5 text-sm';
  const isLoading = flujoQuery.isLoading;
  const isError = flujoQuery.isError;

  return (
    <ModalDialog
      open={open}
      title="Flujo de la plantilla"
      onClose={onClose}
      size="xl"
      footer={(
        <>
          <Button variant="secondary" onClick={onClose}>Cancelar</Button>
          {canEdit && (
            <Button onClick={handleGuardar} loading={guardarMut.isPending} disabled={isLoading || isError}>
              Guardar
            </Button>
          )}
        </>
      )}
    >
      <p className="mb-3 text-sm text-gray-600">
        Definí la lista ordenada de pasos obligatorios de la plantilla{' '}
        <strong>{nomForm}</strong>. Cada paso es una acción a cargo de un responsable.
      </p>

      {isLoading ? (
        <div className="flex justify-center py-10"><Spinner size="lg" /></div>
      ) : isError ? (
        <p role="alert" className="rounded border border-rose-200 bg-rose-50 px-3 py-4 text-sm text-rose-700">
          No se pudo cargar el flujo de la plantilla.
        </p>
      ) : (
        <div className="space-y-3">
          {pasos.length === 0 ? (
            <p className="rounded border border-dashed border-gray-300 px-3 py-6 text-center text-sm text-gray-400">
              No hay pasos configurados. Agregá el primero.
            </p>
          ) : (
            <ul className="space-y-2">
              {pasos.map((paso, index) => {
                const { options, loading } = optionsForTipo(paso.responsableTipo);
                const invalid = invalidKeys.has(paso.key);
                return (
                  <li
                    key={paso.key}
                    data-testid={`flujo-paso-${index}`}
                    className={`rounded-lg border p-3 ${invalid ? 'border-rose-300 bg-rose-50/40' : 'border-gray-200 bg-white'}`}
                  >
                    <div className="flex flex-wrap items-end gap-3">
                      <span
                        aria-label={`Orden del paso ${index + 1}`}
                        className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-slate-100 text-sm font-semibold text-slate-700"
                      >
                        {index + 1}
                      </span>

                      <label className="flex min-w-[9rem] flex-1 flex-col gap-1 text-xs font-medium text-gray-600">
                        Acción
                        <select
                          aria-label={`Acción del paso ${index + 1}`}
                          className={selectClass}
                          value={paso.tipoAccion}
                          disabled={!canEdit}
                          onChange={(e) => updatePaso(paso.key, { tipoAccion: e.target.value as TipoAccionFlujo })}
                        >
                          {ACCIONES.map((accion) => (
                            <option key={accion} value={accion}>{accion}</option>
                          ))}
                        </select>
                      </label>

                      <label className="flex min-w-[9rem] flex-1 flex-col gap-1 text-xs font-medium text-gray-600">
                        Tipo de responsable
                        <select
                          aria-label={`Tipo de responsable del paso ${index + 1}`}
                          className={selectClass}
                          value={paso.responsableTipo}
                          disabled={!canEdit}
                          onChange={(e) => handleTipoChange(paso.key, e.target.value as ResponsableFlujoTipo)}
                        >
                          {TIPOS_RESPONSABLE.map((tipo) => (
                            <option key={tipo} value={tipo}>{tipo}</option>
                          ))}
                        </select>
                      </label>

                      <label className="flex min-w-[12rem] flex-[2] flex-col gap-1 text-xs font-medium text-gray-600">
                        Responsable
                        <SearchableSelect
                          aria-label={`Responsable del paso ${index + 1}`}
                          options={options}
                          value={paso.responsableId}
                          onChange={(v) => updatePaso(paso.key, { responsableId: v })}
                          getOptionLabel={(o: ResponsableOption) => o.nombre}
                          getOptionValue={(o: ResponsableOption) => o.id}
                          placeholder={loading ? 'Cargando…' : 'Buscar…'}
                          disabled={!canEdit || loading}
                          loading={loading}
                        />
                      </label>

                      <label className="flex items-center gap-2 pb-1.5 text-xs font-medium text-gray-600">
                        <input
                          type="checkbox"
                          aria-label={`Obligatorio del paso ${index + 1}`}
                          checked={paso.obligatorio}
                          disabled={!canEdit}
                          onChange={(e) => updatePaso(paso.key, { obligatorio: e.target.checked })}
                          className="h-4 w-4 rounded border-gray-300"
                        />
                        Obligatorio
                      </label>

                      {canEdit && (
                        <div className="flex items-center gap-1 pb-0.5">
                          <IconButton
                            name="arrow-up"
                            tooltip="Subir"
                            appearance="admin"
                            disabled={index === 0}
                            onClick={() => movePaso(index, -1)}
                          />
                          <IconButton
                            name="arrow-down"
                            tooltip="Bajar"
                            appearance="admin"
                            disabled={index === pasos.length - 1}
                            onClick={() => movePaso(index, 1)}
                          />
                          <IconButton
                            name="trash"
                            tooltip="Quitar paso"
                            appearance="admin"
                            variant="danger"
                            onClick={() => removePaso(paso.key)}
                          />
                        </div>
                      )}
                    </div>
                    {invalid && (
                      <p className="mt-2 text-xs text-rose-600">Seleccione un responsable para este paso.</p>
                    )}
                  </li>
                );
              })}
            </ul>
          )}

          {canEdit && (
            <Button variant="secondary" onClick={addPaso}>Agregar paso</Button>
          )}
        </div>
      )}
    </ModalDialog>
  );
}
