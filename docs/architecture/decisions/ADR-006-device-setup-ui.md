# ADR-006: Device Setup UI foundation

**Status:** ACCEPTED_FOR_UI_FOUNDATION

## Context

HitTheKit has device-independent mapping and wizard logic, a portable capture
tool, and an exploratory HAMPBACK Candidate. A product setup experience is
needed before adding OS MIDI access, so interaction design and domain
boundaries can be tested deterministically.

## Decision

- Use runtime UI Toolkit with `UIDocument`, `PanelSettings`, UXML, and USS.
- Keep navigation in a pure `DeviceSetupFlow` state machine.
- Place discovery, normalized capture, and storage behind interfaces.
- Develop against deterministic simulation first.
- Require confirmation for Candidate profiles; never auto-select them.
- Use the existing `KitSetupDefinition`, `KitMappingWizardSession`,
  `RawMidiMessage`, `UserKitConfiguration`, and `MidiKitMappingEngine`.
- Defer real persistence, CoreMIDI runtime access, and device permissions.

## Consequences

The full setup flow is testable without hardware, Unity timing, files, or
network access. A future real backend can replace simulation while preserving
state and presentation. This ADR does not authorize HAMPBACK promotion or
introduce MIDI gameplay.
