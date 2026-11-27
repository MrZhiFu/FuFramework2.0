# Claude Notify - One-time setup for Windows Toast notifications
# Run this ONCE per machine to enable proper Toast notifications.
# Without this, notifications fall back to balloon tips (still works, just less styled).
#
# Only needed on Windows. No admin rights required (all changes are in HKCU).

if (-not $env:WINDIR) {
    Write-Host "Not on Windows, nothing to install."
    exit 0
}

$ErrorActionPreference = "Stop"

Write-Host "=== Claude Notify Setup ==="

# Register AppUserModelId for WinRT Toast API
$aumidKey = "HKCU:\SOFTWARE\Classes\AppUserModelId\ClaudeNotify"
New-Item -Path $aumidKey -Force | Out-Null
Set-ItemProperty -Path $aumidKey -Name "DisplayName" -Value "Claude Notify"
Write-Host "[1/2] Toast AppUserModelId registered."

# Enable notification in Windows settings
$notifyKey = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Notifications\Settings\ClaudeNotify"
New-Item -Path $notifyKey -Force | Out-Null
Set-ItemProperty -Path $notifyKey -Name "Enabled" -Value 1 -Type DWord
Write-Host "[2/2] Notification permission registered."

Write-Host ""
Write-Host "=== Done ==="
Write-Host "Claude notifications are now enabled."
Write-Host "Restart Claude Code to activate."
