import { useEffect, useMemo, useRef, useState } from 'react';
import { Download, HardDriveDownload, MoreHorizontal, Search, Upload } from 'lucide-react';
import { invoke } from '@tauri-apps/api/core';
import type { Profile, SaveFile } from '../App';

type Props = { profile: Profile; setProfiles: React.Dispatch<React.SetStateAction<Profile[]>> };
type SaveLocation = { path: string; files: { name: string; path: string; size: number; modified: number }[] };
function timeAgo(timestamp: number) { const minutes = Math.max(1, Math.floor((Date.now() - timestamp) / 60000)); if (minutes < 60) return `${minutes}m ago`; const hours = Math.floor(minutes / 60); if (hours < 24) return `${hours}h ago`; return `${Math.floor(hours / 24)}d ago`; }

export function ProfilePage({ profile, setProfiles }: Props) {
  const [query, setQuery] = useState('');
  const [gameFolder, setGameFolder] = useState('No game folder selected');
  const [detecting, setDetecting] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);
  const folderInput = useRef<HTMLInputElement>(null);
  const importedFiles = useRef(new Map<string, File>());
  const nativePaths = useRef(new Map<string, string>());
  const folderInputProps = { webkitdirectory: 'true' } as unknown as React.InputHTMLAttributes<HTMLInputElement>;
  const saves = useMemo(() => profile.saves.filter((save) => save.name.toLowerCase().includes(query.toLowerCase())).sort((a, b) => b.modified - a.modified), [profile, query]);
  function importFiles(files: FileList | null) { if (!files) return; const imported: SaveFile[] = Array.from(files).map((file, index) => ({ id: `${file.name}-${Date.now()}-${index}`, name: file.name, size: `${Math.max(1, Math.round(file.size / 1024))} KB`, modified: file.lastModified || Date.now(), source: 'Imported' })); setProfiles((current) => current.map((item) => item.id === profile.id ? { ...item, saves: [...item.saves, ...imported] } : item)); }
  function scanGameFolder(files: FileList | null) {
    if (!files) return;
    const saves = Array.from(files).filter((file) => file.name.toLowerCase().endsWith('.sav'));
    saves.forEach((file) => importedFiles.current.set(`${profile.id}:${file.name}`, file));
    setGameFolder(saves.length ? `${saves.length} save${saves.length === 1 ? '' : 's'} found in selected folder` : 'No .sav files found in selected folder');
    setProfiles((current) => current.map((item) => item.id === profile.id ? { ...item, saves: saves.map((file, index) => ({ id: `folder-${file.name}-${index}`, name: file.name, size: `${Math.max(1, Math.round(file.size / 1024))} KB`, modified: file.lastModified || Date.now(), source: 'Game folder' })) } : item));
  }
  function downloadSave(save: SaveFile) {
    const file = importedFiles.current.get(`${profile.id}:${save.name}`);
    if (file) { const url = URL.createObjectURL(file); const link = document.createElement('a'); link.href = url; link.download = save.name; link.click(); URL.revokeObjectURL(url); return; }
    const path = nativePaths.current.get(`${profile.id}:${save.name}`);
    if (!path) { window.alert('This save is not available for download yet.'); return; }
    invoke<string>('export_save_file', { path }).then((destination) => window.alert(`Save downloaded to ${destination}`)).catch(() => window.alert('Could not download this save file.'));
  }
  async function detectGameFolder() {
    setDetecting(true);
    try {
      const locations = await invoke<SaveLocation[]>('find_green_hell_saves');
      if (locations.length) { const location = locations[0]; location.files.forEach((file) => nativePaths.current.set(`${profile.id}:${file.name}`, file.path)); setGameFolder(`${location.path} · ${location.files.length} save${location.files.length === 1 ? '' : 's'} found`); setProfiles((current) => current.map((item) => item.id === profile.id ? { ...item, saves: location.files.map((file) => ({ id: `native-${file.path}`, name: file.name, size: `${Math.max(1, Math.round(file.size / 1024))} KB`, modified: file.modified, source: 'Steam folder' })) } : item)); }
      else setGameFolder('No Green Hell save folder found');
    } catch { setGameFolder('Automatic detection is available in the Tauri desktop app'); }
    finally { setDetecting(false); }
  }
  useEffect(() => { void detectGameFolder(); }, [profile.id]);
  return <div className="content-wrap">
    <div className="profile-heading"><div className="large-avatar">{profile.icon}</div><div><div className="eyebrow">MY PROFILE</div><h1>{profile.name}</h1><p>{profile.saves.length} saved file{profile.saves.length === 1 ? '' : 's'} · Local library</p></div><button className="more-button"><MoreHorizontal /></button></div>
    <div className="toolbar"><div className="search-box"><Search size={16} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Search saves" /></div><div className="toolbar-actions"><button onClick={() => folderInput.current?.click()} className="outline-button">Choose folder</button><button onClick={() => fileInput.current?.click()} className="primary-button"><Upload size={16} /> Import saves</button></div><input ref={fileInput} onChange={(event) => importFiles(event.target.files)} type="file" multiple accept=".sav" hidden /><input {...folderInputProps} ref={folderInput} onChange={(event) => scanGameFolder(event.target.files)} type="file" multiple hidden /></div>
    <div className="folder-status"><HardDriveDownload size={14} /> {gameFolder}</div>
    <div onDragOver={(event) => event.preventDefault()} onDrop={(event) => { event.preventDefault(); importFiles(event.dataTransfer.files); }} className="save-area">{saves.length === 0 ? <div className="empty-state"><div className="empty-icon"><Upload size={22} /></div><h2>Drop saves here</h2><p>Choose the Green Hell folder to list saves, or drag <code>.sav</code> files into this profile.</p><button onClick={() => folderInput.current?.click()} className="outline-button">Choose game folder</button></div> : <div className="file-grid">{saves.map((save) => <article className="file-card" key={save.id}><div className="file-icon"><span>.sav</span></div><div className="file-info"><strong>{save.name}</strong><span>{save.size} · {timeAgo(save.modified)}</span><small>{save.source}</small></div><button className="card-action" onClick={() => downloadSave(save)} title="Download save"><Download size={16} /></button></article>)}</div>}</div>
    <div className="drop-hint"><Upload size={15} /> Drop files anywhere in this area to add them to <strong>{profile.name}</strong></div>
  </div>;
}
