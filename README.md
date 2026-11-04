# Green Hell Host Mod (WIP)

## Objective

Build a low-resource, host-only Green Hell client that automatically starts the saved multiplayer world and runs with no graphics, or as close to headless as the game permits, so it can act like a dedicated server on an older laptop.

This is an experimental goal, not a capability the game currently exposes as a dedicated-server build. A true headless host may require game-specific startup and networking changes, and disabling rendering must be tested without breaking simulation or co-op connectivity.

## Current Prototype

The current BepInEx plugin follows **Multiplayer → Host Game → Continue** and auto-continues the latest multiplayer host save. `LowerResources` applies configured performance/display settings and audio behavior. `CameraController` scans scene cameras every 0.1 seconds, disables cameras outside `CameraNamesToKeep`, and writes timestamped live-camera and disable events to the camera log. This experimental policy can soft-lock startup if a camera needed during loading is not kept. Routine diagnostics are currently limited to the camera log.

## Commands

- `yarn build` compiles against the local Green Hell/BepInEx assemblies, deploys `GHServer.dll`, and syncs `config/mod.cfg` to the installed `local.greenhell.autostart.cfg` on every build. Workspace config is authoritative; installed-only edits are overwritten.
- `yarn restore` removes only this plugin's deployed DLL. It preserves the config and does not remove BepInEx or other plugins.
- `yarn log` or `yarn log --cameras` follows timestamped camera events; use `yarn log --all` for plugin errors. Stop with Ctrl-C.

The build defaults to `~/.local/share/Steam`, the `Proton 11.0` tool, and app ID `815370`. Override paths with `STEAM_ROOT`, `GREEN_HELL_DIR`, `GREEN_HELL_COMPAT_DATA`, and `PROTON_DIR` when needed.

## Source and runtime files

- Source: `src/GHServer.cs`
- Build output: `build/GHServer.dll` (ignored by Git)
- Config source: `config/mod.cfg` (synced to the installed plugin config on every build)
- Installed plugin: `Green Hell/BepInEx/plugins/GHServer/GHServer.dll`
- Plugin config: `Green Hell/BepInEx/config/local.greenhell.autostart.cfg`

BepInEx is installed separately in the game directory. Keep this repository outside the Steam library; reinstalling Green Hell may remove the deployed plugin and loader, but not this source.

## Session Usage Note

For the Copilot session that created this project, the local session store records 12 conversation turns but does not contain token counts, model usage, billed cost, or plan quota usage. **Total session cost: unavailable. Basic paid-plan quota used: unavailable (percentage cannot be calculated).** Check the account's Copilot usage page for actual plan usage; turn count is not a valid quota estimate.
