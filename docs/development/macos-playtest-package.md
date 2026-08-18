# macOS playtest package

The standalone playtest targets Apple-silicon Macs and does not require Unity
on the destination Mac. It includes the Main Menu, Gameplay, and Device Setup
scenes, plus the generated arm64 CoreMIDI plug-in. Unity currently emits a
universal player shell; the supported and validated playtest target is arm64.

Build and package it with Unity `6000.5.6f1`:

```bash
./scripts/package-game-macos-arm64.sh 0.1.0
```

The script builds the native plug-in, synchronizes Core, invokes the checked-in
Unity build method, embeds the arm64 plug-in, applies an ad-hoc signature,
verifies the bundle, and creates a resource-fork-safe ZIP under
`artifacts/game/`.

This playtest packaging is intentionally not a production distribution flow.
Unity gives the local build an ad-hoc signature, but downloading it on another
Mac can still trigger Gatekeeper because it has no Developer ID signature or
Apple notarization ticket. On first launch, control-click the app and choose
**Open**. A public release should instead use a Developer ID Application
identity, hardened runtime, Apple notarization, and stapling.
