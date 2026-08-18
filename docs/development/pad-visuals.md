# Chart-driven drum-pad visuals

This vertical slice adds a deliberately simple presentation layer to
`GameplayPrototype`. Three low 3D cylinders represent the MVP `DrumPad` values:
Kick, Snare, and Hi-Hat. Their chart-driven state consumes upcoming notes and
does not itself read player input or resolve gameplay state. A separate
keyboard-matching presenter may temporarily overlay a result-colored flash.

## Visual timing

`PadVisualStateCalculator` receives the current DSP-derived song position, the
chart look-ahead, and the already ordered upcoming timeline notes. For the
earliest note matching a pad it computes:

```text
timeUntilNote = effectiveNoteTime - songPosition
progress = 1 - (timeUntilNote / lookAhead)
intensity = clamp(progress, 0, 1)
```

At the look-ahead boundary intensity is zero, halfway through the window it is
0.5, and at the exact effective note time it is one. Once the note is elapsed,
it is absent from the upcoming collection and the pad returns immediately to
its inactive state. A zero look-ahead produces full intensity only for a note
exactly at the current song position. Multiple upcoming notes for one pad are
not combined; the earliest note wins, with timeline order retained on ties.

The calculator receives effective note times from `ChartTimeline`. It does not
apply chart offset again and has no Unity clock or rendering dependency.

## Rendering and presenter

`DrumPadVisual` applies a normalized state to one `Renderer`. It interpolates
base color and local scale and writes base/emission colors through a reusable
`MaterialPropertyBlock`. It never calls `Renderer.material`, so refreshing the
presentation does not instantiate materials or mutate the shared asset.

`PadVisualTimelinePresenter` connects the three visual components to
`ChartTimelinePrototype` and `DspSongClockPrototype`. Its `Update` method is a
presentation refresh only: song position always comes from the DSP-backed
clock, and the presenter neither accumulates frame time nor changes the chart.
Before the clock and timeline are ready, it applies inactive states.

The three scene renderers share the original `PadBase.mat` Standard material.
Per-pad active colors are serialized on the visual components: orange/red for
Kick, blue for Snare, and yellow for Hi-Hat. No textures, custom shaders, or
external art assets are used.

## Running and testing

Synchronize the generated Core DLL before opening Unity:

```bash
./scripts/sync-core-to-unity.sh
```

Open `src/HitTheKit.Unity` with Unity `6000.5.6f1`, then open
`Assets/HitTheKit/Scenes/GameplayPrototype.unity` and press Play. The generated
click track and tracked chart fixture drive the three pads.

EditMode tests cover the deterministic calculator, validation, interpolation,
scale, emission, and material-instance behavior. PlayMode tests cover live DSP
progression, elapsed-note reset, simultaneous pads, serialized scene wiring,
and the scene smoke path. Tests assert component state rather than pixels or
screenshots.

## Current limits

- no MIDI gameplay input;
- no judgment text, scoring, or combo presentation;
- no note highway or moving notes;
- no final meshes, textures, animation, camera motion, or post-processing;
- no UI or menus;
- no persistence after a note becomes elapsed.
