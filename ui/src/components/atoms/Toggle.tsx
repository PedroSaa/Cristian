import { forwardRef, type InputHTMLAttributes } from 'react';

interface ToggleProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string;
}

const Toggle = forwardRef<HTMLInputElement, ToggleProps>(
  ({ label, id, checked, className = '', disabled, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-');

    return (
      <label
        htmlFor={inputId}
        className={[
          'inline-flex items-center gap-2 cursor-pointer',
          disabled ? 'cursor-not-allowed opacity-50' : '',
          className,
        ].join(' ')}
      >
        <span className="relative inline-flex items-center">
          <input
            ref={ref}
            id={inputId}
            type="checkbox"
            role="switch"
            checked={checked}
            disabled={disabled}
            className="peer sr-only"
            {...props}
          />
          <span
            className={[
               'block h-5 w-9 rounded-full transition-colors',
               'bg-border-base peer-checked:bg-primary-600',
               'peer-focus:ring-2 peer-focus:ring-primary-500 peer-focus:ring-offset-1',
               disabled ? 'peer-disabled:cursor-not-allowed' : '',
             ].join(' ')}
           />
           <span
             className={[
              'absolute left-0.5 top-0.5 h-4 w-4 rounded-full bg-surface shadow-sm transition-transform',
              'peer-checked:translate-x-4',
            ].join(' ')}
          />
        </span>
        {label && <span className="text-sm text-text-base/70">{label}</span>}
      </label>
    );
  },
);

Toggle.displayName = 'Toggle';
export default Toggle;
