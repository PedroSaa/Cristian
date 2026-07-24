import { describe, expect, it } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import LegacyCrudSection from './LegacyCrudSection';

interface Item {
  id: number;
  nombre: string;
}

function buildItems(count: number): Item[] {
  return Array.from({ length: count }, (_, i) => ({ id: i + 1, nombre: `Item ${i + 1}` }));
}

function renderSection(items: Item[]) {
  return render(
    <LegacyCrudSection<Item>
      title="Listado"
      items={items}
      columns={[{ header: 'Nombre', render: (item) => item.nombre }]}
      getRowKey={(item) => String(item.id)}
      isLoading={false}
      isError={false}
      errorMessage="No se pudo cargar."
      emptyMessage="Sin datos."
      canEdit={false}
    />,
  );
}

describe('LegacyCrudSection client-side pagination', () => {
  it('shows only the first 20 rows and the pagination controls with more than 20 items', async () => {
    renderSection(buildItems(25));

    expect(await screen.findByText('Filas por página')).toBeInTheDocument();
    expect(screen.getByText('1–20 de 25')).toBeInTheDocument();

    // 1 header row + 20 data rows
    expect(screen.getAllByRole('row')).toHaveLength(21);
    expect(screen.getByText('Item 1')).toBeInTheDocument();
    expect(screen.getByText('Item 20')).toBeInTheDocument();
    expect(screen.queryByText('Item 21')).not.toBeInTheDocument();
  });

  it('shows the remaining rows when navigating to page 2', async () => {
    renderSection(buildItems(25));

    await screen.findByText('Filas por página');
    fireEvent.click(screen.getByRole('button', { name: 'Página siguiente' }));

    expect(screen.getByText('21–25 de 25')).toBeInTheDocument();
    // 1 header row + 5 data rows
    expect(screen.getAllByRole('row')).toHaveLength(6);
    expect(screen.getByText('Item 21')).toBeInTheDocument();
    expect(screen.getByText('Item 25')).toBeInTheDocument();
    expect(screen.queryByText('Item 1')).not.toBeInTheDocument();
  });

  it('does not render pagination when there are no items', () => {
    renderSection([]);

    expect(screen.queryByText('Filas por página')).not.toBeInTheDocument();
    expect(screen.getByText('Sin datos.')).toBeInTheDocument();
  });
});
