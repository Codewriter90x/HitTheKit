# Build from Source

## Validate the engine-independent projects

```sh
dotnet restore HitTheKit.sln
dotnet test HitTheKit.sln --no-restore --configuration Release
./scripts/check-nuget-vulnerabilities.sh
```

## Synchronize the Unity core

```sh
./scripts/sync-core-to-unity.sh
```

On Windows PowerShell:

```powershell
./scripts/sync-core-to-unity.ps1
```

The generated `HitTheKit.Core.dll` is intentionally ignored by Git. Run the
sync command after a clean checkout and whenever the core changes.

## Optional macOS CoreMIDI plug-in

On Apple Silicon macOS:

```sh
./scripts/build-coremidi-plugin-macos-arm64.sh
```

The generated native plug-in is also ignored by Git. A missing or incompatible
plug-in must fail safely to keyboard/simulated input.

## Open Unity

Use Unity `6000.5.6f1` and open `src/HitTheKit.Unity`. Run EditMode and PlayMode
tests from the Unity Test Runner before submitting a change.

## Canonical technical guides

- [Unity integration](https://github.com/Codewriter90x/HitTheKit/blob/main/docs/architecture/unity-integration.md)
- [CoreMIDI plug-in build](https://github.com/Codewriter90x/HitTheKit/blob/main/docs/development/coremidi-plugin-build.md)
- [macOS packaging](https://github.com/Codewriter90x/HitTheKit/blob/main/docs/development/macos-playtest-package.md)
- [Windows packaging](https://github.com/Codewriter90x/HitTheKit/blob/main/docs/development/windows-playtest-package.md)
- [Release process](https://github.com/Codewriter90x/HitTheKit/blob/main/docs/release/RELEASE_PROCESS.md)

Public player binaries are not currently approved. Building locally does not
change that distribution boundary.
