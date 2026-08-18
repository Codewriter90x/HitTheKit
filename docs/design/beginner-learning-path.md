# Drum school learning path

Status: first semester playable

## Goal

`Impara` is a structured beginner course, not a collection of unrelated songs.
It combines pulse, technique, coordination, reading, musicality, and repertoire
inside the same gameplay session used by free play. Keyboard and configured MIDI
drums therefore share chart timing, hit matching, audio feedback, scoring, and
saved progress.

The full syllabus contains 24 lessons in six modules. Modules 1–3 (lessons
1–12) form the playable first semester. Modules 4–6 remain visible as the
declared second-semester syllabus until velocity-aware grading, triplet charts,
sight-reading preparation, and bounded improvisation can be assessed honestly.

## Teaching principles

- Introduce one new coordination problem at a time.
- Alternate technique work with musical application.
- End every four-lesson module with an observable performance test.
- Use original generated drumless accompaniment; the player supplies every drum
  hit.
- Allow study at `0.5x` and `0.75x`, but require at least 80% at `1.0x` to pass.
- Mark 90% or better at `1.0x` as mastery without hiding the original score.
- Show a concrete outcome and sticking/pattern before starting each exercise.
- Preserve the same gameplay shell for lessons, assessment pieces, and free play.

This structure is informed by established percussion programmes rather than
copying their protected repertoire. PAS organises the international rudiments
around rolls, paradiddles, flams, and drags and recommends slow-fast-slow
practice. Rockschool Debut combines short performance pieces with single
strokes, doubles, paradiddles, fills, pulse, reading, ear work, and musicianship.
ABRSM and Trinity use progressive performance grades with varied contemporary
styles and backing tracks.

Primary references:

- [Percussive Arts Society — International Drum Rudiments](https://pas.org/rudiments/)
- [RSL Awards — Rockschool Drums](https://www.rslawards.com/learn-drums/)
- [ABRSM — Drum Kit](https://www.abrsm.org/en-us/instruments/drum-kit)
- [Trinity College London — Rock & Pop Drums](https://www.trinitycollege.com/resource?id=7215)
- [NIOSH — Reducing hearing risk among musicians](https://www.cdc.gov/niosh/bulletin/2015/musicians-hearing-loss.html)

## Curriculum

| Module | Lessons | Disciplines | Module assessment |
| --- | --- | --- | --- |
| 1. Fundamentals | 1–4 | pulse, basic coordination | first complete groove |
| 2. Basic technique | 5–8 | singles, doubles, paradiddle, fills | three bars groove + one bar fill |
| 3. Coordination | 9–12 | eighth grooves, anticipation, hands/feet, sixteenths | sustained sixteenth-note groove |
| 4. Control and dynamics | 13–16 | ghost notes, swing, syncopation, linear playing | linear groove performance |
| 5. Musical styles | 17–20 | half-time, shuffle, funk, rock form | original rock set |
| 6. Stage musician | 21–24 | dynamics, reading, improvisation, repertoire | three-part final concert |

Every lesson definition records its discipline, learning outcome, exercise
pattern, target tempo, chart length, expected practice time, availability, and
whether it closes a module. The selector presents all 24 lessons while clearly
distinguishing playable work from future syllabus content.

## First-semester progression

1. Kick quarter-note pulse at 64 BPM.
2. Kick and snare backbeat at 68 BPM.
3. Eighth-note hi-hat over the backbeat at 72 BPM.
4. Eight-bar first groove and closing crash at 76 BPM.
5. Alternating single strokes at 70 BPM.
6. Orchestrated double strokes at 70 BPM.
7. Orchestrated single paradiddle at 70 BPM.
8. Three groove bars followed by a one-bar fill at 76 BPM.
9. Eighth-note groove with kick variation at 78 BPM.
10. Backbeat with an offbeat snare anticipation at 80 BPM.
11. Stable hand ostinato over changing kick placement at 80 BPM.
12. Sixteenth-note timekeeping and backbeat at 72 BPM.

The sticking lessons use different kit surfaces to make their order observable.
HitTheKit does not claim to identify the player's physical right or left hand.
Likewise, velocity-based ghost-note assessment remains unavailable until the
scoring model explicitly supports dynamics.

## Lesson loop

1. Read the discipline, outcome, pattern, target tempo, and expected duration.
2. Take position during the four-beat musical count-in.
3. Study the same chart at `0.5x` or `0.75x` when needed.
4. Perform at `1.0x` over the drumless accompaniment.
5. Review accuracy and the timing breakdown.
6. Repeat, or continue after reaching the 80% pass threshold.

Short attempts and deliberate breaks are preferred to an enforced long grind.
Practice time is recorded for reflection, not used as a competitive score. When
playing an acoustic kit or loud monitors, the interface should remind the player
to control exposure and take quiet breaks; it must not pretend to measure sound
pressure without calibrated hardware.

## Audio and integrity boundaries

- The accompaniment contains no pre-rendered drum performance.
- Correct input plays the struck drum; an incorrect input also adds the distinct
  mistake cue.
- Generated lesson charts use schema v1 and the canonical chart loader.
- Charts, accompaniment, and generated feedback are deterministic and contain no
  externally licensed music.
- Lessons do not add a second matcher or a hardware-specific MIDI path.
- Future material stays non-playable whenever the current engine cannot grade its
  defining skill faithfully.
