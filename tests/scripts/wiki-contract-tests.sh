#!/bin/sh
set -eu

REPOSITORY_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
cd "$REPOSITORY_ROOT"

fail() {
  echo "wiki contract: $*" >&2
  exit 1
}

for page in \
  Home.md \
  Getting-Started.md \
  Features.md \
  Build-from-Source.md \
  Learning-and-Gameplay.md \
  MIDI-and-Device-Setup.md \
  Local-Songs.md \
  Platforms-and-Limitations.md \
  Troubleshooting.md \
  FAQ-and-Support.md \
  _Sidebar.md \
  _Footer.md; do
  [ -s "docs/wiki/$page" ] || fail "required page is missing or empty: $page"
done

[ -s docs/wiki/README.md ] || fail "Wiki source instructions are missing"
[ -s scripts/publish-github-wiki.sh ] || fail "Wiki publishing script is missing"
sh -n scripts/publish-github-wiki.sh

grep -Fq 'HitTheKit does not bundle commercial music.' docs/wiki/Local-Songs.md ||
  fail "local-song guide must retain the commercial-content boundary"
grep -Fq 'No approved public Unity binary is currently distributed.' docs/wiki/FAQ-and-Support.md ||
  fail "FAQ must retain the source-only distribution boundary"

if grep -R -Eq '/Users/|lucabaron|BEGIN (RSA|OPENSSH|EC) PRIVATE KEY|password|secret|token' docs/wiki; then
  fail "Wiki contains personal paths or sensitive markers"
fi

TEMP_ROOT=$(mktemp -d "${TMPDIR:-/tmp}/hitthekit-wiki-test.XXXXXX")
cleanup() {
  rm -rf "$TEMP_ROOT"
}
trap cleanup EXIT HUP INT TERM

git init --bare --initial-branch=master "$TEMP_ROOT/remote.git" >/dev/null
HTK_WIKI_REMOTE="$TEMP_ROOT/remote.git" \
  scripts/publish-github-wiki.sh >/dev/null
git clone "$TEMP_ROOT/remote.git" "$TEMP_ROOT/result" >/dev/null 2>&1

[ -s "$TEMP_ROOT/result/Home.md" ] || fail "publisher omitted Home.md"
[ -s "$TEMP_ROOT/result/_Sidebar.md" ] || fail "publisher omitted _Sidebar.md"
[ ! -e "$TEMP_ROOT/result/README.md" ] || fail "publisher exposed source-only README.md"

SOURCE_PAGE_COUNT=$(find docs/wiki -maxdepth 1 -type f -name '*.md' ! -name README.md | wc -l | tr -d '[:space:]')
PUBLISHED_PAGE_COUNT=$(find "$TEMP_ROOT/result" -maxdepth 1 -type f -name '*.md' | wc -l | tr -d '[:space:]')
[ "$SOURCE_PAGE_COUNT" -eq "$PUBLISHED_PAGE_COUNT" ] ||
  fail "publisher page count differs: source=$SOURCE_PAGE_COUNT published=$PUBLISHED_PAGE_COUNT"

echo "WIKI_CONTRACTS_OK pages=$PUBLISHED_PAGE_COUNT"
