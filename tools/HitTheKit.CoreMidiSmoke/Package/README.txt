HitTheKit CoreMIDI Smoke - macOS arm64
======================================

This package is a hardware smoke tool. It does not require Unity, the .NET
runtime, or a repository checkout. Keep HitTheKitCoreMidi.dylib next to the
HitTheKit.CoreMidiSmoke executable.

Commands:

  ./HitTheKit.CoreMidiSmoke doctor
  ./HitTheKit.CoreMidiSmoke list
  ./HitTheKit.CoreMidiSmoke listen --device <index> --seconds 20
  ./HitTheKit.CoreMidiSmoke guided-smoke

guided-smoke always requires an explicit device index. It collects only five
kick strikes followed by five snare-center strikes, prints observed events and
summaries, then stops. It does not create captures, profiles, or mappings.

Verify package files from this directory with:

  shasum -a 256 -c SHA256SUMS

The executable targets macOS 12 or later on Apple Silicon. If macOS quarantine
blocks a package copied from another Mac, use Finder's Open action to review and
approve the unsigned development tool.
