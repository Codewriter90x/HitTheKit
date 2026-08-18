# Gameplay highway themes

`GameplayPrototype` presents one timing and hit-matching experience through
three selectable visual themes. The themes deliberately share the same chart,
DSP clock, input events, lane identities, hit judgments, and progress state.
The player chooses the environment in the main-menu settings. That preference
is persisted and captured when the next gameplay session is created, so a run
cannot change presentation halfway through.

## Themes

- **Arcade Neon** uses a luminous arena, crisp colored targets, and a classic
  perspective rhythm highway optimized for instant readability.
- **Concert Stage** places the same highway over a warm live-stage environment
  and gives the drum targets a more physical, performance-oriented character.
- **Precision Grid** reduces stage detail and emphasizes timing, latency, lane
  geometry, and competitive clarity.

The gameplay screen intentionally has no theme selector and the `1`, `2`, and
`3` keys do not change presentation. Choose Arcade Neon, Concert Stage, or
Precision Grid under **Impostazioni / Settings** before starting a session.

## Complete visual kit

The presentation has seven upper lanes for Hi-Hat, Snare, Tom 1, Tom 2,
Floor Tom, Crash, and Ride. Kick / Grancassa uses a separate, full-width pulse
track so foot notes cannot be confused with hand hits. Each target combines
color, a stable label, and a keyboard binding; color is never the only cue.

Keyboard bindings are:

| Piece | Key |
| --- | --- |
| Crash | D |
| Ride | S |
| Tom 1 | G |
| Tom 2 | H |
| Snare | J |
| Hi-Hat | K |
| Floor Tom | L |
| Kick / Grancassa | F |

The chart loader and engine-independent `DrumPad` model now recognize all eight
visual lanes. The existing matcher remains the sole source of timing grades;
the gameplay UI only projects chart and matching state.

## Original visual assets

The three tracked background plates were generated specifically for HitTheKit.
They contain no third-party game art, logos, characters, or copied interface
elements. Notes, lane geometry, targets, HUD, and feedback are rendered live by
Unity UI Toolkit.
