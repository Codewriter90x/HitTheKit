# Troubleshooting

## Unity opens but the project does not compile

1. Confirm Unity `6000.5.6f1`.
2. Confirm the .NET SDK selected by `global.json`.
3. Run `./scripts/sync-core-to-unity.sh` before opening Unity.
4. Check the Unity Console for the first compiler error, not only later cascades.

## The game opens but MIDI is unavailable

- real MIDI currently requires Apple Silicon macOS;
- build the plug-in with `./scripts/build-coremidi-plugin-macos-arm64.sh`;
- refresh devices after connecting and powering the drum module;
- verify keyboard gameplay separately;
- see [[MIDI and Device Setup]].

## A local song is visible but cannot be played

Visibility is not proof of playability. Confirm that the local binding provides:

- authorized `.ogg` or `.wav` audio;
- a valid chart file;
- verified BPM, bars and beats per bar;
- relative paths contained inside the song directory.

Metadata-only songs are intentionally rejected rather than receiving synthetic
fallback content. See [[Local Songs]].

## Before reporting a bug

- reproduce with the original `Neon Circuit` demo when possible;
- record the exact commit/version, OS, Unity version and input type;
- include minimal steps and a short sanitized log excerpt;
- remove personal paths, credentials, commercial media and private captures.

Use the [bug report form](https://github.com/Codewriter90x/HitTheKit/issues/new/choose)
or continue with [[FAQ and Support]]. Security vulnerabilities must be reported
privately according to
[SECURITY.md](https://github.com/Codewriter90x/HitTheKit/blob/main/SECURITY.md).
