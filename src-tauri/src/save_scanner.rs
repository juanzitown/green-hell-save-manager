use serde::Serialize;
use std::collections::HashSet;
use std::fs;
use std::path::{Path, PathBuf};

const GREEN_HELL_STEAM_APP_ID: &str = "815370";

#[derive(Debug, Serialize)]
pub struct SaveFileInfo { pub name: String, pub path: String, pub size: u64, pub modified: u64 }

#[derive(Debug, Serialize)]
pub struct SaveLocation { pub account_id: String, pub path: String, pub files: Vec<SaveFileInfo> }

fn default_roots() -> Vec<PathBuf> {
    let mut roots = Vec::new();
    if let Some(home) = dirs::home_dir() {
        roots.extend([home.join(".local/share/Steam"), home.join(".steam/steam"), home.join(".steam/root"), home.join(".var/app/com.valvesoftware.Steam/.local/share/Steam")]);
    }
    if let Ok(program_files) = std::env::var("PROGRAMFILES(X86)") { roots.push(PathBuf::from(program_files).join("Steam")); }
    if let Ok(program_files) = std::env::var("PROGRAMFILES") { roots.push(PathBuf::from(program_files).join("Steam")); }
    roots
}

fn scan_roots(roots: Vec<PathBuf>) -> Vec<SaveLocation> {
    let mut locations = Vec::new();
    for root in roots {
        let Ok(accounts) = fs::read_dir(root.join("userdata")) else { continue; };
        for account in accounts.flatten() {
            let account_id = account.file_name().to_string_lossy().to_string();
            let game_dir = account.path().join(GREEN_HELL_STEAM_APP_ID);
            if game_dir.is_dir() { locations.push(scan_folder(&game_dir.join("remote"), account_id)); }
        }
    }
    locations
}

pub fn scan_folder(path: &Path, account_id: String) -> SaveLocation {
    let mut files = fs::read_dir(path).into_iter().flatten().flatten().filter_map(|entry| {
        let file = entry.path();
        if !file.extension().is_some_and(|ext| ext.eq_ignore_ascii_case("sav")) { return None; }
        let metadata = file.metadata().ok()?;
        let modified = metadata.modified().ok()?.duration_since(std::time::UNIX_EPOCH).ok()?.as_millis() as u64;
        Some(SaveFileInfo { name: file.file_name()?.to_string_lossy().to_string(), path: file.display().to_string(), size: metadata.len(), modified })
    }).collect::<Vec<_>>();
    files.sort_by(|a, b| b.modified.cmp(&a.modified));
    SaveLocation { account_id, path: path.display().to_string(), files }
}

pub fn find() -> Vec<SaveLocation> {
    let mut locations = scan_roots(default_roots());
    if locations.is_empty() {
        if let Ok(steam_root) = std::env::var("STEAM_ROOT") {
            locations = scan_roots(vec![PathBuf::from(steam_root)]);
        }
    }
    locations.sort_by(|a, b| {
        let timestamp = |location: &SaveLocation| location.files.first().map(|file| file.modified).or_else(|| fs::metadata(&location.path).ok()?.modified().ok()?.duration_since(std::time::UNIX_EPOCH).ok().map(|time| time.as_millis() as u64)).unwrap_or(0);
        timestamp(b).cmp(&timestamp(a))
    });
    let mut seen_accounts = HashSet::new();
    locations.retain(|location| seen_accounts.insert(location.account_id.clone()));
    locations
}

pub fn steam_folder(account_id: Option<&str>) -> Result<PathBuf, String> {
    let locations = find();
    let location = match account_id {
        Some(id) => locations.iter().find(|location| location.account_id == id).ok_or_else(|| format!("Could not find the Green Hell save folder for Steam account {id}"))?,
        None => locations.first().ok_or_else(|| "Could not find the Green Hell Steam save folder".to_string())?,
    };
    Ok(PathBuf::from(&location.path))
}
