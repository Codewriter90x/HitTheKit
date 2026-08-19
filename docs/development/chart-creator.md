# Chart Creator foundation

Chart Creator turns a performance from the existing keyboard/CoreMIDI gameplay
input into a reviewable schema-v1 chart draft. It deliberately reuses the real
gameplay scene rather than maintaining a second audio clock, input mapper, or
timeline.

## Workflow

1. Select **Import audio & create** in the Song Library and choose a local WAV
   or OGG file, or select an existing playable song.
2. For new audio, enter title, artist, verified BPM, bar count and beats per bar.
   HitTheKit copies the selected audio into a private local authoring folder; it
   never modifies the source file.
3. Choose practice speed and select **Record chart**. A new audio-only source
   starts with a real empty schema-v1 timeline; an existing song keeps its
   current chart only as a playback reference.
4. Play during the normal count-in and backing track. Hits before song time zero
   or beyond the declared song duration are ignored.
5. At the result screen, review the captured notes against the bounded waveform.
   Drag its playhead to scrub the source timeline, zoom around a passage, and
   preview audio from the selected time. Select a note to change its time, drum
   pad, velocity or physical articulation, add missing notes, or delete unwanted
   notes. The original recorded take remains unchanged in memory.
6. Save the edited timing as recorded, or quantize the edited draft
   non-destructively to an eighth- or sixteenth-note grid.

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
overwritten. Select the imported entry, choose **Bind local audio**, select
your own authorized WAV/OGG copy, and confirm the local-only binding. The game
copies that audio into the imported song folder and makes the entry playable;
the source file and portable package are not modified.

The publish is atomic and both documents are parsed by the production loaders
before the folder or package becomes visible. When the recording used a local
WAV/OGG source, the private exported folder receives its own local audio copy and
is immediately playable. The `.htksong` manifest still declares audio as
`missing`: Chart Creator never embeds, downloads, or redistributes source audio
in the portable package. A recipient supplies their own authorized local copy.

Import is fail-closed. Unknown/archive entries, audio declarations, symbolic
links, duplicate names, unsupported versions, malformed JSON, invalid charts,
oversized data and path traversal are rejected before extraction. Version 1 is
intentionally chart-only; adding optional distributable audio requires a future
explicit package version and rights-aware UX.

The exported title is marked `Recorded Take` and the difficulty hint says that
the performance must be reviewed before sharing. This is a captured performance,
not a claim of authoritative transcription.

## Note expression

Schema v1 remains backward compatible: `velocity` and `articulation` are
optional note properties. A legacy note without them keeps unknown velocity and
the `default` articulation wildcard. Newly recorded/exported notes preserve
velocity; non-default articulations use explicit identifiers such as `rim`,
`bell`, `bow`, `edge`, `open`, or `pedal`.

Known articulations are validated against the selected pad. An explicit Ride
Bell target, for example, matches only a bell hit and highlights the bell zone
of the instructional kit. Changing a note to an incompatible pad resets its
articulation to the backward-compatible default rather than inventing a zone.

The waveform is an editor envelope, not a second clock or audio importer. It is
sampled from the already-loaded `AudioClip` into a bounded 512-point model, and
preview playback reuses the existing `DspSongClockPrototype` audio source. The
chart timing remains source-time based and the recorded take remains immutable.

## Current foundation limits

- The waveform provides envelope scrubbing, zoom and preview; it is not yet a
  sample-accurate destructive audio editor.
- The native picker currently targets macOS. WAV and OGG are supported; MP3 is
  intentionally rejected by the production loader.
- The author must enter BPM, bars and meter explicitly. Unknown timing never
  receives a hidden default.
- Portable packages remain chart-only; the receiving computer must explicitly
  bind an authorized local WAV/OGG copy before the imported chart is playable.
