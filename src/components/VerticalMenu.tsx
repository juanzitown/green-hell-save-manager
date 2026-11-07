import { Users } from 'lucide-react';
import { IconButton } from './ui/IconButton';
import { Avatar } from './ui/Avatar';
import { useProfiles } from '../contexts/ProfilesContext';

export function VerticalMenu() {
  const { profiles, selectedId, setSelectedId, openCreateProfile } = useProfiles();
  return <aside className="sidebar">
    <div className="section-label profiles-label">Profiles <IconButton icon="plus" tooltip="Create profile" size="xs" onClick={openCreateProfile} /></div>
    <div className="profiles-heading"><Users size={15} /><span>{profiles.length} profiles</span></div>
    <div className="profile-list">{profiles.map((profile) => <button key={profile.id} onClick={() => setSelectedId(profile.id)} className={`profile-row ${profile.id === selectedId ? 'selected' : ''}`}>{profile.id === 'my' && <Avatar />}<span>{profile.name}</span><span className="save-count">{profile.saves.length}</span></button>)}</div>
  </aside>;
}
