#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
APP_PATH=${1:-}
OUTPUT_DMG=${2:-}
PROFILE=${HITTHEKIT_NOTARY_PROFILE:-HitTheKit-Notary}

fail() {
  echo "macOS DMG packaging: $*" >&2
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

[ -n "$APP_PATH" ] && [ -n "$OUTPUT_DMG" ] ||
  fail "usage: $0 <notarized-HitTheKit.app> <output.dmg>"
[[ "$APP_PATH" == *.app ]] || fail "input must be an .app bundle"
[ -d "$APP_PATH/Contents" ] || fail "app bundle does not exist or is invalid: $APP_PATH"
[[ "$OUTPUT_DMG" == *.dmg ]] || fail "output must use the .dmg extension"
[ ! -e "$OUTPUT_DMG" ] || fail "output already exists: $OUTPUT_DMG"

command -v ditto >/dev/null 2>&1 || fail "ditto was not found"
command -v hdiutil >/dev/null 2>&1 || fail "hdiutil was not found"
command -v xcrun >/dev/null 2>&1 || fail "xcrun was not found"
command -v spctl >/dev/null 2>&1 || fail "spctl was not found"
command -v codesign >/dev/null 2>&1 || fail "codesign was not found"
command -v security >/dev/null 2>&1 || fail "security was not found"

"$SCRIPT_DIR/verify-macos-signature.sh" "$APP_PATH"
xcrun notarytool history --keychain-profile "$PROFILE" --output-format json >/dev/null ||
  fail "notarytool Keychain profile '$PROFILE' is unavailable"

OUTPUT_DIRECTORY=$(CDPATH='' cd -- "$(dirname -- "$OUTPUT_DMG")" && pwd)
OUTPUT_DMG="$OUTPUT_DIRECTORY/$(basename -- "$OUTPUT_DMG")"
ARTIFACT_ROOT=${HITTHEKIT_DMG_NOTARIZATION_ARTIFACTS:-"$OUTPUT_DIRECTORY/notarization-dmg"}
mkdir -p "$ARTIFACT_ROOT"
WORK_ROOT=$(mktemp -d "$OUTPUT_DIRECTORY/.hitthekit-dmg.XXXXXX")
STAGE_ROOT="$WORK_ROOT/stage"
TEMP_DMG="$WORK_ROOT/HitTheKit.dmg"
RESULT_PATH="$WORK_ROOT/submission.json"

cleanup() { rm -rf "$WORK_ROOT"; }
trap cleanup EXIT INT TERM

mkdir -p "$STAGE_ROOT"
ditto "$APP_PATH" "$STAGE_ROOT/HitTheKit.app"
ln -s /Applications "$STAGE_ROOT/Applications"
hdiutil create -quiet -volname "HitTheKit" -srcfolder "$STAGE_ROOT" -format UDZO "$TEMP_DMG"
hdiutil verify "$TEMP_DMG" >/dev/null
IDENTITY=$(resolve_identity)
codesign --force --sign "$IDENTITY" --timestamp "$TEMP_DMG"
codesign --verify --strict --verbose=4 "$TEMP_DMG"

if ! xcrun notarytool submit "$TEMP_DMG" \
  --keychain-profile "$PROFILE" \
  --wait \
  --output-format json > "$RESULT_PATH"; then
  cp "$RESULT_PATH" "$ARTIFACT_ROOT/submission-failed.json" 2>/dev/null || true
  fail "notarytool submit failed; diagnostic saved under $ARTIFACT_ROOT"
fi

SUBMISSION_ID=$(plutil -extract id raw -o - "$RESULT_PATH" 2>/dev/null) ||
  fail "notarytool response did not include a submission ID"
STATUS=$(plutil -extract status raw -o - "$RESULT_PATH" 2>/dev/null) ||
  fail "notarytool response did not include a final status"
cp "$RESULT_PATH" "$ARTIFACT_ROOT/submission-$SUBMISSION_ID.json"
[ "$STATUS" = "Accepted" ] || fail "DMG notarization finished with status '$STATUS'"

xcrun stapler staple "$TEMP_DMG"
xcrun stapler validate "$TEMP_DMG"
spctl --assess --type open --context context:primary-signature --verbose=4 "$TEMP_DMG"
mv "$TEMP_DMG" "$OUTPUT_DMG"

echo "HITTHEKIT_MACOS_DMG_READY"
echo "dmg=$OUTPUT_DMG"
echo "identity=$IDENTITY"
echo "submissionId=$SUBMISSION_ID"
ls -lh "$OUTPUT_DMG"
shasum -a 256 "$OUTPUT_DMG"
