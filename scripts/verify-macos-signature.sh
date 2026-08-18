#!/bin/bash
set -euo pipefail

MODE=post-notarization
if [ "${1:-}" = "--pre-notarization" ]; then
  MODE=pre-notarization
  shift
fi
APP_PATH=${1:-}

fail() {
  echo "macOS signature verification: $*" >&2
  exit 1
}

signature_details() {
  codesign -dv --verbose=4 "$1" 2>&1
}

entitlement_details() {
  codesign -d --entitlements - "$1" 2>/dev/null
}

verify_component() {
  local component=$1 expected_team=$2 details
  codesign --verify --strict --verbose=4 "$component"
  details=$(signature_details "$component")
  printf '%s\n' "$details" | grep -q '^Authority=Developer ID Application:' ||
    fail "component is not signed with Developer ID Application: $component"
  printf '%s\n' "$details" | grep -q "^TeamIdentifier=$expected_team$" ||
    fail "component TeamIdentifier does not match the app: $component"
  printf '%s\n' "$details" | grep -q '^Runtime Version=' ||
    fail "component does not enable Hardened Runtime: $component"
  printf '%s\n' "$details" | grep -q '^Timestamp=' ||
    fail "component has no secure timestamp: $component"
  if printf '%s\n' "$details" | grep -q '^Signature=adhoc$'; then
    fail "component is ad-hoc signed: $component"
  fi
}

[ -n "$APP_PATH" ] || fail "usage: $0 [--pre-notarization] <HitTheKit.app>"
[[ "$APP_PATH" == *.app ]] || fail "input must be an .app bundle"
[ -d "$APP_PATH/Contents" ] || fail "app bundle does not exist or is invalid: $APP_PATH"

codesign --verify --deep --strict --verbose=4 "$APP_PATH"
APP_DETAILS=$(signature_details "$APP_PATH")
printf '%s\n' "$APP_DETAILS"
printf '%s\n' "$APP_DETAILS" | grep -q '^Authority=Developer ID Application:' ||
  fail "outer app is not signed with Developer ID Application"
TEAM_ID=$(printf '%s\n' "$APP_DETAILS" | sed -n 's/^TeamIdentifier=//p' | head -1)
[ -n "$TEAM_ID" ] && [ "$TEAM_ID" != "not set" ] || fail "outer app has no TeamIdentifier"
printf '%s\n' "$APP_DETAILS" | grep -q '^Runtime Version=' || fail "outer app does not enable Hardened Runtime"
printf '%s\n' "$APP_DETAILS" | grep -q '^Timestamp=' || fail "outer app has no secure timestamp"
APP_ENTITLEMENTS=$(entitlement_details "$APP_PATH")
if printf '%s\n' "$APP_ENTITLEMENTS" | awk '
  /\[Key\] com[.]apple[.]security[.]cs[.]allow-jit$/ { found = 1; next }
  found && /\[Bool\] true$/ { enabled = 1 }
  END { exit enabled ? 0 : 1 }
'; then
  :
elif printf '%s\n' "$APP_ENTITLEMENTS" | tr '\n' ' ' | grep -Eq \
  '<key>com[.]apple[.]security[.]cs[.]allow-jit</key>[[:space:]]*<true[[:space:]]*/>';
then
  :
else
  fail "outer app does not enable the required allow-jit entitlement"
fi
if printf '%s\n' "$APP_ENTITLEMENTS" | grep -Eq \
  'com[.]apple[.]security[.]cs[.](allow-unsigned-executable-memory|disable-executable-page-protection|disable-library-validation)'; then
  fail "outer app contains a broader executable-code entitlement than required"
fi

while IFS= read -r -d '' candidate; do
  if file -b "$candidate" | grep -q 'Mach-O'; then
    verify_component "$candidate" "$TEAM_ID"
  fi
done < <(find "$APP_PATH/Contents" -type f -print0)

if [ "$MODE" = "post-notarization" ]; then
  spctl --assess --type execute --verbose=4 "$APP_PATH"
else
  if spctl --assess --type execute --verbose=4 "$APP_PATH"; then
    echo "Gatekeeper accepted the pre-notarization app."
  else
    echo "Gatekeeper has not accepted the app yet; notarization is still required."
  fi
fi

echo "HITTHEKIT_MACOS_SIGNATURE_VERIFIED"
echo "teamId=$TEAM_ID"
echo "allowJit=true"
echo "mode=$MODE"
