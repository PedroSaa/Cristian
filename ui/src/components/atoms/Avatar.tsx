interface AvatarProps {
  src?: string;
  alt?: string;
  name?: string;
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

const sizeClasses = { sm: 'h-8 w-8 text-xs', md: 'h-10 w-10 text-sm', lg: 'h-12 w-12 text-base' };

const bgColors = [
  'bg-primary-50 text-primary-700',
  'bg-primary-100 text-primary-700',
  'bg-primary-200 text-primary-700',
  'bg-primary-500 text-white',
  'bg-primary-600 text-white',
  'bg-primary-700 text-white',
  'bg-surface-secondary text-text-base',
  'bg-border-base text-text-base',
];

function getInitials(name?: string): string {
  if (!name) return '?';
  return name
    .split(' ')
    .map((w) => w[0])
    .filter(Boolean)
    .slice(0, 2)
    .join('')
    .toUpperCase();
}

function getColorIndex(name?: string): number {
  if (!name) return 0;
  return [...name].reduce((acc, c) => acc + c.charCodeAt(0), 0) % bgColors.length;
}

export default function Avatar({ src, alt, name, size = 'md', className = '' }: AvatarProps) {
  if (src) {
    return (
      <img
        src={src}
        alt={alt ?? name ?? 'Avatar'}
        className={[
          'inline-block rounded-full object-cover',
          sizeClasses[size],
          className,
        ].join(' ')}
      />
    );
  }

  return (
    <span
      role="img"
      aria-label={alt ?? name ?? 'Avatar'}
      className={[
        'inline-flex items-center justify-center rounded-full font-medium',
        bgColors[getColorIndex(name)],
        sizeClasses[size],
        className,
      ].join(' ')}
    >
      {getInitials(name)}
    </span>
  );
}
