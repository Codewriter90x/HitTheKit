#!/bin/sh
set -eu

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH='' cd -- "$SCRIPT_DIR/.." && pwd)
APP_PATH=${1:-}
ICON_SOURCE="$REPOSITORY_ROOT/branding/app-icon/macos/HitTheKit.icns"

if [ -z "$APP_PATH" ] || [ ! -d "$APP_PATH/Contents" ]; then
  echo "usage: $0 /path/to/HitTheKit.app" >&2
  exit 2
fi

if [ ! -f "$ICON_SOURCE" ]; then
  echo "HitTheKit macOS icon is missing: $ICON_SOURCE" >&2
  exit 3
fi

PLIST="$APP_PATH/Contents/Info.plist"
RESOURCES="$APP_PATH/Contents/Resources"
ICON_NAME=HitTheKit.icns

if [ ! -f "$PLIST" ]; then
  echo "Application Info.plist is missing: $PLIST" >&2
  exit 4
fi

mkdir -p "$RESOURCES"
cp "$ICON_SOURCE" "$RESOURCES/$ICON_NAME"

if /usr/libexec/PlistBuddy -c "Print :CFBundleIconFile" "$PLIST" >/dev/null 2>&1; then
  /usr/libexec/PlistBuddy -c "Set :CFBundleIconFile $ICON_NAME" "$PLIST"
else
  /usr/libexec/PlistBuddy -c "Add :CFBundleIconFile string $ICON_NAME" "$PLIST"
fi

echo "HITTHEKIT_MACOS_APP_ICON_APPLIED path=$RESOURCES/$ICON_NAME"
