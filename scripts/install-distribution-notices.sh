#!/bin/bash
set -euo pipefail

SCRIPT_DIR=$(CDPATH='' cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH='' cd -- "$SCRIPT_DIR/.." && pwd)
DESTINATION=${1:-}
VERSION=${2:-}
SOURCE_REPOSITORY=https://github.com/Codewriter90x/HitTheKit

fail() {
  echo "distribution notices: $*" >&2
  exit 1
}

[ -n "$DESTINATION" ] && [ -n "$VERSION" ] ||
  fail "usage: $0 <destination-directory> <version>"
[[ "$VERSION" =~ ^[0-9]+([.][0-9]+){1,2}([-.][0-9A-Za-z.-]+)?$ ]] ||
  fail "version must identify a release, for example 0.5.0 or 0.5.0-rc.1"

SOURCE_COMMIT=${HITTHEKIT_SOURCE_COMMIT:-}
if [ -z "$SOURCE_COMMIT" ]; then
  [ -z "$(git -C "$REPOSITORY_ROOT" status --porcelain)" ] ||
    fail "the repository is dirty; commit the exact candidate or set HITTHEKIT_SOURCE_COMMIT explicitly"
  SOURCE_COMMIT=$(git -C "$REPOSITORY_ROOT" rev-parse HEAD)
fi

[[ "$SOURCE_COMMIT" =~ ^[0-9a-f]{40}$ ]] ||
  fail "HITTHEKIT_SOURCE_COMMIT must be a full 40-character lowercase Git SHA"
git -C "$REPOSITORY_ROOT" cat-file -e "$SOURCE_COMMIT^{commit}" 2>/dev/null ||
  fail "source commit is not present in this repository: $SOURCE_COMMIT"

for required_file in LICENSE NOTICE THIRD_PARTY_NOTICES.md LICENSING.md; do
  [ -f "$REPOSITORY_ROOT/$required_file" ] ||
    fail "required legal file is missing: $required_file"
done

mkdir -p "$DESTINATION"
install -m 0644 "$REPOSITORY_ROOT/LICENSE" "$DESTINATION/LICENSE"
install -m 0644 "$REPOSITORY_ROOT/NOTICE" "$DESTINATION/NOTICE"
install -m 0644 "$REPOSITORY_ROOT/THIRD_PARTY_NOTICES.md" \
  "$DESTINATION/THIRD_PARTY_NOTICES.md"
install -m 0644 "$REPOSITORY_ROOT/LICENSING.md" \
  "$DESTINATION/LICENSING.md"

printf '%s\n' \
  'HitTheKit corresponding source code notice' \
  '' \
  "Binary version: $VERSION" \
  "Exact source revision: $SOURCE_COMMIT" \
  '' \
  'The Source Code Form of MPL-2.0-covered HitTheKit files in this' \
  'distribution is available from the exact public revision below:' \
  '' \
  "$SOURCE_REPOSITORY/tree/$SOURCE_COMMIT" \
  '' \
  'A downloadable archive of that revision is available at:' \
  '' \
  "$SOURCE_REPOSITORY/archive/$SOURCE_COMMIT.zip" \
  '' \
  'The MPL-2.0 license text, project notice, licensing overview, and' \
  'third-party notices are included in this Legal directory.' \
  '' \
  'Unity and third-party components retain their own licenses and are not' \
  'relicensed by HitTheKit.' \
  > "$DESTINATION/SOURCE-CODE.txt"

echo "HITTHEKIT_DISTRIBUTION_NOTICES_INSTALLED"
echo "destination=$DESTINATION"
echo "sourceCommit=$SOURCE_COMMIT"
