#!/usr/bin/env bash
set -euo pipefail

STEAM_ROOT="${STEAM_ROOT:-${HOME}/.local/share/Steam}"
GAME_DIR="${GREEN_HELL_DIR:-${STEAM_ROOT}/steamapps/common/Green Hell}"
LOG_FILE="${GAME_DIR}/BepInEx/LogOutput.log"

case "${1:-}" in
    ""|--memory)
        LOG_FILE="${GAME_DIR}/BepInEx/GHServer-MemoryFindings.log"
        FILTER='scene=|  type='
        ;;
    --all)
        LOG_FILE="${GAME_DIR}/BepInEx/LogOutput.log"
        FILTER='GHServer'
        ;;
    *)
        printf 'Usage: yarn log [--memory|--all]\n' >&2
        exit 2
        ;;
esac

if [[ ! -f "$LOG_FILE" ]]; then
    printf 'BepInEx log not found: %s\n' "$LOG_FILE" >&2
    exit 1
fi

printf 'Following GHServer logs from %s (Ctrl-C to stop)\n' "$LOG_FILE"
tail -n 100 -F "$LOG_FILE" | grep --line-buffered -E "$FILTER"
