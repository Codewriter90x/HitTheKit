# Audio and latency sound check

The guided sound check lives in **Settings → Input calibration** and reuses the
same local timing-offset preferences consumed by gameplay.

## Flow

1. **Test audio** plays an original generated click through Unity's active
   output device. HitTheKit does not choose the hardware output; macOS or the
   drum module must route it to the desired headphones/speakers.
2. Select **Keyboard** or **MIDI**. The two recommendations never share samples.
3. Start the check, listen to four count-in clicks, then strike with the next
   twelve clicks.
4. At least eight matched strikes are required. The existing
   `TimingCalibrationAdvisor` calculates a median and rejects out-of-window
   hits. Applying the recommendation updates only the selected input offset.

The generated track is scheduled against Unity DSP time. Target timestamps use
the corresponding unscaled-time origin, so the measurement does not depend on
frame-by-frame accumulated time. All samples remain in memory and local to the
running app; there is no telemetry, upload, or raw-hit persistence.

This is a guided user calibration, not laboratory round-trip latency
measurement: human timing variation is reduced by the median and the minimum
sample gate, not claimed to be eliminated.
