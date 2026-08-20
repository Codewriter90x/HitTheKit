# Contributing to HitTheKit

Thank you for helping make HitTheKit more reliable, accessible, and useful.

Before participating, read the [Code of Conduct](CODE_OF_CONDUCT.md),
[support guide](SUPPORT.md), and [project governance](GOVERNANCE.md).
HitTheKit is intended to remain available under both the open-source MPL-2.0
Community license and a possible separate commercial-license path. That model
requires clear copyright permissions for every substantial contribution.

## Current contribution policy

External source-code contributions are not currently accepted until the
project contribution agreement and review process have been finalized. Please
do not submit substantial source-code pull requests unless the project
maintainer has explicitly confirmed the applicable contribution terms in
writing.

You may still:

- report reproducible bugs with sanitized evidence;
- report electronic-drum compatibility through the hardware template;
- propose features or lesson ideas through the matching issue form;
- provide reproducible test cases that do not include substantial source code;
- review or comment on documentation; and
- submit a small documentation correction after confirming it contains only
  material you are authorized to contribute.

Do not open a pull request containing substantial code, chart data, music,
recordings, MIDI captures, branding, or visual assets unless the maintainer has
first confirmed the applicable contribution terms in writing.

## Reporting well

- Search existing issues before opening a duplicate.
- Include the exact version or commit, operating system, input type, expected
  result, actual result, and minimum reproduction.
- Use the original bundled demo when possible.
- Never attach credentials, signing material, personal data, commercial music,
  third-party chart data, or an unsanitized MIDI capture.
- Security vulnerabilities follow [SECURITY.md](SECURITY.md), never a public
  issue.

## Authorized pull requests

If the maintainer has explicitly authorized a pull request:

1. keep the change focused;
2. add or update tests and documentation for changed behavior;
3. run `dotnet test HitTheKit.sln` and
   `./scripts/check-nuget-vulnerabilities.sh`;
4. run applicable Unity EditMode and PlayMode tests, or state clearly why they
   were not run; and
5. complete every relevant item in the pull-request template.

Approval to discuss an idea is not approval to contribute substantial code or
assets. A merged change must still satisfy provenance, security, accessibility,
and release requirements.

### Branch target

Authorized feature, fix, documentation, and maintenance pull requests normally
target `develop`. The `main` branch represents the latest stable, releasable
source state and accepts only reviewed release-candidate or urgent hotfix pull
requests. Use the repository prefixes `feature/`, `fix/`, `chore/`, `docs/`, or
`hotfix/` for short-lived branches; do not create permanent `beta` or `stable`
branches.

The complete development, beta, release-candidate, stable, and hotfix workflow
is documented in
[`docs/development/branching-and-releases.md`](docs/development/branching-and-releases.md).

A Contributor License Agreement, copyright assignment, or another contribution
mechanism may be introduced after professional legal review. This document is
not a CLA, does not transfer copyright, and does not create an implied
assignment or commercial-license grant.

A Harmony-based copyright-license CLA is currently being evaluated in
[`docs/legal/CONTRIBUTOR_LICENSE_AGREEMENT_DRAFT.md`](docs/legal/CONTRIBUTOR_LICENSE_AGREEMENT_DRAFT.md),
with unresolved adoption gates recorded in
[`docs/legal/CLA_ADOPTION_CHECKLIST.md`](docs/legal/CLA_ADOPTION_CHECKLIST.md).
Both documents are non-operative drafts. Do not sign or rely on them. Opening a
pull request, issue, or discussion does not accept the draft, and no CLA
automation or electronic acceptance process is active.

Until a reviewed mechanism exists, maintainers should not merge a substantial
external code or asset contribution merely because it was submitted under the
repository's Community license. The required rights for the proposed
commercial licensing model must be confirmed separately.
