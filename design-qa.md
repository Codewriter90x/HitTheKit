# Design QA — Learn Section Setlist

## Scope

- Full-screen Learn / Impara section.
- Beginner curriculum navigation, selected lesson controls, and study speed.
- Start-button interaction and return path from a learning session.
- Visual target: the left-hand setlist section of the supplied mockup only.

## Evidence

- Source visual: user-supplied setlist mockup (1672×941).
- Final standalone implementation: 1366×768 macOS player capture.
- A temporary side-by-side comparison was inspected locally and was not added to the repository.

## Comparison

- Reproduced the narrow backstage navigation rail, active cyan marker, dark framed setlist panel, progress card, mastery rule, chapter hierarchy, compact lesson rows, cyan selection, amber accents, and stage background.
- Learn is now a section that replaces the home content, rather than a centered modal over it.
- The selected lesson control deck retains the product’s existing 0.5×/0.75×/1.0× speed feature while matching the reference visual language.
- The Start action has a large explicit hit target and distinct hover, focus, active, disabled, and keyboard states.
- A 1366×768 standalone inspection showed no clipped primary controls, overlapping labels, or unreachable actions. Four lesson rows remain visible at this compact height; the remainder are reachable in the bounded list.

## Functional verification

- Core: 33/33 passed.
- MIDI capture tool: 39/39 passed.
- Unity EditMode: 382/382 passed.
- Unity PlayMode: 36/36 passed.
- Standalone macOS build compiled and was inspected directly.
- Opening Learn and starting a lesson are separated across frames, preventing one Enter press from performing both actions.
- Learning results return to the same Learn section, lesson, and study speed.
- Two intentional kick hits within the bounded gesture window confirm the current action; zero-velocity hits, electrical bounce, slow pairs, and interrupted pairs do not.

## Remaining differences

- The mockup places Continue inline in the selected row. The implementation places Start in a persistent command deck so the existing speed controls and lesson explanation remain accessible without a second screen.
- The reference contains illustrative completion history; a fresh implementation correctly shows zero completed lessons.

final result: passed

---

# Design QA — Gameplay Target Bar Module

## Source and implementation

- Approved reference: `mockup/gameplay-target-controls-selected-reference.png`
  (2172 x 724).
- Runtime artwork: `UI/Gameplay/TargetBar/gameplay-target-bar-soft-interlock.png`
  (2172 x 360).
- Actual Unity Game View capture:
  `mockup/gameplay-target-bar-implementation-clean.png` (988 x 557).
- Temporary comparison artifact:
  `artifacts/gameplay-target-bar/reference-vs-implementation.png`.

## Exact-match checks

- The eight controls preserve the approved order: Charleston, Rullante, Crash,
  Tom 1, Cassa, Tom 2, Floor Tom and Ride.
- The center Cassa panel remains wider, symmetric and visually dominant.
- Adjacent panels retain the complementary inward/outward facets from the
  supplied reference rather than becoming independent rounded rectangles.
- Neon colors, geometric symbols, line-art instruments and Italian labels are
  preserved in one approved raster asset, preventing UI layout engines from
  changing their relative geometry.
- Eight transparent runtime hit zones align with the artwork and retain live
  hit pulses without reconstructing or approximating the visible design.
- The target order is shared with the highway lane model so notes terminate at
  the matching control.

## Module boundary

- `GameplayTargetBar` owns ordering, proportional hit zones and pulse state.
- `GameplayTargetBar.uss` owns sizing, responsiveness and the fixed artwork.
- `GameplayHighwayController` only instantiates the module and forwards pulse
  deadlines; it no longer creates individual target cards.
- The module is documented in `docs/design/gameplay-target-bar.md`, including
  the rule that highway, HUD, stage and drum-kit work must not edit it.

## Verification

- Unity EditMode: 521/521 passed.
- Unity PlayMode: 55/55 passed.
- The PlayMode scene test verifies the approved artwork, exact eight-piece order,
  centered kick and all runtime pulse layers.
- The EditMode tests verify deterministic ordering, isolated construction and
  pulse activation/reset behavior.
- The .NET suite could not run because the repository requires SDK 8.0.411 and
  that exact SDK is not installed on this machine; `global.json` was not changed.
- No P0, P1 or P2 visual mismatch remains in the verified 16:9 Game View.

final result: passed

---

# Design QA — Gameplay Arena Highway

## Source and implementation

- Source reference: `mockup/gameplay-visual-direction-01-arena-highway.png`
- Selected target reference: `mockup/gameplay-visual-direction-01-arena-highway.png`
- Implemented screen: `mockup/gameplay-target-controls-final.png`
- Side-by-side comparison: `mockup/gameplay-target-controls-comparison.png`
- Reference source dimensions: 1672 x 941
- Captured player viewport: 1366 x 768 (1920 x 1080 logical player surface)
- State: Neon Circuit gameplay, approximately three seconds after launch

## Comparison history

1. The foundation pass established the arena, highway, HUD and target hierarchy,
   but still used a procedural instructional kit and secondary diagnostic rails.
2. The asset pass introduced an original photorealistic neon arena and a separate
   realistic kit cutout. The first kit was rejected because it contained extra
   toms and cymbals beyond the eight playable targets.
3. The corrected kit was generated with exactly three cymbals (hi-hat, crash and
   ride) and five drums (snare, two rack toms, floor tom and kick). Its neon rim
   colors now match the target cards below it.
4. The lane pass aligned all eight highway lanes with the eight bottom targets,
   converted kick from a full-width bar into its own lane, strengthened note glow
   and raised the impact line to match the reference perspective.
5. The target-control pass reproduced the selected mockup's exact semantic order:
   Charleston cross, Rullante circle, Tom 1 square, Tom 2 circle, Cassa capsule,
   Floor Tom diamond, Crash triangle and Ride square. Each card now includes a
   dedicated instrument image instead of a text glyph or placeholder.
6. The final depth pass reduced and lowered the physical kit, moved it behind the
   highway layer and removed Arcade-only shortcut letters, so the road remains
   fully readable while the eight colored controls match the supplied composition.

## Final assessment

- The note highway is the dominant visual element.
- Combo, score, accuracy and song progress occupy distinct, readable regions.
- The impact line is bright and unambiguous.
- The foreground kit uses exactly the eight playable components requested by the
  maintainer, with no extra drums or cymbals.
- Lane markers, drum rims and bottom targets share one ordered color system:
  hi-hat, snare, Tom 1, Tom 2, kick, floor tom, crash and ride.
- Kick now occupies the same fifth visual lane as its fifth bottom target instead
  of appearing as a disconnected full-highway bar.
- Arcade notes use the same color/geometry vocabulary as the bottom controls:
  circles, squares, diamond, triangle and kick capsule remain identifiable without
  relying on color alone.
- The physical kit begins in the lower/rear stage and never occludes the road or
  impact line. Its visual role is spatial grounding rather than foreground chrome.
- No P0, P1 or P2 clipping, overlap or hierarchy defect remains in the verified
  16:9 viewport.
- Residual P3 difference: the generated instrument thumbnails are more detailed
  than the concept's thin line icons, but preserve the same hierarchy and are
  clearer at the verified player resolution.

final result: passed
