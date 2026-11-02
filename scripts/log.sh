#!/usr/bin/env bash
set -euo pipefail

STEAM_ROOT="${STEAM_ROOT:-${HOME}/.local/share/Steam}"
GAME_DIR="${GREEN_HELL_DIR:-${STEAM_ROOT}/steamapps/common/Green Hell}"
LOG_FILE="${GAME_DIR}/BepInEx/LogOutput.log"

case "${1:-}" in
    "")
        FILTER='Menu diagnostics active|Active menu screen:|Auto sequence state:|Resource settings:|Audio paused:|Requested window resolution:|World transition:|World status:|World scene loaded:|Active world scene changed:|Disabling menu camera:|Waiting to disable gameplay camera:|Camera shutdown ready:|Disabling gameplay camera rendering:|Gameplay Main Camera became active again|Disabling camera rendering:|Camera active after disable|Set menu display mode|display-mode enum|multiplayer display mode|UI click:|UI click listener:|Continue handler invoked:|Selecting Multiplayer\.|Selecting Host Game\.|Multiplayer save found|No multiplayer save found|Auto-continue failed|Menu diagnostics failed'
        ;;
    --all)
        FILTER='Green Hell Auto Start|GreenHellAutoStart'
        ;;
    --cameras)
        LOG_FILE="${GAME_DIR}/BepInEx/GreenHellAutoStart-Camera.log"
        FILTER='.'
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