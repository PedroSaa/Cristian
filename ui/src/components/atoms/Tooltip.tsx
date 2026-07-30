import { useState, useRef, useEffect, type ReactNode } from 'react';

interface TooltipProps {
  content: ReactNode;
  children: ReactNode;
  position?: 'top' | 'bottom' | 'left' | 'right';
  delay?: number;
}

const positionClasses: Record<string, string> = {
  top: 'bottom-full left-1/2 -translate-x-1/2 mb-2',
  bottom: 'top-full left-1/2 -translate-x-1/2 mt-2',
  left: 'right-full top-1/2 -translate-y-1/2 mr-2',
  right: 'left-full top-1/2 -translate-y-1/2 ml-2',
};

const arrowClasses: Record<string, string> = {
  top: 'top-full left-1/2 -translate-x-1/2 border-l-4 border-r-4 border-t-4 border-transparent border-t-surface',
  bottom: 'bottom-full left-1/2 -translate-x-1/2 border-l-4 border-r-4 border-b-4 border-transparent border-b-surface',
  left: 'left-full top-1/2 -translate-y-1/2 border-t-4 border-b-4 border-l-4 border-transparent border-l-surface',
  right: 'right-full top-1/2 -translate-y-1/2 border-t-4 border-b-4 border-r-4 border-transparent border-r-surface',
};

export default function Tooltip({ content, children, position = 'top', delay = 300 }: TooltipProps) {
  const [visible, setVisible] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout>>(null);

  useEffect(() => {
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, []);

  const show = () => {
    timerRef.current = setTimeout(() => setVisible(true), delay);
  };

  const hide = () => {
    if (timerRef.current) clearTimeout(timerRef.current);
    setVisible(false);
  };

  if (!content) return <>{children}</>;

  return (
    <span className="relative inline-flex" onMouseEnter={show} onMouseLeave={hide} onFocus={show} onBlur={hide}>
      {children}
      {visible && (
        <span
          role="tooltip"
          className={[
            'absolute z-50 whitespace-nowrap rounded border border-border-base bg-surface px-2 py-1 text-xs text-text-base shadow-lg',
            positionClasses[position],
          ].join(' ')}
        >
          {content}
          <span className={['absolute', arrowClasses[position]].join(' ')} />
        </span>
      )}
    </span>
  );
}
