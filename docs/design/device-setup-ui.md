# Device Setup UI

## Purpose

`DeviceSetupPrototype` is a UI foundation for configuring an electronic drum
kit before a real macOS MIDI backend is introduced. It presents the complete
product flow against deterministic simulated devices and raw MIDI-shaped
events. It does not claim that a simulated device is connected.

## Flow

The pure `DeviceSetupFlow` owns these states:

```text
Welcome → DeviceSelection → ProfileSelection → KitStructure
        → GuidedMapping → ConflictReview → ConfigurationReview
        → TestKit → Completed
```

Invalid transitions return `DeviceSetupTransitionResult`; the view does not
change domain state directly. Back, retry, optional skip, draft save, conflict
resolution, test, and completion are explicit transitions.

## Screen structure

- Welcome explains MIDI-only privacy and starts setup.
- Device Selection lists the display name, manufacturer, port, connection
  state, and candidate count. No ambiguous device is selected automatically.
- Profile Selection distinguishes Candidate from Verified choices and never
  offers an unverified “use without verification” action.
- Kit Structure exposes the existing Minimal, Standard, Extended, and Custom
  `KitSetupDefinition` paths.
- Guided Mapping shows step progress, Italian and English terminology, target
  diagram, sample status, connection state, and a bounded ten-event monitor.
- Conflict Review requires retry or an explicit unresolved decision.
- Configuration Review exposes mapping trigger, state, and origin.
- Test Kit uses the existing `MidiKitMappingEngine` and highlights mapped,
  unsupported, or unmapped input without changing gameplay.

## Responsive layout

The UI uses three columns at desktop width: navigation, drum diagram, and
event/status monitor. Below 1050 logical pixels it stacks those panels. Panel
scaling targets 1280×720 and matches width/height equally, covering 16:9 and
16:10 layouts such as 1440×900 and 1920×1080.

## Accessibility foundation

Buttons have visible labels, keyboard focus receives a high-contrast outline,
body text is at least 15 px, and status always combines text with color. Blue,
green, orange, and red supplement `Waiting`, `ReadyToConfirm`, `Conflict`, and
`Disconnected`; they are never the sole signal. Motion is limited to a 0.1 s
target emphasis and can be removed without affecting state.

## HAMPBACK candidate

The screen describes `dream-edrum-hampback-candidate-001` as exploratory,
Candidate, not verified, not production-ready, not auto-selectable, and
requiring confirmation. Candidate mappings are presented as evidence; Ride,
choke, and continuous Hi-Hat ambiguity stays unresolved. The original profile
asset is not modified or promoted.
