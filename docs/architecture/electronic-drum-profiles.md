# Electronic drum profiles

HitTheKit keeps device protocol details outside gameplay:

```text
raw MIDI-shaped message
→ built-in profile or user mapping
→ normalized kit hit
→ MVP DrumInputEvent adapter
→ HitMatchingSession
```

This foundation models data; it does not open a MIDI port and has no dependency
on a MIDI library.

## Kit taxonomy

`KitPiece` identifies the physical role. The baseline covers Kick, Snare,
HiHat, four numbered toms, FloorTom, three numbered crashes, and Ride; the
additional numbered values exist specifically so a setup can express the
documented 0–4 tom and 0–3 crash bounds. `KitArticulation` independently identifies
Default, Head, Rim, Bow, Edge, Bell, Closed, HalfOpen, Open, Pedal, or Choke.
`KitElementDefinitionValidator` defines the current valid combinations. Default
is accepted for every known piece as an evolution fallback; obviously invalid
combinations such as Kick + Bell and HiHat + Head are rejected.

Stable element IDs such as `snare.rim` are identities. Display names are not.

## Raw messages and triggers

`RawMidiMessage` represents Note On, Note Off, Control Change, polyphonic
aftertouch, and channel aftertouch using only primitive .NET values. Channels
are 0–15 and data bytes are 0–127. A Note On with velocity zero has Note Off
semantics. The optional timestamp is retained without choosing its clock; a
future backend is responsible for supplying a monotonic or song-relative time.

`MidiTrigger` matches message kind, optional exact channel (null means any),
data1, and an inclusive value range. It can detect overlapping trigger ranges.
No crosstalk filtering or velocity curve is implied.

## Profile schema version 1

`ElectronicDrumProfile` contains a stable profile ID and version, optional
manufacturer/model/aliases/port patterns/vendor and product IDs, kit elements,
default mappings, capability flags, and notes. Supported flags cover multi-zone
snare/toms, continuous or note-based hi-hat pedal data, crash choke, and ride
bell/edge.

`ElectronicDrumProfileLoader` uses Unity `JsonUtility` with DTOs and explicit,
case-sensitive string mappings for every enum. Required fields, duplicate IDs,
duplicate exact triggers, invalid triggers, and missing element references are
rejected. Schema versions other than 1 are rejected. Unknown JSON properties
are ignored for forward-compatible additive metadata; they cannot change the
meaning of known fields. A future incompatible change requires a new schema
version and an explicit migration decision.

The tracked `generic-gm-drums-v1.json` fixture maps common GM percussion notes.
It is a generic starting profile, not guaranteed device identification, and
requires user confirmation. It is not a verified profile for any named kit.

## User configuration

`UserKitConfiguration` is separate from a built-in profile. It records its own
ID/name, optional base profile ID/version, optional `MidiDeviceIdentity`, an
immutable snapshot of elements and mappings, disabled element IDs, notes, and
whether a wizard draft is complete. Deriving a configuration does not mutate
the source profile. Filesystem location and storage policy are deliberately not
part of this foundation.

`UserKitConfigurationSerializer` and `UserKitConfigurationLoader` implement a
deterministic, validated schema version 1 round trip with no filesystem access.

## Matching profiles and messages

`ElectronicDrumProfileMatcher` ranks candidates using available identity data:
vendor/product, manufacturer/model, exact alias, and port-name substring. Text
comparison is ordinal case-insensitive. Missing metadata is never invented.
Equal top candidates yield `Ambiguous`; the caller must confirm. When no known
profile matches, the explicitly generic profile may be returned as
`GenericFallback`. `ElectronicDrumProfileLibrary` indexes immutable built-ins by
unique ID and delegates identity matching.

`MidiKitMappingEngine` applies mappings in this order: UserOverride,
WizardCapture, BuiltInProfile; priority breaks ties within a source. Equally
ranked overlapping mappings to different elements return `Ambiguous`. Disabled
entries/elements, invalid input, and no match are distinct results.
Incomplete wizard drafts are rejected by runtime mapping until finalized.
`NormalizedKitHit` preserves the element, piece/articulation, raw velocity,
original immutable message, optional timestamp, mapping source, and source
mapping ID.

`MvpDrumInputMapper` bridges Kick, Snare, and HiHat (including the supported
articulations and pedal) to the current three `DrumPad` values. Tom, Crash, and
Ride return `UnsupportedInCurrentGameplay`; they are not silently discarded or
added to Core.

## Limits

- no Unity MIDI backend, port scanning, or DryWetMIDI integration;
- no final HAMPBACK profile or real device capture;
- no runtime setup UI, downloads, cloud profiles, or PlayerPrefs storage;
- no changes to charts, Core matching, scoring, visuals, or input gameplay;
- no advanced crosstalk, positional sensing, or velocity curves.
