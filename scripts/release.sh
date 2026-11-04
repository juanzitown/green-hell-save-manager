#!/usr/bin/env bash
set -euo pipefail

PROJECT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION="$(sed -n 's/.*"version": "\([^"]*\)".*/\1/p' "$PROJECT_DIR/package.json" | head -n 1)"
RELEASE_DIR="$PROJECT_DIR/release"
ARCHIVE="$RELEASE_DIR/GHServer-${VERSION:-dev}.zip"
STAGE_DIR="$(mktemp -d)"
STEAM_ROOT="${STEAM_ROOT:-${HOME}/.local/share/Steam}"
GAME_DIR="${GREEN_HELL_DIR:-${STEAM_ROOT}/steamapps/common/Green Hell}"
trap 'rm -rf -- "$STAGE_DIR"' EXIT

bash "$PROJECT_DIR/scripts/build.sh"
mkdir -p "$RELEASE_DIR"
install -m 0644 "$PROJECT_DIR/build/GHServer.dll" "$STAGE_DIR/GHServer.dll"
install -m 0755 "$PROJECT_DIR/scripts/install.sh" "$STAGE_DIR/install.sh"
install -m 0644 "$PROJECT_DIR/config/guide.cfg" "$STAGE_DIR/guide.cfg"
install -m 0644 "$PROJECT_DIR/config/mod.cfg" "$STAGE_DIR/local.ghserver.cfg"
if [[ ! -d "$GAME_DIR/BepInEx/core" || ! -f "$GAME_DIR/winhttp.dll" || ! -f "$GAME_DIR/doorstop_config.ini" ]]; then
    printf 'BepInEx installation not found in %s\n' "$GAME_DIR" >&2
    exit 1
fi
mkdir -p "$STAGE_DIR/BepInEx"
cp -a "$GAME_DIR/BepInEx/core" "$STAGE_DIR/BepInEx/core"
mkdir -p "$STAGE_DIR/BepInEx/config"
cp -a "$GAME_DIR/BepInEx/config/BepInEx.cfg" "$STAGE_DIR/BepInEx/config/BepInEx.cfg"
install -m 0644 "$GAME_DIR/winhttp.dll" "$STAGE_DIR/winhttp.dll"
install -m 0644 "$GAME_DIR/doorstop_config.ini" "$STAGE_DIR/doorstop_config.ini"
if [[ -f "$GAME_DIR/.doorstop_version" ]]; then
    install -m 0644 "$GAME_DIR/.doorstop_version" "$STAGE_DIR/.doorstop_version"
fi

rm -f -- "$ARCHIVE"
(cd "$STAGE_DIR" && zip -q -9 -r "$ARCHIVE" GHServer.dll install.sh guide.cfg local.ghserver.cfg BepInEx winhttp.dll doorstop_config.ini .doorstop_version)
printf 'Created %s\n' "$ARCHIVE"
