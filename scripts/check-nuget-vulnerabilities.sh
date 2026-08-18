#!/bin/sh
set -eu

SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
REPOSITORY_ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)

if ! command -v jq >/dev/null 2>&1; then
  echo "jq is required to evaluate the NuGet vulnerability report." >&2
  exit 2
fi

REPORT=$(dotnet list "$REPOSITORY_ROOT/HitTheKit.sln" package \
  --vulnerable --include-transitive --format json)

if ! printf '%s\n' "$REPORT" | jq -e \
  '[.. | objects | .vulnerabilities? // empty | .[]] | length == 0' >/dev/null; then
  echo "NuGet vulnerability audit failed:" >&2
  printf '%s\n' "$REPORT" >&2
  exit 1
fi

echo "NuGet vulnerability audit passed."
