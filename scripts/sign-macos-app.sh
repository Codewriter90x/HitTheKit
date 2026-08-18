#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
APP_PATH=${1:-}
ENTITLEMENTS_PATH="$SCRIPT_DIR/HitTheKit.entitlements"

fail() {
  echo "macOS signing: $*" >&2
  exit 1
}

resolve_identity() {
  local identities candidate count
  identities=$(security find-identity -v -p codesigning)
  if [ -n "${HITTHEKIT_CODESIGN_IDENTITY:-}" ]; then
    candidate=$HITTHEKIT_CODESIGN_IDENTITY
    printf '%s\n' "$identities" | grep -F -- "$candidate" >/dev/null ||
      fail "HITTHEKIT_CODESIGN_IDENTITY is not a valid codesigning identity"
    printf '%s\n' "$candidate"
    return
  fi

  count=$(printf '%s\n' "$identities" | grep -c '"Developer ID Application:' || true)
  [ "$count" -eq 1 ] || fail "expected exactly one valid Developer ID Application identity; found $count"
  printf '%s\n' "$identities" | awk -F'"' '/"Developer ID Application:/{gsub(/^[[:space:]]*[0-9]+\)[[:space:]]*/, "", $1); gsub(/[[:space:]]/, "", $1); print $1; exit}'
}

[ -n "$APP_PATH" ] || fail "usage: $0 <HitTheKit.app>"
[[ "$APP_PATH" == *.app ]] || fail "input must be an .app bundle"
[ -d "$APP_PATH/Contents" ] || fail "app bundle does not exist or is invalid: $APP_PATH"
command -v codesign >/dev/null 2>&1 || fail "codesign was not found"
command -v security >/dev/null 2>&1 || fail "security was not found"
command -v ditto >/dev/null 2>&1 || fail "ditto was not found"
[ -f "$ENTITLEMENTS_PATH" ] || fail "entitlements file is missing: $ENTITLEMENTS_PATH"
plutil -lint "$ENTITLEMENTS_PATH" >/dev/null || fail "entitlements file is invalid"

APP_PATH=$(cd "$(dirname -- "$APP_PATH")" && pwd)/$(basename -- "$APP_PATH")
PLIST="$APP_PATH/Contents/Info.plist"
[ -f "$PLIST" ] || fail "Info.plist is missing"
EXECUTABLE=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$PLIST" 2>/dev/null) ||
  fail "CFBundleExecutable is missing"
MAIN_BINARY="$APP_PATH/Contents/MacOS/$EXECUTABLE"
[ -x "$MAIN_BINARY" ] || fail "main executable is missing or not executable"

IDENTITY=$(resolve_identity)
PARENT=$(dirname -- "$APP_PATH")
WORK_ROOT=$(mktemp -d "$PARENT/.hitthekit-signing.XXXXXX")
STAGED_APP="$WORK_ROOT/$(basename -- "$APP_PATH")"
BACKUP_APP="$WORK_ROOT/original.app"
committed=0

cleanup() {
  if [ "$committed" -eq 0 ] && [ -d "$BACKUP_APP" ] && [ ! -e "$APP_PATH" ]; then
    mv "$BACKUP_APP" "$APP_PATH"
  fi
  rm -rf "$WORK_ROOT"
}
trap cleanup EXIT INT TERM

ditto "$APP_PATH" "$STAGED_APP"
STAGED_MAIN="$STAGED_APP/Contents/MacOS/$EXECUTABLE"

UNSUPPORTED_BUNDLE=$(find "$STAGED_APP/Contents" -mindepth 1 -type d \
  \( -name '*.app' -o -name '*.framework' -o -name '*.xpc' \) -print -quit)
[ -z "$UNSUPPORTED_BUNDLE" ] ||
  fail "unsupported nested code bundle requires an explicit signing rule: $UNSUPPORTED_BUNDLE"

while IFS= read -r -d '' candidate; do
  [ "$candidate" != "$STAGED_MAIN" ] || continue
  if file -b "$candidate" | grep -q 'Mach-O'; then
    codesign --force --sign "$IDENTITY" --options runtime --timestamp "$candidate"
  fi
done < <(find "$STAGED_APP/Contents" -type f -print0)

codesign --force --sign "$IDENTITY" --options runtime --timestamp \
  --entitlements "$ENTITLEMENTS_PATH" "$STAGED_APP"
codesign --verify --deep --strict --verbose=4 "$STAGED_APP"

mv "$APP_PATH" "$BACKUP_APP"
mv "$STAGED_APP" "$APP_PATH"
committed=1
rm -rf "$BACKUP_APP"

echo "HITTHEKIT_MACOS_SIGNING_SUCCEEDED"
echo "app=$APP_PATH"
echo "identity=$IDENTITY"
