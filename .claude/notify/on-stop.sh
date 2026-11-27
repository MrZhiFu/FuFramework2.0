#!/usr/bin/env bash
# Claude Code Stop Hook - Windows notification
# Sends a Windows Toast notification when Claude finishes a response.
# On non-Windows systems this script exits silently without doing anything.

# Detect Windows environment
if ! command -v powershell.exe &>/dev/null; then
    exit 0
fi

# Pass stdin (hook JSON) to the PowerShell script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cat | powershell.exe -NoProfile -WindowStyle Hidden -File "$SCRIPT_DIR/on-stop.ps1" 2>/dev/null
