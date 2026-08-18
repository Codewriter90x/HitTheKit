#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIRECTORY="$REPOSITORY_ROOT/artifacts/midi-capture/osx-arm64"
ARCHIVE="$REPOSITORY_ROOT/artifacts/midi-capture/HitTheKit-MidiCapture-macos-arm64.zip"
PACKAGE_SOURCE="$REPOSITORY_ROOT/tools/HitTheKit.MidiCapture/Package"
PACKAGE_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/hitthekit-midi-package.XXXXXX")"
PACKAGE_DIRECTORY="$PACKAGE_ROOT/HitTheKit-MidiCapture-macos-arm64"

cleanup() {
  rm -rf "$PACKAGE_ROOT"
}
trap cleanup EXIT

"$SCRIPT_DIR/publish-midi-capture-macos-arm64.sh"
mkdir -p "$PACKAGE_DIRECTORY/LICENSES"
cp "$PUBLISH_DIRECTORY/hitthekit-midi-capture" "$PACKAGE_DIRECTORY/"
cp "$PACKAGE_SOURCE/README.txt" "$PACKAGE_DIRECTORY/"
cp "$PACKAGE_SOURCE/run.command" "$PACKAGE_DIRECTORY/"
cp "$PACKAGE_SOURCE/LICENSES/DryWetMIDI-MIT.txt" "$PACKAGE_DIRECTORY/LICENSES/"
cp "$REPOSITORY_ROOT/LICENSE" "$PACKAGE_DIRECTORY/LICENSES/HitTheKit-GPL-3.0.txt"
chmod +x "$PACKAGE_DIRECTORY/hitthekit-midi-capture" "$PACKAGE_DIRECTORY/run.command"

mkdir -p "$(dirname "$ARCHIVE")"
rm -f "$ARCHIVE"
(
  cd "$PACKAGE_ROOT"
  find HitTheKit-MidiCapture-macos-arm64 -exec touch -t 198001010000 {} +
  /usr/bin/zip -X -q -r "$ARCHIVE" HitTheKit-MidiCapture-macos-arm64
)
printf 'Packaged transferable tool at %s\n' "$ARCHIVE"
