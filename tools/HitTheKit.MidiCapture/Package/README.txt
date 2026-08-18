HitTheKit MIDI Capture — macOS Apple Silicon

This package is self-contained. Unity and the .NET runtime are not required.

1. Unzip the package.
2. Connect the electronic drum module by USB.
3. Open Terminal in this folder.
4. Run:
     ./hitthekit-midi-capture doctor
5. Run:
     ./hitthekit-midi-capture list
6. Run (replace 0 with the listed device index if necessary):
     ./hitthekit-midi-capture guided-capture --device 0
7. Transfer the resulting capture ZIP to the Mac Studio.

If macOS adds a quarantine attribute during transfer and refuses to start this
unsigned local build, inspect the file first. If you trust this exact package,
you may remove only that attribute:

  xattr -d com.apple.quarantine ./hitthekit-midi-capture

Do not disable Gatekeeper globally. This development build is not notarized or
Developer ID signed.

Privacy: the tool captures MIDI messages, a device display name, and optional
labels/notes that you enter. It does not capture audio, video, the desktop,
microphone input, username, hostname, IP address, computer serial number, or
Apple ID.

Captured observations are evidence for later profile analysis. They are not an
automatically verified device mapping.
