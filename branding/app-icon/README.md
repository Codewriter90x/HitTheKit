# HitTheKit app icon

This directory contains the production exports for the selected HitTheKit
flaming-drum emblem.

## Source and provenance

The source artwork was generated with OpenAI's built-in image generation tool
for this project. The creative direction was: a premium dark rock-game icon,
a realistic bass drum with crossed drumsticks, controlled flames and sparks,
large metallic `HTK` initials, high contrast, and a transparent exterior.

The 1254 px source is preserved without repainting in:

`master/HitTheKit-Icon-Source-1254.png`

## Export families

- `transparent-png/`: the original free-form emblem on transparency, exported
  at 1024, 512, 256, 128, 64, and 32 px.
- `app-png/`: the emblem on a dark rounded application tile with safe margins,
  exported at 1024, 512, 256, 128, 64, 48, 32, 24, and 16 px.
- `macos/HitTheKit.iconset/`: the complete standard and Retina iconset.
- `macos/HitTheKit.icns`: the multi-resolution macOS application icon.
- `windows/HitTheKit.ico`: a multi-resolution Windows icon containing 16, 24,
  32, 48, 64, 128, and 256 px PNG-compressed frames.
- `web/`: 16, 32, and 48 px favicons plus opaque 180 px Apple Touch and
  192/512 px PWA icons.
- `mobile/`: an opaque, unmasked 1024 px iOS App Store icon and 512 px Android
  legacy icon. The platform applies its own final shape.
- `social/`: ready-to-use 512 px GitHub avatar and 1024 px release artwork.
- `preview/HitTheKit-Icon-Family-Preview.png`: a visual QA sheet.

## Regeneration

From the repository root on macOS:

```bash
SWIFT_MODULECACHE_PATH="${TMPDIR:-/tmp}/hitthekit-swift-module-cache" \
CLANG_MODULE_CACHE_PATH="${TMPDIR:-/tmp}/hitthekit-swift-module-cache" \
swift scripts/generate-hit-the-kit-icons.swift \
  /path/to/HitTheKit-Icon-Source-1254.png \
  branding/app-icon
```

The generator derives every output from the absolute source image. It does not
incrementally rescale already reduced files, so repeated runs do not compound
downsampling artifacts.

The macOS packaging scripts install `macos/HitTheKit.icns` into each generated
application bundle and update `CFBundleIconFile` before code signing.
