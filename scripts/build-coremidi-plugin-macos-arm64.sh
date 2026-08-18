#!/bin/sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
SOURCE_ROOT="$REPOSITORY_ROOT/native/HitTheKit.CoreMidi"
BUILD_ROOT="$REPOSITORY_ROOT/artifacts/coremidi-native/osx-arm64"
PLUGIN_DIRECTORY="$REPOSITORY_ROOT/src/HitTheKit.Unity/Assets/Plugins/macOS"
PLUGIN_PATH="$PLUGIN_DIRECTORY/HitTheKitCoreMidi.dylib"

if ! command -v xcrun >/dev/null 2>&1; then
  echo "Xcode command-line tools are required (xcrun not found)." >&2
  exit 2
fi

CLANGXX=$(xcrun --find clang++)
SDK_PATH=$(xcrun --sdk macosx --show-sdk-path)
mkdir -p "$BUILD_ROOT" "$PLUGIN_DIRECTORY"

COMMON_FLAGS="-std=c++17 -arch arm64 -mmacosx-version-min=12.0 -isysroot $SDK_PATH -Wall -Wextra -Werror"

run_with_timeout() {
  timeout_seconds="$1"
  shift
  "$@" &
  command_pid=$!
  while kill -0 "$command_pid" 2>/dev/null; do
    if [ "$timeout_seconds" -le 0 ]; then
      kill "$command_pid" 2>/dev/null || true
      wait "$command_pid" 2>/dev/null || true
      echo "Native CoreMIDI tests timed out." >&2
      return 124
    fi
    sleep 1
    timeout_seconds=$((timeout_seconds - 1))
  done
  wait "$command_pid"
}

"$CLANGXX" $COMMON_FLAGS -fPIC -fvisibility=hidden -dynamiclib \
  -I"$SOURCE_ROOT/include" -I"$SOURCE_ROOT/src" \
  "$SOURCE_ROOT/src/Midi1Parser.cpp" \
  "$SOURCE_ROOT/src/HitTheKitCoreMidi.cpp" \
  -framework CoreMIDI -framework CoreFoundation \
  -Wl,-install_name,@rpath/HitTheKitCoreMidi.dylib \
  -o "$BUILD_ROOT/HitTheKitCoreMidi.dylib"

"$CLANGXX" $COMMON_FLAGS -DHTK_COREMIDI_TESTING \
  -I"$SOURCE_ROOT/include" -I"$SOURCE_ROOT/src" \
  "$SOURCE_ROOT/tests/NativeTests.cpp" \
  "$SOURCE_ROOT/src/Midi1Parser.cpp" \
  "$SOURCE_ROOT/src/HitTheKitCoreMidi.cpp" \
  -framework CoreMIDI -framework CoreFoundation \
  -o "$BUILD_ROOT/HitTheKitCoreMidi.NativeTests"

run_with_timeout 30 "$BUILD_ROOT/HitTheKitCoreMidi.NativeTests"
file "$BUILD_ROOT/HitTheKitCoreMidi.dylib"
lipo -archs "$BUILD_ROOT/HitTheKitCoreMidi.dylib" | grep -qx 'arm64'
otool -L "$BUILD_ROOT/HitTheKitCoreMidi.dylib"

TEMP_PLUGIN="$PLUGIN_DIRECTORY/.HitTheKitCoreMidi.dylib.tmp"
cp "$BUILD_ROOT/HitTheKitCoreMidi.dylib" "$TEMP_PLUGIN"
chmod 755 "$TEMP_PLUGIN"
mv -f "$TEMP_PLUGIN" "$PLUGIN_PATH"
echo "Generated $PLUGIN_PATH"
