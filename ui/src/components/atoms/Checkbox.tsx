import { forwardRef, type InputHTMLAttributes } from 'react';

interface CheckboxProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: string;
}

const Checkbox = forwardRef<HTMLInputElement, CheckboxProps>(
  ({ label, id, className = '', disabled, ...props }, ref) => {
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
        <input
          ref={ref}
          id={inputId}
          type="checkbox"
          disabled={disabled}
          className={[
            'h-4 w-4 rounded border-border-base text-primary-600',
            'focus:ring-2 focus:ring-primary-500 focus:ring-offset-1',
            'disabled:cursor-not-allowed',
          ].join(' ')}
          {...props}
        />
        {label && <span className="text-sm text-text-base/70">{label}</span>}
      </label>
    );
  },
);

Checkbox.displayName = 'Checkbox';
export default Checkbox;
