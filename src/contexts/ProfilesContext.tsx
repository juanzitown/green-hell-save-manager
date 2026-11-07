import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { invoke } from '@tauri-apps/api/core';
import { getCurrentWindow } from '@tauri-apps/api/window';
import { toast } from 'sonner';
import { insertProfile, loadProfiles, renameProfile } from '../services/database';
import type { NativeSave, Profile, SaveFile, SaveLocation } from '../types';

type DialogState = { mode: 'create' } | { mode: 'edit'; profileId: string } | null;
type ContextValue = {
  profiles: Profile[];
  selected: Profile;
  selectedId: string;
  dialog: DialogState;
  setSelectedId: (id: string) => void;
  openCreateProfile: () => void;
  openEditProfile: (id: string) => void;
  closeProfileDialog: () => void;
  createProfile: (name: string) => Promise<void>;
  updateProfile: (name: string) => Promise<void>;
  refreshProfile: (id: string) => Promise<void>;
};

const primaryProfile: Profile = { id: 'my', name: 'My Profile', saves: [] };
const ProfilesContext = createContext<ContextValue | null>(null);

function toSaveFile(file: NativeSave, source: string): SaveFile {
  return { id: file.path, name: file.name, size: `${Math.max(1, Math.round(file.size / 1024))} KB`, modified: file.modified, source };
}

async function hydrateProfile(profile: Profile): Promise<Profile> {
  const location = profile.id === 'my' ? (await invoke<SaveLocation[]>('find_green_hell_saves'))[0] : await invoke<SaveLocation>('find_profile_saves', { profileId: profile.id });
  if (!location) return profile;
  const source = profile.id === 'my' ? 'Steam folder' : 'Profile backup';
  return { ...profile, folderPath: location.path, saves: location.files.map((file) => toSaveFile(file, source)) };
}

export function ProfilesProvider({ children }: { children: ReactNode }) {
  const [profiles, setProfiles] = useState<Profile[]>([primaryProfile]);
  const [selectedId, setSelectedId] = useState('my');
  const [dialog, setDialog] = useState<DialogState>(null);
  const selected = profiles.find((profile) => profile.id === selectedId) ?? profiles[0] ?? primaryProfile;

  useEffect(() => {
    void loadProfiles().then(async (stored) => {
      const hydrated = await Promise.all(stored.map(hydrateProfile));
      if (hydrated.length) setProfiles(hydrated);
    }).catch((error) => { console.error('Could not load profiles', error); toast.error('Could not load saved profiles'); });
  }, []);

  async function refreshProfile(id: string) {
    const profile = profiles.find((item) => item.id === id);
    if (!profile) return;
    try {
      const hydrated = await hydrateProfile(profile);
      setProfiles((current) => current.map((item) => item.id === id ? hydrated : item));
    } catch (error) {
      console.error('Could not refresh profile', error);
      toast.error('Could not refresh profile saves');
    }
  }

  useEffect(() => {
    let stop: (() => void) | undefined;
    let disposed = false;
    void getCurrentWindow().onDragDropEvent((event) => {
      if (event.payload.type !== 'drop') return;
      const paths = event.payload.paths.filter((path) => path.toLowerCase().endsWith('.sav'));
      if (!paths.length) return;
      void invoke('upload_save_paths', { profileId: selectedId, paths }).then(async () => {
        await refreshProfile(selectedId);
        toast.success(paths.length === 1 ? 'Save copied successfully' : `${paths.length} saves copied successfully`);
      }).catch((error) => { console.error('Native save drop failed', error); toast.error('Could not copy the dropped save files'); });
    }).then((unlisten) => { if (disposed) unlisten(); else stop = unlisten; });
    return () => { disposed = true; stop?.(); };
  }, [selectedId, profiles]);

  async function createProfile(name: string) {
    const profile = { id: `${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${Date.now()}`, name, saves: [] } satisfies Profile;
    try {
      await insertProfile(profile);
      const hydrated = await hydrateProfile(profile);
      setProfiles((current) => [...current, hydrated]);
      setSelectedId(profile.id);
      setDialog(null);
      toast.success('Profile created');
    } catch (error) { console.error('Could not create profile', error); toast.error('Could not create profile'); }
  }

  async function updateProfile(name: string) {
    if (!dialog || dialog.mode !== 'edit') return;
    try {
      await renameProfile(dialog.profileId, name);
      setProfiles((current) => current.map((profile) => profile.id === dialog.profileId ? { ...profile, name } : profile));
      setDialog(null);
      toast.success('Profile updated');
    } catch (error) { console.error('Could not update profile', error); toast.error('Could not update profile'); }
  }

  const value = useMemo<ContextValue>(() => ({ profiles, selected, selectedId, dialog, setSelectedId, openCreateProfile: () => setDialog({ mode: 'create' }), openEditProfile: (profileId) => setDialog({ mode: 'edit', profileId }), closeProfileDialog: () => setDialog(null), createProfile, updateProfile, refreshProfile }), [profiles, selected, selectedId, dialog]);
  return <ProfilesContext.Provider value={value}>{children}</ProfilesContext.Provider>;
}

export function useProfiles() {
  const context = useContext(ProfilesContext);
  if (!context) throw new Error('useProfiles must be used inside ProfilesProvider');
  return context;
}
