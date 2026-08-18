# Gameplay session model

`GameplayPrototype` is the single gameplay presentation for Free Play, Learn,
and future modes. Callers do not configure scene objects directly. They create
an immutable `GameplaySessionDefinition` and publish it through
`GameplaySessionContext` before loading the scene.

The definition owns the variable parts of a run:

- session kind and optional lesson identity;
- chart, difficulty, playback speed, BPM, bars and count-in;
- generated-song behavior or detected external chart/audio paths;
- presentation environment;
- title, metadata and return destination.

`GameplaySessionCoordinator` applies that definition to the shared chart and
song-clock components. `GameplayHighwayController` projects the same definition
into the HUD, results, theme, progress record and return behavior. It does not
branch on which menu launched the scene.

## Presentation settings

`GameplaySettingsService` persists the selected `GameplayPresentationTheme`
as a small versioned JSON document under `Application.persistentDataPath`.
The main-menu settings are the only player-facing theme selector. Starting a
new session copies the current preference into the immutable session; changing
the preference later cannot mutate an active run.

This boundary means that pause, metronome, result feedback or future session
types can be added once to `GameplayPrototype`, while launch surfaces only
provide a different session definition.

## Song-library sessions

The Play destination discovers immutable entries through `SongLibraryDiscovery`
and converts a selected playable entry with `GameplaySessionFactory.Song`.
Generated and folder-backed songs therefore use the same coordinator, highway,
input, scoring, pause, and results implementation. The session carries only the
validated song ID and file paths required by the shared scene; menu UI and
filesystem discovery do not leak into gameplay components.

The Song Library launcher adds two immutable choices: a difficulty that must
exist in the selected chart and one of the supported song speeds. The factory
derives effective BPM and a minimum six-second count-in; the coordinator then
applies the same speed to the chart and, for local audio, the DSP playback
source. Missing difficulties are never synthesized.
