#!/bin/bash
set -euo pipefail

SOURCE_ROOT=${1:-}
OUTPUT_ZIP=${2:-}
if [ "$#" -ge 2 ]; then shift 2; else set --; fi

fail() {
  echo "local song pack: $*" >&2
  exit 1
}

[ -n "$SOURCE_ROOT" ] && [ -n "$OUTPUT_ZIP" ] && [ "$#" -gt 0 ] ||
  fail "usage: $0 <Songs-root> <output.zip> <song-id> [song-id ...]"
[ -d "$SOURCE_ROOT" ] || fail "source Songs root does not exist: $SOURCE_ROOT"
[[ "$OUTPUT_ZIP" == *.zip ]] || fail "output must use the .zip extension"
[ ! -e "$OUTPUT_ZIP" ] || fail "output already exists: $OUTPUT_ZIP"

command -v ditto >/dev/null 2>&1 || fail "ditto was not found"
command -v plutil >/dev/null 2>&1 || fail "plutil was not found"
command -v unzip >/dev/null 2>&1 || fail "unzip was not found"

SOURCE_ROOT=$(CDPATH='' cd -- "$SOURCE_ROOT" && pwd -P)
OUTPUT_DIRECTORY=$(CDPATH='' cd -- "$(dirname -- "$OUTPUT_ZIP")" && pwd)
OUTPUT_ZIP="$OUTPUT_DIRECTORY/$(basename -- "$OUTPUT_ZIP")"
WORK_ROOT=$(mktemp -d "$OUTPUT_DIRECTORY/.hitthekit-song-pack.XXXXXX")
PACK_ROOT="$WORK_ROOT/HTKSongs"

cleanup() { rm -rf "$WORK_ROOT"; }
trap cleanup EXIT INT TERM
mkdir -p "$PACK_ROOT"

for song_id in "$@"; do
  [[ "$song_id" =~ ^[a-z0-9]+(-[a-z0-9]+)*$ ]] || fail "invalid song ID: $song_id"
  SONG_ROOT="$SOURCE_ROOT/$song_id"
  [ -d "$SONG_ROOT" ] || fail "song folder does not exist: $song_id"
  [ ! -L "$SONG_ROOT" ] || fail "song folder cannot be a symbolic link: $song_id"
  MANIFEST="$SONG_ROOT/song.json"
  [ -f "$MANIFEST" ] && [ ! -L "$MANIFEST" ] || fail "song.json is missing or linked: $song_id"

  CHART_FILE=$(plutil -extract chartFile raw -o - "$MANIFEST" 2>/dev/null) ||
    fail "song.json does not declare chartFile: $song_id"
  AUDIO_FILE=$(plutil -extract audioFile raw -o - "$MANIFEST" 2>/dev/null) ||
    fail "song.json does not declare audioFile: $song_id"
  case "$CHART_FILE" in ''|/*|*'..'*|*/*) fail "chartFile must be a direct relative file: $song_id" ;; esac
  case "$AUDIO_FILE" in ''|/*|*'..'*|*/*) fail "audioFile must be a direct relative file: $song_id" ;; esac
  [ -f "$SONG_ROOT/$CHART_FILE" ] && [ ! -L "$SONG_ROOT/$CHART_FILE" ] ||
    fail "declared chart is missing or linked: $song_id"
  [ -f "$SONG_ROOT/$AUDIO_FILE" ] && [ ! -L "$SONG_ROOT/$AUDIO_FILE" ] ||
    fail "declared audio is missing or linked: $song_id"

  DESTINATION="$PACK_ROOT/$song_id"
  mkdir -p "$DESTINATION"
  ditto --norsrc "$MANIFEST" "$DESTINATION/song.json"
  ditto --norsrc "$SONG_ROOT/$CHART_FILE" "$DESTINATION/$CHART_FILE"
  ditto --norsrc "$SONG_ROOT/$AUDIO_FILE" "$DESTINATION/$AUDIO_FILE"
done

COPYFILE_DISABLE=1 ditto -c -k --norsrc --keepParent "$PACK_ROOT" "$OUTPUT_ZIP"
unzip -t "$OUTPUT_ZIP" >/dev/null
if unzip -Z1 "$OUTPUT_ZIP" | awk '/(^|\/)\._/ { found = 1 } END { exit !found }'; then
  fail "archive contains AppleDouble metadata"
fi

echo "HITTHEKIT_LOCAL_SONG_PACK_READY"
echo "zip=$OUTPUT_ZIP"
echo "songs=$#"
ls -lh "$OUTPUT_ZIP"
shasum -a 256 "$OUTPUT_ZIP"
