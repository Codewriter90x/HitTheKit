# Clean public repository runbook

Status: **source-readiness preparation authorized; publication execution still pending exact-SHA approval**.

The existing private repository contains historical playtest tags and releases.
The preferred non-destructive publication path is a new repository initialized
from an exact, rights-clean `HEAD`, without copying `.git` history.

Because the website and documentation already use the canonical
`Codewriter90x/HitTheKit` URL, the recommended final topology is:

- rename the existing private repository to an explicitly private archive name;
- keep its history, tags, releases, and access restricted;
- create the clean public repository as `Codewriter90x/HitTheKit`; and
- repoint the local remotes deliberately, preserving a clearly named private
  archive remote when still needed.

The maintainer has selected this topology and authorized its preparation. The
rename, public-repository creation, visibility change, remote rewiring and
workflow activation remain publication actions: execute them only in a later
turn after review of this readiness PR and fresh authorization tied to the
exact source SHA.

GitHub warns that redirects can stop working when a new repository reuses the
old name. Immediately after the archive rename and public repository creation,
update every controlled clone and documentation link to explicit archive/public
remotes; do not rely on the temporary rename redirect.

## Preconditions

- every item required by [PUBLIC_RELEASE_CHECKLIST.md](PUBLIC_RELEASE_CHECKLIST.md)
  is complete or explicitly disclosed as a non-blocking limitation;
- the exact source commit is approved;
- the initial publication is source-and-website only;
- the technical rights/provenance audit and source contracts are green;
- unresolved Unity-runtime/GPL and commercial-license questions are disclosed,
  with qualified legal review still required before public binaries or a
  separate commercial license;
- the worktree is clean;
- historical releases remain private and are not linked as current downloads;
- the target owner, repository name, visibility, and maintainer identity are
  confirmed immediately before creation; and
- the user explicitly authorizes creation and publication of the new public
  repository in that turn.

## Prepare the source snapshot

```sh
./scripts/create-public-source-snapshot.sh /absolute/path/HitTheKit-source-0.5.0.tar.gz
```

Record the printed SHA-256. Extract the archive into a new temporary directory,
run the source contracts from that extracted copy, and inspect its root before
initializing Git. Never copy the private `.git` directory, tags, releases,
branches, stashes, or ignored artifacts. The snapshot script also excludes
`HITTHEKIT_HANDOFF.md` and any `HITTHEKIT_CURRENT_CONTEXT.md` file.

Before creating the first public commit, configure a public-safe Git author
identity (for example the maintainer's verified GitHub `noreply` address) in the
new repository. Verify `git log --format=fuller -1` before push so the clean
repository does not reintroduce a private email through its new history.

## Configure the new repository

Before opening access, configure:

- description, website URL, license, and project topics;
- Issues, Discussions, and the labels referenced by the issue forms;
- private vulnerability reporting;
- `main` protection with pull requests, required CI/CodeQL checks, conversation
  resolution, and no force pushes or deletions;
- least-privilege Actions permissions;
- dependency graph, Dependabot alerts and updates, secret scanning, and push
  protection when available; and
- GitHub Sponsors only after its profile is approved and the funding link is
  confirmed from a logged-out session.

After the public repository exists, rename
`.github/workflows/pages.yml.example` to `.github/workflows/pages.yml`, enable
Pages with GitHub Actions as the source, and verify the deployment before
setting the repository homepage URL. Do not activate this workflow in the
private historical archive.

Also rename `.github/workflows/source-release.yml.example` to
`.github/workflows/source-release.yml` in the public repository. Run it only
for an approved exact version, then verify the downloaded source snapshot's
SHA-256 and GitHub artifact attestation before attaching it to a release.

In the same reviewed publication commit, change `PUBLICATION_STATUS` from
`private-preparation` to `public`. The public-readiness contract deliberately
fails if active deployment workflows appear in the private state or if the
public state is missing them.

## Verification before announcement

From a logged-out browser session:

1. clone the new repository and run the documented setup;
2. verify README images, links, issue forms, license, security route, and sponsor
   route;
3. verify CI and CodeQL on the default branch;
4. verify the website canonical URL, sitemap, social image, and mobile menu;
5. verify the source snapshot SHA-256 and artifact attestation; and
6. confirm that no Unity player binary or historical release is exposed.

Gatekeeper, notarization and binary download verification remain later gates;
they must not be presented as completed during the source-only launch.

Only after these checks should the project be announced publicly. Preserve the
private repository as the restricted historical archive unless a separately
reviewed retention plan says otherwise.
