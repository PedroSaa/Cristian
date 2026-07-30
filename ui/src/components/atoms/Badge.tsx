import type { HTMLAttributes } from 'react';

type BadgeVariant = 'default' | 'success' | 'warning' | 'danger' | 'info' | 'neutral';

interface BadgeProps extends HTMLAttributes<HTMLSpanElement> {
  variant?: BadgeVariant;
  size?: 'sm' | 'md';
}

const variantClasses: Record<BadgeVariant, string> = {
  default: 'bg-accent text-primary-700',
  success: 'bg-success-soft/20 text-success',
  warning: 'bg-warning/20 text-slate-700',
  danger: 'bg-error text-white',
  info: 'bg-primary-100 text-primary-700',
  neutral: 'bg-surface-secondary text-text-base',
};

export default function Badge({ variant = 'default', size = 'md', children, className = '', ...props }: BadgeProps) {
  return (
    <span
      className={[
        'inline-flex items-center rounded font-medium',
        size === 'sm' ? 'px-1.5 py-0.5 text-xs' : 'px-2 py-0.5 text-xs',
        variantClasses[variant],
        className,
      ].join(' ')}
      {...props}
    >
      {children}
    </span>
  );
}
