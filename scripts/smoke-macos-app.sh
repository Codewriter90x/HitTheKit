#!/bin/bash
set -euo pipefail

APP_PATH=${1:-}
SECONDS_TO_RUN=${HITTHEKIT_SMOKE_SECONDS:-10}

fail() {
  echo "macOS runtime smoke: $*" >&2
  exit 1
}

[ -n "$APP_PATH" ] || fail "usage: $0 <HitTheKit.app>"
[ -d "$APP_PATH/Contents" ] || fail "app bundle does not exist or is invalid: $APP_PATH"

# LaunchServices reports the executable using the physical filesystem path.
# Canonicalize aliases such as /tmp -> /private/tmp before matching the process
# command line, otherwise a successfully launched app can look absent.
APP_DIRECTORY=$(CDPATH='' cd -- "$(dirname -- "$APP_PATH")" && pwd -P)
APP_PATH="$APP_DIRECTORY/$(basename -- "$APP_PATH")"
PLIST="$APP_PATH/Contents/Info.plist"
EXECUTABLE=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$PLIST" 2>/dev/null) ||
  fail "CFBundleExecutable is missing"
BINARY="$APP_PATH/Contents/MacOS/$EXECUTABLE"
[ -x "$BINARY" ] || fail "main executable is missing or not executable"
[[ "$SECONDS_TO_RUN" =~ ^[1-9][0-9]*$ ]] || fail "HITTHEKIT_SMOKE_SECONDS must be a positive integer"

LOG_ROOT=${HITTHEKIT_SMOKE_LOG_ROOT:-"$(dirname -- "$APP_PATH")/runtime-smoke"}
mkdir -p "$LOG_ROOT"
LOG_PATH="$LOG_ROOT/player.log"
rm -f "$LOG_PATH"

command -v open >/dev/null 2>&1 || fail "macOS open command was not found"
command -v ps >/dev/null 2>&1 || fail "ps command was not found"

find_app_pid() {
  # -ww prevents BSD ps from truncating the long Unity executable and log
  # paths before awk can compare them. Keep consuming ps output after the first
  # match: with pipefail, exiting awk early can make ps receive SIGPIPE and turn
  # a successful lookup into exit code 141.
  ps -ww -axo pid=,command= | awk -v binary="$BINARY" -v log_path="$LOG_PATH" '
    !found && index($0, binary) != 0 && index($0, "-logFile " log_path) != 0 {
      print $1
      found = 1
    }
  '
}

EXISTING_PID=$(find_app_pid)
[ -z "$EXISTING_PID" ] ||
  fail "a previous smoke instance is still running with PID $EXISTING_PID"

# A Unity macOS player is an AppKit application. LaunchServices is the
# representative Finder/Gatekeeper path; executing Contents/MacOS directly can
# abort inside RegisterApplication on current macOS releases.
open -n "$APP_PATH" --args -logFile "$LOG_PATH"
APP_PID=''
for _ in $(seq 1 50); do
  APP_PID=$(find_app_pid)
  [ -z "$APP_PID" ] || break
  sleep 0.2
done
[ -n "$APP_PID" ] || fail "LaunchServices did not start the app"

cleanup() {
  if [ -n "$APP_PID" ] && kill -0 "$APP_PID" 2>/dev/null; then
    kill -TERM "$APP_PID" 2>/dev/null || true
    for _ in $(seq 1 25); do
      kill -0 "$APP_PID" 2>/dev/null || return 0
      sleep 0.2
    done
    kill -KILL "$APP_PID" 2>/dev/null || true
  fi
}
trap cleanup EXIT INT TERM

sleep "$SECONDS_TO_RUN"
kill -0 "$APP_PID" 2>/dev/null || fail "app exited before the smoke interval completed"

[ -f "$LOG_PATH" ] || fail "Unity did not create the player log: $LOG_PATH"
if grep -Eiq 'DllNotFoundException|Library not loaded|Code Signature Invalid|SIGABRT|segmentation fault|crash!!!' "$LOG_PATH"; then
  fail "player log contains a native loading, signing, or crash diagnostic: $LOG_PATH"
fi
if grep -Eiq 'Obtained [0-9]+ stack frames' "$LOG_PATH"; then
  fail "player log contains an early native stack-trace diagnostic: $LOG_PATH"
fi
grep -q '^Initialize engine version:' "$LOG_PATH" ||
  fail "Unity did not finish initializing the engine: $LOG_PATH"
grep -q '^Metal RecreateSurface' "$LOG_PATH" ||
  fail "Unity did not create the macOS render surface: $LOG_PATH"

cleanup
trap - EXIT INT TERM
echo "HITTHEKIT_MACOS_RUNTIME_SMOKE_SUCCEEDED"
echo "log=$LOG_PATH"
