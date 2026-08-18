#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PROJECT="$REPOSITORY_ROOT/tools/HitTheKit.MidiCapture/HitTheKit.MidiCapture.csproj"
OUTPUT="$REPOSITORY_ROOT/artifacts/midi-capture/osx-arm64"

mkdir -p "$OUTPUT"

# An absolute project path keeps the script independent of its caller's
# directory. On a correctly provisioned repository the SDK in global.json is
# used; the temporary-directory fallback supports development hosts that have
# a compatible newer SDK but not that exact feature band.
if (cd "$REPOSITORY_ROOT" && dotnet --version >/dev/null 2>&1); then
  BUILD_DIRECTORY="$REPOSITORY_ROOT"
else
  BUILD_DIRECTORY="${TMPDIR:-/tmp}"
fi

cd "$BUILD_DIRECTORY"
dotnet publish "$PROJECT" \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -o "$OUTPUT"

test -x "$OUTPUT/hitthekit-midi-capture"
printf 'Published self-contained macOS arm64 tool to %s\n' "$OUTPUT"
