import { Users } from 'lucide-react';
import type { Profile } from '../App';
import { IconButton } from './ui/IconButton';
import { Avatar } from './ui/Avatar';

type Props = { profiles: Profile[]; selectedId: string; onSelectProfile: (id: string) => void; onCreateProfile: () => void };

export function VerticalMenu({ profiles, selectedId, onSelectProfile, onCreateProfile }: Props) {
  return <aside className="sidebar">
    <div className="section-label profiles-label">Profiles <IconButton icon="plus" tooltip="Create profile" size="xs" onClick={onCreateProfile} /></div>
    <div className="profiles-heading"><Users size={15} /><span>{profiles.length} profiles</span></div>
    <div className="profile-list">{profiles.map((profile) => <button key={profile.id} onClick={() => onSelectProfile(profile.id)} className={`profile-row ${profile.id === selectedId ? 'selected' : ''}`}>{profile.id === 'my' && <Avatar />}<span>{profile.name}</span><span className="save-count">{profile.saves.length}</span></button>)}</div>
  </aside>;
}
