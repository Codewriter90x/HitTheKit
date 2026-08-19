# Getting Started

HitTheKit is currently a source-only pre-release. You need Unity to run the game
from this public repository.

## Requirements

- Git
- the .NET SDK selected by `global.json`
- Unity `6000.5.6f1`
- Unity Hub
- macOS Apple Silicon only when testing the CoreMIDI plug-in

## First run

```sh
git clone https://github.com/Codewriter90x/HitTheKit.git
cd HitTheKit
dotnet test HitTheKit.sln
./scripts/sync-core-to-unity.sh
```

Open `src/HitTheKit.Unity` in Unity Hub with Unity `6000.5.6f1`, then open the
project's main scene and enter Play Mode.

Keyboard input is the safest first test and does not require MIDI hardware. On
macOS Apple Silicon, follow [[MIDI and Device Setup]] before connecting a real
kit.

## What to try first

1. Open the main menu.
2. Choose **Play** and launch the original `Neon Circuit` demo.
3. Try one of the 12 available lessons under **Learn**.
4. Open **Configure** only when you are ready to map a MIDI kit.

For platform-specific limitations, read [[Platforms and Limitations]]. For
complete commands and generated-file boundaries, continue with
[[Build from Source]].
