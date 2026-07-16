@echo off
chcp 65001 >nul
:: ============================================================
:: unity-cli 一键安装脚本（Windows）
::
:: 用法：双击运行
:: ============================================================
setlocal enabledelayedexpansion

set "INSTALL_DIR=%LocalAppData%\Programs\unity-cli"
set "EXE_PATH=%INSTALL_DIR%\unity-cli.exe"
set "DOWNLOAD_URL=https://github.com/akiojin/unity-cli/releases/latest/download/unity-cli-win-x64"

echo.
echo ============================================
echo   unity-cli 安装脚本 (Windows)
echo ============================================
echo.

:: 检查 curl
where curl >nul 2>&1
if %errorlevel% neq 0 (
    echo [错误] 未找到 curl，Windows 10+ 自带，无需额外安装
    pause
    exit /b 1
)

:: 检查已安装
if exist "%EXE_PATH%" (
    echo [信息] unity-cli 已安装
    echo [信息] 路径: %EXE_PATH%
    "%EXE_PATH%" --version 2>&1
    echo [信息] 如需重装，请删除上述目录后重新运行
    pause
    exit /b 0
)

:: 创建安装目录
echo [步骤] 创建安装目录...
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"
if %errorlevel% neq 0 (
    echo [错误] 无法创建目录: %INSTALL_DIR%
    pause
    exit /b 1
)

:: 下载
echo [步骤] 下载 unity-cli ...
curl -fSL --progress-bar "%DOWNLOAD_URL%" -o "%EXE_PATH%"
if %errorlevel% neq 0 (
    echo [错误] 下载失败，请检查网络连接
    pause
    exit /b 1
)

:: 验证
echo [步骤] 验证安装...
"%EXE_PATH%" --version
if %errorlevel% neq 0 (
    echo [错误] 验证失败
    pause
    exit /b 1
)

:: 添加到用户 PATH
echo [步骤] 配置 PATH ...
set "PATH_ENTRY=%INSTALL_DIR%"
setx PATH "%PATH%;%PATH_ENTRY%" >nul 2>&1
if %errorlevel% equ 0 (
    echo [信息] PATH 已更新
) else (
    echo [警告] PATH 更新失败，请手动添加: %INSTALL_DIR%
)

echo.
echo ============================================
echo   安装完成!
echo   路径: %EXE_PATH%
echo.
echo   重新打开命令行后可用:
echo     unity-cli system ping
echo.
echo   Bridge 包已在 manifest.json 中配置
echo ============================================
echo.
pause
