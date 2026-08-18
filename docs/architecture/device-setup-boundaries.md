# Device Setup Boundaries

## Dependency direction

```text
DeviceSetupView (UI Toolkit)
        ↓ actions / snapshots
DeviceSetupPresenter
        ↓
DeviceSetupFlow (pure C#)
        ↓
IDrumDeviceDiscovery · IGuidedMidiCaptureSource · IUserKitConfigurationStore
        ↓
Simulated implementations or macOS CoreMIDI adapters
        ↓ normalized only
RawMidiMessage → KitMappingWizardSession → UserKitConfiguration
```

The view formats and renders state; it never decides mapping, confidence, or
wizard validity. The presenter subscribes to capture events and forwards
actions. The pure flow owns navigation and delegates trigger consistency,
overlap, optional-step behavior, and finalization to the existing wizard.

## Current adapters

- Discovery is synchronous and simulated.
- Capture is deterministic and emits the existing Unity-independent
  `RawMidiMessage` model.
- Storage is process-local and in memory.
- Localization uses stable keys and in-memory Italian/English dictionaries.

## Future replacements

`CoreMidiDrumDeviceDiscovery` and `CoreMidiGuidedMidiCaptureSource` now replace
the simulated services at the composition root without changing UI state. The
native callback feeds a bounded queue and managed polling occurs on the Unity
main thread. `PersistentUserKitConfigurationStore` remains future work.
CoreMIDI stays outside the pure flow, and DryWetMIDI remains confined to the
standalone capture tool.

## Candidate safety

A Candidate profile is presentation evidence, not a production profile.
`productionReady=false`, `autoSelectable=false`, and
`requiresConfirmation=true` remain enforced. Only a separately verified
profile can later enter automatic production matching.

## User configuration schema migration

User kit configurations use schema version 2. It requires every mapping to
declare its verification state and every document to contain an explicit
`reviewIssues` array. Candidate and Draft safety state therefore fails closed
when either field is absent.

Schema version 1 existed before candidate verification and review issues were
introduced. The loader migrates only version 1 configurations explicitly
marked Complete and containing no version 2 fields. Their mappings are the
legacy confirmed mappings and are rewritten as version 2 on the next
serialization. Version 1 Draft documents and mixed-version documents are
rejected because their confirmation state cannot be reconstructed safely.

The same pure invariant validator guards configurations created by the wizard,
constructed directly, or loaded from either supported schema. A Complete
configuration requires at least one enabled, confirmed mapping for every
non-optional element and contains no blocking review issue. Every review issue
must target an element contained in that configuration. Candidate evidence for
pieces outside the selected physical setup remains on the source profile and
is not copied into the user configuration.

Disabled element IDs are reserved for optional elements skipped by the user.
A required element cannot be disabled in either a Draft or Complete
configuration. Optional mappings may be retained while their element is
disabled; the mapping engine reports those events as Disabled.
