import { cloneElement, isValidElement, useId } from 'react';
import type { ReactElement, ReactNode } from 'react';
import Label from '../atoms/Label';

interface FormFieldProps {
  label?: string;
  required?: boolean;
  error?: string;
  children: ReactNode;
  className?: string;
}

export default function FormField({ label, required, error, children, className = '' }: FormFieldProps) {
  const generatedId = useId();
  const child = isValidElement(children) ? (children as ReactElement<{
    id?: string;
    'aria-describedby'?: string;
    'aria-invalid'?: boolean | 'false' | 'true';
  }>) : null;
  const controlId = child?.props.id ?? generatedId;
  const errorId = `${controlId}-error`;
  const describedBy = [child?.props['aria-describedby'], error ? errorId : undefined]
    .filter(Boolean)
    .join(' ') || undefined;
  const control = child ? cloneElement(child, {
    id: controlId,
    'aria-describedby': describedBy,
    ...(error ? { 'aria-invalid': true } : {}),
  }) : children;

  return (
    <div className={className}>
      {label && <Label htmlFor={controlId} required={required}>{label}</Label>}
      {control}
      {error && <p id={errorId} className="mt-1 text-xs font-medium text-red-600">{error}</p>}
    </div>
  );
}
