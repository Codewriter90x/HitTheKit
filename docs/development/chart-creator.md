# Chart Creator foundation

Chart Creator turns a performance from the existing keyboard/CoreMIDI gameplay
input into a reviewable schema-v1 chart draft. It deliberately reuses the real
gameplay scene rather than maintaining a second audio clock, input mapper, or
timeline.

## Workflow

1. Add or select a playable song in the Song Library.
2. Choose its difficulty and practice speed.
3. Select **Record chart**.
4. Play during the normal count-in and backing track. Hits before song time zero
   or beyond the declared song duration are ignored.
5. At the result screen, review the captured-hit count and save the raw timing,
   or quantize non-destructively to an eighth- or sixteenth-note grid.

Keyboard and MIDI events reach the recorder through `HitMatchingPrototype`'s
`InputProcessed` boundary. Consequently, the existing per-source timing offset
is applied before recording. A take made at a reduced practice speed is scaled
back to the source song's original timeline before it is serialized.

## Output and rights boundary

The exporter creates a new, never-overwritten folder under
`~/Documents/HTKSongs` containing only:

- `song.json`;
- `notes.json`.

It also creates one portable `<song-id>.htksong` file alongside the folder.
The package is a ZIP-compatible, chart-only container with exactly three
entries:

- `htksong-version` (`1`);
- `song.json`;
- `notes.json`.

To transfer a take, copy only the `.htksong` file into
`Documents/HTKSongs` on the other computer and refresh the Song Library. The
game validates and atomically imports it. Existing song folders are never
overwritten.

The publish is atomic and both documents are parsed by the production loaders
before the folder or package becomes visible. The manifest declares chart availability but
keeps audio as `missing`. Chart Creator never copies, embeds, downloads, or
redistributes the source audio. To play or share the take, the user must add an
audio file they are entitled to use and update the local binding explicitly.

Import is fail-closed. Unknown/archive entries, audio declarations, symbolic
links, duplicate names, unsupported versions, malformed JSON, invalid charts,
oversized data and path traversal are rejected before extraction. Version 1 is
intentionally chart-only; adding optional distributable audio requires a future
explicit package version and rights-aware UX.

The exported title is marked `Recorded Take` and the difficulty hint says that
the performance must be reviewed before sharing. This is a captured performance,
not a claim of authoritative transcription.

## Current foundation limits

- A playable Song Library entry is required; audio import/file-picker UI is not
  part of this first foundation.
- Editing individual notes is not yet available. Raw/1/8/1/16 save choices are
  the initial review tools.
- The schema currently stores pad and time. Velocity and articulation remain in
  the in-memory take but schema v1 does not serialize them.
- Exported takes are intentionally non-playable until authorized audio is bound.
