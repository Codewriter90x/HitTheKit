# Universal Render Pipeline migration

HitTheKit uses Universal Render Pipeline 17.5 with Unity 6000.5.6f1. The
project-level pipeline and renderer assets live under
`Assets/HitTheKit/Rendering` and are assigned to Graphics Settings and every
quality level.

The migration deliberately keeps gameplay, MIDI, timing and scene composition
unchanged. The existing pad material uses `Universal Render Pipeline/Lit`, and
`DrumPadVisual` writes both `_BaseColor` and `_EmissionColor` through a material
property block.

To recreate or repair the project assets from the Unity Editor, run:

```text
HitTheKit.Unity.EditorTools.UrpMigrationTool.Migrate
```

The command is idempotent. It creates the canonical renderer and pipeline
assets when absent, populates required URP resources, assigns the pipeline to
all quality levels, converts the pad material, and keeps Unity-generated global
settings with the other HitTheKit rendering assets.

The migration intentionally does not add custom renderer features,
post-processing volumes, or gameplay-specific lighting. Those remain separate
visual-design decisions.
