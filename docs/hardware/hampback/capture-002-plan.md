# HAMPBACK targeted capture #2 plan

Capture #2 resolves only the ambiguities left by exploratory capture #1. Keep
the kit untouched between related steps, strike only the named surface, and
retry any step with accidental hits. Preserve every observed MIDI event; do
not pre-filter evidence or assume General MIDI controller numbers.

| Priority | Step ID | Italiano / English | Procedure | Samples |
|---:|---|---|---|---:|
| 1 | `ride-bow` | Superficie Ride / Ride bow | Strike only the bow/surface, away from the bell and edge. | 10 strikes |
| 2 | `ride-bell-optional` | Campana Ride / Ride bell | Strike only the bell. | 10 strikes |
| 3 | `crash-1` | Crash normale / Normal Crash | Strike normally without grabbing or choking. | 10 strikes |
| 4 | `crash-choke` | Stop Crash / Crash choke | For each sequence, strike once and then grab the cymbal. | 5 sequences |
| 5 | `ride-choke-optional` | Stop Ride / Ride choke | For each sequence, strike once and then grab the cymbal. | 5 sequences |
| 6 | `hihat-closed` | Hi-Hat chiuso / Closed Hi-Hat | Hold the pedal fully down and strike the Hi-Hat only. | 10 strikes |
| 7 | `hihat-open` | Hi-Hat aperto / Open Hi-Hat | Release the pedal fully and strike the Hi-Hat only. | 10 strikes |
| 8 | `hihat-pedal` | Pedale Hi-Hat / Hi-Hat pedal | Press and release the pedal without striking the cymbal. | 10 presses |
| 9 | `hihat-continuous` | Movimento continuo Hi-Hat / Continuous Hi-Hat | Move the pedal slowly through its full travel without striking. | 10 seconds |
| 10 | `kick` | Cassa / Kick | Perform isolated, clean confirmation strokes. | 10 strikes |

## Review gates

- Ride bow and bell must become distinguishable per attempt, or remain
  conflicted.
- Crash and Ride strike events must be separated from their aftertouch choke
  events.
- Hi-Hat continuous remains `Insufficient` if no Control Change is observed;
  do not invent CC4.
- Notes 42 and 44 must be checked against closed strikes and pedal-only input.
- Kick note 36 should repeat without the earlier note 40 contamination.
- A completed capture #2 still produces candidate evidence. Promotion to
  `Verified` requires a separate review.
