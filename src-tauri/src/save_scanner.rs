use serde::Serialize;
use std::fs;
use std::path::{Path, PathBuf};

const GREEN_HELL_STEAM_APP_ID: &str = "815370";

#[derive(Debug, Serialize)]
pub struct SaveFileInfo { pub name: String, pub path: String, pub size: u64, pub modified: u64 }

#[derive(Debug, Serialize)]
pub struct SaveLocation { pub path: String, pub files: Vec<SaveFileInfo> }

fn candidate_roots() -> Vec<PathBuf> {
    let mut roots = Vec::new();
    if let Some(home) = dirs::home_dir() {
        roots.extend([home.join(".local/share/Steam"), home.join(".steam/steam"), home.join(".steam/root"), home.join(".var/app/com.valvesoftware.Steam/.local/share/Steam")]);
    }
    if let Ok(steam) = std::env::var("STEAM_ROOT") { roots.push(PathBuf::from(steam)); }
    if let Ok(program_files) = std::env::var("PROGRAMFILES(X86)") { roots.push(PathBuf::from(program_files).join("Steam")); }
    if let Ok(program_files) = std::env::var("PROGRAMFILES") { roots.push(PathBuf::from(program_files).join("Steam")); }
    roots
}

pub fn scan_folder(path: &Path) -> SaveLocation {
    let mut files = fs::read_dir(path).into_iter().flatten().flatten().filter_map(|entry| {
        let file = entry.path();
        if !file.extension().is_some_and(|ext| ext.eq_ignore_ascii_case("sav")) { return None; }
        let metadata = file.metadata().ok()?;
        let modified = metadata.modified().ok()?.duration_since(std::time::UNIX_EPOCH).ok()?.as_millis() as u64;
        Some(SaveFileInfo { name: file.file_name()?.to_string_lossy().to_string(), path: file.display().to_string(), size: metadata.len(), modified })
    }).collect::<Vec<_>>();
    files.sort_by(|a, b| b.modified.cmp(&a.modified));
    SaveLocation { path: path.display().to_string(), files }
}

pub fn find() -> Vec<SaveLocation> {
    let mut locations = Vec::new();
    for root in candidate_roots() {
        let Ok(accounts) = fs::read_dir(root.join("userdata")) else { continue; };
        for account in accounts.flatten() {
            let game_dir = account.path().join(GREEN_HELL_STEAM_APP_ID);
            if game_dir.is_dir() { locations.push(scan_folder(&game_dir.join("remote"))); }
        }
    }
    locations.sort_by(|a, b| {
        let timestamp = |location: &SaveLocation| location.files.first().map(|file| file.modified).or_else(|| fs::metadata(&location.path).ok()?.modified().ok()?.duration_since(std::time::UNIX_EPOCH).ok().map(|time| time.as_millis() as u64)).unwrap_or(0);
        timestamp(b).cmp(&timestamp(a))
    });
    locations.dedup_by(|a, b| a.path == b.path);
    locations
}

pub fn steam_folder() -> Result<PathBuf, String> { find().first().map(|location| PathBuf::from(&location.path)).ok_or_else(|| "Could not find the Green Hell Steam save folder".to_string()) }
