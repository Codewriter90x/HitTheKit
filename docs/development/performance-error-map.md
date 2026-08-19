# Performance error map

The results screen groups real `HitResult` values by four-bar Practice Lab
section and drum piece. It highlights where the player struggled, rather than
showing only a whole-song average.

## Analysis

`PracticeErrorMapAnalyzer` lives in Core and consumes the same resolved notes
used by scoring. Each section/piece cell records Perfect, Good, Early, Late and
Miss counts. Its displayed accuracy uses the existing gameplay weights:

- Perfect: 100%;
- Good: 75%;
- Early or Late: 50%;
- Miss: 0%.

The map is deterministic and its section boundaries are start-inclusive and
end-exclusive. Results outside the declared song sections are ignored rather
than assigned to a misleading bucket.

## Targeted practice

The results screen lists the four weakest cells and exposes **Train weakest
area**. That action selects the corresponding Practice Lab range, filters the
existing matcher and seeks the existing DSP/audio transport with the normal
two-beat preparation window. It does not build a second chart or matcher.

Error-map state is per attempt and in-memory. Long-term history is deliberately
left to a later schema change in the existing progress persistence service.
