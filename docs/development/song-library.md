# Folder-based song library

The **Gioca / Play** destination opens a setlist before entering the shared
`GameplayPrototype` scene. The setlist is rebuilt on demand from three roots:

1. `~/Documents/HTKSongs` for player-owned songs;
2. the legacy `Application.persistentDataPath/Songs` location;
3. `StreamingAssets/Songs` for bundled entries.

`~/Documents/HTKSongs` is created automatically when the library is first
discovered. If macOS denies access, discovery remains usable through the legacy
and bundled roots and reports a bounded diagnostic instead of crashing. The
Documents folder is evaluated first, so a local folder can deliberately replace
legacy or bundled metadata with the same stable song ID. Duplicate IDs inside
later roots are ignored and surfaced as diagnostics.

## Folder contract

Each direct child of a `Songs` root is one song and must contain `song.json`:

```text
Songs/
  my-song/
    song.json
```

Schema version 1 is explicit and fail-closed. A metadata-only commercial entry
contains only facts supplied by the maintainer and explicit availability:

```json
{
  "schemaVersion": 1,
  "id": "my-song",
  "title": "My Song",
  "artist": "My Band",
  "difficultyHint": "2 / 5",
  "audioAvailability": "missing",
  "chartAvailability": "unavailable"
}
```

Album, genre, year, BPM, bar count, and difficulty are optional. Missing values
remain null in the domain and render as unverified/unrated; they are never
replaced with numeric or textual guesses. A verified BPM belongs to the exact
audio variant used by a chart, not merely to a title found in an external
catalog.

`id` uses lowercase letters, numbers, and hyphens. `audioAvailability` is one
of `generated`, `missing`, or `available`; `chartAvailability` is one of
`generated`, `unavailable`, or `available`. Metadata-only entries must not
declare file paths. Therefore `song.ogg` and `notes.json` are not reserved or
presumed filenames.

A player-owned binding may override a bundled entry with the same ID from
`~/Documents/HTKSongs`. The previous `Application.persistentDataPath/Songs`
root remains readable for backward compatibility. An asset declared `available` may
provide its corresponding `audioFile` or `chartFile`; a playable binding needs
both assets available and must also provide BPM, bars, and beats per bar
verified against those exact files. Audio supports `.ogg` and `.wav`; charts
use the existing version-1 schema and may contain `easy`, `medium`, `hard`,
and/or `expert` note lists. Paths remain
relative to the binding folder. Linked folders
and files, oversized manifests/charts, unsupported extensions, malformed
charts, and path escapes are rejected before gameplay.

The bundled `Neon Circuit` entry declares generated audio and chart content and
therefore needs no checked-in audio. `Local Song Example` is an original,
metadata-only placeholder showing how a player-owned local binding can provide
audio and a compatible chart. HitTheKit does not bundle third-party song names,
recordings, transcriptions, charts, lyrics, or artwork.

## Bundled catalog boundary

The public catalog is intentionally rights-clean. It contains only project-owned
or generic demonstration entries. Players may create local bindings for music
they are authorized to use, but those bindings and their media remain outside
the repository and release artifacts.

## Runtime boundary

Discovery produces immutable `SongLibraryEntry` values. Selecting a playable
entry creates a `GameplaySessionDefinition`; the gameplay scene remains shared
with Learn and generated free-play content. Before launch the player chooses a
difficulty present in the chart and a playback speed (`0.25×`, `0.50×`,
`0.75×`, or `1.00×`). Audio, chart, clock duration, and effective BPM stay on
the same slowed timeline. Song sessions calculate a count-in of at least six
seconds and return to the same setlist afterward.

The current override mechanism already keeps canonical catalog identity apart
from a local folder binding. A future explicit `SongAudioReference` /
`LocalSongAudioBinding` model can replace that binding layer without putting a
machine-specific path into bundled `song.json`.

The refresh button performs an explicit rescan. Discovery does not watch the
filesystem continuously, allocate every frame, or access folders outside the
two configured roots.

## Local audio authoring

The Song Library's **Import audio & create** action opens the macOS file picker
for WAV/OGG content. After the player supplies title, artist and verified timing
metadata, the importer copies the selected file into a new, never-overwritten
folder under `~/Documents/HTKSongs`. The source file is not changed and its
absolute path is not persisted in `song.json`.

The resulting entry is intentionally audio-only: it is not playable as a normal
song, but it can start Chart Creator with the existing DSP clock and an empty
timeline. Once a take is saved, the local folder contains authorized audio plus
the new chart and is playable immediately. Its sibling `.htksong` remains
chart-only for safe transfer.

## Portable `.htksong` chart packages

Chart Creator writes a portable chart-only package next to each recorded take.
Copy a `.htksong` file directly into `~/Documents/HTKSongs` and select
**Refresh library**. Before normal folder discovery, the game validates the
container and atomically installs its `song.json` and `notes.json` into a folder
named after the validated song ID. The package remains in place so it can be
copied to another computer; subsequent refreshes are idempotent.

Package schema version 1 contains no audio and rejects any audio declaration or
extra archive entry. Imports are bounded to 5 MiB, reject links, duplicate or
case-colliding names, malformed ZIP/JSON/chart data, unsupported versions and
path traversal, and never replace an existing song folder.

An imported chart is therefore visible but unavailable until the player
selects **Bind local audio** and supplies a WAV/OGG file they are entitled to
use. The confirmation panel identifies the selected song and local filename;
on confirmation the game copies the audio into that song's direct user-library
folder, atomically updates its manifest, and refreshes the entry as playable.
The source audio and `.htksong` package are never modified, and no absolute
machine path is persisted.
