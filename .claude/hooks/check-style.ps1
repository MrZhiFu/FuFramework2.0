# FuFramework C# 代码风格检查脚本
# 用法: powershell -File check-style.ps1 <file.cs>
# 退出码: 0 = 通过, 1 = 发现问题

param([string]$TargetFile)

if (-not $TargetFile -or -not (Test-Path $TargetFile)) {
    Write-Output "[StyleCheck] 文件不存在: $TargetFile"
    exit 0
}

# 跳过的目录/文件
$skipPatterns = @(
    "\\Config\\Generate\\",
    "\\Protobuf\\",
    "\.Gen\.cs$",
    "\\UnityWebSocket\\"
)

foreach ($pattern in $skipPatterns) {
    if ($TargetFile -match $pattern) {
        Write-Output "[StyleCheck] 跳过自动生成文件: $TargetFile"
        exit 0
    }
}

$content = Get-Content $TargetFile -Raw
$errors = @()
$fileDir = Split-Path $TargetFile -Parent

# 1. 检查私有字段: _xxx 而非 m_Xxx（排除事件、字符串内容、注释）
$fieldMatches = [regex]::Matches($content, '^\s*(private|protected)\s+\S+\s+(_\w+)', [System.Text.RegularExpressions.RegexOptions]::Multiline)
foreach ($match in $fieldMatches) {
    $fieldName = $match.Groups[2].Value
    $line = ($content.Substring(0, $match.Index) -split "`n").Count
    $errors += "行$line`: 私有字段 '$fieldName' 应使用 m_ 前缀（m_Xxx），不是 _ 前缀"
}

# 2. 检查异步方法是否缺少 Async 后缀
$asyncMethodMatches = [regex]::Matches($content, '(?:private|public|protected|internal|static)\s+(?:async\s+)?(?:UniTask|UniTaskVoid)\s+(\w+)(?!Async)', [System.Text.RegularExpressions.RegexOptions]::Multiline)
foreach ($match in $asyncMethodMatches) {
    $methodName = $match.Groups[1].Value
    if ($methodName -notmatch 'Async$') {
        $line = ($content.Substring(0, $match.Index) -split "`n").Count
        $errors += "行$line`: 异步方法 '$methodName' 应添加 Async 后缀"
    }
}

# 3. 检查手写枚举是否缺少 E 前缀
$enumMatches = [regex]::Matches($content, '(?:public|internal)\s+enum\s+(\w+)', [System.Text.RegularExpressions.RegexOptions]::Multiline)
foreach ($match in $enumMatches) {
    $enumName = $match.Groups[1].Value
    if ($enumName -notmatch '^E[A-Z]') {
        $line = ($content.Substring(0, $match.Index) -split "`n").Count
        $errors += "行$line`: 枚举 '$enumName' 应添加 E 前缀（E$enumName）"
    }
}

# 输出结果
if ($errors.Count -gt 0) {
    Write-Output "============================================="
    Write-Output " [StyleCheck] 风格问题 ($($errors.Count) 个):"
    Write-Output "============================================="
    foreach ($err in $errors) {
        Write-Output "  ✗ $err"
    }
    Write-Output "============================================="
    Write-Output " 完整规范: Docs/FuFramework代码风格规范.md"
    exit 1
}
else {
    Write-Output "[StyleCheck] ✓ 风格检查通过: $TargetFile"
    exit 0
}
