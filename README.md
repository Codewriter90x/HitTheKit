<p align="center">
  Open-source electronic-drum learning meets a rhythm-game highway, progressive
  lessons, and deterministic timing.
</p>

<p align="center">
  <img alt="Version 0.5.0 pre-release" src="https://img.shields.io/badge/version-0.5.0--pre--release-ff4fa3">
  <img alt="License GPL-3.0-only" src="https://img.shields.io/badge/license-GPL--3.0--only-2fe0ff">
  <img alt="Unity 6" src="https://img.shields.io/badge/Unity-6000.5.6f1-ffffff">
  <img alt="MIDI support macOS only" src="https://img.shields.io/badge/MIDI-macOS%20CoreMIDI-f6a623">
  <a href="https://github.com/sponsors/Codewriter90x"><img alt="Sponsor HitTheKit" src="https://img.shields.io/badge/Sponsor-GitHub%20Sponsors-EA4AAA?logo=githubsponsors&logoColor=white"></a>
</p>

<p align="center">
  <img src="website/assets/images/hitthekit-readme-hero.jpg" width="100%" alt="HitTheKit electronic drum kit on a neon concert stage with a rhythm highway">
</p>

<p align="center">
  <a href="#play-learn-and-configure">Experience</a> ·
  <a href="#quick-source-setup">Build from source</a> ·
  <a href="ROADMAP.md">Roadmap</a> ·
  <a href="SUPPORT.md">Support</a> ·
  <a href="FUNDING.md">Sponsor the project</a>
</p>

HitTheKit is an open-source rhythm game for electronic drum kits. It helps
beginners understand what to hit and when through clear visual cues and short,
rewarding practice sessions instead of requiring traditional music notation.
The repository contains a playable Unity vertical slice backed by an
engine-independent, automatically tested hit-matching core.

## Play, learn, and configure

| **Play** | **Learn** | **Configure** |
| --- | --- | --- |
| Follow a perspective drum highway with timing judgments, score, combo, accuracy, pause and results. | Build coordination through a structured beginner curriculum, study speeds and mastery goals. | Discover and map an electronic kit through guided hits, review and sound-check flows. |
| Keyboard on desktop; CoreMIDI electronic drums on macOS Apple Silicon. | Twelve playable first-semester lessons plus a visible second-semester roadmap. | Deterministic simulation remains available when no supported MIDI backend is present. |

The same gameplay session supports three original visual environments—Arcade
Neon, Concert Stage and Precision Grid—without changing the chart, clock or hit
matching rules.

## Project status

> [!IMPORTANT]
> HitTheKit is an active pre-release project. Source code is available, but
> there is no approved public `0.5.0` binary yet. Historical playtest builds are
> unsupported. The first public package remains gated by signing,
> notarization, clean-machine testing, hardware validation and legal review.
> Initial publication is deliberately limited to source code and the project
> website. Historical Git history, tags, releases and Unity player binaries are
> not part of the public repository.

| Capability | Current status |
| --- | --- |
| Keyboard gameplay | macOS, Windows, and Linux |
| Electronic-drum MIDI | macOS Apple Silicon through CoreMIDI |
| Learning path | 12 playable lessons; 12 more planned |
| Included music | Original rights-clean demo only |
| Public binary | Not yet approved for distribution |

## Start here

- **Players and testers:** read [SUPPORT.md](SUPPORT.md) and the
  [current limitations](#current-limitations).
- **Developers:** follow the [quick source setup](#quick-source-setup).
- **Contributors:** read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a
  pull request; substantial external code is temporarily paused pending legal
  review of the contribution model.
- **Project direction:** see [ROADMAP.md](ROADMAP.md) and
  [GOVERNANCE.md](GOVERNANCE.md).
- **Support the project:** visit [GitHub Sponsors](https://github.com/sponsors/Codewriter90x)
  or read [FUNDING.md](FUNDING.md) for the funding boundary and current tiers.

## Quick source setup

Requirements: the .NET SDK selected by [`global.json`](global.json) and, for
the game client, Unity `6000.5.6f1`.

```shell
dotnet test HitTheKit.sln
./scripts/sync-core-to-unity.sh
```

Then open `src/HitTheKit.Unity` in Unity Hub. On macOS Apple Silicon, build the
optional CoreMIDI plug-in before testing a real electronic kit:

```shell
./scripts/build-coremidi-plugin-macos-arm64.sh
```

## Current limitations

- MIDI input is implemented only for macOS CoreMIDI; keyboard input remains
  available on the other desktop targets.
- Windows and Linux player behavior is implemented but still needs formal
  clean-machine release-candidate validation.
- Hardware compatibility is evidence-based and not universal; an unverified
  device can be mapped through the guided setup.
- The bundled catalog contains only the original `Neon Circuit` demo. Players
  must provide their own rights-cleared local content.
- `0.5.x` is a pre-release line, not a stable or supported commercial product.

See the [public-release checklist](docs/release/PUBLIC_RELEASE_CHECKLIST.md) for
the exact remaining gates.

Third-party names are used only to describe compatibility. HitTheKit is not
sponsored, endorsed or certified by Unity Technologies, Apple Inc. or any
referenced hardware manufacturer. See [NOTICE](NOTICE),
[TRADEMARKS.md](TRADEMARKS.md) and
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Engine-independent core

The timing and hit-matching domain is implemented as a `netstandard2.1` class
library so it can be tested without Unity while the Unity 6 runtime consumes the
same deterministic implementation.

```text
src/HitTheKit.Core/             Domain models and hit matcher
tests/HitTheKit.Core.Tests/     Automated timing tests
examples/chart.json             Minimal engine-independent chart
```

Run the tests with:

```shell
dotnet test HitTheKit.sln
./scripts/check-nuget-vulnerabilities.sh
```

See [Unity integration](docs/architecture/unity-integration.md) for the active
boundary between the core and the Unity project.

## Unity bootstrap

The Unity project is located at `src/HitTheKit.Unity` and uses Unity
`6000.5.6f1`. Batch verification was performed with the Apple Silicon Editor.
Install that Editor version and a .NET SDK compatible with `global.json`.

Before opening the project, build and synchronize the generated core assembly:

```shell
./scripts/sync-core-to-unity.sh
```

For macOS CoreMIDI gameplay, also build the ABI v2 native plug-in (an older
generated plug-in is rejected safely):

```shell
./scripts/build-coremidi-plugin-macos-arm64.sh
```

On Windows PowerShell, run:

```powershell
./scripts/sync-core-to-unity.ps1
```

Then open `src/HitTheKit.Unity` in Unity Hub. The generated
`Assets/Plugins/HitTheKit.Core/HitTheKit.Core.dll` is intentionally ignored by
Git. Unity EditMode tests verify the timing settings conversion and call the
real core matcher through that DLL. `HitTheKit.Core` remains independent of
Unity. On macOS the gameplay scene can also consume the CoreMIDI runtime through
the same normalized input boundary as the keyboard; zero or ambiguous devices
remain a safe keyboard-only state.

## Stage Command main menu

The Unity application opens through an original real-time 3D concert stage:
the drum kit sits in the foreground, six audience rows face it from beyond the
riser, and truss lights, video walls, speaker stacks, haze, and subtle movement
create venue depth behind the UI. The same scene reacts to Arcade Neon,
Concert Stage, and Precision Grid settings and supports Reduced Motion. Its
Blender source and procedural generator are versioned alongside the imported
FBX; the former static artwork remains only as a safe fallback. The menu keeps
three clear destinations: play the drum highway, enter the guided learning
path, or configure an electronic drum kit. It is bilingual and keyboard
navigable, and both Gameplay and Device Setup provide a route back to it. See
the [menu flow](docs/design/main-menu-stage-command.md) and
[3D stage design](docs/design/main-menu-3d-stage.md).

The **Impara** destination now presents a school-style curriculum of 24 lessons
across six modules. Its first semester is already playable: 12 progressive
lessons move from a 64 BPM kick pulse through backbeat, timekeeping, rudimental
coordination, fills, and a first sixteenth-note groove. Each lesson exposes a
learning objective, sticking or groove pattern, suggested practice duration,
three study speeds, and an end-of-module assessment. Passing requires 80%
accuracy at full speed; 90% marks mastery. The second semester remains visibly
planned rather than pretending to assess dynamics, sight-reading, or
improvisation before those capabilities exist. See the
[drum-school learning path](docs/design/beginner-learning-path.md).

The **Gioca** destination opens a full-screen setlist before the shared gameplay
scene. Entries are discovered from `Songs/<song>/song.json` folders, with a
rights-clean bundled demo and a player-owned folder under Unity's persistent
data path. `Neon Circuit` is immediately playable; `Local Song Example` shows
the import boundary without naming or shipping third-party music. See the
[folder-based song library](docs/development/song-library.md).

Standalone builds persist lesson bests, completed-run results, and active
practice time. The Settings panel reports the accumulated training duration
and can export or import a versioned JSON backup for moving progress to another
Mac. Countdown, pause, menus, results, and background time are excluded. See
the [player-progress persistence contract](docs/architecture/player-progress-persistence.md).

## DSP song-clock prototype

The `GameplayPrototype` scene schedules the original 16-second “Neon Circuit”
demo accompaniment, generated entirely in memory, and exposes its position on
Unity's DSP timeline. Run the
Core synchronization script above, open `src/HitTheKit.Unity` in Unity
`6000.5.6f1`, open `Assets/HitTheKit/Scenes/GameplayPrototype.unity`, and press
Play to hear the complete eight-bar demo. The accompaniment intentionally has
no pre-rendered drum performance: drums are produced by player input, while a
short muted cue identifies wrong or missed hits. No audio file is stored in the repository.

This validates audio scheduling independently of player input and rendering.
See
[`docs/development/dsp-song-clock.md`](docs/development/dsp-song-clock.md) for
the timing contract and test commands.

## Versioned chart timeline

The same scene loads the tracked `neon-circuit-demo-chart.json` TextAsset, validates
the version 1 schema, and queries upcoming and elapsed notes against the DSP
song position. See [`docs/development/chart-timeline.md`](docs/development/chart-timeline.md)
for schema, timing boundaries, validation rules, and test instructions.

## Selectable gameplay highway

`GameplayPrototype` now turns the chart, DSP clock, and deterministic matcher
into a complete rhythm-game highway. Eight readable targets cover Hi-Hat,
Snare, two rack toms, Floor Tom, Crash, Ride, and a separate full-width
Kick / Grancassa track. The same live gameplay can be viewed as Arcade Neon,
Concert Stage, or Precision Grid; use the on-screen selector or keys `1`–`3`
without restarting the song. All three directions are original HitTheKit art
and UI, not copied assets from another rhythm game. See the
[gameplay highway design](docs/design/gameplay-highway-themes.md).

The playable vertical slice now includes a countdown, keyboard and CoreMIDI
input, judgments, score, combo, accuracy, pause/restart, and a final rank/results
screen. The generic GM mapping is a runtime fallback only and does not promote
the HAMPBACK candidate profile.

## Chart-driven pad visuals

The legacy prototype layer still presents three simple 3D pads for Kick, Snare,
and Hi-Hat beneath the new UI. Their color, emission, and scale are driven exclusively by upcoming
chart notes: intensity increases as each effective note time approaches and
returns to inactive as soon as the note is elapsed. The rendering uses a
shared Built-in Pipeline material with per-renderer `MaterialPropertyBlock`
values; it does not create material instances every frame.

Run the Core synchronization script, open the scene, and press Play to see the
generated fixture drive the pads. See
[`docs/development/pad-visuals.md`](docs/development/pad-visuals.md) for the
visual timing contract and test coverage.

## Keyboard hit-matching prototype

In Play mode, the keyboard supports the complete visual kit: `F` Kick, `J`
Snare, `K` Hi-Hat, `G/H` rack toms, `L` Floor Tom, `D` Crash, and `S` Ride. The
keyboard adapter timestamps each key-down against the DSP-backed song position
and sends the normalized hit through the existing Core `HitMatcher`. Correctly
timed hits briefly color the associated pad; unmatched input has a muted red
flash, while expired notes are resolved as Miss without judgment text.

Gameplay includes score, combo, per-hit early/late diagnostics, separate
keyboard/MIDI calibration, persistent rebinding, generated pad feedback and
CoreMIDI input on macOS. An optional persisted metronome follows the effective
session BPM. After a completed session, the result screen suggests both a
timing correction and the weakest kit piece to practice. See
[`docs/development/keyboard-hit-matching.md`](docs/development/keyboard-hit-matching.md)
for the runtime contract, offset handling, diagnostics, and test instructions.

### Matcher semantics

`HitMatcher.TryMatch` returns `false` when a hit has no eligible note. Wrong-pad
and out-of-window hits do not resolve any note. `HitGrade.Miss` is emitted only
when `TryMarkMissed` resolves an expired note.

Timing uses this convention:

```text
effectiveNoteTime = chartNoteTime + offsetSeconds
delta = hitTime - effectiveNoteTime
```

Before matching, a saved source-specific calibration is applied as
`calibratedHitTime = rawHitTime - inputOffsetSeconds`; positive values therefore
compensate input that consistently arrives late. Keyboard and MIDI values are
stored independently.

A negative delta is early and a positive delta is late. Among unresolved notes
on the hit pad and inside the hit window, the matcher selects the smallest
absolute delta; a tie selects the earlier note. If time and pad are also equal,
the original list order is preserved.

Chart JSON uses stable lower-camel-case pad identifiers: `kick`, `snare`,
`hiHat`, `tom1`, `tom2`, `floorTom`, `crash`, and `ride`. The Unity loader maps
these identifiers explicitly; the core does not perform serialization.

## Electronic-drum mapping foundation

The Unity runtime now contains a device-independent foundation for describing
electronic kits as versioned profiles and mapping raw MIDI-shaped data to
logical kit pieces and articulations. A generic General MIDI profile provides
an explicitly provisional starting point, and a pure C# wizard can build a
separate user configuration for unknown or customized kits.

On macOS, Unity opens CoreMIDI ports through the native HitTheKit plugin and the
guided configuration screen discovers a device, lets the player choose a kit
layout, illuminates each requested piece, captures repeated hits and persists
the resulting mapping. Keyboard-only fallback remains available. Windows and
Linux MIDI backends are not implemented, and no HAMPBACK model is claimed as
fully supported; device profiles expand only from verified captures. See
[`docs/architecture/electronic-drum-profiles.md`](docs/architecture/electronic-drum-profiles.md)
and [`docs/development/kit-mapping-wizard.md`](docs/development/kit-mapping-wizard.md).

## Player data and project governance

HitTheKit stores preferences, mappings and progress locally and does not
configure application telemetry. Read [PRIVACY.md](PRIVACY.md),
[SECURITY.md](SECURITY.md), [CONTRIBUTING.md](CONTRIBUTING.md) and the
[asset provenance register](docs/legal/ASSET_PROVENANCE.md). Original project
code is offered under GPL-3.0-only; dependencies and the Unity runtime retain
their own terms as recorded in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Portable MIDI capture

`HitTheKit.MidiCapture` is a self-contained macOS Apple Silicon command-line
tool for collecting raw electronic-drum MIDI evidence on a lightweight Mac
without Unity or an installed .NET runtime. It supports device diagnostics,
live monitoring, resilient capture, guided exploratory steps, summaries,
SHA-256 verification, deterministic ZIP packaging, and synthetic replay tests.

The tool does not generate a device profile automatically and DryWetMIDI is not
part of the Unity runtime. See
[`docs/development/portable-midi-capture.md`](docs/development/portable-midi-capture.md)
for publishing, MacBook-to-Mac-Studio transfer, privacy, and Gatekeeper notes.

### HAMPBACK exploratory mapping

The first integrity-verified HAMPBACK exploratory capture has been analyzed
into a non-production candidate profile. It preserves per-step evidence,
confidence, conflicts, and targeted recapture requirements. It is not
auto-selected and does not claim definitive HAMPBACK support. Capture #2 is
required to resolve Ride, choke, and continuous Hi-Hat ambiguities. See the
[evidence report](docs/hardware/hampback/capture-001-evidence-report.md) and
[targeted recapture plan](docs/hardware/hampback/capture-002-plan.md).

## Device Setup UI

`DeviceSetupPrototype` provides the UI Toolkit experience for selecting an
electronic-drum device, choosing a kit structure, running the guided mapping
wizard, reviewing conflicts, and testing a resulting configuration. On macOS,
the same interfaces can consume real CoreMIDI discovery and capture; clean
checkouts and non-macOS development retain deterministic simulation and
keyboard fallback.

The HAMPBACK exploratory profile is visible only as a Candidate: it remains
not verified, not production-ready, not auto-selectable, and requires explicit
confirmation. Real MIDI input is optional, macOS-only, and still under MVP
validation. See the
[UI design](docs/design/device-setup-ui.md) and
[simulation guide](docs/development/device-setup-simulation.md).

## Gameplay environments

The playable highway includes three complete presentation environments:
Arcade Neon, Concert Stage and Precision Grid. They use distinct stage art,
perspective, note shapes and target composition while preserving the same
chart, timing, keyboard/CoreMIDI input and scoring session. Switch between
them from the main-menu settings; the selected environment is persisted and
captured by the next gameplay session. See the
[gameplay environment design](docs/design/gameplay-environments.md).

## macOS CoreMIDI runtime

An arm64 native plug-in provides macOS CoreMIDI discovery and timestamped input
to guided setup and gameplay. It feeds a bounded queue that Unity polls on its
main thread through the normalized input boundary. The generated plug-in is
optional and ignored by Git; clean checkouts keep the deterministic simulated
backend and keyboard fallback. This does not claim universal electronic-drum
support. See the [runtime boundary](docs/architecture/macos-coremidi-runtime.md),
[build guide](docs/development/coremidi-plugin-build.md), and [hardware smoke](docs/development/coremidi-hardware-smoke.md).

## macOS playtest package

An Apple-silicon standalone build can be created without committing generated
artifacts:

```bash
./scripts/package-game-macos-arm64.sh 0.1.0
```

The resulting ZIP contains the complete game flow and CoreMIDI plug-in and does
not require Unity on the destination Mac. See the
[macOS playtest packaging guide](docs/development/macos-playtest-package.md) for
validation and Gatekeeper notes.

## Windows x64 playtest package

The release Mac can also create a keyboard-only Windows x64 playtest when Unity
`6000.5.6f1` has Windows Build Support (Mono) installed:

```bash
./scripts/package-game-windows-x64.sh 0.5.0
```

The package excludes the macOS CoreMIDI plug-in and states that MIDI is not yet
implemented on Windows. It is currently unsigned and therefore intended for
controlled testing, not public distribution. See the
[Windows packaging and validation guide](docs/development/windows-playtest-package.md).

## Licensing

HitTheKit's original source code is currently available under GPL-3.0-only. A
possible dual-licensing model is being evaluated:

- **Community:** GNU General Public License version 3 only (GPL-3.0-only).
- **Commercial:** separate licensing is a future proposal for proprietary and
  closed-source integrations that require terms incompatible with the GPL. It
  is not currently offered or granted.

Personal, educational, and commercial use is allowed under the GPL when its
terms are respected. No separate commercial license is currently available.
See [`LICENSING.md`](LICENSING.md) for the review status and intended boundary.
