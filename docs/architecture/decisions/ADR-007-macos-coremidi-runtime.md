# ADR-007: macOS CoreMIDI runtime

**Status:** PROPOSED_FOR_MVP0_MACOS_RUNTIME

## Context

Device Setup already depends on normalized discovery and guided-capture
interfaces. The first real runtime input must support Unity Editor on Apple
Silicon without moving the standalone tool’s DryWetMIDI dependency into Unity.

## Decision

- Use Apple CoreMIDI through a minimal native C++ macOS arm64 plug-in.
- Expose a versioned C ABI with fixed-layout values and copied strings.
- Never call managed code from the CoreMIDI callback thread.
- Parse MIDI 1 channel voice messages natively and enqueue bounded snapshots.
- Poll the queue on Unity’s main thread and convert to `RawMidiMessage`.
- Keep `IDrumDeviceDiscovery` and `IGuidedMidiCaptureSource` as the UI boundary.
- Keep Simulated as the safe default and make plug-in absence non-fatal.
- Keep DryWetMIDI confined to the portable CLI tool.
- Defer MIDI 2.0/UMP, output, persistence, and non-macOS backends.

## Consequences

The callback has no managed-runtime or filesystem dependency, and Unity owns a
deterministic dispose boundary for play/stop and scene reload. Native binaries
are generated rather than versioned. A macOS build toolchain is required for
real input, while clean checkouts continue to support simulation.
