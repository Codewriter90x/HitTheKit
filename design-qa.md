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

# Design QA — Gameplay Arena Highway

## Source and implementation

- Source reference: `mockup/gameplay-visual-direction-01-arena-highway.png`
- Implemented screen: `mockup/gameplay-visual-implementation-final.png`
- Side-by-side comparison: `mockup/gameplay-visual-comparison-final.png`
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
4. The composition pass enlarged the kit into the foreground, lifted it behind
   the target row, aligned all eight highway lanes with the eight bottom targets,
   converted kick from a full-width bar into its own lane, strengthened note glow
   and raised the impact line to match the reference perspective.
5. The final pass removed the redundant on-kit label overlays in Arcade Neon,
   kept the timing judgment clear, and compared the source and runtime screen at
   the same normalized 1366×768 viewport.

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
- No P0, P1 or P2 clipping, overlap or hierarchy defect remains in the verified
  16:9 viewport.
- Residual P3 difference: the concept uses bespoke pictograms inside every target
  card, while the runtime keeps the existing keyboard key plus beginner/technical
  labels so the controls remain immediately usable.

Final result: `passed`
