mod save_scanner;

use std::fs;
use std::path::{Path, PathBuf};
use std::process::Command;
use tauri::Manager;
use save_scanner::{SaveLocation, steam_folder};
use tauri_plugin_sql::{Migration, MigrationKind};

#[tauri::command]
fn find_green_hell_saves() -> Vec<SaveLocation> { save_scanner::find() }

#[tauri::command]
fn find_profile_saves(app: tauri::AppHandle, profile_id: String) -> Result<SaveLocation, String> {
    let folder = profile_save_dir(&app, &profile_id)?;
    fs::create_dir_all(&folder).map_err(|error| error.to_string())?;
    Ok(save_scanner::scan_folder(&folder, String::new()))
}

#[tauri::command]
fn open_folder(path: String) -> Result<(), String> {
    let folder = PathBuf::from(path);
    if !folder.is_dir() { return Err("Folder does not exist".to_string()); }
    #[cfg(target_os = "windows")]
    let result = Command::new("explorer").arg(&folder).spawn();
    #[cfg(target_os = "macos")]
    let result = Command::new("open").arg(&folder).spawn();
    #[cfg(target_os = "linux")]
    let result = Command::new("distrobox-host-exec").arg("xdg-open").arg(&folder).spawn().or_else(|_| Command::new("xdg-open").arg(&folder).spawn());
    result.map(|_| ()).map_err(|error| error.to_string())
}

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

fn profile_save_dir(app: &tauri::AppHandle, profile_id: &str) -> Result<PathBuf, String> {
    app.path().app_data_dir().map_err(|error| error.to_string()).map(|dir| dir.join("profiles").join(profile_id).join("saves"))
}

fn safe_save_name(name: &str) -> Result<String, String> {
    let safe = Path::new(name).file_name().ok_or_else(|| "Invalid save filename".to_string())?.to_string_lossy().to_string();
    if !safe.to_lowercase().ends_with(".sav") { return Err("Only .sav files can be uploaded".to_string()); }
    Ok(safe)
}

#[tauri::command]
fn upload_save_to_profile(app: tauri::AppHandle, profile_id: String, steam_account_id: Option<String>, name: String, bytes: Vec<u8>) -> Result<String, String> {
    let name = safe_save_name(&name)?;
    let destination_dir = if profile_id == "my" { steam_folder(steam_account_id.as_deref())? } else { profile_save_dir(&app, &profile_id)? };
    fs::create_dir_all(&destination_dir).map_err(|error| error.to_string())?;
    let destination = destination_dir.join(name);
    fs::write(&destination, bytes).map_err(|error| error.to_string())?;
    Ok(destination.display().to_string())
}

#[tauri::command]
fn upload_save_paths(app: tauri::AppHandle, profile_id: String, steam_account_id: Option<String>, paths: Vec<String>) -> Result<Vec<String>, String> {
    let destination_dir = if profile_id == "my" { steam_folder(steam_account_id.as_deref())? } else { profile_save_dir(&app, &profile_id)? };
    fs::create_dir_all(&destination_dir).map_err(|error| error.to_string())?;
    let mut uploaded = Vec::new();
    for path in paths {
        let source = PathBuf::from(&path);
        let name = safe_save_name(source.file_name().unwrap_or_default().to_string_lossy().as_ref())?;
        let destination = destination_dir.join(name);
        fs::copy(source, &destination).map_err(|error| error.to_string())?;
        uploaded.push(destination.display().to_string());
    }
    Ok(uploaded)
}

#[tauri::command]
fn copy_profile_save_to_steam(app: tauri::AppHandle, profile_id: String, steam_account_id: Option<String>, name: String) -> Result<String, String> {
    let source = profile_save_dir(&app, &profile_id)?.join(safe_save_name(&name)?);
    let destination = steam_folder(steam_account_id.as_deref())?.join(source.file_name().unwrap());
    fs::copy(source, &destination).map_err(|error| error.to_string())?;
    Ok(destination.display().to_string())
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let migrations = vec![Migration {
        version: 1,
        description: "create_profiles_table",
        sql: "CREATE TABLE IF NOT EXISTS profiles (id TEXT PRIMARY KEY NOT NULL, name TEXT NOT NULL, steam_id TEXT, is_primary INTEGER NOT NULL DEFAULT 0, created_at INTEGER NOT NULL); INSERT OR IGNORE INTO profiles (id, name, steam_id, is_primary, created_at) VALUES ('my', 'My Profile', NULL, 1, unixepoch());",
        kind: MigrationKind::Up,
    }];
    tauri::Builder::default()
        .plugin(tauri_plugin_sql::Builder::default().add_migrations("sqlite:green-hell-save-manager.db", migrations).build())
        .invoke_handler(tauri::generate_handler![find_green_hell_saves, find_profile_saves, open_folder, export_save_file, upload_save_to_profile, upload_save_paths, copy_profile_save_to_steam])
        .run(tauri::generate_context!())
        .expect("error while running Green Hell Save Manager");
}
