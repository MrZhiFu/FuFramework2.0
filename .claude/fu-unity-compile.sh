#!/usr/bin/env bash
# FuFramework compile verification via unity-cli.
# Exit codes:
#   0 = compile finished with no errors
#   1 = compile errors found
#   2 = Unity Editor not connected (ping failed)
#   3 = compilation did not finish within the wait window
#   4 = could not read compilation state from unity-cli
set -u

# 禁止自更新：CLI 是版本库文件，自更新会覆盖它并污染工作区（还会阻塞退出上百秒）
export UNITY_CLI_NO_AUTO_UPDATE=1
export UNITY_CLI_HOST=127.0.0.1

# CLI 随项目分发（不在 PATH 中）
UNITY_CLI="$(git rev-parse --show-toplevel)/Tools/UnityCli/unity-cli.exe"

UNITY_CLI_TIMEOUT_MS=15000
POLL_INTERVAL=2
WAIT_SECONDS=120

if ! "$UNITY_CLI" system ping --timeout-ms "$UNITY_CLI_TIMEOUT_MS" >/dev/null 2>&1; then
  echo "[fu-unity-compile] ERROR: Unity Editor not connected (unity-cli ping failed)."
  echo "[fu-unity-compile] Open the Unity project first, then retry."
  exit 2
fi

# A stopped Play mode is required: asset database operations are blocked during Play.
if "$UNITY_CLI" tool call get_editor_state --output json >/dev/null 2>&1; then
  if "$UNITY_CLI" tool call get_editor_state --output json 2>/dev/null | grep -q '"isPlaying"[[:space:]]*:[[:space:]]*true'; then
    echo "[fu-unity-compile] NOTE: stopping Play mode first."
    "$UNITY_CLI" tool call stop_game >/dev/null 2>&1
  fi
fi

"$UNITY_CLI" tool call clear_console >/dev/null 2>&1
if ! "$UNITY_CLI" tool call refresh_assets >/dev/null 2>&1; then
  echo "[fu-unity-compile] WARN: refresh_assets failed; compilation state may be stale."
fi

state=""
compiling="unknown"
elapsed=0
while [ "$elapsed" -lt "$WAIT_SECONDS" ]; do
  state=$("$UNITY_CLI" tool call get_compilation_state --output json --timeout-ms "$UNITY_CLI_TIMEOUT_MS" 2>/dev/null)
  compiling=$(printf '%s' "$state" | grep -o '"isCompiling"[[:space:]]*:[[:space:]]*[a-z]*' | grep -o 'true\|false' | head -1)
  if [ "$compiling" = "false" ]; then
    break
  fi
  sleep "$POLL_INTERVAL"
  elapsed=$((elapsed + POLL_INTERVAL))
done

# Do not report PASS unless compilation actually finished.
if [ "$compiling" != "false" ]; then
  echo "[fu-unity-compile] ERROR: compilation did not finish within ${WAIT_SECONDS}s."
  echo "[fu-unity-compile] Last state:"
  printf '%s\n' "$state"
  exit 3
fi

errors=$(printf '%s' "$state" | grep -o '"errorCount"[[:space:]]*:[[:space:]]*[0-9]*' | grep -o '[0-9]\+' | head -1)

if [ -z "${errors:-}" ]; then
  echo "[fu-unity-compile] ERROR: could not read compilation state from unity-cli."
  printf '%s\n' "$state"
  exit 4
fi

if [ "$errors" = "0" ]; then
  echo "[fu-unity-compile] PASS: no compile errors."
  exit 0
fi

echo "[fu-unity-compile] FAIL: $errors compile error(s):"
params_file="${TMPDIR:-/tmp}/fu-console-params.$$.json"
printf '{"logTypes": ["Error", "Exception"], "count": 20}\n' > "$params_file"
"$UNITY_CLI" tool call read_console --params-file "$params_file" 2>&1
rm -f "$params_file"
exit 1

