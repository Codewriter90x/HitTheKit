#!/bin/sh
set -eu

REPOSITORY_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
SOURCE_DIRECTORY=${HTK_WIKI_SOURCE:-"$REPOSITORY_ROOT/docs/wiki"}
WIKI_REMOTE=${HTK_WIKI_REMOTE:-"https://github.com/Codewriter90x/HitTheKit.wiki.git"}
WORK_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/hitthekit-wiki.XXXXXX")
WIKI_CHECKOUT="$WORK_ROOT/wiki"

cleanup() {
  rm -rf "$WORK_ROOT"
}
trap cleanup EXIT HUP INT TERM

[ -d "$SOURCE_DIRECTORY" ] || {
  echo "Wiki source directory not found: $SOURCE_DIRECTORY" >&2
  exit 1
}

git clone "$WIKI_REMOTE" "$WIKI_CHECKOUT"

find "$WIKI_CHECKOUT" -maxdepth 1 -type f -name '*.md' -delete
for source_page in "$SOURCE_DIRECTORY"/*.md; do
  [ -f "$source_page" ] || continue
  [ "$(basename "$source_page")" = "README.md" ] && continue
  cp "$source_page" "$WIKI_CHECKOUT/"
done

cd "$WIKI_CHECKOUT"
git add -A

if git diff --cached --quiet; then
  echo "HITTHEKIT_WIKI_ALREADY_CURRENT"
  exit 0
fi

git -c user.name="HitTheKit Wiki Publisher" \
  -c user.email="noreply@hitthekit.local" \
  commit -m "docs: sync public handbook"
git push origin HEAD:master

echo "HITTHEKIT_WIKI_PUBLISHED"
