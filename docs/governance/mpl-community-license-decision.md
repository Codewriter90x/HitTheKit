# MPL Community License Decision

Status: `ADOPTED`

Decision date: 2026-08-19

## Decision

HitTheKit changes the Community license for original project source code from
`GPL-3.0-only` to the Mozilla Public License 2.0 (`MPL-2.0`) beginning with
the repository version that merges this decision.

The project retains a possible separate commercial-license path for recipients
who need terms different from MPL-2.0. A commercial license is not granted by
this repository and requires a separate written agreement.

## Scope

Unless a file or directory declares different terms, MPL-2.0 applies to
original HitTheKit source code distributed with the effective repository
version. It does not relicense:

- Unity Editor, Unity Runtime, Unity packages, or Unity-generated technology;
- third-party dependencies or their bundled notices;
- player-owned songs, recordings, charts, MIDI data, or other local content;
- third-party trademarks or compatibility names; or
- HitTheKit branding and trademark rights.

The exact candidate dependency and asset inventories still control binary
release readiness.

## Reason

MPL-2.0 preserves source-level reciprocity for modified HitTheKit files while
allowing those files to be combined in a larger work with separately licensed
software. That model fits a Unity application better than whole-work GPL
copyleft: the proprietary Unity Runtime can retain its own terms while covered
HitTheKit source remains available under MPL-2.0.

The change removes the identified GPL whole-work compatibility concern; it does
not alone approve macOS or Windows binaries. Signing, notarization, platform
testing, dependency notices, content provenance, Unity plan eligibility, and
the applicable Unity distribution terms remain independent release gates.

## Existing GPL copies

Licenses already granted are not revoked. Anyone who validly received an older
HitTheKit copy under GPL-3.0-only may continue to exercise those GPL rights for
that copy. The maintainer does not represent that downstream GPL copies have
been converted to MPL.

## Commercial option

Commercial use is allowed by MPL-2.0 and does not by itself require payment.
The separate commercial option is intended for needs such as:

- proprietary modifications to MPL-covered files;
- private-label or white-label distribution;
- proprietary hardware and platform integrations; or
- separately negotiated warranty, support, or maintenance.

Prices, warranties, support, territory, governing law, and transaction terms
remain to be agreed in writing. HitTheKit can license only rights controlled by
the relevant copyright holder.

## Contributions

MPL-2.0 does not automatically give the maintainer permission to offer another
contributor's work under a proprietary commercial license. Substantial external
contributions remain paused until a reviewed CLA, assignment, dual-license
grant, or equivalent mechanism is adopted.

The repository history identifies the maintainer through the `Baron Luca` and
`Codewriter90x` aliases, plus automated dependency updates. By merging this
decision, the maintainer represents that they are authorized to offer the
original project material in the effective repository version under MPL-2.0.
That representation is not a professional chain-of-title opinion.

## Implementation

- replace the root `LICENSE` with the unmodified official MPL-2.0 text;
- identify the Community grant as MPL-2.0 in README, NOTICE, website, wiki, UI,
  packaging, release, contribution, and governance material;
- keep third-party licenses and trademark guidance separate;
- retain the earlier GPL audit and decision as explicitly superseded historical
  records; and
- validate every future executable from its exact source and dependency set.

## Review boundary

This record is an engineering and project-governance decision, not legal advice
or a guarantee that a particular distribution satisfies every applicable
contract or law.
