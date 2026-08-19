# Practice Lab

Practice Lab adds repeatable song sections and manual A–B loops to the existing
gameplay scene. It deliberately reuses the production DSP clock, chart timeline,
hit matcher, score tracker and audio sources.

## Player flow

Pause a song with `Esc` or `P`, then use the Practice Lab panel:

- choose the previous or next four-bar section and select **Loop section**;
- select **Set A**, resume and pause later, then select **Set B** for a custom
  range;
- select **Whole song** to leave practice mode and restart normally.

Every repetition includes a two-beat preparation window before point A when the
range does not start at the beginning. Only notes inside `[A, B)` are sent to the
matcher. Score, combo and timing analysis restart for each pass, while recorded
practice time remains cumulative.

## Timing contract

`DspSongClock.Seek` re-anchors absolute song position to the current DSP time.
`DspSongClockPrototype.SeekPlayback` moves the audio playhead to the equivalent
clip position, including playback-speed conversion. The highway continues to
derive marker position from absolute chart time, so repeated loops do not
accumulate frame-based drift.

Automatic sections are derived from verified session timing in groups of four
bars. The final section may contain fewer bars. A selected section is clamped to
the real audio duration before it can become active.

## Boundaries

This foundation does not persist loop selections and does not add a second
timeline, scoring engine or audio transport. Named musical sections can be added
later as optional chart metadata without changing the loop transport contract.
