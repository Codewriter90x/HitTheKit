# HitTheKit documentation

This directory is the canonical technical-documentation hub for HitTheKit.
Documentation is versioned with the code so architectural and behavioral
changes can be reviewed in the same pull request that introduces them.

The [GitHub Wiki](https://github.com/Codewriter90x/HitTheKit/wiki) is the public,
beginner-friendly handbook. Its pages are generated from the versioned sources
in [`docs/wiki`](wiki/README.md). This hub remains canonical for deeper
architecture and development material. That separation keeps the Wiki useful
without creating an unreviewed second source of truth.

## Start here

- [Root README](../README.md) — project overview and source setup
- [Contributing](../CONTRIBUTING.md) — contribution workflow and current limits
- [Support](../SUPPORT.md) — supported environments and diagnostic information
- [Roadmap](../ROADMAP.md) — current priorities and public milestones
- [Security](../SECURITY.md) — private vulnerability reporting
- [Public Wiki sources](wiki/README.md) — reviewed user-facing handbook

## Architecture

- [Unity integration](architecture/unity-integration.md)
- [Gameplay session model](architecture/gameplay-session-model.md)
- [Player progress persistence](architecture/player-progress-persistence.md)
- [Electronic-drum profiles](architecture/electronic-drum-profiles.md)
- [Device Setup boundaries](architecture/device-setup-boundaries.md)
- [Capture-to-profile pipeline](architecture/capture-to-profile-pipeline.md)
- [macOS CoreMIDI runtime](architecture/macos-coremidi-runtime.md)

## Product and interaction design

- [Beginner learning path](design/beginner-learning-path.md)
- [Gameplay environments](design/gameplay-environments.md)
- [Gameplay highway themes](design/gameplay-highway-themes.md)
- [Main-menu flow](design/main-menu-stage-command.md)
- [Main-menu 3D stage](design/main-menu-3d-stage.md)
- [Device Setup UI](design/device-setup-ui.md)
- [Kit configuration flow](design/kit-configuration-flow.md)

## Development and testing

- [Branching and release channels](development/branching-and-releases.md)
- [DSP song clock](development/dsp-song-clock.md)
- [Chart timeline](development/chart-timeline.md)
- [Practice Lab](development/practice-lab.md)
- [Chart Creator foundation](development/chart-creator.md)
- [Performance error map](development/performance-error-map.md)
- [Demo-song vertical slice](development/demo-song-vertical-slice.md)
- [Keyboard hit matching](development/keyboard-hit-matching.md)
- [Pad visuals](development/pad-visuals.md)
- [Song library](development/song-library.md)
- [Kit-mapping wizard](development/kit-mapping-wizard.md)
- [Device Setup simulation](development/device-setup-simulation.md)
- [Portable MIDI capture](development/portable-midi-capture.md)
- [CoreMIDI plug-in build](development/coremidi-plugin-build.md)
- [CoreMIDI hardware smoke test](development/coremidi-hardware-smoke.md)
- [Unity EditMode/PlayMode gate](development/unity-test-gate.md)
- [URP migration](development/urp-migration.md)

## Packaging and release

- [Public release checklist](release/PUBLIC_RELEASE_CHECKLIST.md)
- [Publication runbook](release/PUBLICATION_RUNBOOK.md)
- [Release process](release/RELEASE_PROCESS.md)
- [0.5.0 release-notes draft](release/0.5.0-release-notes-draft.md)
- [macOS playtest package](development/macos-playtest-package.md)
- [macOS signing and notarization](development/macos-signing-notarization.md)
- [Windows playtest package](development/windows-playtest-package.md)

## Rights and governance

- [Asset provenance](legal/ASSET_PROVENANCE.md)
- [Contributor License Agreement draft](legal/CONTRIBUTOR_LICENSE_AGREEMENT_DRAFT.md)
- [CLA adoption checklist](legal/CLA_ADOPTION_CHECKLIST.md)
- [Dual-licensing readiness report](legal/dual-licensing-readiness-report.md)
- [MPL Community license decision](governance/mpl-community-license-decision.md)
- [Historical GPL dual-licensing proposal](governance/dual-licensing-decision.md)
- [Commercial-license draft](legal/COMMERCIAL-LICENSE-DRAFT.md)
- [Project governance](../GOVERNANCE.md)
- [License](../LICENSE)
- [Licensing overview](../LICENSING.md)
- [Third-party notices](../THIRD_PARTY_NOTICES.md)
- [Trademarks](../TRADEMARKS.md)

## Community and launch material

- [Launch kit](launch-kit/README.md)
- [Community post draft](launch-kit/community-post.md)
- [LinkedIn post draft](launch-kit/linkedin-post.md)
- [GitHub Discussions](https://github.com/Codewriter90x/HitTheKit/discussions)
- [Issue templates](https://github.com/Codewriter90x/HitTheKit/issues/new/choose)

If a guide is missing or inaccurate, open a documentation issue or propose a
small pull request. Avoid duplicating the same instructions in the root README,
the website and this hub; link to the canonical guide instead.
