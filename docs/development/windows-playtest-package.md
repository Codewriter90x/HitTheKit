# Windows x64 playtest package

HitTheKit can produce a Windows x64 keyboard-only playtest from the release Mac.
This is a compatibility target, not a claim that MIDI input works on Windows.
The macOS CoreMIDI plug-in is explicitly excluded from Windows builds.

## Requirements on the build Mac

- Unity `6000.5.6f1` with **Windows Build Support (Mono)** installed;
- the .NET SDK selected by `global.json`;
- enough free space for Unity to switch platform and rebuild the player cache;
  and
- a clean release-candidate checkout.

Build and package with:

```sh
./scripts/package-game-windows-x64.sh 0.5.0
```

The script synchronizes the tested Core assembly, asks Unity for a clean
`StandaloneWindows64` player, rejects a package containing the CoreMIDI native
plug-in, adds player instructions, creates a ZIP, and prints its SHA-256.
Generated output remains under the ignored `artifacts/game` directory.

The Windows player contains `HitTheKit.exe`, `HitTheKit_Data`, Unity runtime
libraries, and Mono runtime files. The complete directory must be distributed;
copying only the executable does not produce a valid game.

## Required validation

Before calling a Windows package supported or attaching it to a public release,
test the exact ZIP on clean Windows 10 and Windows 11 machines:

1. compare the published SHA-256;
2. extract the complete archive;
3. complete first-run keyboard setup;
4. play the original demo and at least one lesson;
5. verify audio, timing calibration, pause, results, settings, progress backup,
   high contrast, reduced motion, and keyboard-only navigation;
6. confirm Device Setup states clearly that production MIDI is macOS-only; and
7. collect logs and screenshots without personal paths or third-party content.

## Signing and public distribution

The current script intentionally creates an **unsigned playtest**. It is not a
public-distribution pipeline. For a good public Windows experience, choose and
verify one of these release paths:

- Microsoft Store with an MSIX package signed by Microsoft after certification;
- a trusted code-signing service or certificate for direct distribution; or
- an explicitly limited private playtest, with no claim of public release
  readiness.

A self-signed certificate is suitable only for controlled testing. The final
release process must verify the publisher signature, timestamp, package hash,
and Windows Defender SmartScreen behavior on the downloaded artifact.
