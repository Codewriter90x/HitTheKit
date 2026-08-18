# Versioned chart timeline

This vertical slice loads a small versioned chart and queries it against the
DSP-backed song position. It supplies timing data only: there is no player
input, hit judgment, note state, or visual presentation.

## Schema version 1

The supported document shape is:

```json
{
  "version": 1,
  "offsetSeconds": 0.125,
  "difficulties": {
    "easy": [
      { "time": 1.0, "pad": "kick" },
      { "time": 1.5, "pad": "snare" },
      { "time": 2.0, "pad": "hiHat" }
    ]
  }
}
```

The loader supports the exact, case-sensitive difficulty identifiers `easy`,
`medium`, `hard`, and `expert`. A chart may provide any non-empty subset; the
Song Library presents only those choices before launch. `version`,
`offsetSeconds`, `difficulties`, and each note's `time` and `pad` are required.
It rejects missing required data, unsupported versions, malformed JSON,
non-finite offsets or note times, negative note times, and unknown pads.
Unknown JSON properties are ignored for forward compatibility; they never
replace or provide defaults for required fields. Pad matching is case-sensitive
and explicit:

| JSON identifier | Core value |
| --- | --- |
| `kick` | `DrumPad.Kick` |
| `snare` | `DrumPad.Snare` |
| `hiHat` | `DrumPad.HiHat` |

No enum-name deserialization, aliases, or fuzzy matching are used. Notes are
ordered by chart time and retain their original JSON order when times are
equal. Duplicate events are preserved as distinct `ChartNote` instances.

## Offset and query boundaries

The loader preserves original chart times and the timeline applies the offset
once:

```text
effectiveNoteTime = chartNoteTime + offsetSeconds
```

`GetUpcoming(songPosition, lookAhead)` uses inclusive boundaries:

```text
songPosition <= effectiveNoteTime
effectiveNoteTime <= songPosition + lookAhead
```

Negative song positions are valid, so the query works during DSP pre-roll. A
zero look-ahead matches only notes exactly at the current position.

`GetElapsed(songPosition)` uses an exclusive boundary:

```text
effectiveNoteTime < songPosition
```

A note exactly at the current position is not elapsed. Both queries are
read-only, preserve deterministic order, and reject non-finite inputs; a
negative look-ahead is invalid.

## Unity prototype

The tracked fixture is
`Assets/HitTheKit/Fixtures/Charts/mvp0-easy-chart.json`. The
`GameplayPrototype` scene assigns it as a `TextAsset` to one
`ChartTimelinePrototype`, along with the scene's `DspSongClockPrototype`.
The component loads once, then derives upcoming and elapsed queries from
`DspSongClock.PositionSeconds`; it does not accumulate frame time.

Before opening Unity, synchronize the Core DLL:

```bash
./scripts/sync-core-to-unity.sh
```

On Windows PowerShell:

```powershell
./scripts/sync-core-to-unity.ps1
```

Open `src/HitTheKit.Unity` with Unity `6000.5.6f1`, open
`Assets/HitTheKit/Scenes/GameplayPrototype.unity`, and press Play. EditMode
tests cover parsing, validation, mapping, ordering, duplicates, offsets, and
query boundaries. PlayMode tests cover the serialized fixture and its live DSP
clock connection. Both suites are also runnable in batch mode with Unity's
`-runTests` and the corresponding `-testPlatform` value.

## Current limits

- only schema version 1 and the four explicit difficulty identifiers are supported;
- no keyboard or MIDI gameplay input;
- no runtime hit matching, judgments, scoring, or note-resolution state;
- no pad visuals, highway, menu, or UI;
- folder-based song discovery and local `.ogg`/`.wav` playback are available,
  but there is no file picker, continuous hot reload, download service, or chart
  editor;
- the runtime never bundles third-party recordings or community charts.
