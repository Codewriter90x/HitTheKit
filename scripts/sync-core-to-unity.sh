#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
CORE_PROJECT="$REPOSITORY_ROOT/src/HitTheKit.Core/HitTheKit.Core.csproj"
PLUGIN_DIRECTORY="$REPOSITORY_ROOT/src/HitTheKit.Unity/Assets/Plugins/HitTheKit.Core"
CONFIGURATION="${CONFIGURATION:-Debug}"

if ! command -v dotnet >/dev/null 2>&1; then
    echo "Error: dotnet is required to build HitTheKit.Core." >&2
    exit 1
fi

TARGET_FRAMEWORK="$(sed -n 's:.*<TargetFramework>\([^<]*\)</TargetFramework>.*:\1:p' "$CORE_PROJECT" | head -n 1)"
if [[ "$TARGET_FRAMEWORK" != "netstandard2.1" ]]; then
    echo "Error: expected HitTheKit.Core to target netstandard2.1, found '${TARGET_FRAMEWORK:-missing}'." >&2
    exit 1
fi

dotnet build "$CORE_PROJECT" --configuration "$CONFIGURATION" --framework "$TARGET_FRAMEWORK"

SOURCE_DLL="$REPOSITORY_ROOT/src/HitTheKit.Core/bin/$CONFIGURATION/$TARGET_FRAMEWORK/HitTheKit.Core.dll"
if [[ ! -f "$SOURCE_DLL" ]]; then
    echo "Error: core build succeeded but '$SOURCE_DLL' was not produced." >&2
    exit 1
fi

mkdir -p "$PLUGIN_DIRECTORY"
cp "$SOURCE_DLL" "$PLUGIN_DIRECTORY/HitTheKit.Core.dll"
echo "Synchronized HitTheKit.Core.dll to '$PLUGIN_DIRECTORY'."
