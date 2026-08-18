# macOS CoreMIDI runtime

## Boundary

The macOS MVP uses a dependency-free C++ plug-in linked only to CoreMIDI and
CoreFoundation. The stable, versioned C ABI is declared in
`native/HitTheKit.CoreMidi/include/HitTheKitCoreMidi.h`. Managed code never
passes retained pointers to native code and CoreMIDI never invokes C#.

```text
CoreMIDI MIDIReadProc
→ MIDI 1 byte-stream parser
→ bounded native queue (4096 snapshots)
→ DeviceSetupController.Update polling (maximum 1024 messages/frame)
→ RawMidiMessage
→ existing Device Setup presenter and wizard
```

The implementation uses Apple’s documented `MIDIClientCreate`,
`MIDIInputPortCreate`, `MIDIPortConnectSource`, `MIDIPacketList`, MIDI object
properties, and notification callback. CoreMIDI object properties supply the
unique ID, endpoint display name, device and entity names, manufacturer,
model, protocol, and offline flag:

- [MIDIClientCreate](https://developer.apple.com/documentation/coremidi/midiclientcreate(_:_:_:_:))
- [MIDIInputPortCreate](https://developer.apple.com/documentation/coremidi/midiinputportcreate(_:_:_:_:))
- [MIDIPacketList](https://developer.apple.com/documentation/coremidi/midipacketlist)
- [MIDI object properties](https://developer.apple.com/documentation/coremidi/midi-object-properties)

`MIDIPacket.timeStamp` is host time. Zero timestamps fall back to
`mach_absolute_time`; `mach_timebase_info` converts ticks to monotonic seconds.
ABI v2 also exposes the current value of the same native clock. Gameplay
subtracts the measured queue age from the DSP song position, so polling does
not collapse a batch of distinct strikes onto one frame timestamp. Wall time
never orders events.

## Lifecycle and threading

The native client is created once by `CoreMidiNativeSession`. Opening an input
first closes any previous port. Close disables acceptance under the lifecycle
mutex before disconnecting and disposing the port; a callback already in
progress drains before close obtains that mutex. Each opened input also has a
monotonic generation token. The callback must present the currently active
token, so a callback dispatched by an older port is rejected even if a new port
has already opened. Queue entries are immutable,
sequence is assigned inside the queue lock, and overflow drops the newest
message while incrementing diagnostics. Stop clears queued events, so events
accepted for an earlier guided step cannot leak into the next one.

The managed capture source is created and polled by the Unity main thread.
`Poll` rejects calls from another thread before touching the native queue or
raising `MessageReceived`; therefore the public API cannot emit guided-capture
events on a worker thread. A newly selected CoreMIDI endpoint starts in the
Disconnected state until `Start` successfully opens it. The presenter invokes
that initial start while the flow is Waiting, while a flow explicitly marked
Disconnected still requires reconnect/resume before another capture can start.

CoreMIDI setup/property notifications increment a generation counter. Device
Setup refreshes only when that counter changes and the device-selection screen
is active; it does not enumerate every frame. An offline selected endpoint
transitions capture to Disconnected. Reconnection is explicit and starts a new
capture, so old samples are not accepted.

The plug-in build runs deterministic parser, queue, ABI and callback-boundary
tests before copying the generated library into Unity. It deliberately does
not create a live CoreMIDI client during that build, because service state and
connected hardware are environmental. Use the packaged smoke tool's `doctor`,
`list` or `guided-smoke` command for those checks; the native test binary can
also opt into its live lifecycle case with
`HTK_COREMIDI_RUN_LIVE_TESTS=1` in a controlled environment.

## MIDI 1 parser

The parser supports Note On, Note Off, Control Change, Polyphonic Aftertouch,
Channel Aftertouch, Pitch Bend, and Program Change, including running status,
multiple messages in a packet, and state carried across packets. Note On with
velocity zero remains NoteOn in `RawMidiMessage` and exposes its existing
NoteOff-equivalent semantic kind. Realtime, Active Sensing, system-common, and
SysEx bytes are ignored; they never become drum hits. MIDI 2.0/UMP and complete
SysEx capture are deferred.

## Device identity and privacy

Runtime identity prefers `kMIDIPropertyUniqueID`. If an endpoint does not
publish one, the current CoreMIDI endpoint reference is namespaced as a
session-local fallback. Duplicate display names therefore remain distinct.
The backend reads MIDI endpoint metadata only. It does not read usernames,
hostnames, Mac serial numbers, Apple IDs, IP addresses, home paths, audio,
video, or network data.

Profile suggestions are resolved outside the CoreMIDI adapter. The observed
DREAM/eDrum identity may expose the existing HAMPBACK Candidate, but it stays
non-production, non-auto-selectable, and confirmation-required.

## Platform and failure behavior

The plug-in is macOS arm64 only. P/Invoke calls are guarded by
`UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX`; other platforms and clean checkouts
without a generated plug-in return a structured unavailable state. The
simulated backend remains the serialized default. No DryWetMIDI assembly is
referenced by Unity.

Unity’s official plug-in documentation describes native libraries under
`Assets` and per-platform import configuration: [Import and configure
plug-ins](https://docs.unity3d.com/6000.0/Documentation/Manual/plug-in-inspector.html).
