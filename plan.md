# Green Hell Save Manager

## Product direction

Build a cross-platform Green Hell save manager using Tauri with a Vite frontend.
The application manages local saves first and adds cloud synchronization later.

The former GHServer/BepInEx dedicated-host experiment is out of scope for the
application. Existing GHServer files are kept temporarily as historical
reference and must not be used by the new app.

## Technology

- Tauri desktop application
- Vite frontend
- TypeScript frontend
- Rust Tauri backend for filesystem access and native integrations
- Windows and Linux first
- macOS optional after the first two platforms are stable
- Local metadata stored in SQLite or a small versioned local database

## Product rules

- Green Hell is the only supported game in the first release.
- Profiles are independent containers; saves must never mix between profiles.
- A profile has a name, icon, and optional Steam ID.
- Profiles have no roles.
- The main view should feel like a folder browser.
- Save items are sorted by latest modification time, then filename.
- Dates and other metadata are shown inside the save item/card, not in a separate history panel.
- Replacing a game save must create a backup first.
- Cloud sync must never silently overwrite a newer local or cloud save.

## Phase 01 — Local save manager and Steam data

### Scope

- Create, rename, delete, and select profiles.
- Assign a profile icon.
- Optionally associate a Steam ID with a profile.
- Detect Green Hell installation and local save locations on Windows and Linux.
- Import saves into the selected profile.
- Drag and drop save files into `My Profile`.
- Export individual saves.
- Export the complete profile.
- Restore a selected save to Green Hell after creating a safety backup.
- Browse each profile's saves in a folder-like interface.
- Sort saves by modification time and filename.
- Fetch Steam display name, avatar, and basic profile information when available.
- Optionally fetch the user's Steam friends for profile association.

### Phase 01 storage layout

The app-owned data must remain separate from the game's live save directory:

```text
app-data/
  profiles/
    <profile-id>/
      profile.json
      saves/
        MPSlot0.sav
        MPSlot1.sav
```

Each profile owns its own `saves/` directory. The app must not merge or
deduplicate saves across profiles.

### Phase 01 completion criteria

- A user can open the app and select a profile.
- A user can import or drag a Green Hell save into that profile.
- A user can see and sort the profile's saves.
- A user can export a save or the full profile.
- A user can restore a save to Green Hell with an automatic backup.
- Steam data is optional and failure to fetch it does not block local use.
- The app works without network access except for Steam lookup.

## Phase 02 — Cloud synchronization

### Scope

- Add a cloud-provider abstraction.
- Implement one provider first, then add the second:
  - Google Drive
  - Dropbox
- Authenticate through OAuth.
- Upload an individual save manually.
- Download an individual save manually.
- Upload or download a complete profile.
- Keep cloud data isolated by profile.
- Display local and cloud saves in the same folder-like view.
- Record upload/download time and source machine in save metadata.
- Keep timestamped cloud snapshots for recovery.
- Detect changed local and cloud versions.
- Require an explicit choice when both sides changed.

### Phase 02 cloud layout

```text
GreenHellSaveManager/
  profiles/
    <profile-id>/
      saves/
        <save-file>
      snapshots/
        <timestamp>-<machine-id>/
          <save-file>
          metadata.json
```

The game should not run directly against a mounted cloud folder. The app
should download a complete file, validate it, back up the current local file,
and then install it into Green Hell.

### Phase 02 completion criteria

- A user can connect a cloud provider.
- A user can manually upload and download profile saves.
- Cloud saves remain separated by profile.
- The app preserves recoverable snapshots.
- Conflicts are visible and never resolved by silent overwrite.
- The app continues to work when the cloud provider is unavailable.

## Later, not required for Phase 01/02

- Automatic sync after Green Hell exits.
- Background filesystem watching.
- Periodic sync.
- Additional games and game-specific adapters.
- Browser companion view.
- macOS support.
- Shared app-managed libraries and invitations.
- Syncthing, WebDAV, S3, or self-hosted storage.

## Development checkpoints

Update this file during development. For each checkpoint, record:

- Current phase and status.
- Completed items.
- Open technical decisions.
- Known limitations.
- Verification performed.

### Current status

- Phase: Phase 01
- Status: In progress — frontend skeleton and local profile prototype created
- Completed: Tauri/Vite package setup, Tauri configuration scaffold, game-view UI with profile-only sidebar, `My Profile` as the default profile, separated `VerticalMenu`, `ProfilePage`, and `ProfileFormModal` components, isolated profile save lists, sorting, search, Tauri-only `.sav` listing/download/upload, direct uploads from `My Profile` to Steam, backup uploads for other profiles, `Copy Save` from backup profiles to Steam, drag-and-drop upload, and Steam-root detection for the fixed `userdata/<SteamID>/815370/remote` path.
- Completed: `npm run dev` now launches the Tauri desktop app through the Debian Distrobox container; Rust save discovery is separated into `src-tauri/src/save_scanner.rs`, with shared profile-directory and filename validation helpers in `lib.rs`.
- Completed: Removed mocked saves/profiles, Steam detection now resolves App ID `815370` even when `remote` contains no saves, backup profiles reload from native storage, and detected folder paths open in the OS file explorer.
- Completed: Profile metadata is persisted in SQLite through Tauri's official SQL plugin and migrations. Sonner provides top-center success/error feedback for profile, upload, copy, download, and folder actions.
- Completed: Global `ProfilesContext` now owns profile selection, SQLite CRUD, dialogs, drag/drop, save refreshes, and initial hydration for every profile. Sidebar counts are populated from native folders before stored profiles are rendered, while `App.tsx` is layout-only.
- Open: Load detected saves directly from the Tauri backend, implement native export/restore, persist profiles, and add Steam lookup.
- Next step: Run `npm run tauri dev` and verify detection against the local Steam/Proton installation.
