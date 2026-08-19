#!/bin/sh
set -eu

REPOSITORY_ROOT=$(CDPATH= cd -- "$(dirname -- "$0")/../.." && pwd)
cd "$REPOSITORY_ROOT"

fail() {
  echo "website localization contract: $*" >&2
  exit 1
}

ITALIAN_PAGE=website/index.html
ENGLISH_PAGE=website/en/index.html
SCRIPT=website/assets/js/site.js
SITEMAP=website/sitemap.xml

for path in "$ITALIAN_PAGE" "$ENGLISH_PAGE" "$SCRIPT" "$SITEMAP"; do
  [ -s "$path" ] || fail "required file is missing or empty: $path"
done

grep -Fq '<html lang="it" data-locale="it">' "$ITALIAN_PAGE" ||
  fail "Italian page must declare the Italian locale"
grep -Fq '<html lang="en" data-locale="en">' "$ENGLISH_PAGE" ||
  fail "English page must declare the English locale"

for page in "$ITALIAN_PAGE" "$ENGLISH_PAGE"; do
  grep -Fq 'hreflang="it" href="https://codewriter90x.github.io/HitTheKit/"' "$page" ||
    fail "$page is missing the Italian alternate"
  grep -Fq 'hreflang="en" href="https://codewriter90x.github.io/HitTheKit/en/"' "$page" ||
    fail "$page is missing the English alternate"
  grep -Fq 'hreflang="x-default" href="https://codewriter90x.github.io/HitTheKit/en/"' "$page" ||
    fail "$page is missing the global default alternate"
done

grep -Fq 'rel="canonical" href="https://codewriter90x.github.io/HitTheKit/"' "$ITALIAN_PAGE" ||
  fail "Italian canonical URL is incorrect"
grep -Fq 'rel="canonical" href="https://codewriter90x.github.io/HitTheKit/en/"' "$ENGLISH_PAGE" ||
  fail "English canonical URL is incorrect"

grep -Fq 'src="../assets/js/site.js"' "$ENGLISH_PAGE" ||
  fail "English page does not load the shared script from its locale directory"
grep -Fq 'href="../assets/css/styles.css"' "$ENGLISH_PAGE" ||
  fail "English page does not load the shared stylesheet from its locale directory"

grep -Fq 'getBrowserLanguage' "$SCRIPT" ||
  fail "browser-language detection is missing"
grep -Fq 'window.location.replace(routeForLanguage("en"))' "$SCRIPT" ||
  fail "the first-visit English route is missing"
grep -Fq 'saveLanguage(language)' "$SCRIPT" ||
  fail "the explicit language preference is not persisted"

ITALIAN_SITEMAP_COUNT=$(grep -Fc '<loc>https://codewriter90x.github.io/HitTheKit/</loc>' "$SITEMAP")
ENGLISH_SITEMAP_COUNT=$(grep -Fc '<loc>https://codewriter90x.github.io/HitTheKit/en/</loc>' "$SITEMAP")
[ "$ITALIAN_SITEMAP_COUNT" -eq 1 ] || fail "sitemap must contain the Italian URL exactly once"
[ "$ENGLISH_SITEMAP_COUNT" -eq 1 ] || fail "sitemap must contain the English URL exactly once"

README_LINES=$(wc -l < README.md | tr -d '[:space:]')
[ "$README_LINES" -le 220 ] ||
  fail "root README should remain a concise entry point (found $README_LINES lines)"
grep -Fq '[documentation hub](docs/README.md)' README.md ||
  fail "root README must link to the canonical documentation hub"

echo "WEBSITE_LOCALIZATION_CONTRACTS_OK locales=it,en readme_lines=$README_LINES"
