# HitTheKit CoreMIDI native boundary

This directory contains the dependency-free macOS arm64 native input boundary.
It exposes the versioned C ABI in `include/HitTheKitCoreMidi.h`, parses MIDI 1.0
channel messages on the CoreMIDI callback thread, and places immutable snapshots
in a bounded queue for managed main-thread polling.

Run `scripts/build-coremidi-plugin-macos-arm64.sh` from any directory. Generated
objects, tests, and the Unity `.dylib` are intentionally not committed.
