#!/usr/bin/env bash
# FuFramework Unity 编译验证脚本。
#
# 用途：改完 .cs 后跑一次，确认 Unity 侧编译 0 错误。
# 内部流程：连上编辑器 -> 停 Play -> 清控制台 -> 刷新资源 -> 轮询编译状态 -> 判定结果。
#
# 退出码：
#   0 = 编译完成且无错误
#   1 = 有编译错误（会打印错误列表）
#   2 = Unity 编辑器未连接
#   3 = 等待超时，编译未结束（不能当作通过）
#   4 = 读不到编译状态（unity-cli 返回异常）
set -u

# 禁止自更新：CLI 是版本库文件，自更新会覆盖它并污染工作区（还会阻塞退出上百秒）
export UNITY_CLI_NO_AUTO_UPDATE=1
export UNITY_CLI_HOST=127.0.0.1

# CLI 随项目分发（不在 PATH 中），用仓库根解析绝对路径，保证任意 cwd 都能找到
UNITY_CLI="$(git rev-parse --show-toplevel)/Tools/UnityCli/unity-cli.exe"

UNITY_CLI_TIMEOUT_MS=15000   # 单条 unity-cli 命令的超时
POLL_INTERVAL=2              # 轮询编译状态的间隔（秒）
WAIT_SECONDS=120             # 等待编译完成的上限（秒）

# ---------- 步骤 1：确认编辑器在线 ----------
# 连不上就没法继续，直接退出 2，避免后面的命令逐个超时拖时间
if ! "$UNITY_CLI" system ping --timeout-ms "$UNITY_CLI_TIMEOUT_MS" >/dev/null 2>&1; then
  echo "[fu-unity-compile] ERROR: Unity Editor not connected (unity-cli ping failed)."
  echo "[fu-unity-compile] Open the Unity project first, then retry."
  exit 2
fi

# ---------- 步骤 2：停掉 Play 模式 ----------
# Play 模式下资源库操作会被 bridge 阻断（PLAY_MODE_BLOCKED），不停 Play 刷新必然失败
if "$UNITY_CLI" tool call get_editor_state --output json >/dev/null 2>&1; then
  if "$UNITY_CLI" tool call get_editor_state --output json 2>/dev/null | grep -q '"isPlaying"[[:space:]]*:[[:space:]]*true'; then
    echo "[fu-unity-compile] NOTE: stopping Play mode first."
    "$UNITY_CLI" tool call stop_game >/dev/null 2>&1
  fi
fi

# ---------- 步骤 3：清控制台 + 刷新资源 ----------
# 必须先清：read_console 是消费式读取，上一次的残留错误会干扰本次判定
"$UNITY_CLI" tool call clear_console >/dev/null 2>&1
# refresh_assets 触发重新导入与编译；失败只告警不退出，因为可能编译状态仍可读
if ! "$UNITY_CLI" tool call refresh_assets >/dev/null 2>&1; then
  echo "[fu-unity-compile] WARN: refresh_assets failed; compilation state may be stale."
fi

# ---------- 步骤 4：轮询等待编译结束 ----------
# 编译是异步的：刷新后要等 isCompiling 变 false 才能读错误数，否则读到的是上一轮状态
state=""
compiling="unknown"
elapsed=0
while [ "$elapsed" -lt "$WAIT_SECONDS" ]; do
  state=$("$UNITY_CLI" tool call get_compilation_state --output json --timeout-ms "$UNITY_CLI_TIMEOUT_MS" 2>/dev/null)
  # JSON 字段用 grep 提取，不依赖 jq（Git Bash 环境不保证有 jq）
  compiling=$(printf '%s' "$state" | grep -o '"isCompiling"[[:space:]]*:[[:space:]]*[a-z]*' | grep -o 'true\|false' | head -1)
  if [ "$compiling" = "false" ]; then
    break
  fi
  sleep "$POLL_INTERVAL"
  elapsed=$((elapsed + POLL_INTERVAL))
done

# ---------- 步骤 5：编译没结束就不能判通过 ----------
# 这一条是防"假 PASS"的关键：超时退出循环时 isCompiling 可能仍是 true，
# 若此时直接读 errorCount 会得到 0，从而误报编译通过
if [ "$compiling" != "false" ]; then
  echo "[fu-unity-compile] ERROR: compilation did not finish within ${WAIT_SECONDS}s."
  echo "[fu-unity-compile] Last state:"
  printf '%s\n' "$state"
  exit 3
fi

# ---------- 步骤 6：读错误数并判定 ----------
errors=$(printf '%s' "$state" | grep -o '"errorCount"[[:space:]]*:[[:space:]]*[0-9]*' | grep -o '[0-9]\+' | head -1)

# 取不到 errorCount 说明返回体异常，与"0 错误"是两回事，用独立退出码区分
if [ -z "${errors:-}" ]; then
  echo "[fu-unity-compile] ERROR: could not read compilation state from unity-cli."
  printf '%s\n' "$state"
  exit 4
fi

if [ "$errors" = "0" ]; then
  echo "[fu-unity-compile] PASS: no compile errors."
  exit 0
fi

# ---------- 步骤 7：有错时列出错误详情 ----------
# 只取 Error/Exception 两类，限制 20 条避免刷屏；参数经临时文件传入（schema 要求严格）
echo "[fu-unity-compile] FAIL: $errors compile error(s):"
params_file="${TMPDIR:-/tmp}/fu-console-params.$$.json"
printf '{"logTypes": ["Error", "Exception"], "count": 20}\n' > "$params_file"
"$UNITY_CLI" tool call read_console --params-file "$params_file" 2>&1
rm -f "$params_file"
exit 1
