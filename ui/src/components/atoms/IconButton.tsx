import { useMemo } from 'react';
import type { ButtonHTMLAttributes } from 'react';
import Icon from './Icon';
import type { IconName, IconStrokeWidth, IconVariant } from './Icon';
import Button from './Button';
import Tooltip from './Tooltip';

interface IconButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'children'> {
  name: IconName;
  tooltip: string;
  variant?: 'ghost' | 'danger' | 'secondary';
  appearance?: 'default' | 'admin';
  iconStrokeWidth?: IconStrokeWidth;
  iconSize?: number;
  loading?: boolean;
  disabledTooltip?: string;
}

/**
 * Maps IconButton variant to the Icon variant for contextual coloring.
 * Example: danger button → red icon that fades on hover.
 */
const iconVariantMap: Record<string, IconVariant> = {
  ghost: 'ghost',
  danger: 'danger',
  secondary: 'default',
};

/**
 * Some icons benefit from a subtle rotation on hover (e.g. refresh-cw).
 */
const rotatingIcons = new Set<IconName>(['refresh-cw', 'loader']);

const adminIconNameMap: Partial<Record<IconName, IconName>> = {
  edit: 'square-pen',
  eye: 'file-search',
  trash: 'trash-clean',
  x: 'ban',
  check: 'check-circle',
  clock: 'history',
  'alert-circle': 'shield-ban',
  'refresh-cw': 'rotate-ccw',
  settings: 'settings-2',
};

const defaultButtonClasses =
  'group h-10 w-10 shrink-0 rounded-xl border shadow-sm p-0 transition-all duration-150 hover:-translate-y-0.5 hover:shadow-md';

type AdminActionTone = 'neutral' | 'primary' | 'success' | 'warning' | 'danger' | 'utility';

const adminActionToneMap: Partial<Record<IconName, AdminActionTone>> = {
  edit: 'primary',
  eye: 'utility',
  download: 'success',
  check: 'success',
  x: 'warning',
  clock: 'utility',
  settings: 'neutral',
  'refresh-cw': 'warning',
  'key-round': 'warning',
  'archive-restore': 'warning',
  'alert-circle': 'danger',
  trash: 'danger',
  ruler: 'neutral',
};

const adminButtonBaseClasses =
  'group h-10 w-10 shrink-0 !rounded-lg border p-0 !shadow-none transition-all duration-150 focus-visible:!ring-2 focus-visible:!ring-offset-1 disabled:opacity-45';

const adminButtonToneClasses: Record<AdminActionTone, string> = {
  neutral: '!border-transparent !bg-transparent !text-slate-500 hover:!border-slate-200 hover:!bg-slate-50 hover:!text-slate-800',
  primary: '!border-transparent !bg-transparent !text-blue-600 hover:!border-blue-100 hover:!bg-blue-50 hover:!text-blue-800',
  success: '!border-transparent !bg-transparent !text-emerald-600 hover:!border-emerald-100 hover:!bg-emerald-50 hover:!text-emerald-800',
  warning: '!border-transparent !bg-transparent !text-amber-600 hover:!border-amber-100 hover:!bg-amber-50 hover:!text-amber-800',
  danger: '!border-transparent !bg-transparent !text-rose-600 hover:!border-rose-100 hover:!bg-rose-50 hover:!text-rose-800',
  utility: '!border-transparent !bg-transparent !text-sky-600 hover:!border-sky-100 hover:!bg-sky-50 hover:!text-sky-800',
};

const adminIconToneClasses: Record<AdminActionTone, string> = {
  neutral: '!text-slate-500 group-hover:!text-slate-800',
  primary: '!text-blue-600 group-hover:!text-blue-800',
  success: '!text-emerald-600 group-hover:!text-emerald-800',
  warning: '!text-amber-600 group-hover:!text-amber-800',
  danger: '!text-rose-600 group-hover:!text-rose-800',
  utility: '!text-sky-600 group-hover:!text-sky-800',
};

const adminIconVariantMap: Record<'ghost' | 'danger' | 'secondary', IconVariant> = {
  ghost: 'default',
  danger: 'danger',
  secondary: 'default',
};

export default function IconButton({
  name,
  tooltip,
  variant = 'ghost',
  appearance = 'default',
  iconStrokeWidth,
  iconSize,
  loading = false,
  disabled = false,
  disabledTooltip,
  className = '',
  ...props
}: IconButtonProps) {
  const iconVariant = iconVariantMap[variant] ?? 'ghost';
  const shouldRotate = useMemo(() => rotatingIcons.has(name), [name]);
  const isAdminAppearance = appearance === 'admin';
  const resolvedIconSize = iconSize ?? (isAdminAppearance ? 22 : 18);
  const resolvedIconStrokeWidth = iconStrokeWidth ?? (isAdminAppearance ? 'default' : 'thin');
  const adminTone = variant === 'danger' ? 'danger' : adminActionToneMap[name] ?? (variant === 'secondary' ? 'neutral' : 'primary');
  const iconName = isAdminAppearance ? adminIconNameMap[name] ?? name : name;
  const buttonVariant = isAdminAppearance ? 'ghost' : variant;
  const buttonClassName = isAdminAppearance
    ? `${adminButtonBaseClasses} ${adminButtonToneClasses[adminTone]}`
    : `${defaultButtonClasses} ${variant === 'danger' ? 'border-rose-200 bg-rose-50/80 text-rose-700 hover:bg-rose-100' : variant === 'secondary' ? 'border-slate-200 bg-slate-50/90 text-slate-700 hover:bg-white' : 'border-slate-200 bg-white/90 text-slate-700 hover:bg-slate-50 hover:border-slate-300'}`;
  const buttonIconVariant = isAdminAppearance ? adminIconVariantMap[variant] : iconVariant;
  const iconClassName = isAdminAppearance
    ? `transition-transform duration-150 group-hover:scale-110 ${adminIconToneClasses[adminTone]} ${loading ? 'animate-spin' : ''}`
    : `transition-all duration-200
          group-hover:scale-110
          ${shouldRotate ? 'group-hover:rotate-45' : ''}
          ${loading ? 'animate-spin' : ''}`;

  const button = (
      <Button
        variant={buttonVariant}
        size="sm"
        disabled={disabled}
        loading={loading}
        aria-label={tooltip}
        className={`${buttonClassName} ${className}`}
        {...props}
      >
      <Icon
        name={iconName}
        size={resolvedIconSize}
        strokeWidth={resolvedIconStrokeWidth}
        variant={buttonIconVariant}
        className={iconClassName}
      />
    </Button>
  );

  // When disabled with a disabledTooltip, wrap in Tooltip so the reason is visible
  if (disabled && disabledTooltip) {
    return <Tooltip content={disabledTooltip}>{button}</Tooltip>;
  }

  // When not disabled, show normal tooltip
  if (!disabled) {
    return <Tooltip content={tooltip}>{button}</Tooltip>;
  }

  // Disabled without disabledTooltip → no tooltip
  return button;
}
