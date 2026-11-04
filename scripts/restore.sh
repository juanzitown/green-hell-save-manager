#!/usr/bin/env bash
set -euo pipefail

STEAM_ROOT="${STEAM_ROOT:-${HOME}/.local/share/Steam}"
GAME_DIR="${GREEN_HELL_DIR:-${STEAM_ROOT}/steamapps/common/Green Hell}"
PLUGIN_DIR="${GAME_DIR}/BepInEx/plugins/GHServer"
CONFIG_FILE="${GAME_DIR}/BepInEx/config/local.greenhell.autostart.cfg"

rm -f -- "$PLUGIN_DIR/GHServer.dll"
if [[ -d "$PLUGIN_DIR" ]]; then
    rmdir "$PLUGIN_DIR" 2>/dev/null || true
fi

printf 'Removed GHServer plugin. Config and other mods were left alone.\n'
