# Green Hell Host Mod (WIP)

## Objective

Build a low-resource, host-only Green Hell client that automatically starts the saved multiplayer world and runs with no graphics, or as close to headless as the game permits, so it can act like a dedicated server on an older laptop.

This is an experimental goal, not a capability the game currently exposes as a dedicated-server build. A true headless host may require game-specific startup and networking changes, and disabling rendering must be tested without breaking simulation or co-op connectivity.

## Current Prototype

The current BepInEx plugin auto-continues the latest **single-player** save after the main menu is ready. It explicitly selects single-player mode; it does not start a co-op host, disable graphics, or provide a dedicated server. This prototype is a starting point for investigating the game's startup and save-loading flow.

## Commands

- `yarn build` compiles against the local Green Hell/BepInEx assemblies and deploys the DLL into the game's `BepInEx/plugins/GHSaveAutoStart` folder.
- `yarn restore` removes only this plugin's deployed DLL. It preserves the config and does not remove BepInEx or other plugins.

The build defaults to `~/.local/share/Steam`, the `Proton 11.0` tool, and app ID `815370`. Override paths with `STEAM_ROOT`, `GREEN_HELL_DIR`, `GREEN_HELL_COMPAT_DATA`, and `PROTON_DIR` when needed.

## Source and runtime files

- Source: `src/GreenHellAutoStart.cs`
- Build output: `build/GreenHellAutoStart.dll` (ignored by Git)
- Default config: `config/local.greenhell.autostart.cfg` (deployed only when no config exists)
- Installed plugin: `Green Hell/BepInEx/plugins/GHSaveAutoStart/GreenHellAutoStart.dll`
- Plugin config: `Green Hell/BepInEx/config/local.greenhell.autostart.cfg`

BepInEx is installed separately in the game directory. Keep this repository outside the Steam library; reinstalling Green Hell may remove the deployed plugin and loader, but not this source.

## Session Usage Note

For the Copilot session that created this project, the local session store records 12 conversation turns but does not contain token counts, model usage, billed cost, or plan quota usage. **Total session cost: unavailable. Basic paid-plan quota used: unavailable (percentage cannot be calculated).** Check the account's Copilot usage page for actual plan usage; turn count is not a valid quota estimate.
