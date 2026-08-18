# Demo song vertical slice

`GameplayPrototype` is a complete, intentionally short rhythm-game run built
from repository-owned data and generated audio.

## Runtime flow

```text
Neon Circuit chart + DSP song clock
→ keyboard and CoreMIDI composite input
→ HitMatchingSession
→ GameplayScoreTracker
→ highway HUD and results overlay
```

The 16-second easy chart covers Kick, Snare, Hi-Hat, Tom 1, Tom 2, Floor Tom,
Crash, and Ride. Keyboard controls are `F`, `J`, `K`, `G`, `H`, `L`, `D`, and
`S`. On macOS, exactly one available CoreMIDI input is opened and mapped through
the canonical generic GM profile. Zero endpoints keeps keyboard play available;
multiple endpoints require selection in Device Setup rather than choosing one
arbitrarily.

Score and accuracy are derived only from canonical matcher results. `Escape` or
`P` pauses the audio and DSP position together. Restart replaces the matcher and
score state before scheduling a fresh run. Completion displays score, accuracy,
maximum combo, rank, and judgment counts.

The HAMPBACK profile remains Candidate and is neither auto-selected nor changed
by this gameplay slice. No persistence, scoring backend, cloud service, or AI is
introduced.
