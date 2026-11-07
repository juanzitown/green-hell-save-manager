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
  steamAccounts: SaveLocation[];
  selectedSteamAccount?: SaveLocation;
  showAccountPicker: boolean;
  setSelectedId: (id: string) => void;
  selectSteamAccount: (accountId: string) => Promise<void>;
  openAccountPicker: () => void;
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

async function hydrateProfile(profile: Profile, steamAccount?: SaveLocation): Promise<Profile> {
  const location = profile.id === 'my' ? steamAccount : await invoke<SaveLocation>('find_profile_saves', { profileId: profile.id });
  if (!location) return { ...profile, saves: [], folderPath: undefined };
  const source = profile.id === 'my' ? 'Steam folder' : 'Profile backup';
  return { ...profile, folderPath: location.path, saves: location.files.map((file) => toSaveFile(file, source)) };
}

export function ProfilesProvider({ children }: { children: ReactNode }) {
  const [profiles, setProfiles] = useState<Profile[]>([primaryProfile]);
  const [selectedId, setSelectedId] = useState('my');
  const [dialog, setDialog] = useState<DialogState>(null);
  const [steamAccounts, setSteamAccounts] = useState<SaveLocation[]>([]);
  const [selectedSteamAccountId, setSelectedSteamAccountId] = useState<string>();
  const [showAccountPicker, setShowAccountPicker] = useState(false);
  const selected = profiles.find((profile) => profile.id === selectedId) ?? profiles[0] ?? primaryProfile;
  const selectedSteamAccount = steamAccounts.find((account) => account.account_id === selectedSteamAccountId);

  useEffect(() => {
    void Promise.all([loadProfiles(), invoke<SaveLocation[]>('find_green_hell_saves')]).then(async ([stored, accounts]) => {
      setSteamAccounts(accounts);
      const account = accounts.length === 1 ? accounts[0] : undefined;
      if (account) setSelectedSteamAccountId(account.account_id);
      if (accounts.length > 1) setShowAccountPicker(true);
      const hydrated = await Promise.all(stored.map((profile) => hydrateProfile(profile, account)));
      if (hydrated.length) setProfiles(hydrated);
    }).catch((error) => { console.error('Could not initialize profiles', error); toast.error('Could not load saved profiles'); });
  }, []);

  async function selectSteamAccount(accountId: string) {
    const account = steamAccounts.find((item) => item.account_id === accountId);
    if (!account) return;
    setSelectedSteamAccountId(accountId);
    setShowAccountPicker(false);
    setSelectedId('my');
    const hydrated = await Promise.all(profiles.map((profile) => hydrateProfile(profile, account)));
    setProfiles(hydrated);
  }

  async function refreshProfile(id: string) {
    const profile = profiles.find((item) => item.id === id);
    if (!profile) return;
    try {
      const hydrated = await hydrateProfile(profile, selectedSteamAccount);
      setProfiles((current) => current.map((item) => item.id === id ? hydrated : item));
    } catch (error) { console.error('Could not refresh profile', error); toast.error('Could not refresh profile saves'); }
  }

  useEffect(() => {
    let stop: (() => void) | undefined;
    let disposed = false;
    void getCurrentWindow().onDragDropEvent((event) => {
      if (event.payload.type !== 'drop' || showAccountPicker) return;
      const paths = event.payload.paths.filter((path) => path.toLowerCase().endsWith('.sav'));
      if (!paths.length) return;
      void invoke('upload_save_paths', { profileId: selectedId, steamAccountId: selectedSteamAccountId, paths }).then(async () => {
        await refreshProfile(selectedId);
        toast.success(paths.length === 1 ? 'Save copied successfully' : `${paths.length} saves copied successfully`);
      }).catch((error) => { console.error('Native save drop failed', error); toast.error('Could not copy the dropped save files'); });
    }).then((unlisten) => { if (disposed) unlisten(); else stop = unlisten; });
    return () => { disposed = true; stop?.(); };
  }, [selectedId, selectedSteamAccountId, profiles, showAccountPicker]);

  async function createProfile(name: string) {
    const profile = { id: `${name.toLowerCase().replace(/[^a-z0-9]+/g, '-')}-${Date.now()}`, name, saves: [] } satisfies Profile;
    try {
      await insertProfile(profile);
      const hydrated = await hydrateProfile(profile, selectedSteamAccount);
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

  const value = useMemo<ContextValue>(() => ({ profiles, selected, selectedId, dialog, steamAccounts, selectedSteamAccount, showAccountPicker, setSelectedId, selectSteamAccount, openAccountPicker: () => steamAccounts.length > 0 && setShowAccountPicker(true), openCreateProfile: () => setDialog({ mode: 'create' }), openEditProfile: (profileId) => setDialog({ mode: 'edit', profileId }), closeProfileDialog: () => setDialog(null), createProfile, updateProfile, refreshProfile }), [profiles, selected, selectedId, dialog, steamAccounts, selectedSteamAccount, showAccountPicker]);
  return <ProfilesContext.Provider value={value}>{children}</ProfilesContext.Provider>;
}

export function useProfiles() {
  const context = useContext(ProfilesContext);
  if (!context) throw new Error('useProfiles must be used inside ProfilesProvider');
  return context;
}
