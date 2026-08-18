# Modern Drum Kit HD Concept

This project-owned image is the visual reference for the main-menu hero drum
kit. It was generated with the built-in image-generation tool and is not used
as a runtime texture.

## Asset

```text
modern-drum-kit-hd-concept.png
SHA-256: 0a150d383929d087670d0aefe28964e3786875964f88901fe8053a96e8895321
```

## Final prompt

```text
Use case: photorealistic-natural
Asset type: visual reference for an HD-quality 3D game asset in Unity
Primary request: create a highly realistic modern premium acoustic drum kit,
photographed as a clean hero product shot and suitable as a modeling reference
Scene/backdrop: dark neutral studio with a subtle concert-stage atmosphere, no
audience and no distracting props
Subject: one complete modern drum kit with a large kick drum and realistic
pedal/beater, snare drum, two rack toms, one floor tom, hi-hat with pedal, left
crash, right crash, large ride cymbal, realistic chrome stands, tension rods,
lugs, hoops, drum heads and cymbal thickness
Style/medium: photorealistic high-end game cinematic reference, physically
plausible proportions and construction, crisp material detail
Composition/framing: wide 16:9, front three-quarter view slightly above
drum-head height, entire kit visible with no cropped hardware, clear silhouette
and spatial separation of every component
Lighting/mood: dramatic but readable studio key light, cyan rim light and subtle
warm amber backlight, realistic reflections and soft shadows
Color palette: satin graphite/black drum shells, brushed chrome hardware,
natural bronze cymbals, restrained cyan accent rings
Materials/textures: fine brushed metal, realistic drum-head translucency,
subtle fingerprints and micro-scratches, rubber pedal surfaces, convincing
wood/metal shell depth
Constraints: mechanically believable drum layout; coherent perspective; no
logos, no brand names, no text, no watermark; no extra drums or duplicated
hardware; no low-poly aesthetic
Avoid: cartoon, illustration, toy drums, warped circles, floating pedals,
impossible stands, excessive neon, exaggerated fisheye perspective
```

## Translation into Blender

The image is translated into original procedural geometry rather than traced
or projected. The Blender generator reproduces the reference's mechanical and
material cues with:

- 128-segment kick geometry and 96-segment drum shells;
- independent batter/resonant heads, bearing rings and hoops;
- individual lug bodies and tension rods;
- rack mounts, basket arms and floor-tom legs;
- articulated tripod and boom stands;
- hi-hat pull rod, clutch, linkage and pedal;
- kick pedal board, hinge, spring, shaft and beater;
- curved, double-sided cymbal meshes with bells and concentric grooves;
- graphite lacquer, chrome, bronze and neutral drum-head material roles.

The generated FBX is the runtime asset. The local `.blend` remains ignored
because the Python generator is the deterministic, reviewable source of truth.
