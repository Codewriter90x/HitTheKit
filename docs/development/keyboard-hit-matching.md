# Keyboard hit matching

This vertical slice connects a minimal keyboard adapter to the deterministic
Core matcher without moving timing rules into Unity:

```text
KeyboardDrumInput
→ DrumInputEvent(songPosition)
→ DrumHit
→ HitMatcher
```

## Input and timestamp contract

The default mapping covers the full kit and can be changed in Settings:

- `F` → Kick;
- `J` → Snare;
- `K` → Hi-Hat;
- `G/H/L` → Tom 1, Tom 2 and Floor Tom;
- `D/S` → Crash and Ride.

`KeyboardDrumInput` uses the legacy Unity `GetKeyDown` edge, assigns velocity
100, and reads `DspSongClock.PositionSeconds` once for all keys detected in a
frame. It neither reads Unity DSP time directly nor derives gameplay time from
frame time. `DrumInputEvent` is an immutable, Unity-independent normalized
event containing a `DrumPad`, MIDI-compatible velocity in the range 0–127, and
a finite DSP-relative song position.

`IDrumInput` exposes only `HitReceived`. This keeps the runtime matching path
independent of keyboard details. The macOS CoreMIDI adapter uses the same event
boundary and preserves native monotonic event age when polling.

Both input adapters execute before the matching component expires overdue
notes, so an input captured on the final eligible frame is evaluated before the
same note can become a Miss.

## Matching session and chart offset

`HitMatchingSession` owns one Core `HitMatcher` and a defensive collection of
the original `ChartNote` references. It converts each normalized input to a
`DrumHit`, forwards it to `TryMatch`, and delegates expired-note decisions to
`TryMarkMissed`.

`ChartTimeline` materializes each note's effective time from the authored chart
offset and selected playback speed. `CreateMatchingNotes` passes those effective
times to the session, which constructs the Core matcher with zero additional
offset so the transformation is applied exactly once:

```text
effectiveNoteTime = (chartNoteTime + offsetSeconds) / playbackSpeed
```

NoMatch means an input could not be associated with an unresolved note: for
example, the pad was wrong, the timestamp was outside the hit window, no note
was available, or the candidate was already resolved. NoMatch increments its
diagnostic counter but does not fabricate a `HitResult`.

Miss is different: it is produced only when `HitMatcher.TryMarkMissed` resolves
an expired chart note. Wrong-pad and out-of-window inputs never become Miss.
The session reuses the same note instances for hits and expiry checks, which
prevents any chart event from being resolved twice.

`HitMatchingSnapshot` is a read-only diagnostic view of the last input/result,
grade counts, NoMatch count, and resolved/total notes. A separate score tracker
derives score, combo and accuracy without changing matching decisions.

## Runtime wiring and feedback

`HitMatchingPrototype` connects the scene input, loaded chart, DSP clock, and
serialized timing windows. It creates one session after the chart is ready,
subscribes to input while enabled, and reads one DSP-derived song position per
frame when asking the Core matcher to mark expired notes.

`HitResultVisualPresenter` maps input outcomes to a short, 0.12-second pad
flash: light green for Perfect, cyan for Good, orange for Early, purple for
Late, and muted red for NoMatch. Automatic Miss results do not flash a pad.
The overlay continues to use the existing reusable `MaterialPropertyBlock`.
Its realtime duration is presentation-only; when it ends, the pad immediately
returns to the current chart-driven state. It never affects matching time.

## Running and testing

Synchronize the generated Core DLL, then open the Unity project and scene:

```bash
./scripts/sync-core-to-unity.sh
```

Open `src/HitTheKit.Unity` with Unity `6000.5.6f1`, load
`Assets/HitTheKit/Scenes/GameplayPrototype.unity`, and press Play. Use `F` for
Kick, `J` for Snare, and `K` for Hi-Hat. Pressing close to a matching chart note
shows a grade-colored flash; a wrong-pad or unassociated press shows the
NoMatch flash. The same normalized path drives judgment text, score, combo,
accuracy, and the final results screen.

EditMode tests cover normalization, mapping, offsets, grade orchestration,
duplicate resolution, expiry boundaries, snapshots, and feedback state.
PlayMode tests inject controlled `IDrumInput` events rather than physical OS
keys and verify real component wiring, matching, NoMatch recovery, automatic
Miss, feedback, and the serialized scene smoke path.

## Current limits

- multiple CoreMIDI endpoints require an explicit selection in Device Setup;
- no HAMPBACK-specific profile promotion without a second verified capture;
- MIDI gameplay is currently macOS-only;
- keyboard capture still uses Unity's legacy `GetKeyDown` API;
- preferences and progress are local-only; no cloud synchronization is present.
