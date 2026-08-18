# Capture-to-profile pipeline

This pipeline converts verified capture evidence into device profiles without
turning observations into unsupported claims:

```text
verified capture bundle
→ capture schema adapter
→ evidence analyzer
→ candidate mappings
→ candidate profile
→ targeted recapture
→ verified profile
→ built-in profile library
```

The pure `GuidedCaptureProfileAnalyzer` receives normalized session metadata,
step state, and events. It does not read ZIP files, use DryWetMIDI, open MIDI
ports, or depend on Unity frames. `GuidedCaptureSchemaV1Loader` is the separate
adapter for schema-v1 session JSON and JSONL data. Real raw captures stay
outside the repository; tests use an explicitly synthetic minimized fixture.

## Lifecycle states

- **Exploratory**: raw evidence has passed integrity checks but may be noisy or
  incomplete. No product support is implied.
- **Candidate**: evidence has been analyzed into candidate triggers with
  confidence and recapture requirements. It is not production-ready or
  auto-selectable.
- **Verified**: required evidence has been independently confirmed and the
  profile has passed the future production review gate. Only this state may
  enter the built-in profile library.
- **Deprecated**: a formerly usable profile has been superseded or should no
  longer be selected for new configurations.

The candidate schema is deliberately separate from the production
`ElectronicDrumProfile` schema. `DeviceProfileCandidate.CanEnterBuiltInLibrary`
is false unless all four conditions hold: status is `Verified`, production is
ready, automatic selection is allowed, and confirmation is no longer required.
The existing production loader therefore does not accept candidate JSON.

## Confidence rules

Confidence is categorical and evidence-based:

- **High**: a dominant trigger is repeated in a clean attempt with enough
  samples and no unresolved element-level conflict.
- **Medium**: a clear trigger exists but sample count, contamination, or related
  evidence still requires confirmation.
- **Low**: limited or noisy evidence suggests a candidate but does not isolate
  it reliably.
- **Insufficient**: no usable evidence exists, a required message kind is
  absent, or the step was skipped.
- **Conflicted**: two or more plausible triggers cannot be assigned safely, or
  a trigger collides with an incompatible element.

Per-attempt evidence takes precedence over aggregate totals. Secondary notes,
aftertouch, skipped steps, and absent controller data remain visible. A
confidence label never promotes a mapping by itself.

## Current boundary

This foundation analyzes evidence only. It does not add a Unity MIDI backend,
runtime configuration UI, device downloads, cloud synchronization, automatic
profile generation, or scoring.
