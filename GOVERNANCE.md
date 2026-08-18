# HitTheKit governance

HitTheKit currently uses a maintainer-led governance model. The maintainer is
accountable for project direction, releases, security coordination, licensing
boundaries, and the final decision to merge or reject a change.

## Roles

- **Maintainer:** listed in [MAINTAINERS.md](MAINTAINERS.md); owns final project
  decisions and release authorization.
- **Contributor:** submits an accepted documentation, reproduction, test, or
  other explicitly authorized contribution under the current contribution
  policy.
- **Community member:** reports issues, proposes ideas, tests releases, or
  participates in discussions without repository write access.

Roles do not create employment, agency, compensation, ownership, or a promise
of support.

## Decision process

1. Decisions start from reproducible evidence, project goals, security,
   accessibility, legal constraints, and maintenance cost.
2. Material architectural decisions are recorded under
   `docs/architecture/decisions` or an equivalent tracked decision record.
3. Public-facing behavior changes must update tests and documentation where
   applicable.
4. The maintainer may decline a technically valid change when its rights,
   provenance, maintenance burden, or product direction are unresolved.
5. Security reports follow the private process in [SECURITY.md](SECURITY.md).

## Contribution gate

Substantial external source-code and asset contributions are paused until a
professionally reviewed inbound contribution mechanism is adopted. This keeps
the intended dual-licensing path from silently becoming impossible. Bug
reports, ideas, sanitized reproductions, and documentation review remain
welcome under [CONTRIBUTING.md](CONTRIBUTING.md).

## Releases

A release requires an exact commit, passing automated checks, documented manual
validation, verified rights and notices, checksums, and honest known
limitations. The maintainer is the only release approver while this governance
model is active. See [docs/release/RELEASE_PROCESS.md](docs/release/RELEASE_PROCESS.md).

## Changes to governance

Governance changes are proposed through a pull request, explained in plain
language, and recorded in the changelog. Material licensing or contribution
changes require professional legal review before they are presented as final.

## Conduct and enforcement

All project spaces follow [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). The
maintainer may moderate content or restrict participation to protect safety,
privacy, legal compliance, and constructive collaboration.
