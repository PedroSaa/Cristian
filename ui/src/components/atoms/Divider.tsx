interface DividerProps {
  orientation?: 'horizontal' | 'vertical';
  label?: string;
  className?: string;
}

export default function Divider({ orientation = 'horizontal', label, className = '' }: DividerProps) {
  if (orientation === 'vertical') {
    return (
      <div
        role="separator"
        aria-orientation="vertical"
        className={['inline-block h-full w-px bg-border-base', className].join(' ')}
      />
    );
  }

  if (label) {
    return (
      <div className={['flex items-center gap-3', className].join(' ')} role="separator">
        <span className="flex-1 border-t border-border-base" />
        <span className="whitespace-nowrap text-xs font-medium text-text-base/70">{label}</span>
        <span className="flex-1 border-t border-border-base" />
      </div>
    );
  }

  return (
    <hr
      role="separator"
      className={[
        'border-t border-border-base',
        className,
      ].join(' ')}
    />
  );
}
