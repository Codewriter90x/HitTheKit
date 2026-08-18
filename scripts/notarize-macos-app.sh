#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
APP_PATH=${1:-}
PROFILE=${HITTHEKIT_NOTARY_PROFILE:-HitTheKit-Notary}

fail() {
  echo "macOS notarization: $*" >&2
  exit 1
}

[ -n "$APP_PATH" ] || fail "usage: $0 <HitTheKit.app>"
[[ "$APP_PATH" == *.app ]] || fail "input must be an .app bundle"
[ -d "$APP_PATH/Contents" ] || fail "app bundle does not exist or is invalid: $APP_PATH"
command -v xcrun >/dev/null 2>&1 || fail "xcrun was not found"
command -v ditto >/dev/null 2>&1 || fail "ditto was not found"

"$SCRIPT_DIR/verify-macos-signature.sh" --pre-notarization "$APP_PATH"
xcrun notarytool history --keychain-profile "$PROFILE" --output-format json >/dev/null ||
  fail "notarytool Keychain profile '$PROFILE' is unavailable"

APP_PATH=$(cd "$(dirname -- "$APP_PATH")" && pwd)/$(basename -- "$APP_PATH")
ARTIFACT_ROOT=${HITTHEKIT_NOTARIZATION_ARTIFACTS:-"$(dirname -- "$APP_PATH")/notarization"}
mkdir -p "$ARTIFACT_ROOT"
WORK_ROOT=$(mktemp -d "$ARTIFACT_ROOT/.submission.XXXXXX")
ZIP_PATH="$WORK_ROOT/HitTheKit-notarization.zip"
RESULT_PATH="$WORK_ROOT/submission.json"

cleanup() {
  rm -rf "$WORK_ROOT"
}
trap cleanup EXIT INT TERM

ditto -c -k --keepParent "$APP_PATH" "$ZIP_PATH"
if ! xcrun notarytool submit "$ZIP_PATH" \
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

if [ "$STATUS" != "Accepted" ]; then
  LOG_PATH="$ARTIFACT_ROOT/notary-$SUBMISSION_ID.json"
  xcrun notarytool log "$SUBMISSION_ID" --keychain-profile "$PROFILE" "$LOG_PATH" || true
  fail "notarization finished with status '$STATUS'; log saved to $LOG_PATH"
fi

xcrun stapler staple "$APP_PATH"
xcrun stapler validate "$APP_PATH"
"$SCRIPT_DIR/verify-macos-signature.sh" "$APP_PATH"

echo "HITTHEKIT_MACOS_NOTARIZATION_ACCEPTED"
echo "submissionId=$SUBMISSION_ID"
echo "status=$STATUS"
