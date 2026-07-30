import type { LabelHTMLAttributes } from 'react';

interface LabelProps extends LabelHTMLAttributes<HTMLLabelElement> {
  required?: boolean;
}

export default function Label({ required, children, className = '', ...props }: LabelProps) {
  return (
    <label
      className={[
        'block mb-1 text-sm font-medium text-text-base/70',
        className,
      ].join(' ')}
      {...props}
    >
      {children}
      {required && <span aria-hidden="true" className="ml-0.5 text-primary-700">*</span>}
    </label>
  );
}
