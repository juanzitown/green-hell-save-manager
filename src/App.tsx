import { useState } from 'react';
import { Gamepad2 } from 'lucide-react';
import { ProfileFormModal } from './components/ProfileFormModal';
import { VerticalMenu } from './components/VerticalMenu';
import { ProfilePage } from './pages/ProfilePage';

export type SaveFile = { id: string; name: string; size: string; modified: number; source: string };
export type Profile = { id: string; name: string; icon: string; steamId?: string; saves: SaveFile[] };

const initialProfiles: Profile[] = [
  { id: 'my', name: 'My', icon: '🌿', steamId: '', saves: [
    { id: 'mpslot0', name: 'MPSlot0.sav', size: '48 KB', modified: Date.now() - 1000 * 60 * 18, source: 'Green Hell' },
    { id: 'mpslot1', name: 'MPSlot1.sav', size: '51 KB', modified: Date.now() - 1000 * 60 * 60 * 26, source: 'Green Hell' },
  ] },
  { id: 'maria', name: 'Maria', icon: '🦜', steamId: '', saves: [] },
];

export function App() {
  const [profiles, setProfiles] = useState(initialProfiles);
  const [selectedId, setSelectedId] = useState('my');
  const [showNewProfile, setShowNewProfile] = useState(false);
  const selected = profiles.find((profile) => profile.id === selectedId) ?? profiles[0];

  function createProfile(name: string, icon: string) {
    const profile = { id: `${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${Date.now()}`, name, icon, saves: [] } satisfies Profile;
    setProfiles((current) => [...current, profile]);
    setSelectedId(profile.id);
    setShowNewProfile(false);
  }

  return <div className="app-shell">
    <VerticalMenu profiles={profiles} selectedId={selected.id} onSelectProfile={setSelectedId} onCreateProfile={() => setShowNewProfile(true)} />
    <main className="main-content">
      <header className="topbar"><div className="brand-compact"><div className="brand-mark"><Gamepad2 size={19} /></div><strong>Green Hell</strong></div><span className="game-view-label">GAME VIEW</span></header>
      <ProfilePage profile={selected} setProfiles={setProfiles} />
    </main>
    {showNewProfile && <ProfileFormModal onClose={() => setShowNewProfile(false)} onSubmit={createProfile} />}
  </div>;
}
