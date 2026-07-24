import { useRef } from 'react';
import Button from '../../atoms/Button';
import DetalleCampo from './DetalleCampo';
import EstadoOrdenBadge from './EstadoOrdenBadge';
import MercadoPublicoSeccion from './MercadoPublicoSeccion';
import TotalesResumen from './TotalesResumen';
import { formatCLP, formatFecha, formatFechaHora } from '../../../lib/ordenesCompra/format';
import type { OrdenCompraDto } from '../../../types/ordenCompra';

export interface OrdenCompraDetalleProps {
  oc: OrdenCompraDto;
  canCrear: boolean;
  uploading: boolean;
  onUploadAdjunto: (id: string, file: File) => void;
  onDownloadAdjunto: (id: string, adjuntoId: string, nombreArchivo: string) => void;
  onEliminarAdjunto: (id: string, adjuntoId: string, nombreArchivo: string) => void;
  onNotify: (type: 'success' | 'error', message: string) => void;
}

// Detail modal content: general data, items, totals, Mercado Público link,
// attachments (upload/download/delete) and approval history.
export default function OrdenCompraDetalle({
  oc,
  canCrear,
  uploading,
  onUploadAdjunto,
  onDownloadAdjunto,
  onEliminarAdjunto,
  onNotify,
}: OrdenCompraDetalleProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const puedeGestionarAdjuntos = canCrear && oc.estado !== 'Anulada';

  return (
    <div className="space-y-5">
      {/* Datos generales */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
        <DetalleCampo label="Número" value={oc.numero ?? '(borrador)'} />
        <DetalleCampo label="Fecha" value={formatFecha(oc.fecha)} />
        <div className="flex flex-col gap-0.5">
          <span className="text-[11px] font-semibold uppercase tracking-wide text-text-base/45">Estado</span>
          <span><EstadoOrdenBadge estado={oc.estado} /></span>
        </div>
        <DetalleCampo label="Proveedor" value={oc.proveedorNombre} />
        <DetalleCampo label="RUT proveedor" value={oc.proveedorRut} />
        <DetalleCampo label="Moneda" value={oc.moneda} />
        <DetalleCampo label="Forma de pago" value={oc.formaPago ?? ''} />
        <DetalleCampo label="Plazo de entrega" value={oc.plazoEntrega ?? ''} />
        <DetalleCampo label="Lugar de entrega" value={oc.lugarEntrega ?? ''} />
      </div>

      {oc.observaciones && (
        <DetalleCampo label="Observaciones" value={oc.observaciones} />
      )}

      {/* Items */}
      <div className="space-y-2">
        <h3 className="text-sm font-semibold text-text-base">Ítems</h3>
        {oc.items.length === 0 ? (
          <p className="text-xs text-text-base/55">La orden no tiene ítems.</p>
        ) : (
          <div className="overflow-x-auto rounded border border-border-base">
            <table className="w-full divide-y divide-border-base text-sm">
              <thead className="bg-surface-secondary/60">
                <tr>
                  <th className="px-3 py-2 text-left text-xs font-semibold text-text-base/70">#</th>
                  <th className="px-3 py-2 text-left text-xs font-semibold text-text-base/70">Descripción</th>
                  <th className="px-3 py-2 text-right text-xs font-semibold text-text-base/70">Cantidad</th>
                  <th className="px-3 py-2 text-right text-xs font-semibold text-text-base/70">Precio unitario</th>
                  <th className="px-3 py-2 text-right text-xs font-semibold text-text-base/70">Total línea</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-base">
                {oc.items.map((item) => (
                  <tr key={item.id}>
                    <td className="px-3 py-2 text-text-base/55">{item.numeroLinea}</td>
                    <td className="px-3 py-2">{item.descripcion}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{item.cantidad}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{formatCLP(item.precioUnitario)}</td>
                    <td className="px-3 py-2 text-right tabular-nums">{formatCLP(item.totalLinea)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Totales */}
      <TotalesResumen totales={{ neto: oc.neto, iva: oc.iva, total: oc.total }} />

      {/* Mercado Público */}
      <MercadoPublicoSeccion
        oc={oc}
        puedeGestionar={canCrear && oc.estado !== 'Anulada'}
        onNotify={onNotify}
      />

      {/* Adjuntos */}
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-text-base">Adjuntos</h3>
          {puedeGestionarAdjuntos && (
            <>
              <input
                ref={fileInputRef}
                type="file"
                className="hidden"
                aria-label="Archivo adjunto"
                onChange={(e) => {
                  const file = e.target.files?.[0];
                  if (file) onUploadAdjunto(oc.id, file);
                  e.target.value = '';
                }}
              />
              <Button
                variant="secondary"
                size="sm"
                loading={uploading}
                onClick={() => fileInputRef.current?.click()}
              >
                Subir adjunto
              </Button>
            </>
          )}
        </div>
        {oc.adjuntos.length === 0 ? (
          <p className="text-xs text-text-base/55">Sin adjuntos.</p>
        ) : (
          <ul className="divide-y divide-border-base rounded border border-border-base">
            {oc.adjuntos.map((adjunto) => (
              <li key={adjunto.id} className="flex items-center justify-between gap-3 px-3 py-2">
                <div className="min-w-0 flex-1">
                  <p className="truncate text-sm text-text-base">{adjunto.nombreArchivo}</p>
                  <p className="text-xs text-text-base/50">
                    {(adjunto.tamano / 1024).toFixed(1)} KB · {formatFechaHora(adjunto.creadoEn)}
                  </p>
                </div>
                <div className="flex gap-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-primary-700 text-xs"
                    onClick={() => onDownloadAdjunto(oc.id, adjunto.id, adjunto.nombreArchivo)}
                  >
                    Descargar
                  </Button>
                  {puedeGestionarAdjuntos && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="text-red-600 text-xs"
                      onClick={() => onEliminarAdjunto(oc.id, adjunto.id, adjunto.nombreArchivo)}
                    >
                      Eliminar
                    </Button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Historial de aprobación / anulación */}
      {(oc.aprobadoEn || oc.motivoAnulacion) && (
        <div className="space-y-2">
          <h3 className="text-sm font-semibold text-text-base">Historial</h3>
          {oc.aprobadoEn && (
            <div className="rounded border border-border-base bg-surface-secondary/30 px-3 py-2 text-sm">
              <p>
                {oc.estado === 'Rechazada' ? 'Rechazada' : 'Aprobada'} el {formatFechaHora(oc.aprobadoEn)}
              </p>
              {oc.comentarioAprobacion && (
                <p className="text-text-base/65">Comentario: {oc.comentarioAprobacion}</p>
              )}
            </div>
          )}
          {oc.motivoAnulacion && (
            <div className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800">
              Motivo de anulación: {oc.motivoAnulacion}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
