import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import RespaldoConfigForm from './RespaldoConfigForm';

const mockConfig = {
  id: '80000000-0000-0000-0000-000000000001',
  intervaloMinutos: 60,
  habilitado: true,
  maxBackupCount: 5,
  retentionDays: 7,
  outputPath: './Respaldos',
  timeoutMinutos: 30,
  actualizadoEn: '2026-05-17T00:00:00Z',
};

function renderForm(props: Partial<Parameters<typeof RespaldoConfigForm>[0]> = {}) {
  const onSave = vi.fn();
  return {
    onSave,
    ...render(
      <RespaldoConfigForm
        config={mockConfig}
        onSave={onSave}
        {...props}
      />,
    ),
  };
}

describe('RespaldoConfigForm', () => {
  it('renders all form fields with mock config values', () => {
    renderForm();

    const intervalInput = screen.getByLabelText(/intervalo/i) as HTMLInputElement;
    expect(intervalInput).toBeInTheDocument();
    expect(intervalInput.value).toBe('60');

    // Toggle should be checked (habilitado = true)
    const toggle = screen.getByRole('switch');
    expect(toggle).toBeChecked();

    const maxBackupInput = screen.getByLabelText(/máximo de respaldos/i) as HTMLInputElement;
    expect(maxBackupInput).toBeInTheDocument();
    expect(maxBackupInput.value).toBe('5');

    const retentionInput = screen.getByLabelText(/días de retención/i) as HTMLInputElement;
    expect(retentionInput).toBeInTheDocument();
    expect(retentionInput.value).toBe('7');

    const outputPathInput = screen.getByLabelText(/ruta de salida/i) as HTMLInputElement;
    expect(outputPathInput).toBeInTheDocument();
    expect(outputPathInput.value).toBe('./Respaldos');

    const timeoutInput = screen.getByLabelText(/timeout/i) as HTMLInputElement;
    expect(timeoutInput).toBeInTheDocument();
    expect(timeoutInput.value).toBe('30');
  });

  it('shows loading message when isLoading is true', () => {
    renderForm({ isLoading: true });
    expect(screen.getByText('Cargando configuración…')).toBeInTheDocument();
  });

  it('shows error message when isError is true', () => {
    renderForm({ isError: true });
    expect(screen.getByText('No se pudo cargar la configuración.')).toBeInTheDocument();
  });

  it('disables save button when form is pristine', () => {
    renderForm();
    const saveBtn = screen.getByRole('button', { name: /guardar configuración/i });
    expect(saveBtn).toBeDisabled();
  });

  it('disables save button when saving is in progress', async () => {
    renderForm({ isSaving: true });

    const saveBtn = screen.getByRole('button', { name: /guardar configuración/i });
    expect(saveBtn).toBeDisabled();
    expect(saveBtn).toHaveAttribute('aria-busy', 'true');
  });

  it('calls onSave with form data on valid submit', async () => {
    const user = userEvent.setup();
    const { onSave } = renderForm();

    // Change a field to make form dirty
    const intervalInput = screen.getByLabelText(/intervalo/i);
    await user.clear(intervalInput);
    await user.type(intervalInput, '120');

    const saveBtn = screen.getByRole('button', { name: /guardar configuración/i });
    expect(saveBtn).not.toBeDisabled();

    await user.click(saveBtn);

    expect(onSave).toHaveBeenCalledTimes(1);
    expect(onSave).toHaveBeenCalledWith(
      expect.objectContaining({ intervaloMinutos: 120 }),
      expect.anything(),
    );
  });

  it('shows validation error for intervaloMinutos=0 and blocks save', async () => {
    const user = userEvent.setup();
    const { onSave } = renderForm();

    const intervalInput = screen.getByLabelText(/intervalo/i);
    await user.clear(intervalInput);
    await user.type(intervalInput, '0');

    // Submit the form directly to trigger validation
    const form = intervalInput.closest('form')!;
    fireEvent.submit(form);

    await waitFor(() => {
      expect(screen.getByText('Debe ser mayor a 0')).toBeInTheDocument();
    });
    expect(onSave).not.toHaveBeenCalled();
  });

  it('shows validation error for negative retentionDays', async () => {
    const user = userEvent.setup();
    const { onSave } = renderForm();

    const retentionInput = screen.getByLabelText(/días de retención/i);
    await user.clear(retentionInput);
    await user.type(retentionInput, '-1');

    const form = retentionInput.closest('form')!;
    fireEvent.submit(form);

    await waitFor(() => {
      expect(screen.getByText('No puede ser negativo')).toBeInTheDocument();
    });
    expect(onSave).not.toHaveBeenCalled();
  });
});
