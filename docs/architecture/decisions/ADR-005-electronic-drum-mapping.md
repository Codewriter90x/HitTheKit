# ADR-005: Electronic drum mapping foundation

Status: **ACCEPTED_FOR_FOUNDATION**

## Context

Electronic drum modules expose device-specific notes, channels, continuous
controllers, zones, and choke messages. Embedding those details in gameplay
would bind deterministic matching to one device and make unknown kits hard to
support.

## Decision

- Gameplay remains independent of MIDI note numbers and port identity.
- Physical piece and articulation are separate concepts.
- Raw messages are represented by backend-neutral primitive values.
- Built-in profiles are immutable and separate from user configurations.
- Known profiles are ranked with reasons; ambiguous matches require confirmation.
- Unknown and customized kits use a guided, deterministic capture session.
- Mapping produces a normalized kit hit before the current or future gameplay
  adapter.
- Profile and user-configuration schemas begin at version 1; unsupported
  versions fail explicitly rather than migrating implicitly.
- The profile library is locally extensible and initially contains only a
  generic GM starting point.

## Consequences

The existing three-pad Core contract remains unchanged. Tom and cymbal events
can be represented but are explicitly unsupported by the MVP bridge. User
overrides do not mutate shipped profiles. Mapping ambiguity and disabled state
are observable rather than silent.

This layer does not itself prove device compatibility. A production MIDI
backend, setup UI, configuration storage, and migration policy are later work.
DryWetMIDI integration in Unity is deferred. A HAMPBACK profile is deferred
until a real capture is validated; the existing standalone probe remains on its
separate branch.
