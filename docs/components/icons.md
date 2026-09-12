# Component icon artwork

The supplied PNGs in `docs/Icon` are 267 × 267 RGBA images. The plugin embeds
32 exact-name component icons and `FlahaGrow_Icon_logo.png`; the logo serves
as both assembly and FlahaGrow category artwork. No external icon directory
is needed at runtime. Source PNG bytes are unchanged.

`ComponentIcons` maps the component's exact display name to its embedded PNG.
This includes matching hidden compatibility components with the same name.
It creates a cached 24 × 24 bitmap using high-quality proportional resampling,
preserving alpha. It does not dispose bitmaps while Grasshopper uses them.
Missing names return the original no-custom-icon fallback, not an unrelated icon.

The [README catalog](../../README.md#complete-component-catalog) records which
placeable components have supplied artwork. In particular, **PPFD Each Sensor**
does not automatically supply artwork for **Annual PPFD at Sensor**: they are
different components. New icons can be supplied using exact component names.

`FlahaGrow_Icon_System-18.png` is deliberately excluded until its intended
component is identified. Illustrator `.ai` and temporary files are not embedded
or otherwise modified. The authoritative artwork is the user-supplied PNG set;
no generated or replacement designs were introduced.

Validation: the component smoke runner checks all 33 embedded images decode
to 24 × 24, all 32 component filenames match registered components, component
Icon properties return their expected bitmap, caching works, and unknown names
do not receive unrelated artwork. The Release build and timing/plant-light
regressions passed. Live Rhino toolbar, canvas, high-DPI and theme legibility
still need visual acceptance; this change does not itself deploy the plugin.
