import { Leaf } from 'lucide-react';

type Props = { size?: 'sm' | 'lg' };

export function Avatar({ size = 'sm' }: Props) {
  return <span className={`ui-avatar ui-avatar-${size}`} aria-hidden="true"><Leaf /></span>;
}
