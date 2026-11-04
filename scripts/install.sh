#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
GAME_DIR="${GREEN_HELL_DIR:-}"

usage() {
    printf 'Usage: %s [--game-dir PATH]\n' "$(basename -- "$0")"
    printf '       GREEN_HELL_DIR=/path/to/Green\ Hell %s\n' "$(basename -- "$0")"
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --game-dir)
            [[ $# -ge 2 ]] || { usage >&2; exit 2; }
            GAME_DIR="$2"
            shift 2
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            usage >&2
            exit 2
            ;;
    esac
done

if [[ -z "$GAME_DIR" ]]; then
    for candidate in \
        "${STEAM_ROOT:-}/steamapps/common/Green Hell" \
        "${HOME:-}/.local/share/Steam/steamapps/common/Green Hell" \
        "${HOME:-}/.steam/steam/steamapps/common/Green Hell"; do
        if [[ -n "$candidate" && -d "$candidate/GH_Data" ]]; then
            GAME_DIR="$candidate"
            break
        fi
    done
fi

if [[ -z "$GAME_DIR" || ! -d "$GAME_DIR/GH_Data" ]]; then
    printf 'Could not find Green Hell. Use --game-dir or GREEN_HELL_DIR.\n' >&2
    exit 1
fi

if [[ ! -f "$SCRIPT_DIR/GHServer.dll" ]]; then
    printf 'GHServer.dll is missing beside this installer.\n' >&2
    exit 1
fi

if [[ ! -d "$SCRIPT_DIR/BepInEx/core" || ! -f "$SCRIPT_DIR/winhttp.dll"
    || ! -f "$SCRIPT_DIR/doorstop_config.ini" ]]; then
    printf 'Bundled BepInEx files are missing from this release.\n' >&2
    exit 1
fi

if [[ ! -f "$SCRIPT_DIR/guide.cfg" || ! -f "$SCRIPT_DIR/local.ghserver.cfg" ]]; then
    printf 'Release config files are missing beside this installer.\n' >&2
    exit 1
fi

PLUGIN_DIR="$GAME_DIR/BepInEx/plugins/GHServer"
CONFIG_DIR="$GAME_DIR/BepInEx/config"
mkdir -p "$GAME_DIR/BepInEx" "$PLUGIN_DIR"
cp -a "$SCRIPT_DIR/BepInEx/." "$GAME_DIR/BepInEx/"
install -m 0644 "$SCRIPT_DIR/winhttp.dll" "$GAME_DIR/winhttp.dll"
install -m 0644 "$SCRIPT_DIR/doorstop_config.ini" "$GAME_DIR/doorstop_config.ini"
mkdir -p "$CONFIG_DIR"
install -m 0644 "$SCRIPT_DIR/GHServer.dll" "$PLUGIN_DIR/GHServer.dll"
install -m 0644 "$SCRIPT_DIR/guide.cfg" "$CONFIG_DIR/guide.cfg"
install -m 0644 "$SCRIPT_DIR/local.ghserver.cfg" "$CONFIG_DIR/local.ghserver.cfg"
printf 'Installed GHServer.dll to %s\n' "$PLUGIN_DIR"
printf 'Installed GHServer configuration to %s\n' "$CONFIG_DIR"
