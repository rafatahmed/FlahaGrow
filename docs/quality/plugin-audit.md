# Plugin cleanup audit

Reviewed 2026-09-13 against the local Release build. The compiled-plugin and authorized repository cleanup checks pass. The 27 historical `src/Code` files are preserved at the user’s explicit request; the 10 approved `docs/archive` files have been removed.

| Panel | Registered components | Purpose |
| --- | ---: | --- |
| 00 Setup | 3 | Paths, workspace, engine readiness |
| 01 Materials | 2 | Opaque material and glazing selection |
| 02 Spectral | 2 | Referenced and custom source profiles |
| 03 Annual | 8 | Simulation, progress, loader, illuminance composition/reading, time, marker, plot |
| 04 Electric Light | 5 | IES selection/conversion, placement, compilation, simulation |
| 05 PPFD | 5 | Context, source composition, hour/sensor readers, explicit numeric conversion |
| 06 DLI | 4 | Day/sensor readers, numeric integration, target comparison |
| 07 Energy | 1 | Electrical schedule integration |

The 30 executable registrations have unique names and GUIDs, 206 ports and matching documentation. There are no hidden executable registrations. Eight typed parameter classes remain outside the toolbar because registered ports use them to carry data; the smoke test proves usage and GUID uniqueness. Host discovery also requires the assembly-info and assembly-priority classes; they are not orphaned components.

Runtime artwork contains 23 component images and the logo. Every embedded component image matches a current component. Illustrator and stock-art inputs are design resources, not runtime components. Historical release receipts are evidence about earlier binaries and do not define the current catalog.

## Configuration and source audit

- Three duplicate executable-search implementations now use Core's bounded Radiance discovery. Installation candidates derive from Windows system/user folders and PATH. An explicit missing or incomplete location never falls back to another installation; required executables must come from one bin directory.
- Numeric Lux to PPFD requires a supplied factor. It no longer assumes a universal spectrum. Existing saved instances may retain previously stored numeric inputs; review their factor when rebuilding a definition.
- Numerical-reference scripts require an explicit Radiance bin location. No fixed drive or user-home paths remain in the checked production C# or PowerShell tools.
- Stable GUIDs, file-format names, units, the supported non-leap hourly axis, scientific coefficients and documented quality presets remain explicit code values. They are contracts and calculation inputs, not portable-path defects. This audit does not claim all literals should be removed.
- Packaging source stages the plugin/Core pair, notices and library assets; it does not include the historical component scripts. The matching assemblies were subsequently installed locally; see the [deployment receipt](../releases/plugin-audit-deployment-2026-09-13.md). No package registry publication was performed.

## Deep audit fixes

| Defect found | Correction and regression evidence |
| --- | --- |
| Process stdin could block before timeout and pipe draining started | Timeout now includes input writing; output drains concurrently. Blocked-input and large bidirectional-output tests pass. |
| Illuminance reader duplicated cache validation and accepted unknown modes | Uses the shared locked Core reader; rejects unknown modes, changed dimensions and corrupt provenance before publishing values. |
| IES conversion could reuse stale output and rewrite geometry triples as RGB | Isolated staging requires fresh RAD and referenced DAT files. Primitive-aware rewriting targets light/illum emitters only and preserves geometry and literal DAT paths. |
| IES conversion blocked evaluation and repeated while Run stayed True | Background false-to-true execution, bounded process capture, two-minute timeout, cancellation on changed inputs/document closure, exclusive output lock and staging cleanup. Idle evaluation writes nothing. |
| IES channel/multiplier/DAT validation was incomplete | Finite nonnegative RGB with at least one positive channel, overflow-safe normalization, finite positive optional multiplier and existing optional DAT required. Explicit DAT is preserved. |
| Nonfinite DLI/energy inputs and overflowing outputs could escape | Reject invalid values before outputs. Energy checks duration/total overflow; annual DLI mean avoids overflowing a sum of finite daily results. |
| Invalid plot metadata could silently select numeric-only behavior | Connected or supplied invalid attributes produce an error. Unwired numeric-only plotting remains available. |
| Invalid geometry and unsafe generated command paths were accepted | Placement points/rotations and marker sizes are validated; referenced files must exist and unsupported command-path characters are rejected. |
| Documentation could certify an old build | Successful smoke stamps bind source and runtime hashes. Documentation audit rejects stale evidence. Local packaging runs Core tests, smoke checks and the inventory/documentation audit before staging. |

## Validation

- Release build: zero warnings/errors.
- Local Yak packaging: all gates passed and produced `artifacts/yak-staging/flahagrow-0.1.0-rh8_33-win.yak`. Its 86 entries contain byte-matching validated plugin/Core assemblies, with historical source and Rhino host assemblies excluded. The matching assemblies were subsequently deployed locally as recorded in the deployment receipt; the Yak package was not published.
- Documentation freshness: an intentionally stale smoke stamp was rejected, the original stamp was restored and the full audit passed again. Active documentation has no broken local file links.
- Core: 198 passing tests, including explicit-location isolation and coherent executable selection.
- Smoke runner: all 30 components complete XML archive round trips with identity, revision and port contracts retained; typed parameters and icons are covered.
- Integration: annual provenance and cache rejection, workspace identity, plant-light readers, hourly integral trees, plot matching, explicit numeric factors, weather-derived timing and numeric/geometry rejection pass. Local Radiance fixtures exercise real rmtxop and ies2rad, including background completion and held-True reuse.
- Component documentation: all eight category references, 30 components and 206 ports match registration metadata, defaults, revision notes and local links.
- Plugin audit: 66 production source files checked, tool syntax checked, inventory issues empty. The machine-readable result is generated at `artifacts/plugin-audit.json`.

Run the commands in [development and packaging](../architecture/plugin-development.md). The inventory report records no issues and explicitly records preserved reference source. These are bounded automated checks, not a guarantee that no software defects remain.

## Remaining scope

`src/Code` remains historical reference material outside the compiled projects and package, as requested. No files in that directory were changed. Historical release receipts and source artwork are retained; they are not registered executable components.

This build is installed in the local Grasshopper Libraries folder; see the [deployment receipt](../releases/plugin-audit-deployment-2026-09-13.md). Dialog behavior, document scheduling and actual canvas wiring after consolidation still require Rhino host acceptance. The real Radiance fixtures cover one bundled IES conversion and matrix operations; they do not establish all-fixture compatibility or physical PPFD accuracy for a study. Full numerical reference runs, spectral assumptions and model-specific acceptance remain separate checks.

RAD/DAT publication is atomic per file, not a transaction over the set: a failure during final moves can leave a partially updated set. Cache readers retain full content validation; representative large-grid/storage performance remains unmeasured. Compile Luminaires accepts trusted Radiance command text and remains level-triggered; it was not redesigned as a scene parser. These limits are not certified by the component inventory audit.
