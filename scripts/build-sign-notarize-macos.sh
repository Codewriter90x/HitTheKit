#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH='' cd -- "$SCRIPT_DIR/.." && pwd)
VERSION=${1:-}
OUTPUT_ROOT=${2:-}

fail() {
  echo "macOS distribution pipeline: $*" >&2
  exit 1
}

[ -n "$VERSION" ] || fail "usage: $0 <version> [output-directory]"
OUTPUT_ROOT=${OUTPUT_ROOT:-"$REPOSITORY_ROOT/artifacts/macos-distribution/$VERSION"}
[ ! -e "$OUTPUT_ROOT" ] || fail "output directory already exists: $OUTPUT_ROOT"
mkdir -p "$OUTPUT_ROOT"

APP_PATH="$OUTPUT_ROOT/HitTheKit.app"
FINAL_ZIP="$OUTPUT_ROOT/HitTheKit-macos-arm64-$VERSION.zip"
FINAL_DMG="$OUTPUT_ROOT/HitTheKit-macos-arm64-$VERSION.dmg"

"$SCRIPT_DIR/build-macos-distribution-app.sh" "$VERSION" "$APP_PATH"
"$SCRIPT_DIR/sign-macos-app.sh" "$APP_PATH"
"$SCRIPT_DIR/verify-macos-signature.sh" --pre-notarization "$APP_PATH"
"$SCRIPT_DIR/smoke-macos-app.sh" "$APP_PATH"
"$SCRIPT_DIR/notarize-macos-app.sh" "$APP_PATH"
"$SCRIPT_DIR/package-macos-dmg.sh" "$APP_PATH" "$FINAL_DMG"

ditto -c -k --keepParent "$APP_PATH" "$FINAL_ZIP"
EXTRACT_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/hitthekit-distribution-check.XXXXXX")
cleanup() { rm -rf "$EXTRACT_ROOT"; }
trap cleanup EXIT INT TERM
ditto -x -k "$FINAL_ZIP" "$EXTRACT_ROOT"
"$SCRIPT_DIR/verify-macos-signature.sh" "$EXTRACT_ROOT/HitTheKit.app"
cleanup
trap - EXIT INT TERM

echo "HITTHEKIT_MACOS_DISTRIBUTION_READY"
echo "app=$APP_PATH"
echo "zip=$FINAL_ZIP"
echo "dmg=$FINAL_DMG"
ls -lh "$FINAL_ZIP"
shasum -a 256 "$FINAL_ZIP"
