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
SOURCE="${PROJECT_DIR}/src/GHServer.cs"
AUDIO_CONTROLLER_SOURCE="${PROJECT_DIR}/src/AudioController.cs"
CAMERA_CONTROLLER_SOURCE="${PROJECT_DIR}/src/CameraController.cs"
PERFORMANCE_CONTROLLER_SOURCE="${PROJECT_DIR}/src/PerformanceController.cs"
RESOURCE_COORDINATOR_SOURCE="${PROJECT_DIR}/src/ResourceControllerCoordinator.cs"
MEMORY_FINDINGS_SOURCE="${PROJECT_DIR}/src/MemoryFindings.cs"
WORLD_UI_SOURCE="${PROJECT_DIR}/src/WorldUIController.cs"
GUIDE_SOURCE="${PROJECT_DIR}/src/GuideController.cs"
GUIDE_CONFIG="${PROJECT_DIR}/config/guide.cfg"
CONFIG_TEMPLATE="${PROJECT_DIR}/config/mod.cfg"
CONFIG_FILE="${GAME_DIR}/BepInEx/config/local.ghserver.cfg"
BUILD_DIR="${PROJECT_DIR}/build"
PLUGIN_DIR="${GAME_DIR}/BepInEx/plugins/GHServer"

for required in "$GAME_DIR/GH_Data/Managed/Assembly-CSharp.dll" \
    "$GAME_DIR/GH_Data/Managed/UnityEngine.AudioModule.dll" \
    "$GAME_DIR/BepInEx/core/BepInEx.dll" \
    "$GAME_DIR/BepInEx/core/0Harmony.dll" \
    "$WINE" "$COMPILER" "$SOURCE" \
    "$AUDIO_CONTROLLER_SOURCE" "$CAMERA_CONTROLLER_SOURCE" "$PERFORMANCE_CONTROLLER_SOURCE" \
    "$RESOURCE_COORDINATOR_SOURCE" "$MEMORY_FINDINGS_SOURCE" "$WORLD_UI_SOURCE" "$GUIDE_SOURCE" "$CONFIG_TEMPLATE" "$GUIDE_CONFIG"; do
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
    "/out:$(windows_path "$BUILD_DIR/GHServer.dll")" \
    "/reference:$(windows_path "$GAME_DIR/BepInEx/core/BepInEx.dll")" \
    "/reference:$(windows_path "$GAME_DIR/BepInEx/core/0Harmony.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/Assembly-CSharp.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.CoreModule.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.UIModule.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.UI.dll")" \
    "/reference:$(windows_path "$GAME_DIR/GH_Data/Managed/UnityEngine.AudioModule.dll")" \
    "$(windows_path "$SOURCE")" \
    "$(windows_path "$AUDIO_CONTROLLER_SOURCE")" \
    "$(windows_path "$CAMERA_CONTROLLER_SOURCE")" \
    "$(windows_path "$PERFORMANCE_CONTROLLER_SOURCE")" \
    "$(windows_path "$RESOURCE_COORDINATOR_SOURCE")" \
    "$(windows_path "$MEMORY_FINDINGS_SOURCE")" \
    "$(windows_path "$WORLD_UI_SOURCE")" \
    "$(windows_path "$GUIDE_SOURCE")"

cp -f "$BUILD_DIR/GHServer.dll" "$PLUGIN_DIR/GHServer.dll"
if ! cmp -s "$BUILD_DIR/GHServer.dll" "$PLUGIN_DIR/GHServer.dll"; then
    printf 'Deployed plugin verification failed: %s\n' "$PLUGIN_DIR/GHServer.dll" >&2
    exit 1
fi
cp -f "$CONFIG_TEMPLATE" "$CONFIG_FILE"
if ! cmp -s "$CONFIG_TEMPLATE" "$CONFIG_FILE"; then
    printf 'Config sync verification failed: %s\n' "$CONFIG_FILE" >&2
    exit 1
fi
cp -f "$GUIDE_CONFIG" "$(dirname -- "$CONFIG_FILE")/guide.cfg"
printf 'Synced plugin config from %s\n' "$CONFIG_TEMPLATE"
printf 'Built and deployed GHServer.dll to %s\n' "$PLUGIN_DIR"
