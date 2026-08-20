# Ghost Replay

Ghost Replay is a local visual comparison against the immediately preceding
take.

After a run, **Riprova con Ghost** freezes the player's evaluated input times
and restarts the same session. The previous hits appear as white outlined
cross-markers on the existing DSP-driven highway. Their position is derived
from absolute song time, exactly like chart notes.

The ghost is deliberately isolated from gameplay rules:

- it never enters `HitMatchingSession`;
- it never changes score, combo, accuracy, misses, or audio;
- count-in hits are ignored;
- a take is bounded to 8,192 hits;
- it is kept in memory only and is discarded with the gameplay scene.

Normal restart discards the unfinished current take while preserving an
already committed ghost. This keeps pause/restart deterministic and makes the
comparison an explicit player choice.
