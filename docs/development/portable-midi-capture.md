# Portable MIDI capture

`HitTheKit.MidiCapture` is a standalone command-line tool for collecting raw
electronic-drum MIDI evidence on an Apple Silicon Mac. A self-contained build
does not require Unity, Unity Hub, the .NET SDK, or the .NET runtime on the
capture machine. DryWetMIDI 8.0.3 and its macOS Multimedia API remain confined
to this tool; the Unity runtime does not open MIDI ports.

## Workflow

```text
electronic drum kit
→ USB MIDI
→ MacBook Air capture tool
→ verified portable capture bundle
→ Mac Studio analysis
→ manually verified device profile
```

On the development Mac, publish and package the tool:

```shell
./scripts/publish-midi-capture-macos-arm64.sh
./scripts/package-midi-capture-macos-arm64.sh
```

The ignored output is under `artifacts/midi-capture/`. Transfer
`HitTheKit-MidiCapture-macos-arm64.zip` to the MacBook, unzip it, connect the
drum module, then run:

```shell
./hitthekit-midi-capture doctor
./hitthekit-midi-capture list
./hitthekit-midi-capture guided-capture --device 0
```

`doctor` reports the operating system, process architecture, tool and
DryWetMIDI versions, Multimedia API availability, and whether temporary output
is writable. `list` prints MIDI input indices, display names, and manufacturer
when CoreMIDI supplies it. Having no connected devices is a successful result.

## Commands

- `listen` streams compact events until Ctrl+C or an optional duration.
- `capture` writes every event for a duration or until Ctrl+C.
- `guided-capture` labels exploratory kit-piece steps. Each step can be
  accepted, retried, skipped, or used to finish and save early.
- `summarize` derives cautious per-step observations from an existing bundle.
- `verify` checks schemas, sequences, monotonic timestamps, required files,
  JSON, and the SHA-256 manifest.
- `pack` creates a deterministic ZIP from a verified bundle.
- `replay` turns an explicitly synthetic fixture into a complete bundle for
  hardware-independent tests. It never represents fixture data as hardware.

The guided sequence covers kick, snare center/rim, three tom positions, two
crashes, ride bow/bell, closed/open/pedal/continuous hi-hat, crash/ride chokes,
and free play. Optional elements can be skipped. Normal strikes default to five
samples, chokes to three attempts, continuous hi-hat movement to eight seconds,
and free play to twenty seconds. `--samples` and `--duration` override the
applicable defaults.

## Event path and clock

Foundation-compatible DryWetMIDI events follow this path:

```text
DryWetMIDI event → tool adapter → RawMidiMessage → capture serialization
```

The tool links the pure `RawMidiMessage` source used by the merged mapping
foundation; it does not create a competing Note/CC/aftertouch contract. Pitch
bend, program change, SysEx, and unknown events are retained as bounded raw
capture evidence because the current foundation intentionally has no gameplay
mapping for them. SysEx stores its length and at most the first 64 bytes.

Elapsed time comes only from `System.Diagnostics.Stopwatch`. UTC wall-clock
time is metadata, never an event delta. A MIDI callback assigns a thread-safe
sequence and monotonic timestamp, creates a small snapshot, and enqueues it. A
separate consumer handles JSONL writes and console output. Original NoteOn with
velocity zero stays `noteOn` and gains `isNoteOffEquivalent: true`; summaries
do not count it as a positive strike.

Accepted callbacks pass through a short in-memory gate: sequence assignment,
step labeling and queue insertion therefore have one canonical order. On
shutdown the gate closes before the queue is completed, and the consumer drains
every event accepted before that boundary.

## Bundle

```text
hampback-capture-YYYYMMDD-HHMMSS/
├── session.json
├── events.jsonl
├── events.json
├── summary.json
├── summary.txt
├── manifest.sha256
├── README.txt
└── logs/capture.log
```

`events.jsonl` is flushed progressively and is the resilient source. Guided
retries and skipped attempts remain in the raw evidence with their step and
attempt labels; accepting or retrying never erases messages already observed. Final JSON
and report files are written through temporary files and atomic rename. The
manifest hashes every other file in deterministic relative-path order. Summary
wording says what was *observed during a step*; it does not claim a definitive
HAMPBACK or other device profile.

Capture and replay require a new or empty output directory and never silently
replace an existing session. Verification and packing reject symbolic links,
unexpected nested directories, case-insensitive path collisions and unlisted
files so a bundle cannot escape its own directory through a linked path.

## Privacy and transfer

Capture bundles contain MIDI messages, a device display name, minimal OS/tool
metadata, and labels or notes supplied voluntarily. The tool does not collect
audio, video, desktop content, microphone input, username, home path, hostname,
IP address, computer serial number, Apple ID, telemetry, or external files.

Transfer a bundle or its ZIP with AirDrop, iCloud Drive, removable storage, or
a trusted local network, then run `verify` again on the Mac Studio before
analysis.

## Gatekeeper and current limits

This development package is not notarized or Developer ID signed. A transferred
file may receive a quarantine attribute. After verifying and trusting the exact
package, remove only that attribute if macOS requires it:

```shell
xattr -d com.apple.quarantine ./hitthekit-midi-capture
```

Do not disable Gatekeeper globally. There is no installer, graphical app,
cloud upload, telemetry, automatic mapping, final HAMPBACK profile, or Unity
MIDI runtime integration in this slice.
