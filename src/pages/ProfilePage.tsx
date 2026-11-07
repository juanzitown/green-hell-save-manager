import { useEffect, useMemo, useRef, useState } from 'react';
import { ExternalLink, HardDriveDownload, Search, Upload } from 'lucide-react';
import { invoke } from '@tauri-apps/api/core';
import type { Profile, SaveFile } from '../App';
import { Button } from '../components/ui/Button';
import { IconButton } from '../components/ui/IconButton';
import { Avatar } from '../components/ui/Avatar';

type Props = { profile: Profile; setProfiles: React.Dispatch<React.SetStateAction<Profile[]>>; refreshToken: number; onEditProfile: () => void };
type NativeSave = { name: string; path: string; size: number; modified: number };
type SaveLocation = { path: string; files: NativeSave[] };

function timeAgo(timestamp: number) {
  const minutes = Math.max(1, Math.floor((Date.now() - timestamp) / 60000));
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  return hours < 24 ? `${hours}h ago` : `${Math.floor(hours / 24)}d ago`;
}

function toSaveFile(file: NativeSave, source: string): SaveFile {
  return { id: file.path, name: file.name, size: `${Math.max(1, Math.round(file.size / 1024))} KB`, modified: file.modified, source };
}

export function ProfilePage({ profile, setProfiles, refreshToken, onEditProfile }: Props) {
  const [query, setQuery] = useState('');
  const [folderPath, setFolderPath] = useState('');
  const [folderStatus, setFolderStatus] = useState('Finding save folder...');
  const fileInput = useRef<HTMLInputElement>(null);
  const nativePaths = useRef(new Map<string, string>());
  const saves = useMemo(() => profile.saves.filter((save) => save.name.toLowerCase().includes(query.toLowerCase())).sort((a, b) => b.modified - a.modified), [profile.saves, query]);

  async function loadSaves() {
    try {
      const source = profile.id === 'my' ? 'Steam folder' : 'Profile backup';
      const location = profile.id === 'my' ? (await invoke<SaveLocation[]>('find_green_hell_saves'))[0] : await invoke<SaveLocation>('find_profile_saves', { profileId: profile.id });
      if (!location) { setFolderPath(''); setFolderStatus('Green Hell Steam folder not found'); return; }
      nativePaths.current.clear();
      location.files.forEach((file) => nativePaths.current.set(file.name, file.path));
      setFolderPath(location.path);
      setFolderStatus(`${location.files.length} save${location.files.length === 1 ? '' : 's'}`);
      setProfiles((current) => current.map((item) => item.id === profile.id ? { ...item, saves: location.files.map((file) => toSaveFile(file, source)) } : item));
    } catch (error) {
      console.error('Could not load saves', error);
      setFolderPath('');
      setFolderStatus('Could not load save folder');
    }
  }

  async function importFiles(files: FileList | null) {
    if (!files) return;
    for (const file of Array.from(files)) {
      if (!file.name.toLowerCase().endsWith('.sav')) continue;
      try {
        const bytes = Array.from(new Uint8Array(await file.arrayBuffer()));
        await invoke('upload_save_to_profile', { profileId: profile.id, name: file.name, bytes });
      } catch (error) {
        console.error('Save upload failed', error);
        window.alert(`Could not upload ${file.name}.`);
      }
    }
    await loadSaves();
  }

  function downloadSave(save: SaveFile) {
    const path = nativePaths.current.get(save.name);
    if (!path) return;
    void invoke<string>('export_save_file', { path }).catch(() => window.alert('Could not download this save file.'));
  }

  function copySave(save: SaveFile) {
    void invoke('copy_profile_save_to_steam', { profileId: profile.id, name: save.name }).catch(() => window.alert(`Could not copy ${save.name} to Steam.`));
  }

  function openFolder() {
    if (folderPath) void invoke('open_folder', { path: folderPath }).catch(() => window.alert('Could not open the save folder.'));
  }

  useEffect(() => { void loadSaves(); }, [profile.id, refreshToken]);

  return <div className="content-wrap">
    <div className="profile-heading">{profile.id === 'my' && <Avatar size="lg" />}<div><div className="eyebrow">{profile.id === 'my' ? 'MY PROFILE' : 'BACKUP PROFILE'}</div><h1>{profile.name}</h1><p>{profile.saves.length} saved file{profile.saves.length === 1 ? '' : 's'}</p></div><div className="more-button"><IconButton icon="pencil" tooltip="Edit profile" size="sm" onClick={onEditProfile} /></div></div>
    <div className="toolbar"><div className="search-box"><Search size={16} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search saves" /></div><Button label="Import saves" icon="upload" onClick={() => fileInput.current?.click()} /><input ref={fileInput} onChange={(event) => { void importFiles(event.target.files); event.target.value = ''; }} type="file" multiple accept=".sav" hidden /></div>
    <div className="folder-status"><HardDriveDownload size={14} />{folderPath ? <button className="folder-link" onClick={openFolder} title="Open in file explorer">{folderPath}<ExternalLink size={12} /></button> : <span>{folderStatus}</span>} {folderPath && <span>· {folderStatus}</span>}</div>
    <div className="save-area">{saves.length === 0 ? <div className="empty-state"><div className="empty-icon"><Upload size={22} /></div><h2>No saves yet</h2><p>Import or drag Green Hell <code>.sav</code> files into this profile.</p><Button label="Import saves" icon="upload" variant="outline" onClick={() => fileInput.current?.click()} /></div> : <div className="file-grid">{saves.map((save) => <article className="file-card" key={save.id}><div className="file-icon"><span>.sav</span></div><div className="file-info"><strong>{save.name}</strong><span>{save.size} · {timeAgo(save.modified)}</span><small>{save.source}</small></div><div className="file-actions">{profile.id !== 'my' && <IconButton icon="hardDriveDownload" tooltip="Copy save to Steam" size="sm" onClick={() => copySave(save)} />}<IconButton icon="download" tooltip="Download save" size="sm" onClick={() => downloadSave(save)} /></div></article>)}</div>}</div>
    <div className="drop-hint"><Upload size={15} /> Drop files anywhere in the window to add them to <strong>{profile.name}</strong></div>
  </div>;
}
