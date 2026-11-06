use serde::Serialize;
use std::fs;
use std::path::{Path, PathBuf};

#[derive(Debug, Serialize)]
pub struct SaveFileInfo { pub name: String, pub path: String, pub size: u64, pub modified: u64 }

#[derive(Debug, Serialize)]
pub struct SaveLocation { pub path: String, pub files: Vec<SaveFileInfo> }

fn candidate_roots() -> Vec<PathBuf> {
    let mut roots = Vec::new();
    if let Some(home) = dirs::home_dir() {
        roots.push(home.join(".local/share/Steam"));
        roots.push(home.join(".steam/steam"));
        roots.push(home.join(".steam/root"));
        roots.push(home.join(".var/app/com.valvesoftware.Steam/.local/share/Steam"));
    }
    if let Ok(steam) = std::env::var("STEAM_ROOT") { roots.push(PathBuf::from(steam)); }
    if let Ok(program_files) = std::env::var("PROGRAMFILES(X86)") { roots.push(PathBuf::from(program_files).join("Steam")); }
    if let Ok(program_files) = std::env::var("PROGRAMFILES") { roots.push(PathBuf::from(program_files).join("Steam")); }
    roots
}

fn inspect_save_folder(path: &Path, found: &mut Vec<SaveLocation>) {
    let Ok(entries) = fs::read_dir(path) else { return; };
    let mut saves = Vec::new();
    for entry in entries.flatten() {
        let entry_path = entry.path();
        if entry_path.extension().is_some_and(|ext| ext.eq_ignore_ascii_case("sav")) {
            if let Ok(metadata) = entry_path.metadata() {
                let modified = metadata.modified().ok().and_then(|time| time.duration_since(std::time::UNIX_EPOCH).ok()).map(|time| time.as_millis() as u64).unwrap_or(0);
                saves.push(SaveFileInfo { name: entry_path.file_name().unwrap_or_default().to_string_lossy().to_string(), path: entry_path.display().to_string(), size: metadata.len(), modified });
            }
        }
    }
    if !saves.is_empty() {
        saves.sort_by(|a, b| b.modified.cmp(&a.modified));
        found.push(SaveLocation { path: path.display().to_string(), files: saves });
    }
}

#[tauri::command]
fn find_green_hell_saves() -> Vec<SaveLocation> {
    let mut locations = Vec::new();
    for root in candidate_roots() {
        let userdata = root.join("userdata");
        let Ok(accounts) = fs::read_dir(userdata) else { continue; };
        for account in accounts.flatten() {
            if !account.path().is_dir() { continue; }
            let save_folder = account.path().join("815370").join("remote");
            if save_folder.is_dir() { inspect_save_folder(&save_folder, &mut locations); }
        }
    }
    locations.sort_by(|a, b| b.files.first().map(|file| file.modified).cmp(&a.files.first().map(|file| file.modified)));
    locations.dedup_by(|a, b| a.path == b.path);
    locations
}

#[tauri::command]
fn read_save_file(path: String) -> Result<Vec<u8>, String> { fs::read(path).map_err(|error| error.to_string()) }

#[tauri::command]
fn export_save_file(path: String) -> Result<String, String> {
    let source = PathBuf::from(&path);
    let downloads = dirs::download_dir().or_else(|| dirs::home_dir().map(|home| home.join("Downloads"))).ok_or_else(|| "Could not find a Downloads folder".to_string())?;
    fs::create_dir_all(&downloads).map_err(|error| error.to_string())?;
    let name = source.file_name().ok_or_else(|| "Invalid save filename".to_string())?;
    let destination = downloads.join(name);
    fs::copy(&source, &destination).map_err(|error| error.to_string())?;
    Ok(destination.display().to_string())
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![find_green_hell_saves, read_save_file, export_save_file])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
