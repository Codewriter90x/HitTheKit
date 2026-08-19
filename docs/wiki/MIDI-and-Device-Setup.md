# MIDI and Device Setup

## Current support

Real electronic-drum input is implemented through CoreMIDI on Apple Silicon
macOS. Keyboard and deterministic simulation remain available when the native
plug-in or a compatible device is absent.

## Prepare the plug-in

```sh
./scripts/build-coremidi-plugin-macos-arm64.sh
./scripts/sync-core-to-unity.sh
```

Open the Unity project, enter **Configure**, refresh devices and select the
intended endpoint explicitly.

## Guided mapping

1. Choose the active kit structure.
2. Follow the requested instrument/pad prompt.
3. Strike the requested surface consistently.
4. Review ambiguous candidates or conflicts.
5. Confirm required elements before finalizing.
6. Use sound check to verify the resulting logical kit.

The system must not silently promote uncertain input to a confirmed mapping.
Unknown devices can be configured, but that does not make them officially
verified profiles.

## When a device is missing

- confirm the module is powered and connected before refreshing;
- close other software that may hold the MIDI endpoint;
- verify the native plug-in was built for arm64;
- test keyboard input to separate gameplay problems from MIDI problems;
- include sanitized device/module information in a hardware report.

Never upload credentials, personal paths, commercial audio or unsanitized MIDI
captures. See [[Troubleshooting]] and the
[hardware smoke guide](https://github.com/Codewriter90x/HitTheKit/blob/main/docs/development/coremidi-hardware-smoke.md).
