import { forwardRef, type SelectHTMLAttributes } from 'react';

interface SelectOption {
  value: string;
  label: string;
}

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  options: SelectOption[];
  placeholder?: string;
  error?: boolean;
}

const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ options, placeholder, error = false, className = '', ...props }, ref) => {
    return (
      <select
        ref={ref}
        className={[
          'block w-full rounded border px-3 py-2 text-sm text-text-base transition-colors',
          'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500',
          'disabled:bg-surface-secondary disabled:text-text-base/45 disabled:cursor-not-allowed',
          error ? 'border-primary-600 bg-surface-secondary' : 'border-border-base bg-surface hover:border-primary-300',
          className,
        ].join(' ')}
        {...props}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
    );
  },
);

Select.displayName = 'Select';
export default Select;
