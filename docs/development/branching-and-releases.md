# Branching and release channels

HitTheKit separates active development from stable public source without
keeping duplicate permanent `beta` or `stable` branches.

## Permanent branches

| Branch | Meaning | Accepted changes |
| --- | --- | --- |
| `main` | Latest stable, releasable source state | Reviewed release candidates and urgent hotfixes |
| `develop` | Integration state for the next version | Reviewed feature, fix, documentation, and maintenance pull requests |

Both branches are protected. Pull requests, required CI and CodeQL checks,
linear history, and resolved review conversations are required. Force pushes
and deletion are disabled. The single-maintainer project currently requires
zero external approvals so that protection does not make maintenance
impossible; review evidence and passing gates are still required by the release
process.

The repository default branch remains `main`. It is the public landing point
and must not expose partially integrated work.

## Short-lived branches

Start normal work from the latest `develop` and target `develop`:

- `feature/<description>` for product or engineering capabilities;
- `fix/<description>` for defects;
- `chore/<description>` for maintenance and automation; and
- `docs/<description>` for documentation-only changes.

Delete these branches after merge. Keep each pull request focused and do not
mix unrelated changes.

## Beta and release candidate

When `develop` contains a coherent candidate, create
`release/v<major>.<minor>.<patch>` from its exact reviewed commit. Freeze feature
work on that branch: only release blockers, evidence, versioning, and release
documentation may change.

Preview milestones are immutable tags and GitHub prereleases, not permanent
branches:

- `vX.Y.Z-alpha.N` for an early integration snapshot;
- `vX.Y.Z-beta.N` for a tester-facing candidate; and
- `vX.Y.Z-rc.N` for a candidate expected to become stable if its gates pass.

Source-only prereleases must use the attested source-snapshot workflow and must
not attach Unity player binaries. A prerelease label communicates maturity; it
does not waive content, provenance, security, or release checks.

## Stable release

After the release checklist passes:

1. open a pull request from `release/vX.Y.Z` to `main`;
2. verify the exact head commit and all required checks;
3. merge with the repository's linear-history policy;
4. create the stable `vX.Y.Z` tag from the resulting `main` commit;
5. publish the approved source-only release and its checksum/attestation; and
6. merge or replay any release-only fixes into `develop` before deleting the
   release branch.

A stable tag is never moved or reused. If a published version needs a fix,
release a new patch version.

## Hotfix

Create `hotfix/vX.Y.Z` from `main` only for a defect that cannot wait for the
next normal release. Validate it through a pull request to `main`, create the
new patch tag, then merge or replay the same fix into `develop`. Never force
push a protected branch or silently leave the two permanent branches with
different fixes.

## Flow summary

```text
feature/*  fix/*  chore/*  docs/*
          └── pull request ──> develop
                                  │
                                  ├── optional alpha tag
                                  │
                                  └──> release/vX.Y.Z
                                         ├── beta / rc tags
                                         └── pull request ──> main
                                                                  └── stable vX.Y.Z

main ──> hotfix/vX.Y.Z ── pull request ──> main
                         └── replay the fix ──> develop
```

GitHub Pages continues to deploy only from `main`. CI and CodeQL run for pull
requests and after merges to both permanent branches. Unity CI remains governed
by the separate license-aware gate described in
[`unity-test-gate.md`](unity-test-gate.md).
