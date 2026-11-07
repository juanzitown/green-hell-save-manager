export type SaveFile = { id: string; name: string; size: string; modified: number; source: string };
export type Profile = { id: string; name: string; steamId?: string; saves: SaveFile[]; folderPath?: string };
export type NativeSave = { name: string; path: string; size: number; modified: number };
export type SaveLocation = { path: string; files: NativeSave[] };
