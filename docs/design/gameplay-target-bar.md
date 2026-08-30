# Gameplay target bar

The bottom instrument ribbon is an isolated `GameplayTargetBar` module. Its
approved visual artwork lives under `UI/Gameplay/TargetBar`; the gameplay
controller supplies only pulse deadlines and does not construct cards, labels,
icons, spacing, or ordering.

The fixed visual order is:

1. Charleston;
2. Rullante;
3. Crash;
4. Tom 1;
5. Cassa;
6. Tom 2;
7. Floor Tom;
8. Ride.

The single artwork preserves the interlocking panel geometry exactly while an
eight-zone runtime overlay retains hit feedback. Highway, drum-kit, HUD and
stage changes must not edit this module or its asset unless the target bar
itself is intentionally redesigned.
