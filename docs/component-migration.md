# Component migration and current wiring contract

Updated 2026-09-10. Use the [current status](current-status.md) for release/installation status and the [25-script audit](component-io-audit-2026-09-09.md) for the complete input/output inventory and known differences.

## Identify components before reconnecting

Same display names do not imply the same GUID or ports. The visible Setup trio is:

| Visible component | Implementation / GUID | Purpose |
| --- | --- | --- |
| Simulation Paths | SimulationPathsSetupComponent / `71ce89f2-1439-4730-915f-07436692926c` | Resolves locations; outputs typed Paths and text Folder/Library. |
| Working Directory | ProjectWorkspaceComponent / `d236c57b-eab1-4329-8e4e-beb2285ba04d` | Opens/initializes a named analysis; outputs Project/Analysis contexts and folders. |
| Radiance Status | RadianceSetupComponent / `9cd39fc4-7c35-4aee-a247-1c8b980c4b17` | Checks a workflow-specific environment; outputs Radiance, Ready and diagnostics. |

Python Working Directory maps historically to hidden WorkingDirectoryComponent (`3bc3011e-2b2f-4c14-9344-dcb3554f3722`), not visible Simulation Paths. Python has seven folder toggles; that compiled legacy helper has six and lacks spectral point-in-time render. Python Radiance Version maps to hidden RadianceVersionComponent (`272aa83d-9898-460d-8cbd-7f49374153ba`), not visible Radiance Status.

See the audit's hidden identity table for all six hidden Setup components. Compiled GUID preservation does not automatically replace GhPython components. Exact old wire order/type hints/tree access still require a representative saved definition.

## Annual daylight migration

1. Simulation Paths **Paths** → Working Directory **Paths**. Choose Workflow=0 and initialize/open the analysis.
2. Working Directory **Analysis** → Radiance Status **Analysis** and Annual Simulation **Analysis**.
3. Radiance Status **Radiance** → Annual Simulation **Radiance**. Bin text alone does not carry readiness or a custom calculation library.
4. Export the model through Honeybee ModelToRad. Its source root → Annual Simulation **Project**; EPW file → **EPW**.
5. Source root must contain `model/scene/envelope.rad`, `envelope.mat`, `envelope.blk` and a sensor grid in `model/grid`, unless Pts supplies points. Workspace Inputs/Runs folders alone are not an exported model.
6. Annual Simulation **Folder** → Progress **Folder** and Load Annual Result **Folder**. This is one isolated run, not the source root or Runs container.
7. After valid completion, Build the cache. F32 → a cache-native reader. Use Hourly PAR for all sensors at one hour; PAR Each Sensor for all hours at one sensor. Illuminance readers require explicit Mode/index and Run=True.
8. A sensor's annual PPFD list → Annual Plot PPFD for Sensor / Annual DLI; Daily DLI → DLI Target. The supported DLI/plot case is 8,760 hourly values.

Keep the original sensor order. Optional Pts creates upward normals; exported oriented grids should use the .pts source. List-conversion helpers Hourly PPFD / PPFD Each Sensor accept numeric lists, not cache paths.

Numeric quality levels now match named presets, but the independent Python custom-parameter port has not been restored. Custom spectral CSV factors still have numerical drift (IO03). Calendar/leap-year and trigger behavior remain open. Run=True launches a new run on every solve; a false→true transition does not launch the previously prepared folder.

## Result compatibility

The binary format remains little-endian float32, row-major hours × sensors, with sibling metadata keys `sensors`, `hours`, `ncomp`. New caches also carry run identity, source signature and cache hash.

New progress/cache builders require a schema-2 manifest, successful command states and final scalar ASCII matrices with matching dimensions, finite values and nonnegative illuminance. Headers may contain copied blank provenance before FORMAT; the data separator follows FORMAT. Malformed numeric data must never be skipped to make a cache build succeed.

Flat legacy result folders and schema-1 runs require regeneration under the supported pipeline. Do not fabricate manifests or success states. Existing valid dimension-only caches can be read directly by legacy-compatible readers; they do not gain provenance validation. Selected negative/nonfinite cache values now fail lux/PPFD reading.

Load Annual Result writes F32 and metadata; it no longer exports a merged .ill. It does not delete intermediate data. See [run isolation](annual-run-isolation.md) and the [live-result audit](annual-result-audit-2026-09-10.md).

## Materials and electric preparation

Setup Library is the FlahaGrow asset root; connect it to Materials/Glazing/IES selector folders. Radiance Status Lib is a different calculation library. Material selectors output modifier names, not Honeybee objects or complete definitions; the external model/export workflow must resolve those definitions.

For electric preparation, use Workflow=1 and its checked Radiance environment. IES selector → IES to Radiance → Lighting Geometry → Compile Luminaires. Both conversion and compilation receive the same text Project root and use `Project/Luminaire_files`. Python used `parent(folder)/Luminaire_files`: reconnect the project root explicitly.

The chain ends at luminaries.rad; it does not automatically simulate electric illuminance, combine it with daylight, or produce a power schedule. IES rerun file ownership/rewrite issues (IO07) and selector parsing/persistence issues remain open.

## Migration acceptance

Preserve published compiled GUIDs and port indices; append compatible optional ports. Resolve known Python defects rather than treating every legacy behavior as correct. Documentation mapping is reconciled here, but C06's saved-definition verification remains pending. No full numerical workflow certification is implied.
