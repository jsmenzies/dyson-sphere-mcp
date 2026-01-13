#!/bin/bash
# Check BepInEx log output

LOG_FILE="/mnt/c/Users/James/AppData/Roaming/r2modmanPlus-local/DysonSphereProgram/profiles/Default/BepInEx/LogOutput.log"

LINES=${1:-30}

if [ -f "$LOG_FILE" ]; then
    echo "=== BepInEx Log (last $LINES lines) ==="
    tail -n "$LINES" "$LOG_FILE"
else
    echo "ERROR: Log file not found at $LOG_FILE"
    echo "Is the game running?"
    exit 1
fi
