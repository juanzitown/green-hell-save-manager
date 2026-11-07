import { useEffect, useState } from 'react';
import { Gamepad2 } from 'lucide-react';
import { getCurrentWindow } from '@tauri-apps/api/window';
import { invoke } from '@tauri-apps/api/core';
import { ProfileFormModal } from './components/ProfileFormModal';
import { VerticalMenu } from './components/VerticalMenu';
import { ProfilePage } from './pages/ProfilePage';

export type SaveFile = { id: string; name: string; size: string; modified: number; source: string };
export type Profile = { id: string; name: string; icon: string; steamId?: string; saves: SaveFile[] };

const initialProfiles: Profile[] = [
  { id: 'my', name: 'My Profile', icon: '🌿', steamId: '', saves: [] },
];

export function App() {
  const [profiles, setProfiles] = useState(initialProfiles);
  const [selectedId, setSelectedId] = useState('my');
  const [showNewProfile, setShowNewProfile] = useState(false);
  const [refreshToken, setRefreshToken] = useState(0);
  const selected = profiles.find((profile) => profile.id === selectedId) ?? profiles[0];

  useEffect(() => {
    let stop: (() => void) | undefined;
    void getCurrentWindow().onDragDropEvent((event) => {
      if (event.payload.type !== 'drop') return;
      const paths = event.payload.paths.filter((path) => path.toLowerCase().endsWith('.sav'));
      if (!paths.length) return;
      void invoke('upload_save_paths', { profileId: selectedId, paths }).then(() => setRefreshToken((value) => value + 1)).catch((error) => { console.error('Native save drop failed', error); window.alert('Could not upload the dropped save files.'); });
    }).then((unlisten) => { stop = unlisten; });
    return () => stop?.();
  }, [selectedId]);

  function createProfile(name: string, icon: string) {
    const profile = { id: `${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${Date.now()}`, name, icon, saves: [] } satisfies Profile;
    setProfiles((current) => [...current, profile]);
    setSelectedId(profile.id);
    setShowNewProfile(false);
  }

  return <div className="app-shell">
    <VerticalMenu profiles={profiles} selectedId={selected.id} onSelectProfile={setSelectedId} onCreateProfile={() => setShowNewProfile(true)} />
    <main className="main-content">
      <header className="topbar"><div className="brand-compact"><div className="brand-mark"><Gamepad2 size={19} /></div><strong>Green Hell</strong></div></header>
      <ProfilePage profile={selected} setProfiles={setProfiles} refreshToken={refreshToken} />
    </main>
    {showNewProfile && <ProfileFormModal onClose={() => setShowNewProfile(false)} onSubmit={createProfile} />}
  </div>;
}
