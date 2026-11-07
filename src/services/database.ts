import Database from '@tauri-apps/plugin-sql';
import type { Profile } from '../types';

const DATABASE_URL = 'sqlite:green-hell-save-manager.db';
let database: Promise<Database> | undefined;

type ProfileRow = { id: string; name: string; steam_id: string | null };

function getDatabase() {
  database ??= Database.load(DATABASE_URL);
  return database;
}

export async function loadProfiles(): Promise<Profile[]> {
  const db = await getDatabase();
  const rows = await db.select<ProfileRow[]>('SELECT id, name, steam_id FROM profiles ORDER BY is_primary DESC, created_at ASC');
  return rows.map((row) => ({ id: row.id, name: row.name, steamId: row.steam_id ?? undefined, saves: [] }));
}

export async function insertProfile(profile: Profile) {
  const db = await getDatabase();
  await db.execute('INSERT INTO profiles (id, name, steam_id, is_primary, created_at) VALUES ($1, $2, $3, 0, unixepoch())', [profile.id, profile.name, profile.steamId ?? null]);
}

export async function renameProfile(id: string, name: string) {
  const db = await getDatabase();
  await db.execute('UPDATE profiles SET name = $1 WHERE id = $2', [name, id]);
}
