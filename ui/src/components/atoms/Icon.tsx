import {
  Search,
  Plus,
  PencilLine,
  SquarePen,
  Trash2,
  Trash,
  Eye,
  FileSearch,
  Download,
  Upload,
  Filter,
  CircleCheck,
  CircleX,
  ChevronDown,
  ChevronUp,
  ChevronLeft,
  ChevronRight,
  ArrowUp,
  ArrowDown,
  Clock,
  Calendar,
  User,
  Users,
  Building2,
  Mail,
  Phone,
  File,
  FileText,
  Folder,
  Paperclip,
  AlertCircle,
  AlertTriangle,
  CircleAlert,
  Info,
  CheckCircle,
  Menu,
  LogOut,
  RefreshCw,
  RotateCcw,
  SlidersHorizontal,
  Settings2,
  Loader,
  History,
  KeyRound,
  Ban,
  ShieldBan,
  ArchiveRestore,
  Ruler,
  Signature,
  Workflow,
  type LucideProps,
} from 'lucide-react';

export type IconName =
  | 'search' | 'plus' | 'edit' | 'trash' | 'eye'
  | 'download' | 'upload' | 'filter' | 'x' | 'check'
  | 'chevron-down' | 'chevron-up' | 'chevron-left' | 'chevron-right'
  | 'arrow-up' | 'arrow-down' | 'clock' | 'calendar'
  | 'user' | 'users' | 'building' | 'mail' | 'phone'
  | 'file' | 'file-text' | 'folder' | 'paperclip'
  | 'alert-circle' | 'alert-triangle' | 'info' | 'check-circle'
  | 'menu' | 'settings' | 'logout' | 'refresh-cw' | 'loader'
  | 'square-pen' | 'trash-clean' | 'file-search' | 'rotate-ccw' | 'settings-2' | 'circle-alert'
  | 'history' | 'key-round' | 'ban' | 'shield-ban' | 'archive-restore' | 'ruler'
  | 'signature' | 'workflow';

export type IconStrokeWidth = 'thin' | 'default' | 'bold';
export type IconVariant = 'default' | 'primary' | 'danger' | 'success' | 'warning' | 'ghost';

const iconMap: Record<IconName, React.ComponentType<LucideProps>> = {
  search: Search,
  plus: Plus,
  edit: PencilLine,
  trash: Trash2,
  eye: Eye,
  download: Download,
  upload: Upload,
  filter: Filter,
  x: CircleX,
  check: CircleCheck,
  'chevron-down': ChevronDown,
  'chevron-up': ChevronUp,
  'chevron-left': ChevronLeft,
  'chevron-right': ChevronRight,
  'arrow-up': ArrowUp,
  'arrow-down': ArrowDown,
  clock: Clock,
  calendar: Calendar,
  user: User,
  users: Users,
  building: Building2,
  mail: Mail,
  phone: Phone,
  file: File,
  'file-text': FileText,
  folder: Folder,
  paperclip: Paperclip,
  'alert-circle': AlertCircle,
  'alert-triangle': AlertTriangle,
  info: Info,
  'check-circle': CheckCircle,
  menu: Menu,
  settings: SlidersHorizontal,
  logout: LogOut,
  'refresh-cw': RefreshCw,
  loader: Loader,
  'square-pen': SquarePen,
  'trash-clean': Trash,
  'file-search': FileSearch,
  'rotate-ccw': RotateCcw,
  'settings-2': Settings2,
  'circle-alert': CircleAlert,
  history: History,
  'key-round': KeyRound,
  ban: Ban,
  'shield-ban': ShieldBan,
  'archive-restore': ArchiveRestore,
  ruler: Ruler,
  signature: Signature,
  workflow: Workflow,
};

const strokeWidthMap: Record<IconStrokeWidth, number> = {
  thin: 1.5,
  default: 2,
  bold: 2.5,
};

const variantClasses: Record<IconVariant, string> = {
  default: 'text-slate-700',
  primary: 'text-blue-600',
  danger: 'text-red-600',
  success: 'text-green-600',
  warning: 'text-amber-600',
  ghost: 'text-slate-600 group-hover:text-slate-900',
};

interface IconProps {
  name: IconName;
  size?: number;
  strokeWidth?: IconStrokeWidth;
  variant?: IconVariant;
  className?: string;
}

export default function Icon({
  name,
  size = 20,
  strokeWidth = 'default',
  variant = 'default',
  className = '',
}: IconProps) {
  const LucideIcon = iconMap[name];
  if (!LucideIcon) return null;

  return (
    <LucideIcon
      size={size}
      strokeWidth={strokeWidthMap[strokeWidth]}
      className={`${variantClasses[variant]} transition-colors duration-150 ${className}`}
      aria-hidden="true"
    />
  );
}
