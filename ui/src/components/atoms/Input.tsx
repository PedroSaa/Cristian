import { forwardRef, type InputHTMLAttributes } from 'react';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  error?: boolean;
}

const Input = forwardRef<HTMLInputElement, InputProps>(({ error = false, className = '', ...props }, ref) => {
  return (
    <input
      ref={ref}
      className={[
        'block w-full rounded border px-3 py-2 text-sm text-text-base placeholder:text-text-base/45 transition-colors',
        'focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500',
        'disabled:bg-surface-secondary disabled:text-text-base/45 disabled:cursor-not-allowed',
        error ? 'border-primary-600 bg-surface-secondary' : 'border-border-base bg-surface hover:border-primary-300',
        className,
      ].join(' ')}
      {...props}
    />
  );
});

Input.displayName = 'Input';
export default Input;
