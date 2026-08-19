#!/bin/sh
set -eu

REPOSITORY_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
cd "$REPOSITORY_ROOT"

fail() {
  echo "asset provenance contract: $*" >&2
  exit 1
}

MANIFEST=docs/legal/ASSET_PROVENANCE.sha256
[ -s "$MANIFEST" ] || fail "missing hash inventory: $MANIFEST"

sha256_file() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | awk '{print $1}'
  else
    shasum -a 256 "$1" | awk '{print $1}'
  fi
}

EXPECTED=$(mktemp "${TMPDIR:-/tmp}/hitthekit-assets-expected.XXXXXX")
ACTUAL=$(mktemp "${TMPDIR:-/tmp}/hitthekit-assets-actual.XXXXXX")
cleanup() { rm -f "$EXPECTED" "$ACTUAL"; }
trap cleanup EXIT INT TERM

awk '
  NF < 2 || length($1) != 64 || $1 !~ /^[0-9a-f]+$/ { exit 2 }
  {
    path = substr($0, 67)
    if (path == "" || path ~ /^\// || path ~ /(^|\/)\.\.($|\/)/) exit 3
    print path
  }
' "$MANIFEST" | sort > "$EXPECTED" || fail "malformed hash inventory"

[ "$(wc -l < "$EXPECTED" | tr -d ' ')" -eq "$(sort -u "$EXPECTED" | wc -l | tr -d ' ')" ] ||
  fail "duplicate asset path in hash inventory"

find branding docs/design src/HitTheKit.Unity/Assets website \
  -type f \( \
    -iname '*.png' -o -iname '*.jpg' -o -iname '*.jpeg' -o \
    -iname '*.svg' -o -iname '*.fbx' -o -iname '*.icns' -o \
    -iname '*.wav' -o -iname '*.ogg' -o -iname '*.mp3' -o \
    -iname '*.flac' -o -iname '*.aac' -o -iname '*.m4a' -o \
    -iname '*.mp4' -o -iname '*.webm' -o -iname '*.mov' -o \
    -iname '*.ttf' -o -iname '*.otf' \
  \) -print | sort > "$ACTUAL"

if ! diff -u "$EXPECTED" "$ACTUAL"; then
  fail "media inventory is not exhaustive"
fi

while IFS= read -r line; do
  expected_hash=$(printf '%s\n' "$line" | awk '{print $1}')
  asset_path=$(printf '%s\n' "$line" | cut -c67-)
  [ -f "$asset_path" ] || fail "inventoried asset is missing: $asset_path"
  actual_hash=$(sha256_file "$asset_path")
  [ "$actual_hash" = "$expected_hash" ] ||
    fail "hash mismatch: $asset_path"
done < "$MANIFEST"

echo "ASSET_PROVENANCE_CONTRACTS_OK count=$(wc -l < "$EXPECTED" | tr -d ' ')"
