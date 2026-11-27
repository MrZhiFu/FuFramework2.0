#!/bin/bash
# publish-ui.sh - FairyGUI UI 包自动发布（本项目版）
#
# 流程：确保 FairyGUI 编辑器在线（未开则拉起、已开则复用）→ fairygui-cli 发布 → 落盘稳定门禁 → 产物自检
#
# 用法（在项目根执行）：
#   bash .claude/publish-ui.sh                  发布全部 UI 包
#   bash .claude/publish-ui.sh <包名> [包名...]  仅发布指定包
#   bash .claude/publish-ui.sh --check          只自检编辑器链路，不发布
#
# 环境变量：
#   FAIRYGUI_EDITOR         编辑器 exe 路径（默认 D:\Program Files\FairyGUI-Editor_6.1.4\...）
#   PUBLISH_UI_TIMEOUT_MS   发布命令超时（默认 900000，全包发布较慢）
#
# 工程结构约定：
#   FairyGUIProject/Game.fairy              工程文件
#   FairyGUIProject/fairygui-cli.exe        CLI
#   FairyGUIProject/plugins/MCPBridge/      编辑器插件（bridge/ 为运行时通信目录）
#   Unity/Assets/Bundles/UI/<包名>/         发布产物（_fui.bytes + atlas）
#
# 行为约定：
#   - 工程已在线（bridge healthy）则复用，脚本结束【不】关闭它
#   - 只关闭脚本自己拉起的编辑器进程（按 PID 精确关闭）
#   - 发布响应先于异步写盘，收尾前必须等「最短保持期 + 连续安静」，否则大包会被截杀

set -uo pipefail

PUBLISH_TIMEOUT_MS="${PUBLISH_UI_TIMEOUT_MS:-900000}"
REUSE_WAIT_TRIES=5         # 复用前短等 5x3s
LAUNCH_WAIT_TRIES=60       # 拉起后等 healthy 60x3s（冷启动可达 2-3 分钟）
SETTLE_QUIET_ROUNDS=2      # 落盘稳定：连续 2 轮无新写入
SETTLE_MIN_HOLD_S=15       # 收尾前最短保持期（异步起写有数秒延迟）
SETTLE_TIMEOUT_S=180       # 落盘稳定最长等待

UI_PROJECT_REL="FairyGUIProject/Game.fairy"
CLI_REL="FairyGUIProject/fairygui-cli.exe"
PROJECT_DIR_REL="FairyGUIProject"
PUBLISH_DIR_REL="Unity/Assets/Bundles/UI"
EDITOR_EXE="${FAIRYGUI_EDITOR:-D:/Program Files/FairyGUI-Editor_6.1.4/FairyGUI-Editor/FairyGUI-Editor.exe}"

ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"

log_step()  { printf "\n[%d/%d] %s\n" "$1" "$2" "$3"; }
log_ok()    { printf "  OK  %s\n" "$1"; }
log_fail()  { printf "  ERR %s\n" "$1" >&2; }
fail() { log_fail "$1"; exit 1; }

to_win() {
    if command -v cygpath >/dev/null 2>&1; then cygpath -w "$1"
    else printf '%s' "$1" | sed 's|^/\([a-zA-Z]\)/|\1:/|; s|/|\\\\|g'; fi
}

# --- 参数解析 ---
MODE="publish"
PKGS=()
for arg in "$@"; do
    case "$arg" in
        --check) MODE="check" ;;
        --*)     fail "不支持的选项: $arg（仅支持 --check）" ;;
        *)       PKGS+=("$arg") ;;
    esac
done
if [ "$MODE" = "check" ] && [ "${#PKGS[@]}" -gt 0 ]; then fail "--check 不接受包名参数"; fi

# --- Step 1: 检查工程与工具 ---
log_step 1 4 "检查工程与 FairyGUI 工具"

[ -f "$ROOT/$UI_PROJECT_REL" ] || fail "未找到 $UI_PROJECT_REL，请在项目根执行"
[ -f "$ROOT/$CLI_REL" ]        || fail "未找到 $CLI_REL"
[ -f "$EDITOR_EXE" ]           || fail "未找到 FairyGUI 编辑器: $EDITOR_EXE（可用 FAIRYGUI_EDITOR 环境变量指定）"
log_ok "工程 / CLI / 编辑器均已就绪"

cd "$ROOT/$PROJECT_DIR_REL" || fail "无法进入 $PROJECT_DIR_REL"

# --- Step 2: 确保编辑器在线 ---
LAUNCHED=0
EDITOR_PID=""

# 健康检查：不用 `status` 的 overall——它的 editor_process 检测按窗口/进程特征匹配，
# 对本工程（Game.fairy）恒 down，会导致误判并重复拉起实例。
# 改为直接写一条 bridge 命令，插件在 5s 内响应即视为在线。
BRIDGE_DIR="plugins/MCPBridge/bridge"
cli_healthy() {
    local probe="$1" id="cmd_health_$$_$RANDOM" waited=0
    mkdir -p "$BRIDGE_DIR/commands" "$BRIDGE_DIR/results"
    printf '{"id":"%s","action":"list_components","params":{"package_name":"%s"}}' "$id" "$probe" \
        > "$BRIDGE_DIR/commands/$id.json" 2>/dev/null
    while [ "$waited" -lt 5 ]; do
        if [ -f "$BRIDGE_DIR/results/$id.json" ]; then
            rm -f "$BRIDGE_DIR/results/$id.json" "$BRIDGE_DIR/commands/$id.json"
            return 0
        fi
        sleep 1; waited=$((waited + 1))
    done
    rm -f "$BRIDGE_DIR/commands/$id.json"
    return 1
}

wait_healthy() {
    local probe="$1" tries="$2"
    while [ "$tries" -gt 0 ]; do
        cli_healthy "$probe" && return 0
        tries=$((tries - 1)); sleep 3
    done
    return 1
}

close_launched_editor() {
    local quiet="${1:-}"
    [ "$LAUNCHED" = "1" ] && [ -n "$EDITOR_PID" ] || return 0
    powershell -NoProfile -Command "try { Stop-Process -Id $EDITOR_PID -Force -ErrorAction Stop } catch { }"
    sleep 2
    if powershell -NoProfile -Command "if (Get-Process -Id $EDITOR_PID -ErrorAction SilentlyContinue) { exit 1 }"; then
        [ -n "$quiet" ] || log_ok "编辑器已关闭 (PID $EDITOR_PID)"
        # 强杀不触发插件 onDestroy，owner 文件会残留死 PID（下次实例可自愈接管）
        rm -f "plugins/MCPBridge/bridge/bridge_owner"
    else
        [ -n "$quiet" ] || log_fail "编辑器关闭后仍在运行 (PID $EDITOR_PID)"
    fi
    LAUNCHED=0; EDITOR_PID=""
}
cleanup() { close_launched_editor quiet; }
trap cleanup EXIT

# 发布响应先返回、异步写盘后开始；必须「过了保持期」且「连续安静」才收尾，否则大包被截杀
wait_write_settle() {
    [ "$MODE" = "check" ] && return 0
    local quiet_rounds=0 elapsed=0
    while [ "$elapsed" -lt "$SETTLE_TIMEOUT_S" ]; do
        local recent
        recent=$(find "$ROOT/$PUBLISH_DIR_REL" -type f -newermt "@$(( $(date +%s) - 3 ))" 2>/dev/null | wc -l)
        if [ "$recent" -eq 0 ]; then quiet_rounds=$((quiet_rounds + 1)); else quiet_rounds=0; fi
        elapsed=$((elapsed + 3))
        if [ "$elapsed" -ge "$SETTLE_MIN_HOLD_S" ] && [ "$quiet_rounds" -ge "$SETTLE_QUIET_ROUNDS" ]; then
            return 0
        fi
        sleep 3
    done
    log_fail "产物 ${SETTLE_TIMEOUT_S}s 内未落盘稳定，编辑器已保留，请人工核对"
    return 1
}

log_step 2 4 "确保编辑器在线"

# 工程就绪探测：bridge healthy 只代表插件 poll 就绪，工程包加载更晚；
# 探测必须走与发布相同的路径（GetPackageByName）
wait_project_ready() {
    local probe_pkg="$1" tries=0
    while [ "$tries" -lt 60 ]; do
        local probe_id="cmd_rdy$$_$tries" count="" waited=0
        mkdir -p "plugins/MCPBridge/bridge/commands" "plugins/MCPBridge/bridge/results"
        printf '{"action": "list_components", "params": {"package_name": "%s"}}' "$probe_pkg" > "plugins/MCPBridge/bridge/commands/$probe_id.json"
        while [ "$waited" -lt 10 ]; do
            if [ -f "plugins/MCPBridge/bridge/results/$probe_id.json" ]; then
                count=$(python -c "import json; d=json.load(open(r'plugins/MCPBridge/bridge/results/$probe_id.json',encoding='utf-8')); print(1 if d.get('status')=='success' else 0)" 2>/dev/null)
                rm -f "plugins/MCPBridge/bridge/results/$probe_id.json"
                break
            fi
            sleep 1; waited=$((waited + 1))
        done
        rm -f "plugins/MCPBridge/bridge/commands/$probe_id.json"
        case "$count" in ''|*[!0-9]*) count=0 ;; esac
        if [ "$count" -eq 1 ]; then log_ok "工程已就绪（可查到包 '$probe_pkg'）"; return 0; fi
        tries=$((tries + 1)); sleep 3
    done
    return 1
}

# 同工程多实例共享 bridge 会互相抢命令，发布结果不可信
INSTANCE_COUNT="$(powershell -NoProfile -Command "@(Get-CimInstance Win32_Process -Filter \"Name='FairyGUI-Editor.exe'\" | Where-Object { \$_.CommandLine -like '*Game.fairy*' }).Count")"
case "$INSTANCE_COUNT" in ''|*[!0-9]*) INSTANCE_COUNT=0 ;; esac
if [ "$INSTANCE_COUNT" -gt 1 ]; then
    fail "检测到 $INSTANCE_COUNT 个本工程编辑器实例共享 bridge，命令会被抢占；请关闭多余实例后重试"
fi

# 先取探测用包名（只读取工程 XML，不需要插件在线）
PROBE_PKG="${PKGS[0]:-}"
if [ -z "$PROBE_PKG" ]; then
    PROBE_PKG="$(./fairygui-cli.exe packages list 2>/dev/null | python -c "import json,sys; pkgs=json.load(sys.stdin); print(pkgs[0]['name'] if pkgs else '')" 2>/dev/null | tr -d '\r')"
fi
[ -n "$PROBE_PKG" ] || fail "无法确定探测用包名（fairygui-cli packages list 无输出）"

if cli_healthy "$PROBE_PKG"; then
    log_ok "工程已在线，复用现有编辑器实例"
elif wait_healthy "$PROBE_PKG" "$REUSE_WAIT_TRIES"; then
    log_ok "编辑器启动中，等待就绪后复用"
else
    printf "  编辑器不在线，拉起工程: %s\n" "$UI_PROJECT_REL"
    EDITOR_PID="$(powershell -NoProfile -Command "\$p = Start-Process -FilePath '$(to_win "$EDITOR_EXE")' -ArgumentList '\"$(to_win "$ROOT/$UI_PROJECT_REL")\"' -PassThru; Write-Output \$p.Id")"
    case "$EDITOR_PID" in ''|*[!0-9]*) fail "拉起编辑器失败（未取得 PID）" ;; esac
    LAUNCHED=1
    if wait_healthy "$PROBE_PKG" "$LAUNCH_WAIT_TRIES"; then
        log_ok "编辑器已拉起并就绪 (PID $EDITOR_PID)"
    else
        fail "编辑器拉起后 $((LAUNCH_WAIT_TRIES * 3))s 内插件未响应，请确认 MCPBridge 已加载"
    fi
fi

# 再确认工程完全就绪（插件 online ≠ 包全部加载完）
wait_project_ready "$PROBE_PKG" || fail "工程长时间未就绪（探测 10 分钟无果）"

# --- Step 3: 发布 ---
PUBLISH_DIR="$ROOT/$PUBLISH_DIR_REL"
FAILED_PKGS=()

# 产物自检：存在、非空、FGUI 魔数、mtime 晚于本轮开始（防旧产物骗过检查）
check_artifact() {
    local bytes="$3/${1}_fui.bytes"
    if [ ! -f "$bytes" ]; then log_fail "包 $1 产物缺失: ${1}_fui.bytes"; return 1; fi
    if [ ! -s "$bytes" ]; then log_fail "包 $1 产物为空: ${1}_fui.bytes"; return 1; fi
    if [ "$(head -c 4 "$bytes")" != "FGUI" ]; then log_fail "包 $1 产物缺少 FGUI 魔数头"; return 1; fi
    if [ "$#" -ge 2 ] && [ "$2" != "" ] && [ "$(stat -c %Y "$bytes")" -lt "$2" ]; then
        log_fail "包 $1 产物未更新（mtime 早于本轮发布）"; return 1
    fi
    return 0
}

# 包产物目录：本项目按包分目录（UI/<包名>/<包名>_fui.bytes）；
# 包级 package.xml 的 <publish path> 可重定向则优先
pkg_out_dir() {
    local pkg="$1" p
    p=$(grep -o '<publish[^>]*path="[^"]*"' "assets/$pkg/package.xml" 2>/dev/null | head -1 | sed 's/.*path="\([^"]*\)".*/\1/')
    if [ -n "$p" ]; then
        printf '%s' "$(realpath -m "$p" 2>/dev/null || printf '%s' "$ROOT/$PROJECT_DIR_REL/$p")"
    else
        printf '%s' "$PUBLISH_DIR/$pkg"
    fi
}

publish_once() {
    local pkg="$1" start_ts resp
    start_ts=$(date +%s)
    resp=$(./fairygui-cli.exe --timeout "$PUBLISH_TIMEOUT_MS" publish package "$pkg" 2>&1)
    if [ $? -ne 0 ] || ! printf '%s' "$resp" | grep -q '"published":true'; then
        log_fail "包 $pkg 触发失败: $(printf '%s' "$resp" | grep -o '"error":"[^"]*"' | head -1 | cut -c1-80)"
        return 1
    fi
    wait_write_settle || { log_fail "包 $pkg 落盘未稳定"; return 1; }
    check_artifact "$pkg" "$start_ts" "$(pkg_out_dir "$pkg")" || return 1
    return 0
}

publish_one() {
    local pkg="$1" round
    printf "  发布包: %s\n" "$pkg"
    for round in 1 2 3; do
        if publish_once "$pkg"; then log_ok "包 $pkg 发布完成"; return 0; fi
        log_fail "包 $pkg 第 ${round} 轮失败"
        [ "$round" -lt 3 ] && sleep 5
    done
    FAILED_PKGS+=("$pkg"); return 1
}

log_step 3 4 "发布 UI 包"

if [ "$MODE" = "check" ]; then
    log_ok "--check 模式，跳过发布"
elif [ "${#PKGS[@]}" -eq 0 ]; then
    start_ts=$(date +%s)
    WAS_LAUNCHED=$LAUNCHED
    printf "  原生全包发布（超时 %sms）\n" "$PUBLISH_TIMEOUT_MS"
    resp=$(./fairygui-cli.exe --timeout "$PUBLISH_TIMEOUT_MS" publish all 2>&1)
    if [ $? -ne 0 ] || ! printf '%s' "$resp" | grep -qE '"published":(true|[1-9][0-9]*)'; then
        fail "publish all 失败: $(printf '%s' "$resp" | grep -o '"error":"[^"]*"' | head -1 | cut -c1-120)"
    fi
    LAUNCHED=0   # 写盘在途，失败路径保留编辑器防截杀
    wait_write_settle || fail "全包产物未落盘稳定（编辑器已保留，请人工核对）"
    mapfile -t ALL_PKGS < <(./fairygui-cli.exe packages list 2>/dev/null | python -c "import json,sys; [print(p['name']) for p in json.load(sys.stdin)]" | tr -d '\r')
    [ "${#ALL_PKGS[@]}" -gt 0 ] || fail "获取包列表失败"
    for pkg in "${ALL_PKGS[@]}"; do
        check_artifact "$pkg" "$start_ts" "$(pkg_out_dir "$pkg")" || FAILED_PKGS+=("$pkg")
    done
    [ "${#FAILED_PKGS[@]}" -eq 0 ] || fail "产物自检未通过: ${FAILED_PKGS[*]}（编辑器已保留）"
    LAUNCHED=$WAS_LAUNCHED
    log_ok "全部 ${#ALL_PKGS[@]} 个包发布完成且产物自检通过"
else
    for pkg in "${PKGS[@]}"; do publish_one "$pkg"; done
    if [ "${#FAILED_PKGS[@]}" -gt 0 ]; then
        LAUNCHED=0
        fail "发布失败的包: ${FAILED_PKGS[*]}（编辑器已保留）"
    fi
fi

# --- Step 4: 收尾 ---
log_step 4 4 "收尾"
if ! wait_write_settle; then
    if [ "$LAUNCHED" = "1" ]; then
        log_fail "编辑器保持运行 (PID $EDITOR_PID)，待写入完成后请人工核对并关闭"
        LAUNCHED=0
    fi
    exit 1
fi
if [ "$LAUNCHED" = "1" ]; then close_launched_editor; else log_ok "编辑器为复用实例，保持打开"; fi
log_ok "完成"
