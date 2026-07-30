import { Fragment } from 'react';
import type { ReactNode } from 'react';
import Badge from '../atoms/Badge';

export type SortDirection = 'asc' | 'desc';

export interface ColumnDef<T> {
  key: string;
  header: string;
  icon?: ReactNode;
  width?: string;
  hideBelow?: 'sm' | 'md' | 'lg' | 'xl';
  truncate?: boolean;
  sortable?: boolean;
  filterable?: boolean;
  filterType?: 'text' | 'number' | 'date' | 'select';
  filterOptions?: Array<{ value: string; label: string }>;
  filterPlaceholder?: string;
  render?: (row: T) => ReactNode;
}

interface DataGridProps<T extends { id: string }> {
  columns: ColumnDef<T>[];
  data: T[];
  selectedIds?: string[];
  onSelectRow?: (id: string, checked: boolean) => void;
  onSelectAll?: (checked: boolean) => void;
  onRowClick?: (row: T) => void;
  loading?: boolean;
  emptyMessage?: string;
  expandedId?: string | null;
  renderExpanded?: (row: T) => ReactNode;
  rowClassName?: (row: T) => string;
  sortColumn?: string;
  sortDirection?: SortDirection | null;
  onSort?: (column: string) => void;
  columnFilters?: Record<string, string>;
  onColumnFilterChange?: (column: string, value: string) => void;
}

const HIDE_CLASS: Record<NonNullable<ColumnDef<unknown>['hideBelow']>, string> = {
  sm: 'hidden sm:table-cell',
  md: 'hidden md:table-cell',
  lg: 'hidden lg:table-cell',
  xl: 'hidden xl:table-cell',
};

function cellClass(col: { hideBelow?: ColumnDef<unknown>['hideBelow']; truncate?: boolean }, base: string) {
  return [
    base,
    col.hideBelow ? HIDE_CLASS[col.hideBelow] : '',
    col.truncate ? 'max-w-0' : 'whitespace-nowrap',
  ].filter(Boolean).join(' ');
}

function SkeletonRow({ cols }: { cols: number }) {
  return (
    <tr className="animate-pulse">
      <td className="px-3 py-3"><div className="h-4 w-4 rounded bg-surface-secondary" /></td>
      {Array.from({ length: cols }).map((_, i) => (
        <td key={i} className="px-3 py-3">
          <div className="h-3.5 rounded bg-surface-secondary" style={{ width: `${60 + (i % 3) * 20}%` }} />
        </td>
      ))}
    </tr>
  );
}

function SortIcon({ active, direction }: { active: boolean; direction?: SortDirection | null }) {
  return (
    <span className={[
      'ml-1 inline-flex h-4 w-4 items-center justify-center rounded-full border text-[10px] leading-none',
      active ? 'border-primary-500 bg-primary-50 text-primary-700' : 'border-border-base text-text-base/30',
    ].join(' ')}>
      {direction === 'asc' ? '↑' : direction === 'desc' ? '↓' : '↕'}
    </span>
  );
}

function FilterCell({
  col,
  value,
  onChange,
}: {
  col: ColumnDef<unknown>;
  value: string;
  onChange: (value: string) => void;
}) {
  if (!col.filterable) return <div className="h-8" />;

  const common = 'w-full rounded border border-border-base bg-surface px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-primary-500';
  const placeholder = col.filterPlaceholder ?? `Buscar ${col.header.toLowerCase()}`;

  if (col.filterType === 'select' && col.filterOptions) {
    return (
      <select value={value} onChange={(e) => onChange(e.target.value)} className={common}>
        <option value="">Todos</option>
        {col.filterOptions.map((opt) => (
          <option key={opt.value} value={opt.value}>{opt.label}</option>
        ))}
      </select>
    );
  }

  return (
    <input
      type={col.filterType === 'number' ? 'number' : col.filterType === 'date' ? 'date' : 'text'}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      placeholder={placeholder}
      className={common}
    />
  );
}

function getCellContent<T>(row: T, col: ColumnDef<T>): ReactNode {
  return col.render ? col.render(row) : String((row as Record<string, unknown>)[col.key] ?? '');
}

export default function DataGrid<T extends { id: string }>({
  columns,
  data,
  selectedIds = [],
  onSelectRow,
  onSelectAll,
  onRowClick,
  loading = false,
  emptyMessage = 'No hay documentos para mostrar.',
  expandedId,
  renderExpanded,
  rowClassName,
  sortColumn,
  sortDirection,
  onSort,
  columnFilters,
  onColumnFilterChange,
}: DataGridProps<T>) {
  const allSelected = data.length > 0 && data.every((row) => selectedIds.includes(row.id));
  const someSelected = selectedIds.length > 0 && !allSelected;
  const isActivationKey = (key: string) => key === 'Enter' || key === ' ' || key === 'Spacebar';
  const showFilters = Boolean(onColumnFilterChange);
  const mobileColumns = columns.filter((col) => !col.hideBelow || col.hideBelow === 'sm');

  return (
    <div className="overflow-x-auto rounded border border-border-base bg-surface">
      <div className="space-y-3 p-3 md:hidden">
        {loading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="rounded-lg border border-border-base bg-surface-secondary p-3 animate-pulse">
              <div className="h-4 w-1/2 rounded bg-surface" />
              <div className="mt-2 space-y-2">
                <div className="h-3 w-3/4 rounded bg-surface" />
                <div className="h-3 w-2/3 rounded bg-surface" />
                <div className="h-3 w-1/2 rounded bg-surface" />
              </div>
            </div>
          ))
        ) : data.length === 0 ? (
          <div className="px-2 py-8 text-center text-sm text-text-base/45">{emptyMessage}</div>
        ) : (
          data.map((row) => {
            const isSelected = selectedIds.includes(row.id);
            const isExpanded = expandedId === row.id;
            const expandedRowId = renderExpanded ? `expanded-row-${row.id}` : undefined;
            return (
              <div
                key={row.id}
                className={[
                  'rounded-lg border border-border-base bg-surface px-3 py-3 shadow-sm',
                  isSelected ? 'ring-1 ring-primary-400' : '',
                  isExpanded ? 'bg-primary-50/30' : '',
                ].filter(Boolean).join(' ')}
                role={onRowClick ? 'button' : undefined}
                tabIndex={onRowClick ? 0 : undefined}
                onClick={() => onRowClick?.(row)}
                onKeyDown={(e) => {
                  if (!onRowClick || e.currentTarget !== e.target || !isActivationKey(e.key)) return;
                  e.preventDefault();
                  onRowClick(row);
                }}
                aria-expanded={renderExpanded ? isExpanded : undefined}
                aria-controls={expandedRowId}
              >
                <div className="flex items-start gap-3">
                  {onSelectRow && (
                    <input
                      type="checkbox"
                      checked={isSelected}
                      onChange={(e) => onSelectRow(row.id, e.target.checked)}
                      onClick={(e) => e.stopPropagation()}
                      className="mt-1 h-4 w-4 shrink-0 rounded border-border-base text-primary-600"
                      aria-label={`Seleccionar fila ${row.id}`}
                    />
                  )}
                  <div className="min-w-0 flex-1 space-y-2">
                    {mobileColumns.slice(0, 3).map((col) => (
                      <div key={col.key} className="flex items-start justify-between gap-3">
                        <span className="text-[11px] font-semibold uppercase tracking-wide text-text-base/45">{col.header}</span>
                        <span className="text-right text-sm text-text-base">
                          {col.truncate ? (
                            <span className="block max-w-[18rem] truncate text-right" title={String((row as Record<string, unknown>)[col.key] ?? '')}>
                              {getCellContent(row, col)}
                            </span>
                          ) : (
                            getCellContent(row, col)
                          )}
                        </span>
                      </div>
                    ))}
                    {mobileColumns.length > 3 && (
                      <details className="rounded-md border border-border-base bg-surface-secondary/40 px-3 py-2">
                        <summary className="cursor-pointer text-xs font-medium text-primary-700">Ver más</summary>
                        <div className="mt-2 space-y-2">
                          {mobileColumns.slice(3).map((col) => (
                            <div key={col.key} className="flex items-start justify-between gap-3">
                              <span className="text-[11px] font-semibold uppercase tracking-wide text-text-base/45">{col.header}</span>
                              <span className="text-right text-sm text-text-base">
                                {col.truncate ? (
                                  <span className="block max-w-[18rem] truncate text-right" title={String((row as Record<string, unknown>)[col.key] ?? '')}>
                                    {getCellContent(row, col)}
                                  </span>
                                ) : (
                                  getCellContent(row, col)
                                )}
                              </span>
                            </div>
                          ))}
                        </div>
                      </details>
                    )}
                    {renderExpanded && isExpanded && (
                      <div id={expandedRowId} className="pt-2">
                        {renderExpanded(row)}
                      </div>
                    )}
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>

      <table className="hidden w-full table-fixed divide-y divide-border-base text-sm md:table">
        <colgroup>
          {onSelectRow && <col className="w-10" />}
          {columns.map((col) => (
            <col key={col.key} style={col.width ? { width: col.width } : undefined} />
          ))}
        </colgroup>

        <thead className="sticky top-0 z-20 bg-surface-secondary/95 backdrop-blur supports-[backdrop-filter]:bg-surface-secondary/85">
          <tr>
            {onSelectRow && (
              <th className="w-10 px-3 py-2.5">
                <input
                  type="checkbox"
                  checked={allSelected}
                  ref={(el) => { if (el) el.indeterminate = someSelected; }}
                  onChange={(e) => onSelectAll?.(e.target.checked)}
                  className="h-4 w-4 cursor-pointer rounded border-border-base text-primary-600"
                  aria-label="Seleccionar todos"
                />
              </th>
            )}
            {columns.map((col) => {
              const isSortable = col.sortable && onSort;
              const isActive = sortColumn === col.key;
              return (
                <th
                  key={col.key}
                  className={cellClass(
                    col,
                    [
                      'px-3 py-2.5 text-left text-sm font-semibold tracking-wide text-text-base/75',
                      isSortable ? 'cursor-pointer select-none hover:bg-surface-secondary' : '',
                    ].join(' '),
                  )}
                  onClick={isSortable ? () => onSort(col.key) : undefined}
                  aria-sort={
                    isActive && sortDirection === 'asc' ? 'ascending' :
                    isActive && sortDirection === 'desc' ? 'descending' :
                    isSortable ? 'none' : undefined
                  }
                >
                    <span className={col.truncate ? 'flex items-center gap-1 truncate' : 'flex items-center gap-1'}>
                    {col.icon}
                    <span className="truncate">{col.header}</span>
                    {isSortable && (
                      <SortIcon
                        active={isActive}
                        direction={isActive ? sortDirection : null}
                      />
                    )}
                  </span>
                </th>
              );
            })}
          </tr>
          {showFilters && (
            <tr className="bg-surface/70">
              {onSelectRow && <th className="px-3 py-2" />}
              {columns.map((col) => (
                <th key={`${col.key}-filter`} className={cellClass(col, 'px-2 py-2 align-top')}>
                  <FilterCell
                    col={col}
                    value={columnFilters?.[col.key] ?? ''}
                    onChange={(value) => onColumnFilterChange?.(col.key, value)}
                  />
                </th>
              ))}
            </tr>
          )}
        </thead>

        <tbody className="divide-y divide-border-base">
          {loading ? (
            Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={columns.length} />)
          ) : data.length === 0 ? (
            <tr>
              <td
                colSpan={columns.length + (onSelectRow ? 1 : 0)}
                className="px-4 py-12 text-center text-text-base/45"
              >
                {emptyMessage}
              </td>
            </tr>
          ) : (
            data.map((row, index) => {
              const isSelected = selectedIds.includes(row.id);
              const isExpanded = expandedId === row.id;
              const colSpan = columns.length + (onSelectRow ? 1 : 0);
              const expandedRowId = renderExpanded ? `expanded-row-${row.id}` : undefined;
              return (
                <Fragment key={row.id}>
                  <tr
                    onClick={() => onRowClick?.(row)}
                    onKeyDown={(e) => {
                      if (!onRowClick || e.target !== e.currentTarget || !isActivationKey(e.key)) return;
                      e.preventDefault();
                      onRowClick(row);
                    }}
                    tabIndex={onRowClick ? 0 : undefined}
                    aria-expanded={renderExpanded ? isExpanded : undefined}
                    aria-controls={expandedRowId}
                    className={[
                      'transition-colors duration-150',
                      onRowClick ? 'cursor-pointer' : '',
                      index % 2 === 0 ? 'bg-surface' : 'bg-surface-secondary/30',
                      isSelected && !isExpanded ? 'bg-primary-50' :
                      isExpanded ? 'border-l-4 border-l-primary-500 bg-primary-100' :
                      'hover:bg-primary-50/40',
                      rowClassName?.(row) ?? '',
                    ].join(' ')}
                  >
                    {onSelectRow && (
                      <td className="px-3 py-2.5" onClick={(e) => e.stopPropagation()}>
                        <input
                          type="checkbox"
                          checked={isSelected}
                          onChange={(e) => onSelectRow(row.id, e.target.checked)}
                          className="h-4 w-4 cursor-pointer rounded border-border-base text-primary-600"
                          aria-label={`Seleccionar fila ${row.id}`}
                        />
                      </td>
                    )}
                    {columns.map((col) => (
                      <td
                        key={col.key}
                        className={cellClass(col, 'px-3 py-2.5 text-text-base')}
                      >
                        {col.truncate ? (
                          <span className="block truncate" title={String((row as Record<string, unknown>)[col.key] ?? '')}>
                        {getCellContent(row, col)}
                      </span>
                        ) : (
                          getCellContent(row, col)
                        )}
                      </td>
                    ))}
                  </tr>
                  {isExpanded && renderExpanded && (
                    <tr id={expandedRowId} className="bg-surface-secondary">
                      <td colSpan={colSpan} className="border-l-4 border-l-primary-500 p-0">
                        <div className="animate-[slideDown_150ms_ease-out]">
                          {renderExpanded(row)}
                        </div>
                      </td>
                    </tr>
                  )}
                </Fragment>
              );
            })
          )}
        </tbody>
      </table>
    </div>
  );
}

export function EstadoBadge({ estado }: { estado: string }) {
  const map: Record<string, 'success' | 'warning' | 'danger' | 'info' | 'neutral' | 'default'> = {
    Pendiente: 'warning',
    Recibido: 'neutral',
    EnProceso: 'success',
    Firmado: 'default',
    Despachado: 'info',
    Archivado: 'neutral',
    Anulado: 'danger',
    Cerrado: 'neutral',
  };
  if (estado === 'Firmado') {
    return (
      <span className="inline-flex items-center gap-1 rounded px-2 py-0.5 text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
        <svg viewBox="0 0 16 16" fill="none" stroke="currentColor" strokeWidth="1.7" className="h-3.5 w-3.5">
          <path d="M4 12.5h8" strokeLinecap="round" />
          <path d="M4.2 10.4c1.1-2 2.3-2.2 3.1-1.4l.8.8c.7.7 1.7.5 2.3-.2l1.2-1.2" strokeLinecap="round" strokeLinejoin="round" />
          <path d="M10.8 4.2l1-1a1.4 1.4 0 0 1 2 2l-1 1" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
        <span>Firmado</span>
      </span>
    );
  }
  return <Badge variant={map[estado] ?? 'neutral'}>{estado}</Badge>;
}
