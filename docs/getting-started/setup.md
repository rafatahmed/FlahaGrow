# Setup

The visible Setup panel is optional for basic components, but it is the safest way to create a workspace and pin execution to a checked Radiance installation.

```text
Simulation Paths ──► Working Directory ──► Radiance Status
                         Analysis                 Radiance Environment
```

1. **Simulation Paths** resolves a project location and optional asset-library location without creating folders. Modes are Auto, project-relative, system, and custom.
2. Connect `Paths` to **Working Directory**, choose workflow `0` (annual daylight) or `1` (electric-light preparation), then press `Initialize`. `Adopt` is required to initialize a nonempty unrecognised folder; existing files are preserved.
3. Connect `Analysis` to **Radiance Status**. It checks required files, runs `rcontrib -version`, and runs a small workflow-specific fixture. `Ready` is an installation readiness result, not production-simulation validation.
4. Connect `Radiance` to Annual Simulation or IES to Radiance. This prevents silently selecting a different installation at execution time.

Setup contexts are transient typed Grasshopper connections. Keep their wires in a definition so components can reconstruct contexts from persisted configuration after reopening.

The bundled asset root is `src/Library/FlahaGrow_Library_Small`; it contains `RadMaterials`, `RadGlazing`, and `RadIES`. It is distinct from the Radiance calculation library. Selectors accept the asset root, its parent, or a legacy direct section folder. Radiance discovery uses bounded direct checks of configured locations, common locations, and PATH; its context menu can select a discovered installation or custom calculation library without changing system environment variables.

## Minimum daylight PPFD/DLI definition

Setup prepares the engine; it does not replace the model export or provide a
crop-light assumption. A working annual daylight definition needs:

1. A valid exported Radiance source root and a 365-day EPW, as described in
   [the model boundary](../architecture/ladybug-honeybee-integration.md).
2. An upward-facing sensor grid at the plane at which plant light is to be
   assessed. Its ordering must remain unchanged when sensor-specific results
   are read.
3. A ready annual-daylight Radiance Environment connected to **Annual
   Simulation**.
4. A source-specific profile from **Spectral Profile**, or an explicit factor/CSV
   through **Custom Spectral Profile**. Lux to PPFD requires an explicit
   factor; no default represents every daylight, glazing or luminaire case.
5. A complete hourly PPFD series and a 3,600-second timestep for a normal
   annual DLI calculation.

Use the annual workflow to produce the validated lux cache first. Then select
the appropriate PPFD/DLI reader path from the component catalog. The Setup
check only establishes that a small Radiance fixture can execute; it does not
validate a greenhouse model or conversion factor.
