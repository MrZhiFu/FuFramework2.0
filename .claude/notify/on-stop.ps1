# Claude Code Stop Hook - Windows notification
# Reads: stdin JSON {"session_id":"...","last_assistant_message":"..."}
# Sends: Windows Toast with last assistant message summary
# NOTE: keep string literals ASCII-only (PS 5.1 reads BOM-less files as ANSI on Chinese Windows).

# 1. Read stdin with explicit UTF-8 encoding
$stdinStream = [System.Console]::OpenStandardInput()
$reader = New-Object System.IO.StreamReader($stdinStream, [System.Text.Encoding]::UTF8)
$stdinContent = $reader.ReadToEnd()
$reader.Dispose()
$hookData = $null
try { $hookData = $stdinContent | ConvertFrom-Json } catch {}

# 2. Extract summary from last_assistant_message
$summary = "Claude Done"
if ($hookData -and $hookData.last_assistant_message) {
    $text = $hookData.last_assistant_message
    if ($text -and $text.Trim().Length -gt 0) {
        $summary = $text.Trim().Substring(0, [Math]::Min(150, $text.Trim().Length))
    }
}

# 3. Send Toast notification via WinRT
$appId = "ClaudeNotify"
$escapedSummary = [System.Security.SecurityElement]::Escape($summary)
$toastXml = "<toast duration=" + [char]34 + "long" + [char]34 + ">" +
            "<visual><binding template=" + [char]34 + "ToastGeneric" + [char]34 + ">" +
            "<text>Claude Done</text><text>" + $escapedSummary + "</text>" +
            "</binding></visual></toast>"

$toastSent = $false
try {
    [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
    [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
    $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
    $xml.LoadXml($toastXml)
    $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
    $notifier = [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier($appId)
    $notifier.Show($toast)
    $toastSent = $true
} catch {}

# Fallback: Windows Forms balloon tip (works without AppId registration)
if (-not $toastSent) {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        $balloon = New-Object System.Windows.Forms.NotifyIcon
        $balloon.Icon = [System.Drawing.SystemIcons]::Information
        $balloon.BalloonTipTitle = "Claude Done"
        $balloon.BalloonTipText = $summary
        $balloon.Visible = $true
        $balloon.ShowBalloonTip(5000)
        Start-Sleep -Seconds 6
        $balloon.Dispose()
    } catch {}
}
