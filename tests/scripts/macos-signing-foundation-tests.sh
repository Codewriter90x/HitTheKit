#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH='' cd -- "$SCRIPT_DIR/../.." && pwd)
TEMP_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/hitthekit-signing-tests.XXXXXX")

cleanup() { rm -rf "$TEMP_ROOT"; }
trap cleanup EXIT INT TERM

expect_failure() {
  local label=$1
  shift
  if "$@" >"$TEMP_ROOT/$label.out" 2>"$TEMP_ROOT/$label.err"; then
    echo "Expected failure but command succeeded: $label" >&2
    exit 1
  fi
}

expect_failure sign-missing "$REPOSITORY_ROOT/scripts/sign-macos-app.sh" "$TEMP_ROOT/missing.app"
expect_failure verify-missing "$REPOSITORY_ROOT/scripts/verify-macos-signature.sh" "$TEMP_ROOT/missing.app"
expect_failure notary-missing "$REPOSITORY_ROOT/scripts/notarize-macos-app.sh" "$TEMP_ROOT/missing.app"
expect_failure dmg-missing "$REPOSITORY_ROOT/scripts/package-macos-dmg.sh" \
  "$TEMP_ROOT/missing.app" "$TEMP_ROOT/HitTheKit.dmg"
expect_failure song-pack-missing "$REPOSITORY_ROOT/scripts/package-local-song-pack.sh" \
  "$TEMP_ROOT/missing-songs" "$TEMP_ROOT/Songs.zip" test-song

SONG_SOURCE="$TEMP_ROOT/song-source"
mkdir -p "$SONG_SOURCE/test-song/.analysis"
printf '%s' '{"schemaVersion":1,"id":"test-song","title":"Test","artist":"HitTheKit","audioAvailability":"available","audioFile":"backing.wav","chartAvailability":"available","chartFile":"notes.json"}' \
  > "$SONG_SOURCE/test-song/song.json"
printf '%s' '{"version":1,"offsetSeconds":0,"difficulties":{"easy":[]}}' \
  > "$SONG_SOURCE/test-song/notes.json"
printf 'RIFF\004\000\000\000WAVE' > "$SONG_SOURCE/test-song/backing.wav"
printf 'private analysis' > "$SONG_SOURCE/test-song/.analysis/private.txt"
"$REPOSITORY_ROOT/scripts/package-local-song-pack.sh" \
  "$SONG_SOURCE" "$TEMP_ROOT/Songs.zip" test-song >/dev/null
unzip -Z1 "$TEMP_ROOT/Songs.zip" | grep -q 'HTKSongs/test-song/song.json'
unzip -Z1 "$TEMP_ROOT/Songs.zip" | grep -q 'HTKSongs/test-song/notes.json'
unzip -Z1 "$TEMP_ROOT/Songs.zip" | grep -q 'HTKSongs/test-song/backing.wav'
if unzip -Z1 "$TEMP_ROOT/Songs.zip" | awk '/\.analysis/ { found = 1 } END { exit !found }'; then
  echo "Local song packs must not include analysis files." >&2
  exit 1
fi
if unzip -Z1 "$TEMP_ROOT/Songs.zip" | awk '/(^|\/)\._/ { found = 1 } END { exit !found }'; then
  echo "Local song packs must not include AppleDouble metadata." >&2
  exit 1
fi

ENTITLEMENTS="$REPOSITORY_ROOT/scripts/HitTheKit.entitlements"
plutil -lint "$ENTITLEMENTS" >/dev/null
[ "$(/usr/libexec/PlistBuddy -c 'Print :com.apple.security.cs.allow-jit' "$ENTITLEMENTS")" = true ]
[ "$(plutil -p "$ENTITLEMENTS" | grep -c '=>')" -eq 1 ]

INVALID_APP="$TEMP_ROOT/Invalid.app"
mkdir -p "$INVALID_APP/Contents"
expect_failure sign-invalid "$REPOSITORY_ROOT/scripts/sign-macos-app.sh" "$INVALID_APP"
expect_failure verify-invalid "$REPOSITORY_ROOT/scripts/verify-macos-signature.sh" "$INVALID_APP"

mkdir -p "$TEMP_ROOT/existing-output"
expect_failure existing-output "$REPOSITORY_ROOT/scripts/build-sign-notarize-macos.sh" \
  0.3.0 "$TEMP_ROOT/existing-output"

ICON_APP="$TEMP_ROOT/IconTest.app"
mkdir -p "$ICON_APP/Contents"
plutil -create xml1 "$ICON_APP/Contents/Info.plist"
/usr/libexec/PlistBuddy -c 'Add :CFBundleName string HitTheKit' \
  "$ICON_APP/Contents/Info.plist"
"$REPOSITORY_ROOT/scripts/apply-macos-app-icon.sh" "$ICON_APP" >/dev/null
"$REPOSITORY_ROOT/scripts/apply-macos-app-icon.sh" "$ICON_APP" >/dev/null
[ "$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIconFile' "$ICON_APP/Contents/Info.plist")" = \
  'HitTheKit.icns' ]
cmp "$REPOSITORY_ROOT/branding/app-icon/macos/HitTheKit.icns" \
  "$ICON_APP/Contents/Resources/HitTheKit.icns"
grep -q 'apply-macos-app-icon.sh.*APP_PATH' \
  "$REPOSITORY_ROOT/scripts/package-game-macos-arm64.sh"
grep -q 'apply-macos-app-icon.sh.*APP_PATH' \
  "$REPOSITORY_ROOT/scripts/build-macos-distribution-app.sh"
grep -q 'install-distribution-notices.sh' \
  "$REPOSITORY_ROOT/scripts/build-macos-distribution-app.sh"
grep -q 'Contents/Resources/Legal' \
  "$REPOSITORY_ROOT/scripts/build-macos-distribution-app.sh"
grep -q 'SOURCE_COMMIT=.*HITTHEKIT_SOURCE_COMMIT' \
  "$REPOSITORY_ROOT/scripts/build-macos-distribution-app.sh"
grep -q 'release candidate must be committed and clean' \
  "$REPOSITORY_ROOT/scripts/build-macos-distribution-app.sh"

LEGAL_TEST_ROOT="$TEMP_ROOT/legal"
HITTHEKIT_SOURCE_COMMIT="$(git -C "$REPOSITORY_ROOT" rev-parse HEAD)" \
  "$REPOSITORY_ROOT/scripts/install-distribution-notices.sh" \
  "$LEGAL_TEST_ROOT" 0.5.0 >/dev/null
[ -f "$LEGAL_TEST_ROOT/LICENSE" ]
[ -f "$LEGAL_TEST_ROOT/NOTICE" ]
[ -f "$LEGAL_TEST_ROOT/THIRD_PARTY_NOTICES.md" ]
[ -f "$LEGAL_TEST_ROOT/LICENSING.md" ]
[ -f "$LEGAL_TEST_ROOT/SOURCE-CODE.txt" ]
grep -Fq "$(git -C "$REPOSITORY_ROOT" rev-parse HEAD)" \
  "$LEGAL_TEST_ROOT/SOURCE-CODE.txt"
grep -Fq 'https://github.com/Codewriter90x/HitTheKit/tree/' \
  "$LEGAL_TEST_ROOT/SOURCE-CODE.txt"
grep -q 'ditto.*LEGAL_SOURCE.*STAGE_ROOT/Legal' \
  "$REPOSITORY_ROOT/scripts/package-macos-dmg.sh"

if grep -Eq 'codesign .*--deep.*--sign|codesign .*--sign.*--deep' \
  "$REPOSITORY_ROOT/scripts/package-game-macos-arm64.sh"; then
  echo "Ad-hoc playtest signing must not use --deep as its signing strategy." >&2
  exit 1
fi

grep -q -- '--options runtime --timestamp' "$REPOSITORY_ROOT/scripts/sign-macos-app.sh"
# shellcheck disable=SC2016
grep -q -- '--entitlements "$ENTITLEMENTS_PATH"' "$REPOSITORY_ROOT/scripts/sign-macos-app.sh"
# shellcheck disable=SC2016
grep -q 'gsub(/\[\[:space:\]\]/, "", \$1)' "$REPOSITORY_ROOT/scripts/sign-macos-app.sh"
if grep -Eq 'codesign .*--deep.*--sign|codesign .*--sign.*--deep' "$REPOSITORY_ROOT/scripts/sign-macos-app.sh"; then
  echo "Signing script must not use --deep as its signing strategy." >&2
  exit 1
fi
# The test intentionally searches for the literal shell variable.
# shellcheck disable=SC2016
grep -q -- '--keychain-profile "$PROFILE"' "$REPOSITORY_ROOT/scripts/notarize-macos-app.sh"
grep -q 'STATUS.*Accepted' "$REPOSITORY_ROOT/scripts/notarize-macos-app.sh"
# shellcheck disable=SC2016
grep -q 'notarytool submit "$TEMP_DMG"' "$REPOSITORY_ROOT/scripts/package-macos-dmg.sh"
# shellcheck disable=SC2016
grep -q 'stapler staple "$TEMP_DMG"' "$REPOSITORY_ROOT/scripts/package-macos-dmg.sh"
grep -q 'codesign --force --sign "$IDENTITY" --timestamp "$TEMP_DMG"' \
  "$REPOSITORY_ROOT/scripts/package-macos-dmg.sh"
grep -q 'spctl --assess --type open' "$REPOSITORY_ROOT/scripts/package-macos-dmg.sh"
grep -q 'package-macos-dmg.sh.*FINAL_DMG' "$REPOSITORY_ROOT/scripts/build-sign-notarize-macos.sh"
# shellcheck disable=SC2016
grep -q 'open -n "$APP_PATH" --args -logFile "$LOG_PATH"' \
  "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"
# LaunchServices resolves /tmp to /private/tmp. The smoke test must compare the
# canonical physical path or it will report a false negative after launch.
grep -q 'APP_DIRECTORY=.*pwd -P' "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"
grep -q 'APP_PATH="$APP_DIRECTORY/$(basename -- "$APP_PATH")"' \
  "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"
grep -q 'ps -ww -axo pid=,command=' "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"
grep -q -- '-v log_path="$LOG_PATH"' "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"
if grep -Eq 'print \$1;[[:space:]]*exit' "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"; then
  echo "Runtime smoke must consume ps output instead of triggering SIGPIPE under pipefail." >&2
  exit 1
fi
if grep -q -- '-v log="$LOG_PATH"' "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"; then
  echo "Runtime smoke must not shadow awk's log() built-in." >&2
  exit 1
fi
grep -q "Initialize engine version" "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"
grep -q "Metal RecreateSurface" "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"
grep -q "Obtained \[0-9\].* stack frames" "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"
grep -q "required allow-jit entitlement" "$REPOSITORY_ROOT/scripts/verify-macos-signature.sh"
grep -q "broader executable-code entitlement" "$REPOSITORY_ROOT/scripts/verify-macos-signature.sh"
# shellcheck disable=SC2016
if grep -q '"$BINARY" -logFile' "$REPOSITORY_ROOT/scripts/smoke-macos-app.sh"; then
  echo "Runtime smoke must launch the AppKit bundle through LaunchServices." >&2
  exit 1
fi

echo "macOS signing foundation script tests passed."
