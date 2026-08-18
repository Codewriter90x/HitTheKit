# CoreMIDI hardware smoke

This is a short interactive verification, not a hardware capture and not a
profile-verification procedure.

1. Build the plug-in with `scripts/build-coremidi-plugin-macos-arm64.sh`.
2. Open `DeviceSetupPrototype` in Unity 6000.5.6f1.
3. Select `Core Midi Mac OS` on `DeviceSetupController` and enter Play mode.
4. Connect the drum module by USB and refresh Device Selection.
5. Select the endpoint by its stable identity; never choose solely because a
   duplicate display name appears first.
6. Start guided mapping and strike Kick five times.
7. Confirm five real positive NoteOn observations, their channel, observed
   note, velocity-zero/NoteOff behavior, and monitor updates.
8. Repeat only with five Snare-center strikes, then stop.

For the observed HAMPBACK endpoint, note 36/channel 10 is expected evidence,
not a forced value. The exploratory Candidate remains unverified and requires
the wizard.

If the endpoint is silent, refresh once, reconnect only the USB cable and try
again. Power-cycle the module only after those steps. Do not change mappings
from one stale USB state. Disconnect must clear the current pending samples;
reconnect and capture the step again.

Development diagnostics expose API version, native client/connection state,
endpoint ID, queue size/capacity, received/dropped counts, and the last concise
native error. Do not record raw sessions or hardware logs in the repository.
