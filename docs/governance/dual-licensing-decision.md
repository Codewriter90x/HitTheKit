# Dual-Licensing Decision Record

Status: `PROPOSED_PENDING_LEGAL_REVIEW`

Date: 2026-08-05

## Decision

HitTheKit will preserve its Community edition under GNU General Public License
version 3 only (`GPL-3.0-only`) and prepare a separate commercial-license path
for parties that require proprietary or closed-source terms.

This record proposes governance and documentation. It does not execute a
commercial agreement, establish final commercial terms, or complete legal
review.

## Rationale

The GPL keeps the Community code open and grants recipients the freedoms to
use, study, modify, and share it, subject to the GPL. A copyright holder with
the necessary rights may also offer the same code under another non-exclusive
license.

Open-source commercial use remains permitted under the GPL. The commercial
license is an alternative for users who need proprietary terms, not a fee
imposed on every revenue-generating use.

“Commercial” and “proprietary” are not synonyms. Selling GPL software or using
it in a commercial activity can comply with the GPL. The alternative license
is intended for integrations or distributions that cannot meet the GPL's
copyleft conditions.

## Community edition

- The existing `LICENSE` remains complete and unmodified.
- The repository materials do not contain a project-specific “or any later
  version” election; the current grant is therefore treated as GPL-3.0-only.
- No non-commercial or personal-use-only restriction is added.
- The overview in `LICENSING.md` does not replace the GPL text.

## Commercial option

- Availability requires a separate written agreement.
- A licensor may grant only rights it owns or is authorized to license.
- The agreement cannot relicense Unity or other third-party components.
- Prices, scope, warranties, support, territory, governing law, and other terms
  remain **[TO BE AGREED]** after legal review.

## Contributions

Substantial external source contributions are paused until a legally reviewed
contribution mechanism exists. A future CLA, assignment, inbound-equals-
outbound policy with separate permissions, or another mechanism must be chosen
deliberately. This proposal does not create a CLA or imply an assignment.

## Copyright ownership

Repository history currently identifies two Git author aliases, `Baron Luca`
and `Codewriter90x`, using the same email identity. GitHub reports only
`Codewriter90x` as a contributor. The maintainer represents that AI-assisted
work was produced under Luca Baron's direction and that no substantial third-
party contribution is known.

For governance planning, ownership is `COPYRIGHT_OWNERSHIP_PROVISIONALLY_CLEAR`
and the provisional notice is Copyright © 2026 Luca Baron. Git metadata and a
maintainer representation are not a legal chain of title. Before any
commercial agreement is executed, counsel must verify legal identity,
employment or contractor obligations, contributor permissions, and authority
over every covered version.

## Trademarks

The software license does not automatically grant rights in the HitTheKit name
or branding. Forks remain permitted by the GPL; branding guidance must not
restrict GPL copyright permissions or imply a registered mark.

## Existing GPL copies and non-retroactivity

- Copies already received under the GPL continue to benefit from that license.
- Rights validly granted under the GPL are not withdrawn retroactively by this
  proposal.
- A copyright holder with the necessary rights may offer new or existing code
  under additional non-exclusive licenses.
- Dual licensing does not make GPL copies proprietary.

## Dependencies and distribution risks

The source repository can state a GPL license for original HitTheKit code, but
that does not resolve third-party terms. In particular:

- Unity is proprietary and its current terms govern Editor, package, runtime,
  player, and embedded use independently of HitTheKit licensing.
- Counsel must assess whether and how a distributed Unity player can satisfy
  both the GPL obligations for HitTheKit code and the applicable Unity terms.
- Commercial hardware or embedded distribution may require a separate grant
  from Unity in addition to a HitTheKit commercial license.
- Unity package notices, NuGet notices, and DryWetMIDI's MIT notice must be
  preserved when their distribution triggers those obligations.

No definitive GPL/Unity compatibility conclusion is made by this record.

## Risks

1. Git authorship is not proof of copyright ownership or assignment.
2. Future third-party contributions could prevent single-party relicensing.
3. Unity runtime and package terms may limit particular GPL binary distribution
   or embedded scenarios.
4. A commercial license cannot grant rights in third-party dependencies.
5. Trademark ownership, availability, and enforcement have not been verified.
6. License metadata should eventually be normalized with reviewed SPDX
   notices without changing the selected GPL variant.

## Open questions and next gates

- Confirm the licensor's legal identity and chain of title.
- Obtain professional advice on GPL-3.0-only application wording.
- Review current Unity terms against planned Community binary distribution.
- Review Unity embedded terms for hardware integrations.
- Decide and legally review the contribution agreement.
- Decide a private commercial contact channel.
- Review trademark availability and branding policy.
- Generate a release-specific third-party-notice inventory.
- Replace the commercial outline with transaction-specific counsel-drafted
  terms before execution.
