import { useEffect } from 'react';
import { useForm, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import type { RespaldoConfigDto } from '../../lib/api/admin/adminRespaldosApi';
import FormField from '../../components/molecules/FormField';
import Toggle from '../../components/atoms/Toggle';
import Button from '../../components/atoms/Button';

const respaldoConfigSchema = z.object({
  intervaloMinutos: z.coerce.number().int().min(1, 'Debe ser mayor a 0'),
  habilitado: z.boolean(),
  maxBackupCount: z.coerce.number().int().min(0, 'No puede ser negativo'),
  retentionDays: z.coerce.number().int().min(0, 'No puede ser negativo'),
  outputPath: z.string().min(1, 'La ruta de salida es obligatoria'),
  timeoutMinutos: z.coerce.number().int().min(1, 'Debe ser mayor a 0'),
});

export type RespaldoConfigFormData = z.infer<typeof respaldoConfigSchema>;

interface RespaldoConfigFormProps {
  config?: RespaldoConfigDto;
  isLoading?: boolean;
  isError?: boolean;
  isSaving?: boolean;
  saveError?: string | null;
  canEdit?: boolean;
  onSave: (data: RespaldoConfigFormData) => void;
}

export default function RespaldoConfigForm({
  config,
  isLoading,
  isError,
  isSaving,
  saveError,
  canEdit = true,
  onSave,
}: RespaldoConfigFormProps) {
  const form = useForm<RespaldoConfigFormData>({
    resolver: zodResolver(respaldoConfigSchema) as Resolver<RespaldoConfigFormData>,
    defaultValues: {
      intervaloMinutos: 60,
      habilitado: true,
      maxBackupCount: 5,
      retentionDays: 7,
      outputPath: './Respaldos',
      timeoutMinutos: 30,
    },
  });

  useEffect(() => {
    if (config) {
      form.reset({
        intervaloMinutos: config.intervaloMinutos,
        habilitado: config.habilitado,
        maxBackupCount: config.maxBackupCount,
        retentionDays: config.retentionDays,
        outputPath: config.outputPath,
        timeoutMinutos: config.timeoutMinutos,
      });
    }
  }, [config, form]);

  if (isLoading) {
    return <p className="text-gray-500 text-sm">Cargando configuración…</p>;
  }

  if (isError) {
    return <p className="text-red-600 text-sm">No se pudo cargar la configuración.</p>;
  }

  return (
    <form onSubmit={form.handleSubmit(onSave)} className="space-y-4 max-w-lg">
      {saveError && <p className="text-sm text-red-600">{saveError}</p>}

      <FormField label="Intervalo (minutos)" error={form.formState.errors.intervaloMinutos?.message}>
          <input
            {...form.register('intervaloMinutos')}
            type="number"
            min={1}
            aria-label="Intervalo en minutos"
            disabled={!canEdit}
            className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
          />
      </FormField>

      <div className="mb-4">
          <Toggle
            checked={form.watch('habilitado')}
            onChange={(e) => form.setValue('habilitado', e.target.checked)}
            label="Respaldos automáticos habilitados"
            disabled={!canEdit}
          />
      </div>

      <FormField label="Máximo de respaldos" error={form.formState.errors.maxBackupCount?.message}>
          <input
            {...form.register('maxBackupCount')}
            type="number"
            min={0}
            aria-label="Máximo de respaldos"
            disabled={!canEdit}
            className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
          />
      </FormField>

      <FormField label="Días de retención" error={form.formState.errors.retentionDays?.message}>
          <input
            {...form.register('retentionDays')}
            type="number"
            min={0}
            aria-label="Días de retención"
            disabled={!canEdit}
            className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
          />
      </FormField>

      <FormField label="Ruta de salida" error={form.formState.errors.outputPath?.message}>
          <input
            {...form.register('outputPath')}
            type="text"
            aria-label="Ruta de salida"
            disabled={!canEdit}
            className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
          />
      </FormField>

      <FormField label="Timeout (minutos)" error={form.formState.errors.timeoutMinutos?.message}>
          <input
            {...form.register('timeoutMinutos')}
            type="number"
            min={1}
            aria-label="Timeout en minutos"
            disabled={!canEdit}
            className="w-full border border-gray-300 rounded px-3 py-2 text-sm"
          />
      </FormField>

      <div className="pt-2">
        {canEdit && (
          <Button
            type="submit"
            loading={isSaving}
            disabled={isSaving || !form.formState.isDirty}
          >
            Guardar Configuración
          </Button>
        )}
      </div>
    </form>
  );
}
