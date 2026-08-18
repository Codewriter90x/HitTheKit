#!/bin/sh
set -eu

REPOSITORY_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
cd "$REPOSITORY_ROOT"

fail() {
  echo "public readiness contract: $*" >&2
  exit 1
}

VERSION_VALUE=$(tr -d '[:space:]' < VERSION)
PUBLICATION_STATE=$(tr -d '[:space:]' < PUBLICATION_STATUS)
case "$VERSION_VALUE" in
  [0-9]*.[0-9]*.[0-9]*) ;;
  *) fail "VERSION is not a semantic version: $VERSION_VALUE" ;;
esac

rg -q "^[[:space:]]*bundleVersion: $VERSION_VALUE$" \
  src/HitTheKit.Unity/ProjectSettings/ProjectSettings.asset || \
  fail "Unity bundleVersion does not match VERSION $VERSION_VALUE"

for path in \
  CODE_OF_CONDUCT.md \
  CONTRIBUTING.md \
  FUNDING.md \
  GOVERNANCE.md \
  LICENSE \
  MAINTAINERS.md \
  NOTICE \
  PRIVACY.md \
  README.md \
  ROADMAP.md \
  SECURITY.md \
  SUPPORT.md \
  THIRD_PARTY_NOTICES.md \
  .github/CODEOWNERS \
  PUBLICATION_STATUS \
  docs/legal/ASSET_PROVENANCE.md \
  docs/legal/ASSET_PROVENANCE.sha256 \
  docs/development/windows-playtest-package.md \
  docs/release/PUBLIC_RELEASE_CHECKLIST.md \
  docs/release/RELEASE_PROCESS.md \
  scripts/package-game-windows-x64.sh \
  tests/scripts/asset-provenance-contract-tests.sh \
  tests/scripts/windows-packaging-contract-tests.sh; do
  [ -s "$path" ] || fail "required public file is missing or empty: $path"
done

case "$PUBLICATION_STATE" in
  private-preparation)
    for path in \
      .github/workflows/pages.yml.example \
      .github/workflows/source-release.yml.example; do
      [ -s "$path" ] || fail "inactive publication template is missing: $path"
    done
    [ -s .github/FUNDING.yml ] ||
      fail "approved GitHub Sponsors profile must have an active FUNDING.yml"
    rg -q '^[[:space:]]*-[[:space:]]*Codewriter90x[[:space:]]*$' .github/FUNDING.yml ||
      fail "FUNDING.yml must identify the approved Codewriter90x profile"
    [ ! -e .github/FUNDING.yml.example ] ||
      fail "remove obsolete funding template after Sponsors approval"
    [ ! -e .github/workflows/pages.yml ] || \
      fail "Pages deployment must stay inactive until the public repository is approved"
    [ ! -e .github/workflows/source-release.yml ] || \
      fail "source release workflow must stay inactive until the public repository is approved"
    ;;
  public)
    [ -s .github/workflows/pages.yml ] || fail "public Pages workflow is not active"
    [ -s .github/workflows/source-release.yml ] || fail "public source release workflow is not active"
    if [ -e .github/FUNDING.yml ] && [ -e .github/FUNDING.yml.example ]; then
      fail "keep either the active or pending funding file, not both"
    fi
    [ -e .github/FUNDING.yml ] || [ -e .github/FUNDING.yml.example ] || \
      fail "public funding status is undocumented"
    ;;
  *) fail "unknown PUBLICATION_STATUS: $PUBLICATION_STATE" ;;
esac

for template in bug_report.yml feature_request.yml hardware_compatibility.yml lesson_proposal.yml; do
  [ -s ".github/ISSUE_TEMPLATE/$template" ] || \
    fail "required issue form is missing: $template"
done

find src/HitTheKit.Unity/Assets/StreamingAssets/Songs \
  -name song.json -print0 | xargs -0 -n1 jq empty

if rg -i 'AC/DC|Highway to Hell|Nirvana|Audioslave|Van Halen|Ozzy Osbourne|Thirty Seconds to Mars|Hot Milk' \
  src/HitTheKit.Unity/Assets/StreamingAssets/Songs; then
  fail "commercial artist or title metadata is bundled"
fi

if rg -q 'commercial license (is|è) available|licenza commerciale (è )?disponibile' \
  website README.md; then
  fail "public copy presents the pending commercial-license path as active"
fi

rg -q 'MIDI.*macOS|macOS.*MIDI' README.md || \
  fail "README must state the current macOS MIDI boundary"
rg -q 'DO NOT PUBLISH YET' docs/release/0.5.0-release-notes-draft.md || \
  fail "draft release notes must retain the publication gate"

bash tests/scripts/asset-provenance-contract-tests.sh

echo "PUBLIC_READINESS_CONTRACTS_OK version=$VERSION_VALUE state=$PUBLICATION_STATE"
