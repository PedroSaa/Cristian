import { useEffect, useState, type ReactNode } from 'react';
import Button from '../../components/atoms/Button';
import IconButton from '../../components/atoms/IconButton';
import SearchBar from '../../components/molecules/SearchBar';
import Pagination from '../../components/molecules/Pagination';
import Spinner from '../../components/atoms/Spinner';

export interface LegacyCrudColumn<T> {
  header: string;
  render: (item: T) => ReactNode;
  className?: string;
}

interface LegacyCrudSectionProps<T> {
  title: string;
  description?: string;
  items: T[];
  columns: LegacyCrudColumn<T>[];
  getRowKey: (item: T) => string;
  isLoading: boolean;
  isError: boolean;
  errorMessage: string;
  emptyMessage: string;
  canEdit: boolean;
  onCreate?: () => void;
  onView?: (item: T) => void;
  onDownload?: (item: T) => void;
  onEdit?: (item: T) => void;
  onEditContent?: (item: T) => void;
  onDelete?: (item: T) => void;
  /** Acciones adicionales por fila (ej. "Medidas" en plantillas). */
  extraActions?: (item: T) => ReactNode;
  actionLabel?: string;
  searchValue?: string;
  searchPlaceholder?: string;
  onSearchChange?: (value: string) => void;
  searchEmptyMessage?: string;
}

export default function LegacyCrudSection<T>({
  title,
  description,
  items,
  columns,
  getRowKey,
  isLoading,
  isError,
  errorMessage,
  emptyMessage,
  canEdit,
  onCreate,
  onView,
  onDownload,
  onEdit,
  onEditContent,
  onDelete,
  extraActions,
  actionLabel = 'Crear',
  searchValue,
  searchPlaceholder = 'Buscar...',
  onSearchChange,
  searchEmptyMessage = 'No se encontraron resultados.',
}: LegacyCrudSectionProps<T>) {
  const showActions = Boolean(onView) || Boolean(onDownload) || Boolean(extraActions) || (canEdit && (Boolean(onEdit) || Boolean(onEditContent) || Boolean(onDelete)));
  const isSearching = Boolean(searchValue?.trim());

  const [pagina, setPagina] = useState(1);
  const [tamanoPagina, setTamanoPagina] = useState(20);
  const totalItems = items.length;
  const totalPaginas = Math.max(1, Math.ceil(totalItems / tamanoPagina));

  // Clamp: when items shrink (search/delete) or page size changes, keep the page valid.
  useEffect(() => {
    const maxPagina = Math.max(1, Math.ceil(items.length / tamanoPagina));
    setPagina((current) => Math.min(current, maxPagina));
  }, [items.length, tamanoPagina]);

  const visibleItems = items.slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina);

  return (
    <section className="rounded-lg border border-gray-200 bg-white p-4 shadow-sm">
      <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
        <div>
          <h3 className="text-base font-semibold text-gray-800">{title}</h3>
          {description && <p className="mt-1 text-sm text-gray-500">{description}</p>}
        </div>
        <div className="flex flex-wrap items-end gap-3">
          {onSearchChange && (
            <div className="min-w-56">
              <SearchBar value={searchValue ?? ''} onChange={onSearchChange} placeholder={searchPlaceholder} />
            </div>
          )}
          {canEdit && onCreate && (
            <Button onClick={onCreate}>+ {actionLabel}</Button>
          )}
        </div>
      </div>

      {isLoading && (
        <div className="flex justify-center py-8">
          <Spinner size="md" />
        </div>
      )}

      {isError && !isLoading && (
        <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
          {errorMessage}
        </p>
      )}

      {!isLoading && !isError && (
        <div className="overflow-x-auto rounded border border-gray-200">
          <table className="min-w-full text-sm">
            <thead className="bg-gray-50 text-xs uppercase text-gray-500">
              <tr>
                {columns.map((column) => (
                  <th key={column.header} className={`px-4 py-2 text-left ${column.className ?? ''}`}>
                    {column.header}
                  </th>
                ))}
                {showActions && <th className="px-4 py-2 text-left">Acciones</th>}
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {visibleItems.map((item) => (
                <tr key={getRowKey(item)} className="hover:bg-gray-50">
                  {columns.map((column) => (
                    <td key={column.header} className={`px-4 py-2 align-top ${column.className ?? ''}`}>
                      {column.render(item)}
                    </td>
                  ))}
                  {showActions && (
                    <td className="px-4 py-2">
                      <div className="flex gap-1">
                        {onView && (
                          <IconButton name="eye" tooltip="Ver" appearance="admin" onClick={() => onView(item)} />
                        )}
                        {onDownload && (
                          <IconButton name="download" tooltip="Descargar" appearance="admin" onClick={() => onDownload(item)} />
                        )}
                        {canEdit && onEdit && (
                          <IconButton name="edit" tooltip="Editar" appearance="admin" onClick={() => onEdit(item)} />
                        )}
                        {canEdit && onEditContent && (
                          <IconButton name="file-text" tooltip="Editar contenido (Word)" appearance="admin" onClick={() => onEditContent(item)} />
                        )}
                        {extraActions && extraActions(item)}
                        {canEdit && onDelete && (
                          <IconButton name="trash" tooltip="Eliminar" variant="danger" appearance="admin" onClick={() => onDelete(item)} />
                        )}
                      </div>
                    </td>
                  )}
                </tr>
              ))}
              {totalItems === 0 && (
                <tr>
                  <td colSpan={columns.length + (showActions ? 1 : 0)} className="px-4 py-8 text-center text-sm text-gray-500">
                    {isSearching ? searchEmptyMessage : emptyMessage}
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          {totalItems > 0 && (
            <Pagination
              pagina={pagina}
              totalPaginas={totalPaginas}
              totalItems={totalItems}
              tamanoPagina={tamanoPagina}
              onChange={setPagina}
              onTamanoPaginaChange={(tamano) => { setTamanoPagina(tamano); setPagina(1); }}
            />
          )}
        </div>
      )}
    </section>
  );
}
