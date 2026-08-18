# Build the macOS CoreMIDI plug-in

## Requirements

- macOS on Apple Silicon;
- Xcode command-line tools with the macOS SDK;
- Unity 6000.5.6f1 for import and Editor verification.

From any directory run:

```sh
/path/to/hitthekit/scripts/build-coremidi-plugin-macos-arm64.sh
```

The script compiles C++17 for arm64, links CoreMIDI/CoreFoundation, executes
the native parser/queue/lifecycle harness, validates `file`, `lipo`, and
`otool -L`, then atomically copies:

```text
src/HitTheKit.Unity/Assets/Plugins/macOS/HitTheKitCoreMidi.dylib
```

Objects, tests, dylibs, dSYM bundles, and native build output are ignored by
Git. The source tree is the reproducible artifact. The Editor asset
postprocessor configures the generated plug-in for macOS Editor and macOS
Standalone ARM64 only. If it was imported before scripts compiled, run
`HitTheKit > Configure CoreMIDI Plug-in Importer` once.

The generated library should report Mach-O arm64 and dependencies limited to
CoreMIDI, CoreFoundation, libc++, and libSystem. It does not require a package
manager or third-party native library.

## Clean-checkout behavior

Without the dylib, Unity still compiles, the Device Setup scene opens, and the
Simulated backend works. Choosing CoreMIDI reports that the plug-in has not
been built instead of leaking `DllNotFoundException`. Build the plug-in only
when real macOS MIDI development is needed.

## Selecting the backend

On `DeviceSetupController`, set **Input Backend** to `Core Midi Mac OS` for a
development scene instance. `Simulated` remains the default used by automated
tests. This is a composition choice; the view and wizard do not depend on the
native implementation.
