#!/bin/sh
set -eu

REPOSITORY_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
cd "$REPOSITORY_ROOT"

fail() {
  echo "licensing contract: $*" >&2
  exit 1
}

EXPECTED_LICENSE_SHA256=3f3d9e0024b1921b067d6f7f88deb4a60cbe7a78e76c64e3f1d7fc3b779b9d04
ACTUAL_LICENSE_SHA256=$(shasum -a 256 LICENSE | awk '{print $1}')
[ "$ACTUAL_LICENSE_SHA256" = "$EXPECTED_LICENSE_SHA256" ] ||
  fail "LICENSE is not the reviewed official MPL-2.0 text"

grep -Fq 'Mozilla Public License Version 2.0' LICENSE ||
  fail "root LICENSE is not MPL-2.0"
grep -Fq 'license-MPL--2.0' README.md ||
  fail "README badge does not identify MPL-2.0"
grep -Fq 'Original project code is licensed under' README.md ||
  fail "README lacks the project license boundary"
grep -Fq 'MPL-2.0. See LICENSE and LICENSING.md.' NOTICE ||
  fail "NOTICE lacks the MPL project grant"
grep -Fq 'Existing GPL copies' LICENSING.md ||
  fail "LICENSING.md must preserve the non-retroactivity statement"
grep -Fq 'It does not itself approve a particular binary release.' LICENSING.md ||
  fail "LICENSING.md must preserve the binary release gate"
grep -Fq 'SUPERSEDED_BY_MPL_COMMUNITY_LICENSE_DECISION' docs/governance/dual-licensing-decision.md ||
  fail "historical GPL decision is not marked superseded"
grep -Fq 'Status: `ADOPTED`' docs/governance/mpl-community-license-decision.md ||
  fail "MPL decision status is missing"
grep -Fq 'HitTheKit-MPL-2.0.txt' scripts/package-midi-capture-macos-arm64.sh ||
  fail "developer-tool package does not use the MPL license filename"
grep -Fq 'MPL-2.0' src/HitTheKit.Unity/Assets/HitTheKit/UI/MainMenu/MainMenu.uxml ||
  fail "in-app license label is stale"
grep -Fq 'MPL-2.0 · public source, binaries under validation' website/en/index.html ||
  fail "English website license status is stale"
grep -Fq 'MPL-2.0 · sorgenti pubblici, binari in validazione' website/index.html ||
  fail "Italian website license status is stale"

if rg -n 'HitTheKit-GPL-3\.0\.txt|license-GPL--3\.0' README.md NOTICE scripts src website docs/wiki; then
  fail "an operative GPL label remains outside historical records"
fi

echo "LICENSING_CONTRACTS_OK license=MPL-2.0 sha256=$ACTUAL_LICENSE_SHA256"
