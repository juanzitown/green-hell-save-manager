#!/usr/bin/env bash
set -euo pipefail

STEAM_ROOT="${STEAM_ROOT:-${HOME}/.local/share/Steam}"
GAME_DIR="${GREEN_HELL_DIR:-${STEAM_ROOT}/steamapps/common/Green Hell}"
COMPAT_DATA="${GREEN_HELL_COMPAT_DATA:-${STEAM_ROOT}/steamapps/compatdata/815370}"
PROTON_DIR="${PROTON_DIR:-${STEAM_ROOT}/steamapps/common/Proton 11.0}"
PREFIX="${COMPAT_DATA}/pfx"
WINE="${PROTON_DIR}/files/bin/wine"
COMPILER="${PREFIX}/drive_c/windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe"
PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE="${PROJECT_DIR}/src/GreenHellAutoStart.cs"
UI_UTILS_SOURCE="${PROJECT_DIR}/src/UIUtils.cs"
CONFIG_TEMPLATE="${PROJECT_DIR}/config/local.greenhell.autostart.cfg"
CONFIG_FILE="${GAME_DIR}/BepInEx/config/local.greenhell.autostart.cfg"
BUILD_DIR="${PROJECT_DIR}/build"
PLUGIN_DIR="${GAME_DIR}/BepInEx/plugins/GHSaveAutoStart"

for required in "$GAME_DIR/GH_Data/Managed/Assembly-CSharp.dll" \
    "$GAME_DIR/BepInEx/core/BepInEx.dll" \
    "$GAME_DIR/BepInEx/core/0Harmony.dll" \
    "$WINE" "$COMPILER" "$SOURCE" "$UI_UTILS_SOURCE" "$CONFIG_TEMPLATE"; do
    if [[ ! -f "$required" ]]; then
        printf 'Missing required file: %s\n' "$required" >&2
        exit 1
    fi
done

windows_path() {
    local path="$1"
    path="${path//\//\\}"
    printf 'Z:%s' "$path"
}

mkdir -p "$BUILD_DIR" "$PLUGIN_DIR"
mkdir -p "$BUILD_DIR" "$PLUGIN_DIR" "$(dirname -- "$CONFIG_FILE")"

WINEPREFIX="$PREFIX" WINEDEBUG=-all "$WINE" "$COMPILER" \
    /nologo /target:library /langversion:6 \
    "/out:$(windows_path "$BUILD_DIR/GreenHellAutoStart.dll")" \
    "/reference:$(windows_path "$GAME_DIR/BepInEx/core/BepInEx.dll")" \
    "/reference:$(windows_path "$GAME_DIR/BepInEx/core/0Harmony.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/Assembly-CSharp.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.CoreModule.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.UIModule.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.UI.dll")" \
    "$(windows_path "$SOURCE")" \
    "$(windows_path "$UI_UTILS_SOURCE")"

cp -f "$BUILD_DIR/GreenHellAutoStart.dll" "$PLUGIN_DIR/GreenHellAutoStart.dll"
if ! cmp -s "$BUILD_DIR/GreenHellAutoStart.dll" "$PLUGIN_DIR/GreenHellAutoStart.dll"; then
    printf 'Deployed plugin verification failed: %s\n' "$PLUGIN_DIR/GreenHellAutoStart.dll" >&2
    exit 1
fi
if [[ ! -e "$CONFIG_FILE" ]]; then
    cp "$CONFIG_TEMPLATE" "$CONFIG_FILE"
else
    printf 'Preserved existing config at %s; edit that file to change runtime settings.\n' "$CONFIG_FILE"
fi
printf 'Built and deployed GreenHellAutoStart.dll to %s\n' "$PLUGIN_DIR"
