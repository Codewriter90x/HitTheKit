#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH='' cd -- "$SCRIPT_DIR/.." && pwd)
UNITY_PATH=${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity}
VERSION=${1:-}
OUTPUT_ROOT=${2:-}

fail() {
  echo "Windows x64 playtest package: $*" >&2
  exit 1
}

[ -n "$VERSION" ] || fail "usage: $0 <version> [output-directory]"
[[ "$VERSION" =~ ^[0-9]+([.][0-9]+){1,2}$ ]] || \
  fail "version must be numeric, for example 0.5.0"

OUTPUT_ROOT=${OUTPUT_ROOT:-"$REPOSITORY_ROOT/artifacts/game/windows-x64-$VERSION"}
[ ! -e "$OUTPUT_ROOT" ] || fail "output directory already exists: $OUTPUT_ROOT"
[ -x "$UNITY_PATH" ] || fail "Unity 6000.5.6f1 was not found at: $UNITY_PATH"

SOURCE_COMMIT=${HITTHEKIT_SOURCE_COMMIT:-}
if [ -z "$SOURCE_COMMIT" ]; then
  [ -z "$(git -C "$REPOSITORY_ROOT" status --porcelain)" ] ||
    fail "the playtest source must be committed and clean before packaging"
  SOURCE_COMMIT=$(git -C "$REPOSITORY_ROOT" rev-parse HEAD)
fi

UNITY_APP=$(CDPATH='' cd -- "$(dirname -- "$UNITY_PATH")/../.." && pwd)
UNITY_INSTALL_ROOT=$(CDPATH='' cd -- "$UNITY_APP/.." && pwd)
WINDOWS_SUPPORT="$UNITY_INSTALL_ROOT/PlaybackEngines/WindowsStandaloneSupport"
[ -d "$WINDOWS_SUPPORT" ] || \
  fail "Unity Windows Build Support (Mono) is not installed for 6000.5.6f1"

PLAYER_ROOT="$OUTPUT_ROOT/HitTheKit-windows-x64-$VERSION"
EXE_PATH="$PLAYER_ROOT/HitTheKit.exe"
ZIP_PATH="$OUTPUT_ROOT/HitTheKit-windows-x64-$VERSION.zip"
BUILD_LOG="$OUTPUT_ROOT/unity-windows-build.log"

mkdir -p "$OUTPUT_ROOT"
(
  cd "${TMPDIR:-/tmp}"
  "$REPOSITORY_ROOT/scripts/sync-core-to-unity.sh"
)

(
  cd "${TMPDIR:-/tmp}"
  "$UNITY_PATH" \
    -batchmode \
    -quit \
    -projectPath "$REPOSITORY_ROOT/src/HitTheKit.Unity" \
    -buildTarget StandaloneWindows64 \
    -executeMethod HitTheKit.Unity.EditorTools.WindowsPlaytestBuild.BuildX64 \
    -customBuildPath "$EXE_PATH" \
    -logFile "$BUILD_LOG"
)

[ -f "$EXE_PATH" ] || fail "Unity did not produce HitTheKit.exe"
[ -d "$PLAYER_ROOT/HitTheKit_Data" ] || fail "Unity player data is missing"
[ -f "$PLAYER_ROOT/UnityPlayer.dll" ] || fail "UnityPlayer.dll is missing"

UNEXPECTED_PLUGIN=$(find "$PLAYER_ROOT" -iname '*HitTheKitCoreMidi*' -print -quit)
[ -z "$UNEXPECTED_PLUGIN" ] || \
  fail "macOS CoreMIDI plug-in leaked into the Windows package: $UNEXPECTED_PLUGIN"

HITTHEKIT_SOURCE_COMMIT="$SOURCE_COMMIT" \
  "$REPOSITORY_ROOT/scripts/install-distribution-notices.sh" \
  "$PLAYER_ROOT/Legal" "$VERSION"

printf '%s\n' \
  'HitTheKit Windows x64 playtest' \
  '' \
  '1. Extract the complete ZIP before launching the game.' \
  '2. Run HitTheKit.exe from the extracted folder.' \
  '3. Use the keyboard controls shown during first-run setup.' \
  '' \
  'Electronic-drum MIDI is not implemented on Windows yet.' \
  'This playtest package is unsigned and is not approved for public release.' \
  'Verify the published SHA-256 before running any future release package.' \
  'License, notices, and exact source information are in Legal/.' \
  > "$PLAYER_ROOT/README-FIRST.txt"

ditto --norsrc --noextattr -c -k --keepParent "$PLAYER_ROOT" "$ZIP_PATH"

echo "HITTHEKIT_WINDOWS_PLAYTEST_PACKAGE_READY"
echo "player=$PLAYER_ROOT"
echo "package=$ZIP_PATH"
ls -lh "$ZIP_PATH"
shasum -a 256 "$ZIP_PATH"
