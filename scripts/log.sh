#!/usr/bin/env bash
set -euo pipefail

STEAM_ROOT="${STEAM_ROOT:-${HOME}/.local/share/Steam}"
GAME_DIR="${GREEN_HELL_DIR:-${STEAM_ROOT}/steamapps/common/Green Hell}"
LOG_FILE="${GAME_DIR}/BepInEx/LogOutput.log"

case "${1:-}" in
    ""|--cameras)
        LOG_FILE="${GAME_DIR}/BepInEx/GreenHellAutoStart-Camera.log"
        FILTER=' (LIVE|KEEP LIVE|CHANGE|INACTIVE|DISABLE|REMOVE) depth='
        ;;
    --all)
        LOG_FILE="${GAME_DIR}/BepInEx/LogOutput.log"
        FILTER='Green Hell Auto Start|GreenHellAutoStart'
        ;;
    *)
        printf 'Usage: yarn log [--all|--cameras]\n' >&2
        exit 2
        ;;
esac

if [[ ! -f "$LOG_FILE" ]]; then
    printf 'BepInEx log not found: %s\n' "$LOG_FILE" >&2
    exit 1
fi

printf 'Following Green Hell Auto Start logs from %s (Ctrl-C to stop)\n' "$LOG_FILE"
tail -n 100 -F "$LOG_FILE" | grep --line-buffered -E "$FILTER"