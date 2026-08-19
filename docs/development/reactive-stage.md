# Accessible reactive stage

The gameplay stage reacts to real runtime signals already produced by the hit matcher and score tracker. It does not own timing, scoring, input, or song state.

## Signals

- Combo selects a bounded energy band: calm, building, live, or peak.
- The latest hit grade provides a short accent.
- The latest drum pad selects the accent color already used by that lane.
- Misses and unmatched inputs enter a visible recovery state.

The stage uses text and geometric patterns as well as color. A miss therefore remains distinguishable in high-contrast and color-impaired viewing conditions.

## Motion and intensity safety

- Full visual intensity is capped at `0.78`.
- `Reduce motion` replaces moving patterns with a steady composition and caps intensity at `0.22`.
- Pulses last at least `0.18` seconds and the stage never alternates the full screen on/off.
- `High contrast` increases outlines and preserves the textual stage state.

All visuals are procedural UI Toolkit drawing. No commercial assets, per-frame material creation, telemetry, or additional gameplay systems are introduced.
