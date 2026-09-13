# Component icon artwork

The plugin embeds 23 exact-name component PNGs and the FlahaGrow logo from `docs/Icon`. All top-level PNGs are consumed by the plugin; no exclusions retain retired component artwork. The material artwork is named `Opaque Material.png` to match the consolidated component.

`ComponentIcons` resolves exact display names, caches proportional 24 × 24 bitmaps and preserves alpha. Components without supplied artwork use Grasshopper's normal fallback. Source Illustrator files and the assets subdirectory are design inputs, not executable components or embedded runtime resources.

The [README catalog](../../README.md#complete-component-catalog) lists icon availability. The smoke runner checks that every embedded component icon maps to a current registration, every bitmap decodes to 24 × 24, and the icon cache returns the expected instance. The documentation checker verifies icon availability for every component.

Toolbar appearance, high-DPI scaling and theme legibility still require a Rhino visual check. This source cleanup has not been deployed.
