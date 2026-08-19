# Asset provenance register

This register covers the visual and media assets distributed by HitTheKit. It
is an engineering record for release review, not a legal ownership opinion.
Release packaging must fail closed if a shipped media asset is absent from this
register or its recorded SHA-256 no longer matches.

The complete machine-readable inventory is
[`ASSET_PROVENANCE.sha256`](ASSET_PROVENANCE.sha256). CI executes
`tests/scripts/asset-provenance-contract-tests.sh`, which fails when a tracked
media asset is missing from that inventory, an inventory path is missing, or a
recorded hash changes. The tables below explain the creative origin and purpose;
the hash inventory is the authoritative exhaustive list.

## Project-directed generated artwork

The following artwork was generated specifically for HitTheKit from
project-authored creative direction. It contains no requested third-party logo,
character, album artwork, performer likeness, or copied game interface. The
maintainer must retain the original generation session or prompt record outside
the public repository for chain-of-title review.

| Asset | SHA-256 | Purpose |
| --- | --- | --- |
| `Assets/HitTheKit/UI/DeviceSetup/Images/guided-kit.png` | `007108bf71e5ae1a41c30c499946d2f313dc59d76bcc53f236e4374ca7a5dc5a` | Guided mapping illustration |
| `Assets/HitTheKit/UI/DeviceSetup/Images/preset-extended.png` | `0fb0c434958f9dfcfdf7c463efcaffceabd48997f36a2dc3763ab6ad78e39867` | Extended kit preset |
| `Assets/HitTheKit/UI/DeviceSetup/Images/preset-minimal.png` | `41c21e37d6c7244d6f42c2882e7b2837764f31c287e12ccf98d062cfa4cf2fd2` | Minimal kit preset |
| `Assets/HitTheKit/UI/DeviceSetup/Images/preset-standard.png` | `75ba6bf9a6688c623060477f89ba0fe7af9736b33d2c76417960646fda01b60f` | Standard kit preset |
| `Assets/HitTheKit/UI/Gameplay/Backgrounds/arcade-neon-environment-v2.png` | `105d143259c02d577ca267fd2f0bd6d24f62078fc649c9f321ffedb23a545d88` | Arcade environment |
| `Assets/HitTheKit/UI/Gameplay/Backgrounds/concert-stage-environment-v2.png` | `a123503bd42bbdd04dc4dbfe3b5b89ecde46d5e44678b39a7f5591bf17ca87b3` | Concert environment |
| `Assets/HitTheKit/UI/Gameplay/Backgrounds/precision-grid-environment-v2.png` | `85db4c7b39ec87f3130c7361fcefd6294dceb1f9699def11e16845a38c02c7a4` | Precision environment |
| `Assets/HitTheKit/UI/MainMenu/Backgrounds/stage-command.png` | `ad6047b5e1af7343eceb2706883aa2b8b312ef8ee6d301a5935cda4e35fddfd9` | Main-menu background |
| `Assets/HitTheKit/UI/MainMenu/Icons/drum-kit-neon.png` | `ec15af49ecfd1e21495072a0f1551118044fcea8e67ca27fd472a0bfd2120b06` | Device-setup menu icon |
| `Assets/HitTheKit/UI/MainMenu/Icons/learn-neon.png` | `1406d21ebc1cdb8fe6b0eaf23728264d1569a985dd4b4a568693a119ced3c593` | Learn menu icon |
| `Assets/HitTheKit/UI/MainMenu/Icons/play-neon.png` | `09d65de2c22d0f9b19c0558cfe772b9ebd77ccd95f522031ef5dc9ed0950e562` | Play menu icon |
| `website/assets/images/hitthekit-readme-hero.jpg` | `6808877a6e952da9e6e1a4f660d46fe6b4e60b99cbad97311a3d0e7361fc7f21` | README and GitHub Sponsors hero artwork |
| `website/assets/images/hitthekit-social-preview.jpg` | `8d5610553900157921047bbe8c4ea4ccc02c6e0f925041dc3f9be831e09994c8` | Website and repository social preview |
| `website/assets/images/hitthekit-launch-square.png` | `7926db15a73249f67d9886d0ec4c3ae8c6f19a83cb7289d452220a415344b4a8` | Square community and social launch artwork |

The older gameplay background variants and design-reference images are retained
only as project-authored visual-development evidence. They are not third-party
source material.

The tracked `HitTheKit-MainMenuStage.fbx` is project-owned procedural geometry
created for the menu scene. Website scene images, the FBX, older variants, and
design evidence are individually hashed in the machine-readable inventory.

## First-party application captures

These screenshots were captured directly from the HitTheKit Unity application
using only project-owned interface, environment and synthetic demo content.
They contain no commercial song, recording, chart, artwork or performer
likeness. The JPEG files are publication derivatives of the original PNG
captures retained in the private development archive.

| Asset | SHA-256 | Purpose |
| --- | --- | --- |
| `website/assets/images/screenshots/main-menu.jpg` | `7f00a1b51d05b327c279f0bf199b646bc9899dac5b76a798d0932f7d0e41338a` | README application preview: main menu |
| `website/assets/images/screenshots/gameplay-concert-stage.jpg` | `246771f958d41adc02cf7ef15533ea977f9d0a35676bd9c9edf197eec37f510d` | README application preview: synthetic Concert Stage gameplay |

## Programmatically generated branding

`branding/app-icon/master/HitTheKit-Icon-Source-1254.png` has SHA-256
`7af83da9e58955192f54913a81dbb0dd26dba281cc25a7b0c1a30c175d3812a8`.
All other tracked icon sizes, the `.icns` bundle, transparent marks, favicons,
mobile exports, preview, avatar, and release artwork are deterministic
derivatives produced by `scripts/generate-hit-the-kit-icons.swift`.

## Audio and charts

- `Neon Circuit`, lesson accompaniment, lesson charts, metronome, and feedback
  sounds are synthesized deterministically at runtime from project source.
- The repository and public build contain no third-party recording, stem, MIDI,
  transcription, chart, lyric, album artwork, or commercial-song catalog.
- Player-owned local song folders are data outside the repository and release
  artifact. Users are responsible for having permission to use those files.

## Release review

Before every public release:

1. enumerate packaged raster, vector, font, audio, video, and chart assets;
2. reconcile the list with this register and `THIRD_PARTY_NOTICES.md`;
3. regenerate hashes from the exact release commit;
4. retain generation/source evidence privately;
5. stop the release if provenance or redistribution rights are unresolved.
