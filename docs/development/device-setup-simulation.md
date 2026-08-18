# Device Setup Simulation

## Running the prototype

1. Run `./scripts/sync-core-to-unity.sh`.
2. Open `src/HitTheKit.Unity` with Unity `6000.5.6f1`.
3. Open `Assets/HitTheKit/Scenes/DeviceSetupPrototype.unity`.
4. Enter Play mode and select a simulated device.

The scene is second in Build Settings, after `GameplayPrototype`.

## Simulated services

`SimulatedDrumDeviceDiscovery` returns HAMPBACK/eDrum -1, a generic MIDI kit,
and an unknown kit. It supports empty results, duplicates, refresh, and
disconnection tests. HAMPBACK is represented as an exploratory candidate, not
as a supported production device.

`SimulatedGuidedMidiCaptureSource` emits `RawMidiMessage` through
`IGuidedMidiCaptureSource`. Available deterministic scenarios are:

- `CleanStandardKit`;
- `ContaminatedRide`;
- `MissingHiHatContinuous`;
- `ConflictingTrigger`;
- `DisconnectedMidStep`;
- `HampbackCapture2`.

The Capture #2 simulation separates Kick, Ride bow, Ride bell, Crash, chokes,
closed/open Hi-Hat, pedal note, and continuous CC evidence. It does not modify
the tracked Candidate profile.

## Event and timing contract

The source emits normalized `RawMidiMessage` objects. Domain logic does not use
`Time.time`; tests call deterministic `EmitNext`/`EmitAll`. A future presenter
may space events with a coroutine, but that timing must remain presentational.
NoteOn velocity zero is preserved and displayed as NoteOff-equivalent.

## In-memory persistence

`InMemoryUserKitConfigurationStore` serializes and reloads configurations to
provide defensive copies. It survives screen changes during the process only.
It does not use PlayerPrefs, files, cloud, or system identity.

## Testing

EditMode tests cover state transitions, discovery, profile safety, structures,
guided capture, conflicts, localization, event bounds, store copies, and
simulation determinism. PlayMode tests load the real scene, exercise the
HAMPBACK Candidate path, complete a minimal kit, switch language, save a
draft, and verify kit highlighting without MIDI hardware.
