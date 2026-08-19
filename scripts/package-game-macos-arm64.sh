#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH='' cd -- "$SCRIPT_DIR/.." && pwd)
UNITY_PATH=${UNITY_PATH:-/Applications/Unity/Hub/Editor/6000.5.6f1/Unity.app/Contents/MacOS/Unity}
BUILD_VERSION=${1:-0.1.0}
OUTPUT_ROOT=${2:-"$REPOSITORY_ROOT/artifacts/game/macos-arm64-$BUILD_VERSION"}
APP_PATH="$OUTPUT_ROOT/HitTheKit.app"
PACKAGE_ROOT="$OUTPUT_ROOT/HitTheKit-macos-arm64-$BUILD_VERSION"
ZIP_PATH="$OUTPUT_ROOT/HitTheKit-macos-arm64-$BUILD_VERSION.zip"
BUILD_LOG="$OUTPUT_ROOT/unity-build.log"

if [ ! -x "$UNITY_PATH" ]; then
  echo "Unity 6000.5.6f1 was not found at: $UNITY_PATH" >&2
  exit 2
fi

if [ -e "$OUTPUT_ROOT" ]; then
  echo "Output directory already exists: $OUTPUT_ROOT" >&2
  exit 3
fi

SOURCE_COMMIT=${HITTHEKIT_SOURCE_COMMIT:-}
if [ -z "$SOURCE_COMMIT" ]; then
  [ -z "$(git -C "$REPOSITORY_ROOT" status --porcelain)" ] || {
    echo "The playtest source must be committed and clean before packaging." >&2
    exit 4
  }
  SOURCE_COMMIT=$(git -C "$REPOSITORY_ROOT" rev-parse HEAD)
fi

mkdir -p "$OUTPUT_ROOT"

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

COREMIDI_PLUGIN="$APP_PATH/Contents/Plugins/HitTheKitCoreMidi.dylib"
SOURCE_COREMIDI_PLUGIN="$REPOSITORY_ROOT/src/HitTheKit.Unity/Assets/Plugins/macOS/HitTheKitCoreMidi.dylib"

mkdir -p "$APP_PATH/Contents/Plugins"
cp "$SOURCE_COREMIDI_PLUGIN" "$COREMIDI_PLUGIN"
/usr/libexec/PlistBuddy -c "Set :CFBundleName HitTheKit" "$APP_PATH/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleIdentifier com.codewriter90x.hitthekit" "$APP_PATH/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleShortVersionString $BUILD_VERSION" "$APP_PATH/Contents/Info.plist"
/usr/libexec/PlistBuddy -c "Set :CFBundleVersion $BUILD_VERSION" "$APP_PATH/Contents/Info.plist"
"$REPOSITORY_ROOT/scripts/apply-macos-app-icon.sh" "$APP_PATH"
HITTHEKIT_SOURCE_COMMIT="$SOURCE_COMMIT" \
  "$REPOSITORY_ROOT/scripts/install-distribution-notices.sh" \
  "$APP_PATH/Contents/Resources/Legal" "$BUILD_VERSION"
BINARY_NAME=$(/usr/libexec/PlistBuddy -c "Print :CFBundleExecutable" "$APP_PATH/Contents/Info.plist")
MAIN_BINARY="$APP_PATH/Contents/MacOS/$BINARY_NAME"

while IFS= read -r -d '' candidate; do
  [ "$candidate" != "$MAIN_BINARY" ] || continue
  if file -b "$candidate" | grep -q 'Mach-O'; then
    codesign --force --sign - "$candidate"
  fi
done < <(find "$APP_PATH/Contents" -type f -print0)
codesign --force --sign - "$APP_PATH"

test -x "$MAIN_BINARY"
test -f "$COREMIDI_PLUGIN"
file "$MAIN_BINARY" | grep -q 'arm64'
file "$COREMIDI_PLUGIN" | grep -q 'arm64'
codesign --verify --deep --strict --verbose=2 "$APP_PATH"

mkdir -p "$PACKAGE_ROOT"
cp -R "$APP_PATH" "$PACKAGE_ROOT/HitTheKit.app"
ditto "$APP_PATH/Contents/Resources/Legal" "$PACKAGE_ROOT/Legal"
printf '%s\n' \
  'HitTheKit macOS Apple Silicon playtest' \
  '' \
  '1. Move HitTheKit.app to Applications (optional).' \
  '2. Control-click HitTheKit.app and choose Open on first launch.' \
  '3. Choose Play for the demo song or Configure for drum setup.' \
  '' \
  'This early playtest is ad-hoc signed and not yet Apple-notarized.' \
  'No Unity installation is required.' \
  'License, notices, and exact source information are in Legal/.' \
  > "$PACKAGE_ROOT/README-FIRST.txt"

ditto -c -k --sequesterRsrc --keepParent "$PACKAGE_ROOT" "$ZIP_PATH"

echo "Application: $APP_PATH"
echo "Package: $ZIP_PATH"
ls -lh "$ZIP_PATH"
shasum -a 256 "$ZIP_PATH"
