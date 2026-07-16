#!/usr/bin/env bash
# ============================================================
# unity-cli 一键安装脚本（macOS）
#
# 用法：
#   bash Tools/install-unity-cli.sh          # 安装
#   bash Tools/install-unity-cli.sh --force  # 强制重新安装
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
FORCE="${1:-}"

INSTALL_DIR="$HOME/.local/bin"
EXE_PATH="$INSTALL_DIR/unity-cli"
DOWNLOAD_URL="https://github.com/akiojin/unity-cli/releases/latest/download/unity-cli-osx-arm64"

# ---------- 颜色 ----------
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

info()  { echo -e "${GREEN}[INFO]${NC}  $*"; }
warn()  { echo -e "${YELLOW}[WARN]${NC}  $*"; }
error() { echo -e "${RED}[ERROR]${NC} $*"; }
step()  { echo -e "${CYAN}[STEP]${NC} $*"; }

# ---------- 平台检查 ----------
check_platform() {
    case "$(uname -s)" in
        Darwin*) ;;
        *) error "此脚本仅适用于 macOS，Windows 请运行 install-unity-cli.bat"; exit 1 ;;
    esac
    case "$(uname -m)" in
        arm64) ;;
        *) error "目前仅支持 Apple Silicon (arm64) Mac"; exit 1 ;;
    esac
}

# ---------- 检查已安装 ----------
check_existing() {
    if [[ -f "$EXE_PATH" ]]; then
        local ver
        ver=$("$EXE_PATH" --version 2>/dev/null || echo "unknown")
        info "已安装 unity-cli $ver @ $EXE_PATH"
        if [[ "$FORCE" != "--force" ]]; then
            info "使用 --force 强制重新安装"
            exit 0
        fi
        warn "强制重新安装..."
    fi
}

# ---------- 下载安装 ----------
install_binary() {
    step "下载 $DOWNLOAD_URL ..."
    mkdir -p "$INSTALL_DIR"
    local tmpfile="$INSTALL_DIR/unity-cli.tmp"

    if command -v curl &>/dev/null; then
        curl -fSL --progress-bar "$DOWNLOAD_URL" -o "$tmpfile"
    else
        error "未找到 curl"; exit 1
    fi

    chmod +x "$tmpfile"
    mv "$tmpfile" "$EXE_PATH"
    info "安装完成: $EXE_PATH"
}

# ---------- 配置 PATH ----------
configure_path() {
    local shell_rc
    if [[ "$SHELL" == */zsh ]]; then
        shell_rc="$HOME/.zshrc"
    else
        shell_rc="$HOME/.bash_profile"
    fi

    if ! grep -q "$INSTALL_DIR" "$shell_rc" 2>/dev/null; then
        echo "" >> "$shell_rc"
        echo "# unity-cli" >> "$shell_rc"
        echo "export PATH=\"\$PATH:$INSTALL_DIR\"" >> "$shell_rc"
        info "已添加 PATH 到 $(basename "$shell_rc")"
    fi
}

# ---------- 验证 ----------
verify_install() {
    step "验证安装 ..."
    "$EXE_PATH" --version
    info "验证通过！"
    echo ""
    info "============================================="
    info "  unity-cli 安装成功！"
    info "  重启终端后可用: unity-cli system ping"
    info "============================================="
}

# ---------- 主流程 ----------
main() {
    echo ""
    info "unity-cli 安装脚本 (macOS)"
    info "项目: $PROJECT_ROOT"
    echo ""

    check_platform
    check_existing
    install_binary
    configure_path
    verify_install
}

main
