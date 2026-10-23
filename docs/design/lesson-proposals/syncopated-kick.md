# Lesson proposal: Syncopated kick

Status: proposal (#15) · Target: lesson 15, module 4 "Control and dynamics"

An original, timing-only lesson that fleshes out the planned `SyncopatedKick`
entry in `GameplayLearningPath`. Unlike the ghost-note lesson, its defining
skill is placement in time, which the current matcher already grades
faithfully, so it can become playable without new scoring features.

## Learning objective

Move the kick off the main beats while the hands keep steady time: the player
places kick strokes on the "and" of 2 and on the last sixteenth of beat 3
without dragging the snare backbeat or rushing the hi-hat.

New player wording:

- EN: "Keep the hi-hat and snare steady while the kick moves between the beats."
- IT: "Tieni fermi charleston e rullante mentre la grancassa si sposta fra i
  movimenti."

## Kit pieces

Kick, snare, closed hi-hat, crash (final downbeat only). All four map to
existing `DrumPad` values; no articulation or velocity is graded.

## Tempo

| Use | BPM |
| --- | --- |
| Study (`0.5x` / `0.75x` of target) | 39 / 58.5 |
| Target (`1.0x`) | 78 |
| Comfortable range for the harder variation | 72–84 |

78 BPM matches the neighbouring module-4 lessons and stays below lesson 10/11
(80 BPM), because the new subdivision is harder than the tempo.

## Pattern

Eight bars of 4/4. Beats are counted `1 e & a 2 e & a …`; positions below are
in beats from the start of the bar, as used by `GameplayLessonChartBuilder`.

| Bars | Hi-hat | Snare | Kick | Extra |
| --- | --- | --- | --- | --- |
| 1–2 (anchor) | eighths `0, .5 … 3.5` | `1, 3` | `0, 2` | — |
| 3–4 (eighth syncopation) | eighths | `1, 3` | `0, 1.5, 2` | — |
| 5–7 (full figure) | eighths | `1, 3` | `0, 1.5, 2, 2.75` | — |
| 8 (ending) | eighths, except beat 0 | `1, 3` | `0, 1.5, 2, 2.75` | crash on `0` |

The kick on `2.75` (the "a" of 3) falls between two hi-hat strokes, so it is
the moment the player has to hear the sixteenth grid rather than follow the
hands. The two anchor bars let the player settle before the first syncopation.

Proposed `AddBar` case (sketch, same helpers as the existing lessons):

```csharp
case GameplayLessonId.SyncopatedKick:
    for (int eighth = 0; eighth < 8; eighth++)
        if (!(bar == bars - 1 && eighth == 0)) Add(notes, start, eighth / 2.0, "hiHat");
    Add(notes, start, 1, "snare"); Add(notes, start, 3, "snare");
    Add(notes, start, 0, "kick"); Add(notes, start, 2, "kick");
    if (bar >= 2) Add(notes, start, 1.5, "kick");
    if (bar >= 4) Add(notes, start, 2.75, "kick");
    if (bar == bars - 1) Add(notes, start, 0, "crash");
    break;
```

Note count: 2×12 + 2×13 + 3×14 + 1×14 = 106 notes.

## Success criteria

- Pass: 80% accuracy at `1.0x` (the learning-path threshold).
- Mastery: 90% at `1.0x`.
- Teaching check in the results screen: the Performance Error Map should show
  whether misses cluster on the kick (expected while learning) or on the snare
  (the snare is being pulled early by the syncopation — the habit this lesson
  targets).

## Variations

- **Simpler:** stop at the eighth-note figure (`0, 1.5, 2`) for all eight bars,
  at 64–70 BPM. Suitable as a warm-up or as the "study" chart.
- **Harder:** add a kick on the "and" of 4 (`3.5`) from bar 5, giving
  `0, 1.5, 2, 2.75, 3.5`, and play at 84 BPM. This leads naturally into the
  linear groove of lesson 16.

## Making it playable

Two things beyond the chart case above:

1. **Reachability.** `GameplayLearningProgress.IsUnlocked` requires the previous
   lesson to be completed. Lessons 13 (ghost notes, needs velocity scoring) and
   14 (swing) are not playable, so lesson 15 would stay locked. The smallest
   change is to make Syncopated kick the first lesson of module 4 (swap it with
   Ghost notes in the `lessons` array). Progress is keyed by `GameplayLessonId`,
   so renumbering does not affect saved results.
2. **Public claims.** README, website and Wiki state "12 playable lessons";
   they would move to 13 in the same change.

## Rights

The pattern and all text above are original and written for HitTheKit. They
are not transcribed from any recording, chart, method book or song. Syncopated
kick placements are generic drumming vocabulary; the specific bar plan,
progression and wording are this proposal's own.
