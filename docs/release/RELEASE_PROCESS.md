# Release process

This process separates source readiness, gameplay validation, legal approval,
and public distribution. Passing an early stage does not imply that later
stages passed.

Release candidates follow the branch and tag lifecycle defined in
[`branching-and-releases.md`](../development/branching-and-releases.md). In
particular, normal work integrates through `develop`, while stable release tags
are created only from reviewed commits on `main`.

## 1. Freeze an exact candidate

1. Choose an exact commit on a clean worktree.
2. Confirm that `VERSION`, Unity `bundleVersion`, changelog, and draft release
   notes identify the same version.
3. Stop feature work on the candidate; only reviewed release blockers may
   change it.
4. Record the commit SHA in the release evidence.

## 2. Run source and packaging contracts

From the repository root:

```sh
dotnet test HitTheKit.sln --configuration Release
./scripts/check-nuget-vulnerabilities.sh
bash tests/scripts/macos-signing-foundation-tests.sh
bash tests/scripts/public-readiness-contract-tests.sh
git diff --check
```

Any skipped command is an unresolved gate and must be stated in the draft
release notes.

## 3. Run Unity validation

Use Unity `6000.5.6f1` and the exact candidate commit:

The canonical local commands and the relationship between automated and manual
evidence are documented in
[`unity-test-gate.md`](../development/unity-test-gate.md). Automated Unity jobs
remain disabled until the repository variable `UNITY_CI_ENABLED` is explicitly
set to `true` after valid license secrets have been installed. Pull requests
from forks must provide maintainer-generated manual evidence because repository
secrets are intentionally unavailable to untrusted fork workflows.

1. synchronize the core assembly;
2. build the current CoreMIDI plug-in on macOS;
3. run all Unity EditMode tests;
4. run all Unity PlayMode tests;
5. build the release candidate; and
6. perform the keyboard, navigation, calibration, pause, results, persistence,
   and reduced-motion smoke flows.

Record Editor version, OS version, architecture, test totals, and failures.

When Windows Build Support (Mono) is installed on the release Mac, also create
the keyboard-only Windows x64 candidate:

```sh
./scripts/package-game-windows-x64.sh 0.5.0
```

This unsigned playtest is evidence for Windows validation, not a public release
artifact. Test it on clean Windows 10 and Windows 11, then complete the selected
trusted signing or Microsoft Store path before publication.

## 4. Validate hardware and accessibility

The first public release requires the evidence listed in
[PUBLIC_RELEASE_CHECKLIST.md](PUBLIC_RELEASE_CHECKLIST.md), including two
distinct MIDI modules, two audio-interface paths at the documented buffer, a
clean Mac, keyboard-only navigation, high contrast, and reduced motion.
Hardware observations must not be generalized beyond the tested models.

## 5. Complete rights and security review

1. confirm the bundled catalog contains only authorized original material;
2. refresh asset provenance and third-party notices from the exact candidate;
3. review dependency and CodeQL results;
4. obtain the required professional GPL/Unity and contribution-model review;
5. confirm no secrets, certificates, private captures, or personal data are in
   the candidate; and
6. decide the clean-history public repository boundary.

## 6. Build the distribution

Signing credentials remain outside Git. On the authorized release Mac:

```sh
./scripts/build-sign-notarize-macos.sh 0.5.0
```

The pipeline must finish with `HITTHEKIT_MACOS_DISTRIBUTION_READY`. Verify the
stapled application and each archive again after copying to the clean test Mac.

## 7. Prepare evidence

The release record must include:

- exact commit SHA and version;
- platforms and hardware actually tested;
- automated and Unity test results;
- known limitations;
- SHA-256 for every distributed artifact;
- signing, notarization, Gatekeeper, and smoke-test result;
- refreshed notices and provenance; and
- screenshots or video captured from the candidate itself.

In the public repository, dispatch `.github/workflows/source-release.yml` for
the approved exact version to create an attested, rights-clean source snapshot.
The artifact contains the snapshot, `SHA256SUMS`, and
`RELEASE-EVIDENCE.txt`. Verify the downloaded artifact with:

```sh
shasum -a 256 -c SHA256SUMS
gh attestation verify HitTheKit-source-0.5.0.tar.gz -R Codewriter90x/HitTheKit
```

The attestation supplements SHA-256 and review evidence; it does not replace
legal, content, or binary validation.

## 8. Publish only after approval

The clean public repository and source-only preview are already active. Follow
[PUBLICATION_RUNBOOK.md](PUBLICATION_RUNBOOK.md), publish the approved source
snapshot, and immediately verify links, checksums, issue forms, security
reporting, and branch protection from an unauthenticated session. Public Unity
binaries remain a separate, later gate.

If any claim cannot be supported by evidence, weaken or remove the claim; do
not waive the gate silently.
