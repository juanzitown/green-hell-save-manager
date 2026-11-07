import {
  Download,
  ExternalLink,
  Gamepad2,
  HardDriveDownload,
  Pencil,
  Plus,
  Search,
  Upload,
  Users,
  X,
} from 'lucide-react';

export const icons = {
  download: Download,
  externalLink: ExternalLink,
  gamepad: Gamepad2,
  hardDriveDownload: HardDriveDownload,
  pencil: Pencil,
  plus: Plus,
  search: Search,
  upload: Upload,
  users: Users,
  close: X,
} as const;

export type IconName = keyof typeof icons;
