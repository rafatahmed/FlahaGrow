# FlahaGrow example-definition audit

Audited 2026-09-11. Target: `C:\Users\rafat\Downloads\FlahaGrow Example.gh` (84,953 bytes; modified 2026-09-10 21:23 local time). The definition was not changed.

## Scope and evidence boundary

The file is present, but its saved Grasshopper object graph cannot be reliably inspected from this repository or by raw text search. This audit therefore verifies the current compiled contracts against the example's intended workflow; it does **not** claim the saved component GUIDs, parameter values, wire endpoints, or document state have been proved.

Assumption: the example is intended to demonstrate the annual daylight workflow shown in its supplied canvas image. If it instead targets a different workflow, reopen it in Rhino and replace this audit with a document-level inventory.

The current source and [component I/O audit](component-io-audit-2026-09-09.md) are authoritative for compiled ports. The old Python scripts are behavioral reference only; they are not proof that a saved GhPython node has been replaced.

## Verified current workflow contract

| Example stage | Required current connection / condition | Audit result |
| --- | --- | --- |
| Setup | Simulation Paths **Paths** → Working Directory **Paths**. Working Directory **Analysis** → Radiance Status **Analysis**. | Supported typed connections. |
| Checked execution environment | Radiance Status **Radiance** → Annual Simulation **Radiance**. For annual daylight, the environment must be ready and compatible. | Supported; a Bin text path alone does not carry the checked library/workflow. |
| Honeybee source | Annual Simulation **Project** is a Honeybee ModelToRad export root, not the Setup workspace root. It needs `model/scene/envelope.rad`, `envelope.mat`, `envelope.blk`, and a `.pts` file under `model/grid` unless **Pts** is supplied. | Required. |
| Run ownership | Working Directory **Analysis** → Annual Simulation **Analysis** is optional but recommended. The runner retains an unchanged manifest-owned run across solves and creates a new run only for changed inputs or an explicit new launch after completion. | Supported; do not reuse an old flat result folder. |
| Execution and progress | Set Annual Simulation **Run** only to launch jobs. Its **Folder** → Annual Simulation Progress **Folder**. Completion requires successful declared commands and valid final matrices. | Supported. |
| Cache | The same **Folder** → Load Annual Result **Folder**. Build only after Progress validates all parts. | Required; cache builder requires the schema-2 run manifest. |
| Point-in-time lux | Cache **F32** → Illuminance Point in Time **F32**; set Mode=`hour`, supply a zero-based hour index, and Run=True. | Returns one value per sensor. |
| Sensor annual lux | Cache **F32** → Illuminance Sensor **F32**; set Mode=`sensor`, supply a zero-based sensor index, and Run=True. | Returns one value per hour. |
| PPFD/DLI | Feed extracted numerical lux lists to list-based PPFD components, or use the cache-native PAR readers. DLI/annual plots expect 8,760 hourly values. | Supported with known spectral and timestep limitations. |
| Electric preparation | Use workflow 1 and a ready electric Radiance environment. IES → IES to Radiance → Lighting Geometry → Compile Luminaires uses one project-local `Luminaire_files` folder. Connect the resulting `.rad`, schedule, and sensors to Electric Annual Simulation for an electric annual matrix. | Daylight-plus-electric combination and real Rhino export acceptance remain open. |

## Migration risks found

1. **Setup names are ambiguous.** The visible Working Directory is the typed project/analysis component, while the hidden legacy WorkingDirectory component has a different folder-toggle interface. Visible Radiance Status is also different from the hidden legacy Radiance Version component. Confirm GUIDs before reconnecting a saved canvas.
2. **Project and Folder are different roots.** Project is the immutable Honeybee export source; Folder is the generated isolated run. Sending the workspace root to Project, or the source root to cache/progress, is unsupported.
3. **Reader mode is not inferred from the component name.** Both illuminance readers have `F32`, `Mode`, `Index`, and `Run` inputs. Set the mode explicitly; leading/trailing whitespace is accepted, other values select sensor behavior in the present source.
4. **Old result folders are intentionally rejected by the cache builder.** Do not create a manifest or success files by hand. Regenerate a new run with the current runner.
5. **A successful cache is not scientific validation.** The inspected legacy study had negative final illuminance originating upstream; the current cache/progress path rejects nonfinite or negative final illuminance rather than clamping it. See the [annual-result audit](annual-result-audit-2026-09-10.md).

## Required Rhino audit to close this record

1. Restart Rhino, open the example, and record the loaded assembly path/product version.
2. For every FlahaGrow node, record its GUID, input/output index, persistent data, and wire sources. Compare this with the [migration guide](component-migration.md) and the I/O audit.
3. Confirm the annual runner has the appended **Radiance** and **Analysis** inputs and that they are wired from the visible Setup trio.
4. Confirm the Project root contains the required ModelToRad scene/grid files; confirm Folder is the unique run passed to Progress and Load Annual Result.
5. Run a small new study, wait for validated completion, build the cache, and verify one hour-mode and one sensor-mode read against the expected sensor ordering.
6. Save, close, reopen, and recheck selector state, menu settings, timer refresh, and all wires.

Until that Rhino-host audit is complete, the example is **not certified migrated**. This conclusion is intentionally narrower than the earlier screenshot-based narrative: source-level compatibility is verified, but saved-definition parity is unverified.
