#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH='' cd -- "$SCRIPT_DIR/../.." && pwd)
TEMP_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/hitthekit-windows-package-tests.XXXXXX")

cleanup() { rm -rf "$TEMP_ROOT"; }
trap cleanup EXIT INT TERM

expect_failure() {
  local label=$1
  shift
  if "$@" >"$TEMP_ROOT/$label.out" 2>"$TEMP_ROOT/$label.err"; then
    echo "Expected failure but command succeeded: $label" >&2
    exit 1
  fi
}

PACKAGE_SCRIPT="$REPOSITORY_ROOT/scripts/package-game-windows-x64.sh"
BUILD_SCRIPT="$REPOSITORY_ROOT/src/HitTheKit.Unity/Assets/HitTheKit/Editor/WindowsPlaytestBuild.cs"
IMPORTER_SCRIPT="$REPOSITORY_ROOT/src/HitTheKit.Unity/Assets/HitTheKit/Editor/CoreMidiPluginImporterConfigurator.cs"

expect_failure missing-version "$PACKAGE_SCRIPT"
expect_failure invalid-version "$PACKAGE_SCRIPT" invalid
mkdir -p "$TEMP_ROOT/existing-output"
expect_failure existing-output "$PACKAGE_SCRIPT" 0.5.0 "$TEMP_ROOT/existing-output"

grep -q 'BuildTarget.StandaloneWindows64' "$BUILD_SCRIPT"
grep -q 'HITTHEKIT_WINDOWS_BUILD_SUCCEEDED' "$BUILD_SCRIPT"
grep -q -- '-buildTarget StandaloneWindows64' "$PACKAGE_SCRIPT"
grep -q 'WindowsPlaytestBuild.BuildX64' "$PACKAGE_SCRIPT"
grep -q 'Windows Build Support (Mono)' "$PACKAGE_SCRIPT"
grep -q 'macOS CoreMIDI plug-in leaked' "$PACKAGE_SCRIPT"
grep -q 'Electronic-drum MIDI is not implemented on Windows yet' "$PACKAGE_SCRIPT"
grep -q -- '--norsrc --noextattr' "$PACKAGE_SCRIPT"

grep -q 'SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false)' \
  "$IMPORTER_SCRIPT"
if grep -q 'build-coremidi-plugin-macos-arm64.sh' "$PACKAGE_SCRIPT"; then
  echo "Windows packaging must not build or bundle the macOS CoreMIDI plug-in." >&2
  exit 1
fi

echo "Windows packaging contract tests passed."
