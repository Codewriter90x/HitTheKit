#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH='' cd -- "$SCRIPT_DIR/.." && pwd)
UNITY_PATH=${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity}
VERSION=${1:-}
APP_PATH=${2:-}
BUILD_NUMBER=${HITTHEKIT_BUILD_NUMBER:-$VERSION}

fail() {
  echo "macOS distribution build: $*" >&2
  exit 1
}

[ -n "$VERSION" ] || fail "usage: $0 <version> <output.app>"
[ -n "$APP_PATH" ] || fail "usage: $0 <version> <output.app>"
[[ "$VERSION" =~ ^[0-9]+([.][0-9]+){1,2}$ ]] || fail "version must be numeric, for example 0.3.0"
[[ "$BUILD_NUMBER" =~ ^[0-9]+([.][0-9]+){0,2}$ ]] || fail "HITTHEKIT_BUILD_NUMBER must contain one to three numeric components"
[[ "$APP_PATH" == *.app ]] || fail "output must be an .app bundle"
[ ! -e "$APP_PATH" ] || fail "output already exists: $APP_PATH"
[ -x "$UNITY_PATH" ] || fail "Unity 6000.5.6f1 was not found at: $UNITY_PATH"

SOURCE_COMMIT=${HITTHEKIT_SOURCE_COMMIT:-}
if [ -z "$SOURCE_COMMIT" ]; then
  [ -z "$(git -C "$REPOSITORY_ROOT" status --porcelain)" ] ||
    fail "the release candidate must be committed and clean before building"
  SOURCE_COMMIT=$(git -C "$REPOSITORY_ROOT" rev-parse HEAD)
fi

OUTPUT_ROOT=$(dirname -- "$APP_PATH")
mkdir -p "$OUTPUT_ROOT"
APP_PATH=$(cd "$OUTPUT_ROOT" && pwd)/$(basename -- "$APP_PATH")
OUTPUT_ROOT=$(dirname -- "$APP_PATH")
BUILD_LOG="$OUTPUT_ROOT/unity-distribution-build.log"

"$REPOSITORY_ROOT/scripts/build-coremidi-plugin-macos-arm64.sh"
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
    -buildTarget StandaloneOSX \
    -executeMethod HitTheKit.Unity.EditorTools.MacOSPlaytestBuild.BuildArm64 \
    -customBuildPath "$APP_PATH" \
    -logFile "$BUILD_LOG"
)

[ -d "$APP_PATH/Contents" ] || fail "Unity did not produce an app bundle"
PLUGIN_SOURCE="$REPOSITORY_ROOT/src/HitTheKit.Unity/Assets/Plugins/macOS/HitTheKitCoreMidi.dylib"
PLUGIN_PATH="$APP_PATH/Contents/PlugIns/HitTheKitCoreMidi.dylib"
mkdir -p "$(dirname -- "$PLUGIN_PATH")"
cp "$PLUGIN_SOURCE" "$PLUGIN_PATH"
chmod 755 "$PLUGIN_PATH"

PLIST="$APP_PATH/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleName HitTheKit" "$PLIST"
/usr/libexec/PlistBuddy -c "Set :CFBundleIdentifier com.codewriter90x.hitthekit" "$PLIST"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $VERSION" "$PLIST"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $BUILD_NUMBER" "$PLIST"
"$REPOSITORY_ROOT/scripts/apply-macos-app-icon.sh" "$APP_PATH"
HITTHEKIT_SOURCE_COMMIT="$SOURCE_COMMIT" \
  "$REPOSITORY_ROOT/scripts/install-distribution-notices.sh" \
  "$APP_PATH/Contents/Resources/Legal" "$VERSION"
EXECUTABLE=$(/usr/libexec/PlistBuddy -c "Print :CFBundleExecutable" "$PLIST")
MAIN_BINARY="$APP_PATH/Contents/MacOS/$EXECUTABLE"

[ -x "$MAIN_BINARY" ] || fail "main executable is missing: $MAIN_BINARY"
[ -f "$PLUGIN_PATH" ] || fail "CoreMIDI plug-in is missing: $PLUGIN_PATH"
file "$MAIN_BINARY" | grep -q 'arm64' || fail "main executable has no arm64 slice"
file "$PLUGIN_PATH" | grep -q 'arm64' || fail "CoreMIDI plug-in has no arm64 slice"

echo "HITTHEKIT_MACOS_DISTRIBUTION_BUILD_READY"
echo "app=$APP_PATH"
echo "bundleIdentifier=com.codewriter90x.hitthekit"
echo "version=$VERSION"
echo "build=$BUILD_NUMBER"
