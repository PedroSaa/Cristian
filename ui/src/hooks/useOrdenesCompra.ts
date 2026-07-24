import { useCallback, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  agregarAdjuntoOrdenCompra,
  anularOrdenCompra,
  aprobarOrdenCompra,
  createOrdenCompra,
  desvincularMercadoPublicoOrdenCompra,
  eliminarAdjuntoOrdenCompra,
  enviarAprobacionOrdenCompra,
  getOrdenCompra,
  listOrdenesCompra,
  marcarEnviadaOrdenCompra,
  rechazarOrdenCompra,
  updateOrdenCompra,
  vincularMercadoPublicoOrdenCompra,
} from '../lib/api/ordenesCompra';
import { listProveedores } from '../lib/api/proveedores';
import type {
  ActualizarOrdenCompraRequest,
  AgregarAdjuntoOrdenCompraRequest,
  CrearOrdenCompraRequest,
  OrdenCompraFilters,
} from '../types/ordenCompra';

const QUERY_ROOT = 'ordenes-compra';

// ─── List (server-side pagination + filters) ─────────────────────────────────

const DEFAULT_FILTERS: OrdenCompraFilters = {
  page: 1,
  pageSize: 20,
};

export function useOrdenesCompraList() {
  const [filtros, setFiltrosState] = useState<OrdenCompraFilters>(DEFAULT_FILTERS);

  const { data, isLoading, isError, error } = useQuery({
    queryKey: [QUERY_ROOT, filtros] as const,
    queryFn: () => listOrdenesCompra(filtros),
  });

  const setFiltros = useCallback((partial: Partial<OrdenCompraFilters>) => {
    setFiltrosState((prev) => ({ ...prev, ...partial, page: 1 }));
  }, []);

  const resetFiltros = useCallback(() => {
    setFiltrosState(DEFAULT_FILTERS);
  }, []);

  const handlePaginaChange = useCallback((pagina: number) => {
    setFiltrosState((prev) => ({ ...prev, page: pagina }));
  }, []);

  const handleTamanoPaginaChange = useCallback((tamanoPagina: number) => {
    setFiltrosState((prev) => ({ ...prev, pageSize: tamanoPagina, page: 1 }));
  }, []);

  return {
    data,
    isLoading,
    isError,
    error,
    filtros,
    setFiltros,
    resetFiltros,
    handlePaginaChange,
    handleTamanoPaginaChange,
  };
}

// ─── Detail ──────────────────────────────────────────────────────────────────

export function useOrdenCompra(id: string | null) {
  return useQuery({
    queryKey: [QUERY_ROOT, 'detalle', id] as const,
    queryFn: () => getOrdenCompra(id!),
    enabled: !!id,
  });
}

// ─── Active providers for the selector (read-only consumption) ───────────────

// Backend cap: ListProveedoresValidator allows PageSize between 1 and 100,
// so the selector iterates every page and concatenates the results.
const PROVEEDORES_SELECTOR_PAGE_SIZE = 100;

export function useProveedoresActivos() {
  return useQuery({
    queryKey: ['proveedores', 'selector-activos'] as const,
    queryFn: async () => {
      const first = await listProveedores({
        page: 1,
        pageSize: PROVEEDORES_SELECTOR_PAGE_SIZE,
        incluirInactivos: false,
      });
      const items = [...first.items];
      for (let page = 2; page <= first.totalPaginas; page += 1) {
        const next = await listProveedores({
          page,
          pageSize: PROVEEDORES_SELECTOR_PAGE_SIZE,
          incluirInactivos: false,
        });
        items.push(...next.items);
      }
      return { ...first, items };
    },
    staleTime: 5 * 60 * 1000,
  });
}

// ─── Mutations (all invalidate the OC cache) ─────────────────────────────────

function useInvalidateOrdenesCompra() {
  const queryClient = useQueryClient();
  return useCallback(
    () => queryClient.invalidateQueries({ queryKey: [QUERY_ROOT] }),
    [queryClient],
  );
}

export function useCrearOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: (req: CrearOrdenCompraRequest) => createOrdenCompra(req),
    onSuccess: invalidate,
  });
}

export function useActualizarOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: ActualizarOrdenCompraRequest }) =>
      updateOrdenCompra(id, req),
    onSuccess: invalidate,
  });
}

export function useEnviarAprobacionOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: (id: string) => enviarAprobacionOrdenCompra(id),
    onSuccess: invalidate,
  });
}

export function useAprobarOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: ({ id, comentario }: { id: string; comentario?: string }) =>
      aprobarOrdenCompra(id, comentario),
    onSuccess: invalidate,
  });
}

export function useRechazarOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: ({ id, comentario }: { id: string; comentario: string }) =>
      rechazarOrdenCompra(id, comentario),
    onSuccess: invalidate,
  });
}

export function useMarcarEnviadaOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: (id: string) => marcarEnviadaOrdenCompra(id),
    onSuccess: invalidate,
  });
}

export function useAnularOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: ({ id, motivo }: { id: string; motivo: string }) =>
      anularOrdenCompra(id, motivo),
    onSuccess: invalidate,
  });
}

export function useAgregarAdjuntoOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: ({ id, req }: { id: string; req: AgregarAdjuntoOrdenCompraRequest }) =>
      agregarAdjuntoOrdenCompra(id, req),
    onSuccess: invalidate,
  });
}

export function useEliminarAdjuntoOrdenCompra() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: ({ id, adjuntoId }: { id: string; adjuntoId: string }) =>
      eliminarAdjuntoOrdenCompra(id, adjuntoId),
    onSuccess: invalidate,
  });
}

// ─── Mercado Público (ChileCompra) ───────────────────────────────────────────

export function useVincularMercadoPublico() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: ({ id, codigo }: { id: string; codigo: string }) =>
      vincularMercadoPublicoOrdenCompra(id, codigo),
    onSuccess: invalidate,
  });
}

export function useDesvincularMercadoPublico() {
  const invalidate = useInvalidateOrdenesCompra();
  return useMutation({
    mutationFn: (id: string) => desvincularMercadoPublicoOrdenCompra(id),
    onSuccess: invalidate,
  });
}
