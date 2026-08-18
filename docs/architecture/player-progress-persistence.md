# Player progress persistence

## Purpose

HitTheKit keeps learning progress, completed run results, and active practice time in one versioned player-progress document. `GameplayLearningProgress` remains the curriculum-facing API, but delegates to the same `GameplayProgressService` used by gameplay results and backup operations.

## What counts as practice

Practice time advances only while the shared gameplay scene is in `GameplayRunState.Playing` and the application has focus. Countdown, pause, menus, results, background time, and stalled frames do not add unbounded time. Pending time is flushed every 15 seconds and at pause, restart, results, scene exit, disable, and application shutdown.

The lifetime total includes completed and interrupted attempts. A completed result stores the active duration of that attempt alongside score, accuracy, combo, and judgment counts.

## Local storage

Standalone builds use:

```text
Application.persistentDataPath/player-progress.json
```

Writes use a temporary sibling followed by an atomic replacement. Invalid existing data is loaded fail-closed and is not silently overwritten. The Unity Editor uses an in-memory store so automated and manual editor tests cannot alter a player's standalone progress.

The current schema is version `1`. Required top-level collections, stable IDs, timestamps, lesson/speed pairs, ranges, unique session IDs, and result arithmetic are validated during load.

## Backup and restore

The Settings panel exports a portable JSON file to:

```text
Documents/HitTheKit Backups/HitTheKit-progress-backup.json
```

Import is intentionally replace-only. Merging totals would allow the same backup to double-count practice time. Before a valid import is committed, the current state is written to `HitTheKit-progress-before-import.json`. Validation completes before in-memory or local progress is replaced.

To move to another Mac:

1. choose **Export backup**;
2. copy `HitTheKit-progress-backup.json` to the same backup folder on the new Mac;
3. install and open HitTheKit;
4. choose **Import and replace**.

The backup contains no MIDI captures, device identifiers, local paths, account data, or cloud credentials.

## History boundary

Lifetime practice seconds, completed-session count, and lesson bests are retained. The detailed recent-session history is bounded to 1,000 entries to prevent unbounded file growth.
