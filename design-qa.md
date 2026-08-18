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
