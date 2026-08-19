# Local Songs

HitTheKit does not bundle commercial music. You may use local content only when
you own it or are otherwise authorized to use it.

## Local folder

The game discovers player-owned entries under:

```text
~/Documents/HTKSongs/
```

The directory is created automatically during discovery. Each direct child is
one song and must contain a `song.json` manifest:

```text
HTKSongs/
  my-song/
    song.json
    my-audio.ogg
    my-chart.json
```

Paths in the manifest must remain relative to the song directory. Audio supports
`.ogg` and `.wav`; charts use the existing versioned JSON schema. A playable
binding needs authorized audio, a valid chart and timing metadata verified
against that exact audio variant.

## Metadata-only entries

An entry with missing audio or unavailable charts may be visible in the catalog,
but it must not launch, invent timing or fall back to another song. Unknown BPM,
duration or difficulty stays unknown.

## Content safety

Do not commit or upload commercial audio, transcriptions, lyrics, artwork or
download links to the public repository. Local paths and personal data must also
remain outside tracked manifests.

The full schema and precedence rules are documented in the
[Song Library guide](https://github.com/Codewriter90x/HitTheKit/blob/main/docs/development/song-library.md).
