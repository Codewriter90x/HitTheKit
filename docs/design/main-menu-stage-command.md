# Main Menu — Stage Command

## Purpose

`MainMenuPrototype` is the entry point for HitTheKit. It makes the three current
product areas explicit without presenting unfinished systems as complete:

- **Gioca / Play** opens the playable drum highway;
- **Impara / Learn** explains that guided lessons are the next stage and offers
  the existing highway as a practice surface;
- **Configura batteria / Set up drums** opens the device-setup flow.

## Visual direction

The selected Stage Command direction uses an original backstage concert image,
a dark left-side command rail, cyan focus edges, and magenta stage light. It
shares the gameplay highway's color and typography hierarchy while keeping the
menu readable over a cinematic electronic-drum stage.

The background is a generated original asset. It contains no UI text, logos, or
third-party brand marks; interactive content remains live UI Toolkit content.

## Interaction and accessibility

- The first focus target is **Gioca**.
- Tab and standard button activation work through UI Toolkit.
- Arrow keys move between primary destinations.
- `G`, `I`, and `C` focus the three destinations; `Esc` closes overlays.
- Focus and selection use border thickness, scale, labels, and color together.
- Italian and English copy can be switched in-session.
- Reduced motion disables menu transition timing.

## Navigation contract

Build Settings order is:

1. `MainMenuPrototype`
2. `GameplayPrototype`
3. `DeviceSetupPrototype`

Gameplay and Device Setup both provide a route back to the main menu. No
persistence, scoring expansion, lesson engine, backend, or AI is introduced.
