# HAMPBACK exploratory capture #1 — evidence report

Status: **Exploratory / candidate evidence; not production-ready**

## Provenance and method

- Capture ID: `hampback-exploratory-capture-001`
- Capture ZIP SHA-256:
  `0b0cc8d4f007177ab5d9ae865558a92b3027380ac0bed4eab1acf316b4197162`
- Observed manufacturer label supplied for this analysis: `DREAM S.A.S.`
- Observed MIDI port display name: `eDrum -1`
- Capture schema: version 1
- Events: 422, sequences 1–422 contiguous, monotonic timestamps
- Guided steps: 16 completed; `crash-2-optional` skipped
- Control Change messages: none
- Aftertouch: 13 PolyAftertouch messages on note 26 during `crash-choke`;
  11 on note 59 during `ride-choke-optional`; all observed values were 127

The capture tool independently verified the bundle manifest, JSON, event
count, sequence, and timestamps. Analysis uses positive NoteOn messages per
step and per attempt; NoteOff and NoteOn velocity-zero messages remain evidence
but do not count as positive strikes. The raw ZIP, JSONL, session, and logs are
not committed.

The manufacturer and port strings are observations, not proof of a precise
commercial model. No mapping in this report is a declaration of official
device support.

## Step evidence

| Step | Events | Positive NoteOn evidence | Other evidence | Confidence | Recommendation |
|---|---:|---|---|---|---|
| Kick | 44 | 36×15, 40×7; attempt 3 is clean 36×5 | Early attempts contaminated | High candidate: note 36 | Confirm with 10 isolated strikes; preserve note 40 in evidence |
| Snare center | 10 | 38×5 | — | High candidate | Retain, subject to profile review |
| Snare rim | 10 | 40×5 | — | High candidate | Retain, subject to profile review |
| Tom 1 | 10 | 48×5 | — | High candidate | Retain, subject to profile review |
| Tom 2 | 10 | 45×5 | — | High candidate | Retain, subject to profile review |
| Floor tom | 10 | 43×5 | — | High candidate | Retain, subject to profile review |
| Crash 1 | 10 | 49×5 | Choke step did not reproduce 49 | High candidate | Recapture normal Crash separately before resolving choke |
| Crash 2 optional | 0 | none | Step skipped | Insufficient | Leave unresolved |
| Ride bow | 44 | 51×7, 59×15 across different attempts | Both are plausible | Conflicted | Isolate bow and bell in capture #2 |
| Ride bell optional | 10 | 51×5 | Note 51 also appears in Ride bow | Conflicted at profile level | Isolate bell and bow |
| Hi-Hat closed | 12 | attempt 1: 44×1; attempt 2: 42×5 clean | 44 may be pedal, contamination, or another state | Medium candidate: note 42 | Recapture with pedal held closed |
| Hi-Hat open | 10 | 46×5 | — | High candidate | Confirm in isolated open state |
| Hi-Hat pedal | 10 | 44×5 | — | High candidate | Confirm without striking cymbal |
| Hi-Hat continuous | 4 | 44×2 | no Control Change | Insufficient | Repeat slow pedal movement; do not infer CC4 |
| Crash choke | 53 | 26×8, 46×12 | PolyAftertouch note 26 value 127 ×13 | Conflicted strike; Medium choke evidence | Separate normal strike from five strike-and-grab sequences |
| Ride choke optional | 59 | 51×6, 59×18 | PolyAftertouch note 59 value 127 ×11 | Conflicted strike; Medium choke evidence | Separate strike trigger and choke evidence |
| Free play | 116 | multiple notes, including 55×1 | Uncontrolled context | Exploratory only | Do not promote mappings from free play alone |

Global positive notes observed were 26, 36, 38, 40, 42, 43, 44, 45, 46,
48, 49, 51, 55, and 59. This set is descriptive, not a mapping table.

## Candidate mapping table

| Element | Articulation | Candidate trigger | Channel | Confidence | Evidence | Problems | Recapture needed |
|---|---|---|---:|---|---|---|---|
| Kick | Default | NoteOn 36, velocity 1–127 | 10 (zero-based 9) | High | Clean third attempt | Earlier note 40 contamination | Yes |
| Snare | Head | NoteOn 38 | 10 (zero-based 9) | High | Five isolated positives | — | No immediate gate |
| Snare | Rim | NoteOn 40 | 10 (zero-based 9) | High | Five isolated positives | Also contaminated Kick attempts | Confirm at profile review |
| Tom 1 | Head | NoteOn 48 | 10 (zero-based 9) | High | Five isolated positives | — | No immediate gate |
| Tom 2 | Head | NoteOn 45 | 10 (zero-based 9) | High | Five isolated positives | — | No immediate gate |
| Floor tom | Head | NoteOn 43 | 10 (zero-based 9) | High | Five isolated positives | — | No immediate gate |
| Crash 1 | Bow | NoteOn 49 | 10 (zero-based 9) | High | Five isolated positives | Choke sequence differs | Yes |
| Hi-Hat | Closed | NoteOn 42 | 10 (zero-based 9) | Medium | Clean second attempt | Note 44 retained | Yes |
| Hi-Hat | Open | NoteOn 46 | 10 (zero-based 9) | High | Five isolated positives | Also appears in Crash choke step | Yes |
| Hi-Hat | Pedal | NoteOn 44 | 10 (zero-based 9) | High | Five isolated positives | Also appears in first closed attempt | Yes |
| Crash 1 | Choke | PolyAftertouch note 26, value 127 | 10 (zero-based 9) | Medium | 13 messages in choke attempts | Strike notes conflicted | Yes |
| Ride | Choke | PolyAftertouch note 59, value 127 | 10 (zero-based 9) | Medium | 11 messages in choke attempts | Strike notes conflicted | Yes |
| Ride | Bow | NoteOn 51 or 59 | 10 (zero-based 9) | Conflicted | Different attempts favor different notes | Cannot distinguish bow/bell | Yes |
| Ride | Bell | NoteOn 51 | 10 (zero-based 9) | Conflicted | Clean bell step | Same note occurs in bow attempts | Yes |
| Hi-Hat | Continuous | unresolved | — | Insufficient | No CC observed | Controller unknown | Yes |
| Crash 2 | Bow | unresolved | — | Insufficient | Step skipped | No evidence | Yes if hardware exposes it |

## Limits and disclaimer

This is an evidence report for capture #1, not a verified HAMPBACK profile.
Confidence labels summarize structured observations; they are not probabilities
and do not override conflicts. The candidate profile is not production-ready,
is not auto-selected, and requires confirmation. Only a later `Verified`
profile may enter the built-in library after targeted recapture and review.
