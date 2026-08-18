#!/bin/sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
PROJECT="$REPOSITORY_ROOT/tools/HitTheKit.CoreMidiSmoke/HitTheKit.CoreMidiSmoke.csproj"
NATIVE="$REPOSITORY_ROOT/artifacts/coremidi-native/osx-arm64/HitTheKitCoreMidi.dylib"
ARTIFACT_ROOT="$REPOSITORY_ROOT/artifacts/coremidi-smoke"
PUBLISH_ROOT="$ARTIFACT_ROOT/publish"
PACKAGE_NAME="HitTheKit-CoreMidiSmoke-macos-arm64"
PACKAGE_ROOT="$ARTIFACT_ROOT/$PACKAGE_NAME"
ZIP_PATH="$ARTIFACT_ROOT/$PACKAGE_NAME.zip"

"$SCRIPT_DIR/build-coremidi-plugin-macos-arm64.sh"

dotnet publish "$PROJECT" \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  /p:PublishSingleFile=true \
  /p:DebugType=None \
  -o "$PUBLISH_ROOT"

rm -rf "$PACKAGE_ROOT"
mkdir -p "$PACKAGE_ROOT"
cp "$PUBLISH_ROOT/HitTheKit.CoreMidiSmoke" "$PACKAGE_ROOT/HitTheKit.CoreMidiSmoke"
cp "$NATIVE" "$PACKAGE_ROOT/HitTheKitCoreMidi.dylib"
cp "$REPOSITORY_ROOT/tools/HitTheKit.CoreMidiSmoke/Package/README.txt" "$PACKAGE_ROOT/README.txt"
chmod 755 "$PACKAGE_ROOT/HitTheKit.CoreMidiSmoke" "$PACKAGE_ROOT/HitTheKitCoreMidi.dylib"

(
  cd "$PACKAGE_ROOT"
  shasum -a 256 HitTheKit.CoreMidiSmoke HitTheKitCoreMidi.dylib README.txt > SHA256SUMS
)

rm -f "$ZIP_PATH"
(
  cd "$ARTIFACT_ROOT"
  /usr/bin/zip -X -q -r "$ZIP_PATH" "$PACKAGE_NAME"
)

echo "Generated $ZIP_PATH"
