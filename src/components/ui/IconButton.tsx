import type { MouseEventHandler } from 'react';
import { icons, type IconName } from './icons';

export type ControlSize = 'xs' | 'sm' | 'md' | 'lg' | 'xl';
export type ControlVariant = 'primary' | 'outline';

type Props = {
  icon: IconName;
  tooltip: string;
  variant?: ControlVariant;
  size?: ControlSize;
  onClick?: MouseEventHandler<HTMLButtonElement>;
  disabled?: boolean;
};

export function IconButton({ icon, tooltip, variant = 'outline', size = 'md', onClick, disabled = false }: Props) {
  const Icon = icons[icon];
  return <button type="button" className={`ui-icon-button ui-${variant} ui-${size}`} onClick={onClick} disabled={disabled} title={tooltip} aria-label={tooltip}><Icon /></button>;
}
