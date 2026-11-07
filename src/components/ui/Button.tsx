import type { MouseEventHandler, ReactNode } from 'react';
import { icons, type IconName } from './icons';
import type { ControlSize, ControlVariant } from './IconButton';

type Props = {
  label: ReactNode;
  icon?: IconName;
  variant?: ControlVariant;
  size?: ControlSize;
  onClick?: MouseEventHandler<HTMLButtonElement>;
  disabled?: boolean;
};

export function Button({ label, icon, variant = 'primary', size = 'md', onClick, disabled = false }: Props) {
  const Icon = icon ? icons[icon] : null;
  return <button type="button" className={`ui-button ui-${variant} ui-${size}`} onClick={onClick} disabled={disabled}>{Icon && <Icon />}<span>{label}</span></button>;
}
