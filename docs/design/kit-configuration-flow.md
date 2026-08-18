# Future kit configuration flow

This is a documentation wireframe, not an implemented runtime screen.

1. **Scan devices** — a future backend supplies non-sensitive
   `MidiDeviceIdentity` metadata.
2. **Known profile candidates** — show exact/probable candidates, explain match
   reasons, and require confirmation for ambiguous or generic results.
3. **Confirm kit structure** — choose a preset or adjust optional pieces/zones.
4. **Guided mapping** — show one stable prompt, collect consistent raw samples,
   then let the user accept or retry.
5. **Review conflicts** — identify both target elements and overlapping trigger;
   never overwrite silently.
6. **Test pads** — display normalized elements without invoking gameplay score.
7. **Save configuration** — serialize a complete versioned user configuration.

Navigation must provide Back, Retry, optional Skip, and Reset. Draft export can
support interruption, but completion remains clearly false until all required
steps are accepted. There is intentionally no Canvas, UI Toolkit implementation,
device scan, filesystem store, or automatic cloud/download behavior in this
foundation.
