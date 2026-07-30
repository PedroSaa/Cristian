interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const sizeClasses = { sm: 'h-4 w-4', md: 'h-6 w-6', lg: 'h-8 w-8' };

export default function Spinner({ size = 'md', className = '' }: SpinnerProps) {
  return (
    <span
      role="status"
      aria-label="Cargando"
      className={[
        'inline-block animate-spin rounded-full border-2 border-primary-600 border-t-transparent',
        sizeClasses[size],
        className,
      ].join(' ')}
    />
  );
}
