# HitTheKit roadmap

This roadmap communicates direction, not a delivery promise. Priorities can
change after player feedback, hardware evidence, security findings, or legal
review. A feature is considered complete only when it is implemented, tested,
documented, and represented honestly in the release notes.

## Now: public-ready 0.5.x pre-release

- complete the clean-history publication path in a new public repository;
- produce a signed and notarized macOS Apple Silicon release candidate;
- validate the candidate on a clean Mac with keyboard-only play;
- validate timing and guided mapping on at least two distinct MIDI modules;
- record accessibility checks for keyboard navigation, high contrast, and
  reduced motion;
- publish screenshots, checksums, known limitations, and reproducible release
  notes from the exact candidate commit; and
- validate the MPL-based Unity distribution, contribution, and
  commercial-licensing terms for the exact release candidate.

## Next: broader compatibility and confidence

- implement and validate MIDI backends for Windows and Linux;
- add verified device profiles only from repeatable, privacy-safe captures;
- expand audio-interface and latency testing;
- improve first-run diagnostics and calibration guidance;
- complete the second learning semester only where scoring can assess the
  promised skill honestly; and
- add release telemetry only if it can remain optional, privacy-preserving, and
  clearly documented.

## Later: community ecosystem

- rights-clean chart authoring and validation tools;
- a documented local content-pack format and community review process;
- more practice modes and original visual environments;
- wider hardware coverage; and
- sustainable maintainer and contributor governance.

## Explicitly out of scope for the first public release

- bundled commercial songs, transcriptions, album art, or recordings;
- claims of universal electronic-drum compatibility;
- online accounts, leaderboards, or mandatory telemetry;
- mobile support; and
- a stable-service or commercial-support guarantee.

Feature ideas belong in the feature-request template. Hardware observations
belong in the hardware compatibility template and must not include private or
unsanitized MIDI captures. See [CONTRIBUTING.md](CONTRIBUTING.md).
