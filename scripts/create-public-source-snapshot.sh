#!/bin/sh
set -eu

OUTPUT_PATH=${1:-}
[ -n "$OUTPUT_PATH" ] || {
  echo "usage: $0 <output.tar.gz>" >&2
  exit 2
}

REPOSITORY_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
case "$OUTPUT_PATH" in
  /*) ;;
  *) OUTPUT_PATH="$PWD/$OUTPUT_PATH" ;;
esac

[ ! -e "$OUTPUT_PATH" ] || {
  echo "output already exists: $OUTPUT_PATH" >&2
  exit 3
}

[ -z "$(git -C "$REPOSITORY_ROOT" status --porcelain)" ] || {
  echo "working tree must be clean so the snapshot matches HEAD" >&2
  exit 4
}

bash "$REPOSITORY_ROOT/tests/scripts/public-readiness-contract-tests.sh"

TEMP_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/hitthekit-public-source.XXXXXX")
cleanup() { rm -rf "$TEMP_ROOT"; }
trap cleanup EXIT INT TERM

SNAPSHOT_ROOT="$TEMP_ROOT/HitTheKit-source"
mkdir -p "$SNAPSHOT_ROOT"
git -C "$REPOSITORY_ROOT" archive HEAD \
  -- . \
  ':(exclude)HITTHEKIT_HANDOFF.md' \
  ':(exclude)HITTHEKIT_CURRENT_CONTEXT.md' | tar -x -C "$SNAPSHOT_ROOT"

[ ! -e "$SNAPSHOT_ROOT/HITTHEKIT_HANDOFF.md" ] || {
  echo "private handoff leaked into public snapshot" >&2
  exit 5
}
[ ! -e "$SNAPSHOT_ROOT/HITTHEKIT_CURRENT_CONTEXT.md" ] || {
  echo "private current-context file leaked into public snapshot" >&2
  exit 5
}

find "$SNAPSHOT_ROOT/src/HitTheKit.Unity/Assets/StreamingAssets/Songs" \
  -name song.json -print0 | xargs -0 -n1 jq empty
if rg -i 'AC/DC|Highway to Hell|Nirvana|Audioslave|Van Halen|Ozzy Osbourne|Thirty Seconds to Mars|Hot Milk' \
  "$SNAPSHOT_ROOT/src/HitTheKit.Unity/Assets/StreamingAssets/Songs"; then
  echo "commercial catalog metadata detected; snapshot refused" >&2
  exit 6
fi

mkdir -p "$(dirname -- "$OUTPUT_PATH")"
tar -czf "$OUTPUT_PATH" -C "$TEMP_ROOT" HitTheKit-source
shasum -a 256 "$OUTPUT_PATH"
echo "Public source snapshot created without Git history: $OUTPUT_PATH"
