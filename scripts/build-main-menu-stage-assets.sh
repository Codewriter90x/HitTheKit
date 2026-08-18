#!/bin/sh

set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
BLENDER_APP=${BLENDER_APP:-/Applications/Blender.app/Contents/MacOS/Blender}
GENERATOR="$REPOSITORY_ROOT/tools/blender/create_main_menu_stage.py"
BLEND_OUTPUT="$REPOSITORY_ROOT/artifacts/main-menu-stage/HitTheKit-MainMenuStage.blend"
FBX_OUTPUT="$REPOSITORY_ROOT/src/HitTheKit.Unity/Assets/HitTheKit/Visuals/Models/HitTheKit-MainMenuStage.fbx"

if [ ! -x "$BLENDER_APP" ]; then
    echo "Blender executable not found: $BLENDER_APP" >&2
    exit 1
fi

if [ ! -f "$GENERATOR" ]; then
    echo "Blender stage generator not found: $GENERATOR" >&2
    exit 1
fi

"$BLENDER_APP" \
    --background \
    --factory-startup \
    --python "$GENERATOR" \
    -- "$BLEND_OUTPUT" "$FBX_OUTPUT"

test -s "$BLEND_OUTPUT"
test -s "$FBX_OUTPUT"

echo "Generated: $BLEND_OUTPUT"
echo "Generated: $FBX_OUTPUT"
