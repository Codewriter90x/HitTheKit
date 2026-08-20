# Auto Tempo Coach

Auto Tempo Coach turns a completed lesson or Song Library attempt into a safe
next-speed recommendation. It uses the existing score and hit-matching results;
it does not introduce a second scoring model.

## Progression

Song Library sessions follow the already supported song speeds. Lessons follow
the existing study progression (`0.5x`, `0.75x`, `1.0x`). A step unlocks when:

- accuracy is at least 85%;
- misses are no more than 10% of chart notes;
- unmatched hits are no more than 10% of chart notes.

Below those guardrails the coach asks the player to repeat the current speed.
At the final supported speed it reports the target as mastered.

## Applying a recommendation

The results screen displays the recommendation. The player explicitly confirms
the next speed; the gameplay scene then reloads the same lesson or song with a
new immutable `GameplaySessionDefinition`. Original BPM is recovered from the
current effective BPM and multiplier, then chart timing, audio pitch, count-in
and display metadata are rebuilt together.

This avoids changing playback speed halfway through a scored attempt and keeps
the DSP clock, chart timeline and external audio on one timing contract.
