import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import {
  getAuditoria,
  getRegistroAuditoria,
  getValoresFiltro,
  exportAuditoria,
  type RegistroAuditoriaDto,
  type AuditoriaFilters,
} from '../../lib/api/admin/adminAuditoriaApi';
import Spinner from '../../components/atoms/Spinner';
import IconButton from '../../components/atoms/IconButton';
import ModalDialog from '../../components/organisms/ModalDialog';
import Button from '../../components/atoms/Button';
import Badge from '../../components/atoms/Badge';
import Pagination from '../../components/molecules/Pagination';
import { useToast } from '../../contexts/ToastContext';

function getErrorMessage(error: unknown, fallback: string): string {
  if (error && typeof error === 'object') {
    const err = error as { userMessage?: string; message?: string };
    return err.userMessage || err.message || fallback;
  }
  return fallback;
}

function renderDetalle(detalle: string | null | undefined): { structured: boolean; lines: string[] } {
  if (!detalle?.trim()) return { structured: false, lines: ['Sin detalle adicional.'] };

  try {
    const parsed = JSON.parse(detalle);
    if (parsed && (parsed.valorAnterior || parsed.valorNuevo || parsed.metadata)) {
      const lines: string[] = [];
      if (parsed.valorAnterior) lines.push(`Valor anterior: ${parsed.valorAnterior}`);
      if (parsed.valorNuevo) lines.push(`Valor nuevo: ${parsed.valorNuevo}`);
      if (parsed.metadata) lines.push(`Metadata: ${typeof parsed.metadata === 'string' ? parsed.metadata : JSON.stringify(parsed.metadata)}`);
      return { structured: true, lines };
    }
  } catch {}

  return { structured: false, lines: [detalle] };
}

function getBadgeVariant(accion: string): string {
  if (/failed|fallida|bloqueado/i.test(accion)) return 'danger';
  if (/exitosa|cerrada|refresh/i.test(accion)) return 'warning';
  if (/eliminar/i.test(accion)) return 'danger';
  if (/crear/i.test(accion)) return 'success';
  if (/actualizar|modificar/i.test(accion)) return 'info';
  return 'default';
}

export default function AdminAuditoriaPage() {
  const toast = useToast();
  const [page, setPage] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(20);
  const [filters, setFilters] = useState<AuditoriaFilters>({});
  const [localFilters, setLocalFilters] = useState<AuditoriaFilters>({});
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const valoresQuery = useQuery({
    queryKey: ['admin-auditoria-valores'],
    queryFn: () => getValoresFiltro(),
    staleTime: 5 * 60 * 1000,
  });

  const exportMut = useMutation({
    mutationFn: (activeFilters: AuditoriaFilters) => exportAuditoria(activeFilters),
    onSuccess: (blob) => {
      const url = URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `auditoria-${new Date().toISOString().slice(0, 19).replace(/[:T]/g, '-')}.csv`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      URL.revokeObjectURL(url);
      toast.success('Exportación generada. La descarga del CSV comenzó.');
    },
    onError: (error) => toast.error(getErrorMessage(error, 'No se pudo exportar el registro de auditoría.')),
  });

  const { data, isLoading, isError } = useQuery({
    queryKey: ['admin-auditoria', page, tamanoPagina, filters],
    queryFn: () => getAuditoria(page, tamanoPagina, filters),
  });

  const detailQuery = useQuery({
    queryKey: ['admin-auditoria-detalle', selectedId],
    queryFn: () => getRegistroAuditoria(selectedId!),
    enabled: selectedId !== null,
  });

  function applyFilters() {
    setFilters(localFilters);
    setPage(1);
  }

  function clearFilters() {
    setLocalFilters({});
    setFilters({});
    setPage(1);
  }

  function openDetalle(registro: RegistroAuditoriaDto) {
    setSelectedId(registro.id);
  }

  function closeDetalle() {
    setSelectedId(null);
  }

  function renderFilterSelect(
    label: string,
    value: string | undefined,
    options: string[] | undefined,
    isError: boolean,
    placeholder: string,
    onChange: (v: string | undefined) => void,
  ) {
    return (
      <div>
        <label className="block text-xs text-gray-500 mb-1">{label}</label>
        {isError ? (
          <input
            type="text"
            placeholder={placeholder}
            value={value ?? ''}
            onChange={(e) => onChange(e.target.value || undefined)}
            className="border border-gray-300 rounded px-2 py-1 text-sm"
          />
        ) : (
          <select
            value={value ?? ''}
            onChange={(e) => onChange(e.target.value || undefined)}
            className="border border-gray-300 rounded px-2 py-1 text-sm min-w-[140px]"
          >
            <option value="">Todas</option>
            {(options ?? []).map((o) => (
              <option key={o} value={o}>{o}</option>
            ))}
          </select>
        )}
      </div>
    );
  }

  return (
    <div>
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold text-gray-800">Auditoría</h2>
        <Button
          variant="secondary"
          onClick={() => exportMut.mutate(filters)}
          loading={exportMut.isPending}
        >
          Exportar CSV
        </Button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3 mb-4 p-3 bg-gray-50 rounded border border-gray-200">
        <div>
          <label className="block text-xs text-gray-500 mb-1">Desde</label>
          <input
            type="date"
            value={localFilters.desde ?? ''}
            onChange={(e) => setLocalFilters((f) => ({ ...f, desde: e.target.value || undefined }))}
            className="border border-gray-300 rounded px-2 py-1 text-sm"
          />
        </div>
        <div>
          <label className="block text-xs text-gray-500 mb-1">Hasta</label>
          <input
            type="date"
            value={localFilters.hasta ?? ''}
            onChange={(e) => setLocalFilters((f) => ({ ...f, hasta: e.target.value || undefined }))}
            className="border border-gray-300 rounded px-2 py-1 text-sm"
          />
        </div>
        {renderFilterSelect(
          'Entidad',
          localFilters.entidad,
          valoresQuery.data?.entidades,
          valoresQuery.isError,
          'ej: Usuario, Documento',
          (v) => setLocalFilters((f) => ({ ...f, entidad: v })),
        )}
        {renderFilterSelect(
          'Acción',
          localFilters.accion,
          valoresQuery.data?.acciones,
          valoresQuery.isError,
          'ej: Login, Crear, Actualizar',
          (v) => setLocalFilters((f) => ({ ...f, accion: v })),
        )}
        <div>
          <label className="block text-xs text-gray-500 mb-1">Nombre de usuario</label>
          <input
            type="text"
            placeholder="Nombre del usuario"
            value={localFilters.usuarioNombre ?? ''}
            onChange={(e) => setLocalFilters((f) => ({ ...f, usuarioNombre: e.target.value || undefined }))}
            className="border border-gray-300 rounded px-2 py-1 text-sm"
          />
        </div>
        <div className="flex items-end gap-2">
          <Button size="sm" onClick={applyFilters}>
            Filtrar
          </Button>
          <Button size="sm" variant="secondary" onClick={clearFilters}>
            Limpiar
          </Button>
        </div>
      </div>

      {isLoading && (
        <div className="flex justify-center py-12"><Spinner size="lg" /></div>
      )}

      {isError && (
        <p className="text-red-600 text-sm">No se pudo cargar el registro de auditoría.</p>
      )}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="min-w-full text-sm">
              <thead className="bg-gray-50 text-gray-600 uppercase text-xs">
                <tr>
                  <th className="px-4 py-2 text-left">Fecha</th>
                  <th className="px-4 py-2 text-left">Usuario</th>
                  <th className="px-4 py-2 text-left">Acción</th>
                  <th className="px-4 py-2 text-left">Entidad</th>
                  <th className="px-4 py-2 text-left">Detalle</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {data.items.map((r) => (
                  <tr key={r.id} className="hover:bg-gray-50">
                    <td className="px-4 py-2 text-gray-500 text-xs whitespace-nowrap">
                      {new Date(r.creadoEn).toLocaleString('es-CL')}
                    </td>
                    <td className="px-4 py-2 text-gray-700">
                      {r.usuarioNombre || 'Sistema'}
                    </td>
                    <td className="px-4 py-2">
                      <Badge variant={getBadgeVariant(r.accion) as any} size="sm">
                        {r.accion}
                      </Badge>
                    </td>
                    <td className="px-4 py-2 text-gray-600">{r.entidad}</td>
                    <td className="px-4 py-2">
                      <IconButton
                        name="eye"
                        tooltip="Ver detalle"
                        appearance="admin"
                        onClick={() => openDetalle(r)}
                      />
                    </td>
                  </tr>
                ))}
                {data.items.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-8 text-center text-sm text-gray-500">
                      No hay registros para los filtros aplicados.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="mt-4">
            <Pagination
              pagina={data.page}
              totalPaginas={data.totalPaginas}
              totalItems={data.total}
              tamanoPagina={tamanoPagina}
              onChange={setPage}
              onTamanoPaginaChange={(t) => { setTamanoPagina(t); setPage(1); }}
            />
          </div>
        </>
      )}

      <ModalDialog
        open={selectedId !== null}
        title="Detalle de auditoría"
        onClose={closeDetalle}
        size="lg"
        footer={<Button variant="secondary" onClick={closeDetalle}>Cerrar</Button>}
      >
        {detailQuery.isLoading ? (
          <div className="flex justify-center py-8"><Spinner size="md" /></div>
        ) : detailQuery.isError ? (
          <p className="text-sm text-red-600">No se pudo cargar el detalle del registro.</p>
        ) : detailQuery.data ? (
          <div className="space-y-4 text-sm">
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              <div>
                <p className="text-xs uppercase tracking-wide text-gray-400">Fecha</p>
                <p className="text-gray-800">{new Date(detailQuery.data.creadoEn).toLocaleString('es-CL')}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wide text-gray-400">Usuario</p>
                <p className="text-gray-800">{detailQuery.data.usuarioNombre || detailQuery.data.usuarioId}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wide text-gray-400">Acción</p>
                <p className="text-gray-800">{detailQuery.data.accion}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wide text-gray-400">Entidad</p>
                <p className="text-gray-800">{detailQuery.data.entidad}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wide text-gray-400">ID del registro</p>
                <p className="font-mono text-gray-800 break-all">{detailQuery.data.entidadId}</p>
              </div>
              <div>
                <p className="text-xs uppercase tracking-wide text-gray-400">Dirección IP</p>
                <p className="font-mono text-gray-800">{detailQuery.data.direccionIp || '—'}</p>
              </div>
              <div className="md:col-span-2">
                <p className="text-xs uppercase tracking-wide text-gray-400">User-Agent</p>
                <p className="font-mono text-gray-800 text-xs break-all">{detailQuery.data.userAgent || '—'}</p>
              </div>
            </div>

            {(() => {
              const detalle = renderDetalle(detailQuery.data.detalle);
              return (
                <div>
                  <p className="mb-1 text-xs uppercase tracking-wide text-gray-400">
                    Detalle {detalle.structured && <span className="text-green-600">(estructurado)</span>}
                  </p>
                  {detalle.structured ? (
                    <div className="rounded border border-gray-200 bg-gray-50 p-3 space-y-1">
                      {detalle.lines.map((line, i) => (
                        <p key={i} className="text-xs text-gray-700">{line}</p>
                      ))}
                    </div>
                  ) : (
                    <pre className="max-h-72 overflow-auto rounded border border-gray-200 bg-gray-50 p-3 text-xs text-gray-700 whitespace-pre-wrap break-words">
                      {detalle.lines[0]}
                    </pre>
                  )}
                </div>
              );
            })()}
          </div>
        ) : (
          <p className="text-sm text-gray-500">No hay información disponible para este registro.</p>
        )}
      </ModalDialog>
    </div>
  );
}
