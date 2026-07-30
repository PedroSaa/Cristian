import { forwardRef } from 'react';

interface DatepickerProps {
  value?: string;
  onChange?: (value: string) => void;
  label?: string;
  error?: string;
  min?: string;
  max?: string;
  required?: boolean;
  disabled?: boolean;
  className?: string;
}

const Datepicker = forwardRef<HTMLInputElement, DatepickerProps>(
  ({ value, onChange, label, error, min, max, required, disabled, className = '' }, ref) => {
    const inputId = label?.toLowerCase().replace(/\s+/g, '-');

    return (
      <div className={className}>
        {label && (
          <label htmlFor={inputId} className="mb-1 block text-sm font-medium text-text-base/70">
            {label}
            {required && <span aria-hidden="true" className="ml-0.5 text-primary-700">*</span>}
          </label>
        )}
        <input
          ref={ref}
          id={inputId}
          type="date"
          value={value}
          min={min}
          max={max}
          required={required}
          disabled={disabled}
          onChange={(e) => onChange?.(e.target.value)}
          className={[
            'block w-full rounded border px-3 py-2 text-sm text-text-base shadow-sm transition-colors',
            'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500',
            disabled
              ? 'cursor-not-allowed bg-surface-secondary text-text-base/45'
              : 'bg-surface',
            error
              ? 'border-primary-600 focus:ring-primary-500 focus:border-primary-600'
              : 'border-border-base',
          ].join(' ')}
        />
        {error && <p className="mt-1 text-xs text-primary-700">{error}</p>}
      </div>
    );
  },
);

Datepicker.displayName = 'Datepicker';
export default Datepicker;
