# Gameplay environments

The gameplay scene offers three presentation environments over the same chart,
timing, input and scoring domain. Environment selection is a persisted setting,
not an in-song action, and never changes hit matching.

## Arcade Neon

- Holographic runway and portal stage.
- Wide, dramatic highway perspective.
- Diamond notes and console-like strike targets.
- Cyan/violet lighting with restrained amber accents.

## Concert Stage

- Live arena viewed from behind a complete electronic drum kit.
- Narrower highway that stops above the foreground kit.
- Disc-shaped notes and circular drum-head targets.
- Warm amber stage lighting with blue and magenta rim light.

The foreground kit is presentation only. The canonical target row remains the
interactive and accessible hit surface.

## Precision Grid

- Technical training chamber with wireframe drums at the sides.
- Flatter, measured perspective and square geometry.
- Outlined notes and low-noise target treatment.
- Dark navy/cyan palette with articulation colors retained.

## Shared behavior

The three environments use `GameplayPresentationTheme` and
`GameplayEnvironmentProfile`. Each profile owns only visual geometry, labels
and note shape. `ChartTimelinePrototype`, `HitMatchingPrototype`, keyboard
input, CoreMIDI input and `GameplayScoreTracker` remain shared.

Players choose an environment under **Impostazioni / Settings** in the main
menu. `GameplaySessionFactory` copies the selected environment into an
immutable `GameplaySessionDefinition` when a Free Play or Learn run starts.
The active run therefore remains visually stable, and future modes can reuse
the same gameplay scene without adding another presentation controller.
