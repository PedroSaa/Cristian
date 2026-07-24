import { useCallback, useState } from 'react';
import Button from '../../atoms/Button';
import Input from '../../atoms/Input';
import ConfirmDialog from '../ConfirmDialog';
import DetalleCampo from './DetalleCampo';
import {
  useDesvincularMercadoPublico,
  useVincularMercadoPublico,
} from '../../../hooks/useOrdenesCompra';
import { buscarOrdenMercadoPublico } from '../../../lib/api/ordenesCompra';
import { extractErrorMessage, formatCLP } from '../../../lib/ordenesCompra/format';
import type { MercadoPublicoOrden, OrdenCompraDto } from '../../../types/ordenCompra';

export interface MercadoPublicoSeccionProps {
  oc: OrdenCompraDto;
  puedeGestionar: boolean;
  onNotify: (type: 'success' | 'error', message: string) => void;
}

// Mercado Público section of the detail modal: link/unlink the order to a
// portal code and query the portal for the linked order's data.
export default function MercadoPublicoSeccion({
  oc,
  puedeGestionar,
  onNotify,
}: MercadoPublicoSeccionProps) {
  const [codigoInput, setCodigoInput] = useState('');
  const [portalData, setPortalData] = useState<MercadoPublicoOrden | null>(null);
  const [consultando, setConsultando] = useState(false);
  const [confirmarDesvincular, setConfirmarDesvincular] = useState(false);

  const vincularMutation = useVincularMercadoPublico();
  const desvincularMutation = useDesvincularMercadoPublico();

  const handleConsultar = useCallback(async () => {
    if (!oc.codigoMercadoPublico) return;
    setConsultando(true);
    try {
      setPortalData(await buscarOrdenMercadoPublico(oc.codigoMercadoPublico));
    } catch (error) {
      onNotify(
        'error',
        extractErrorMessage(error, 'No se pudo consultar la orden en Mercado Público.'),
      );
    } finally {
      setConsultando(false);
    }
  }, [oc.codigoMercadoPublico, onNotify]);

  const handleVincular = useCallback(async () => {
    const codigo = codigoInput.trim();
    if (!codigo) return;
    try {
      await vincularMutation.mutateAsync({ id: oc.id, codigo });
      setCodigoInput('');
      onNotify('success', 'Orden de compra vinculada a Mercado Público.');
    } catch (error) {
      onNotify(
        'error',
        extractErrorMessage(error, 'No se pudo vincular la orden a Mercado Público.'),
      );
    }
  }, [codigoInput, oc.id, vincularMutation, onNotify]);

  const handleDesvincular = useCallback(async () => {
    if (desvincularMutation.isPending) return;
    try {
      await desvincularMutation.mutateAsync(oc.id);
      setPortalData(null);
      onNotify('success', 'Orden de compra desvinculada de Mercado Público.');
    } catch (error) {
      onNotify(
        'error',
        extractErrorMessage(error, 'No se pudo desvincular la orden de Mercado Público.'),
      );
    } finally {
      setConfirmarDesvincular(false);
    }
  }, [oc.id, desvincularMutation, onNotify]);

  return (
    <div className="space-y-2">
      <h3 className="text-sm font-semibold text-text-base">Mercado Público</h3>

      {oc.codigoMercadoPublico ? (
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-mono text-sm text-text-base">{oc.codigoMercadoPublico}</span>
            <Button
              variant="secondary"
              size="sm"
              loading={consultando}
              onClick={handleConsultar}
            >
              Consultar en portal
            </Button>
            {puedeGestionar && (
              <Button
                variant="ghost"
                size="sm"
                className="text-red-600 hover:text-red-800 text-xs"
                loading={desvincularMutation.isPending}
                onClick={() => setConfirmarDesvincular(true)}
              >
                Desvincular
              </Button>
            )}
          </div>

          <ConfirmDialog
            open={confirmarDesvincular}
            title="Desvincular de Mercado Público"
            message={`¿Desvincular la orden de compra del código ${oc.codigoMercadoPublico}?`}
            confirmLabel="Desvincular"
            danger
            loading={desvincularMutation.isPending}
            onConfirm={handleDesvincular}
            onCancel={() => {
              if (!desvincularMutation.isPending) setConfirmarDesvincular(false);
            }}
          />

          {portalData && (
            <div className="grid grid-cols-1 gap-3 rounded border border-border-base bg-surface-secondary/30 p-3 sm:grid-cols-2 lg:grid-cols-3">
              <DetalleCampo label="Nombre en portal" value={portalData.nombre ?? ''} />
              <DetalleCampo label="Estado en portal" value={portalData.estado ?? ''} />
              <DetalleCampo
                label="Monto total"
                value={portalData.montoTotal != null ? formatCLP(portalData.montoTotal) : ''}
              />
              <DetalleCampo label="Comprador" value={portalData.compradorNombre ?? ''} />
              <DetalleCampo label="RUT comprador" value={portalData.compradorRut ?? ''} />
              <DetalleCampo label="Proveedor" value={portalData.proveedorNombre ?? ''} />
              <DetalleCampo label="RUT proveedor" value={portalData.proveedorRut ?? ''} />
            </div>
          )}
        </div>
      ) : puedeGestionar ? (
        <div className="flex flex-wrap items-end gap-2">
          <div className="flex w-64 flex-col gap-1">
            <label className="text-[11px] font-medium text-text-base/60">
              Código OC del portal
            </label>
            <Input
              type="text"
              maxLength={40}
              aria-label="Código OC del portal"
              placeholder="Ej: 1123-109-SE13"
              value={codigoInput}
              onChange={(e) => setCodigoInput(e.target.value)}
            />
          </div>
          <Button
            variant="secondary"
            size="sm"
            loading={vincularMutation.isPending}
            disabled={!codigoInput.trim()}
            onClick={handleVincular}
          >
            Vincular
          </Button>
        </div>
      ) : (
        <p className="text-xs text-text-base/55">Sin vínculo con Mercado Público.</p>
      )}
    </div>
  );
}
