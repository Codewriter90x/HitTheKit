# Dual-Licensing Readiness Report

Audit date: 2026-08-05

Repository: `Codewriter90x/HitTheKit`

Baseline: `44d47f3acc8e6f305fb95fc8f9a0baae06c57f84`

> Historical snapshot: repository contents have changed since this baseline.
> Current visual-asset evidence is maintained in
> [`ASSET_PROVENANCE.md`](ASSET_PROVENANCE.md), and every public release still
> requires a fresh exact-commit legal and dependency review.

## Scope and limitations

This report is a preliminary engineering and repository-governance audit. It
is not legal advice, a title opinion, a dependency compatibility opinion, or
approval to execute a commercial agreement.

The audit covered the complete Git history available locally, all branches,
Git author and committer metadata, the GitHub contributors endpoint, tracked
license and policy files, tracked source and asset types, .NET/NuGet project
metadata, Unity package manifests and resolved package license metadata, and
the separate MIDI probe branch.

## Existing license

- `LICENSE` contains the complete standard GNU General Public License version
  3 text and was not modified by this governance change. A byte-for-byte
  comparison with the GNU project's official `gpl-3.0.txt` succeeded; both
  files had SHA-256
  `3972dc9744f6499f0f9b2dbf76696f2ae7ad8af9b23dde66d6af86c9dfb36986`.
- No repository-specific “or any later version” notice was found.
- Existing project documents say “GNU GPL v3” or “GPL-3.0”. The conservative
  interpretation used for this proposal is `GPL-3.0-only`.
- There was no prior `COPYING`, `NOTICE`, `AUTHORS`, `COPYRIGHT`, or
  `CONTRIBUTING.md` file.
- No source-level license headers were found. Adding SPDX headers is deferred
  to avoid a broad source diff before legal review confirms the exact grant.

Professional review should confirm that the current repository-level grant is
unambiguous and that `GPL-3.0-only` is the intended variant.

## Authors and contributors

`git shortlog -sne --all` reported:

| Git identity | Commits | Observation |
| --- | ---: | --- |
| `Baron Luca` | 11 | Substantive feature commits |
| `Codewriter90x` | 9 | Initial and squash-merge commits; same email identity as `Baron Luca` |

The GitHub contributors endpoint reported one login, `Codewriter90x`, with
eight contributions at audit time. The generic `GitHub` identity appears only
as committer metadata on GitHub squash merges, not as a source-code author. No
bot-authored or third-party-authored substantive commit was identified.

The maintainer represents for this audit that the work, including AI-assisted
work, was produced under Luca Baron's direction and that no substantial third-
party source or asset contribution is known. That representation and Git
metadata do not establish legal title by themselves.

## Provisional ownership result

Result: `COPYRIGHT_OWNERSHIP_PROVISIONALLY_CLEAR`

For purposes of preparing a legal-review package, the provisional holder is
Luca Baron and the repository's observed creation period is 2026. This result
does not confirm employment, contractor, collaborator, moral-rights, or prior-
assignment questions. Those matters must be checked before a commercial
license is offered or signed.

## External code, assets, and templates

- No tracked third-party recording, MIDI performance, transcription, chart,
  lyric, album artwork, or downloaded commercial asset was found at the audited
  baseline. The repository now contains project-directed visual artwork whose
  hashes and provisional provenance are recorded separately.
- The click track is generated at runtime; the chart fixture, material, and
  Unity primitive-based scene content are represented as project-authored or
  engine-generated content by the maintainer.
- `.gitignore` states that it is adapted from GitHub's Unity template. The
  upstream `github/gitignore` repository publishes its templates under CC0-1.0.
- Unity-generated project settings, scene serialization, `.meta` files, and
  package manifests are present. Their presence does not transfer ownership of
  Unity technology to HitTheKit.
- The separate `feature/midi-device-probe` branch documents use of
  Melanchall.DryWetMidi 8.0.3 under MIT. It remains outside `main` and outside
  the Unity runtime.

No copied source with a demonstrated license conflict was identified. This is
a repository audit, not a source-provenance forensic analysis.

## Dependency inventory

| Component | Version | License / terms found | Use | Distribution observation | Apparent status |
| --- | --- | --- | --- | --- | --- |
| Unity Editor / modules | 6000.5.6f1 / 1.0.0 | Proprietary Unity terms | Editor, engine APIs, future player runtime | No Unity binaries tracked; future builds may redistribute runtime | **Legal review required** for GPL player and embedded distribution |
| Unity Test Framework | 1.7.0 | Unity Companion License v1.4 | Tests | Registry-resolved, not tracked | No blocker identified for development use; notice duties apply if redistributed |
| Unity Custom NUnit | 2.1.0 / NUnit 3.5 base | Unity Package Distribution License v2.1 plus NUnit MIT notice | Tests | Transitive registry package, not tracked | No blocker identified for test use; preserve notices when applicable |
| .NET SDK | 8.0.411 requested | MIT plus distribution notices | Build | Not redistributed by repository | No blocker identified |
| Microsoft.NET.Test.Sdk | 17.8.0 | MIT | Tests | Restored, not tracked | No blocker identified |
| xUnit and runner | 2.5.3 | Apache-2.0 | Tests | Restored, not tracked | No blocker identified; preserve notices if redistributed |
| Melanchall.DryWetMidi | 8.0.3 | MIT | Separate MIDI diagnostic branch | Not on `main`; package can include managed/native binaries | No blocker identified from declared MIT license; audit packaged native notices before probe distribution |
| GitHub Unity `.gitignore` template | Revision not recorded | CC0-1.0 | Repository configuration | Adapted text tracked | No blocker identified |

The detailed inventory and source references are in
[`THIRD_PARTY_NOTICES.md`](../../THIRD_PARTY_NOTICES.md).

## Material legal risks and blocks

No dependency was proven incompatible for licensing the original HitTheKit
source repository under GPL-3.0-only or under a separate license from an
authorized copyright holder. The following issue is nevertheless a release
gate, not a resolved compatibility finding:

1. Unity's current terms independently regulate combining and distributing
   Unity technology, player runtimes, packages, and embedded projects. Counsel
   must evaluate the planned Community binary distribution against GPLv3 and
   the exact Unity plan and terms in force. A HitTheKit commercial license does
   not grant Unity rights.

Additional risks:

- ownership is provisional rather than supported by a signed chain-of-title
  package;
- external source contributions cannot safely be accepted under the proposed
  model without reviewed inbound permissions;
- the commercial agreement, CLA mechanism, commercial contact, trademark
  position, governing law, pricing, warranties, and support terms are unset;
- release-specific dependency and notice scans remain necessary.

## Files added or updated

- Added `LICENSING.md`.
- Added `CONTRIBUTING.md`.
- Added `COPYRIGHT.md`.
- Added `TRADEMARKS.md`.
- Added `THIRD_PARTY_NOTICES.md`.
- Added `docs/legal/COMMERCIAL-LICENSE-DRAFT.md`.
- Added `docs/governance/dual-licensing-decision.md`.
- Added this readiness report.
- Updated the README with a concise dual-licensing overview.

No gameplay, Unity source, package, assembly definition, scene, asset, or
project setting is changed. `HITTHEKIT_HANDOFF.md` and `LICENSE` remain
unchanged.

## Technical validation

- Core synchronization completed successfully.
- .NET restore completed successfully.
- .NET build completed with 0 warnings and 0 errors.
- Core tests passed 33/33.
- The host did not have the pinned .NET SDK/runtime 8.0.411 installed. The
  validation temporarily selected installed SDK 10.0.103 and used runtime
  roll-forward for the `net8.0` test host; `global.json` was restored before
  the audit and is not part of the change.
- Unity tests were not rerun because no Unity source, package, assembly,
  project setting, scene, or asset changed.

## Items for professional legal review

1. Verify Luca Baron's legal identity, chain of title, and authority to license
   every covered commit and asset.
2. Confirm the current grant is correctly expressed as GPL-3.0-only.
3. Review GPL compliance for source and binary distribution, including
   corresponding source and installation-information obligations.
4. Analyze the exact Unity Editor, runtime, package, and embedded terms against
   intended GPL and commercial distributions.
5. Draft the executable commercial license, including third-party exclusions.
6. Select and draft a contribution mechanism suitable for dual licensing.
7. Review the provisional trademark and branding policy.
8. Establish privacy-safe contact and contracting details.
9. Review release-specific third-party notices, especially any native binaries.

## Verdict

`DUAL_LICENSING_STRUCTURE_READY_FOR_LEGAL_REVIEW`

The repository is ready to present a coherent proposed structure to counsel.
It is not `legally approved`, and no commercial license or contribution
agreement should be executed from these drafts.
