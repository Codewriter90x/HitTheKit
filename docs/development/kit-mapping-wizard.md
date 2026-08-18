# Kit mapping wizard foundation

`KitMappingWizardSession` is a pure C# state machine for building a
`UserKitConfiguration` without knowing a MIDI backend or Unity frame loop. A
future screen supplies `RawMidiMessage` values and renders the current step.

## Setup presets

- `Minimal3Piece`: Kick, Snare Head, HiHat Closed/Open.
- `Standard5Piece`: the minimal roles plus optional Snare Rim and HiHat Pedal,
  Tom1, Tom2, FloorTom, Crash1 Bow, and Ride Bow.
- `ExtendedElectronicKit`: standard plus optional half-open hi-hat, tom rims,
  Crash2, crash chokes, and Ride Edge/Bell.

Each `KitMappingWizardStep` exposes a stable ID, target `KitElement`, localized
instruction key, English fallback instruction, required/optional flag,
expected message kinds, capture count, and conflict policy. Prompts use correct
hi-hat pedal terminology; crash choke is a separate articulation.

## Capture policy

Normal strike steps require two coherent positive-velocity Note On messages
with the same kind, channel, and note number. Note Off and velocity-zero Note On
are ignored. Pedal steps accept two coherent Control Change or Note On samples;
for CC the captured minimum/maximum becomes the inclusive range. Choke steps
accept one supported Note Off or aftertouch sample. The pure engine uses no
wall clock and performs no active-sensing or MIDI-clock parsing because those
message kinds are outside its deliberately small raw model.

After enough samples, a capture is pending until `Accept` is called. The state
machine reports `Accepted`, `NeedsMoreSamples`, `Conflict`, `Ignored`, or
`Completed`. A trigger overlapping an existing mapping to a different element
is a conflict and is never overwritten silently.

## Navigation and output

- `Retry` clears samples for the current step.
- `Back` returns to the prior step and removes its accepted mapping/skip.
- `SkipOptional` rejects required steps and records optional elements disabled.
- `Reset` returns to the first step and clears all session output.
- `ExportDraft` produces a configuration marked incomplete.
- `FinalizeConfiguration` succeeds only after every required step is mapped and
  every optional step is either mapped or skipped.

Serialization is explicit: the wizard never writes automatically, and the JSON
serializer itself has no path or filesystem dependency.

## Development checks

EditMode tests cover presets, deterministic ordering, capture counts, ignored
messages, coherent/incoherent samples, conflict detection, retry/back/skip,
reset, incomplete/final output, deterministic JSON round trips, and source
profile immutability. They use synthetic raw messages only; no MIDI hardware or
port is required.

## Deferred work

The production MIDI backend, verified device profiles, setup interface,
persistent storage location, advanced CC interpretation, crosstalk handling,
and gameplay support for toms/cymbals remain separate phases.
