# Unity integration

`HitTheKit.Core` targets .NET Standard 2.1 and has no dependency on
`UnityEngine`, `MonoBehaviour`, scene state, frame timing, input devices, audio,
or serialization libraries. Unity 6 uses .NET Standard 2.1 as its default API
compatibility level. The repository builds and synchronizes the core as a
managed Unity plug-in.

## Active boundary

The Unity project remains an adapter around the core:

1. Unity's DSP-backed song clock will provide timestamps in seconds.
2. Keyboard and CoreMIDI adapters translate input into normalized hit values.
3. The adapter will pass the chart's unresolved notes and each `DrumHit` to
   `HitMatcher.TryMatch`.
4. Unity visuals, scoring feedback, and effects will consume `HitResult` without
   moving matching rules into `MonoBehaviour` classes.
5. The scheduler calls `TryMarkMissed` after a note's outer timing
   window expires without an eligible hit.
6. Chart JSON loading will live outside the core and map validated data to
   `ChartNote` instances.

## Timing and resolution contract

The core applies one chart offset with the following convention:

```text
effectiveNoteTime = chartNoteTime + offsetSeconds
delta = hitTime - effectiveNoteTime
```

Positive offsets move notes later. Negative deltas are early; positive deltas
are late. `TryMatch` returns no result for wrong-pad or out-of-window hits, so
those hits do not consume notes. Only an expired, unresolved note can produce
`HitGrade.Miss` through `TryMarkMissed`.

The matcher owns in-memory resolution state for each `ChartNote` instance. A
note can produce exactly one result, either a matched grade or `Miss`. The
adapter owns the clock, chart lifetime, and invocation order; the core
owns candidate selection, timing grades, and duplicate prevention.

Resolution uses reference identity deliberately. Two distinct `ChartNote`
instances remain independent even when their time and pad values are equal.
This keeps chart events separate without adding persistent IDs. When candidates
also tie on time and pad, the matcher preserves their original list order.

## Build and test

1. Build `HitTheKit.Core` in Release mode.
2. Run `scripts/sync-core-to-unity.sh` to copy the resulting DLL into the ignored
   managed plug-in location.
3. Keep Unity-specific adapters in the Unity project and reference the core in
   one direction only.
4. Run .NET tests independently, then Unity EditMode and PlayMode tests for the
   adapter and scene layers.
