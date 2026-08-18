# DSP song-clock prototype

This prototype establishes a frame-rate-independent audio timeline for the
Unity layer. It deliberately contains no chart, note scheduler, player input,
The clock remains independent of chart and scoring logic, while the gameplay
prototype now consumes it for a complete demonstration run.

## Timing contract

Unity schedules the generated clip with `AudioSource.PlayScheduled`. The clock
uses the exact same scheduled DSP instant:

```text
songPositionSeconds = AudioSettings.dspTime - startDspTime
```

A negative position is intentional pre-roll. Zero is the scheduled start,
positive values are elapsed song time, and the clock is complete when position
is greater than or equal to the clip duration. The runtime never derives song
position from frame time or `AudioSource.time`.

`DspSongClock` is an ordinary C# class and accepts `IDspTimeSource`, so EditMode
tests use a deterministic fake. `UnityDspTimeSource` is the production adapter
for `AudioSettings.dspTime`. `DspSongClockPrototype` owns the generated clip,
configures its `AudioSource`, schedules playback with a 0.5-second lead-in, and
destroys the clip with the component.

Pause freezes the domain position and shifts the scheduled origin on resume;
restart creates a fresh scheduled clock while reusing the generated clip.

## Generated fixture

`GeneratedDemoSongFactory` creates the stereo, non-streaming “Neon Circuit”
arrangement in memory. The scene defaults are 120 BPM, eight bars, four beats
per bar, and 48 kHz, producing a 16-second song with deterministic kick, snare,
hi-hat and bass voices. `GeneratedClickTrackFactory` remains available for
isolated timing tests. Samples are bounded; no audio asset or file-system write
is involved.

## Run the prototype

From the repository root, synchronize the engine-independent Core assembly:

```bash
./scripts/sync-core-to-unity.sh
```

On Windows:

```powershell
./scripts/sync-core-to-unity.ps1
```

Open `src/HitTheKit.Unity` with Unity `6000.5.6f1`, open
`Assets/HitTheKit/Scenes/GameplayPrototype.unity`, and press Play.

## Tests

After synchronizing Core, run the EditMode and PlayMode suites from Unity Test
Runner. They can also be run in batch mode with `-runTests -testPlatform
EditMode` and `-runTests -testPlatform PlayMode`. PlayMode tests use explicit
timeouts and do not depend on physical speakers or perceived sound.

## Current limits

- no calibration or audio/input offset;
- no speed change;
- no audio-device-change handling;
- no external/licensed audio asset.
