# Python-to-compiled component input/output audit

2026-09-10 follow-up: [live-result audit and fixes](annual-result-audit-2026-09-10.md). Numeric/named annual quality mapping and whitespace reader modes are now corrected in the working tree; independent custom-quality input design and other IO findings remain open. The historical probes below describe the September 9 baseline; the executable quality probe now asserts equality.

Date: 2026-09-09. Baseline: commit `4f28a33` **plus the current working-tree changes**, including F01–F04 and F12. This is an audit, not an implementation change.

The compiled plugin preserves much of the Python workflow's purpose, but it is **not a drop-in input/output equivalent**. Setup is a deliberate redesign; several downstream differences are undocumented regressions or removed capabilities. The earlier `Match` labels in [legacy-compiled-comparison.md](legacy-compiled-comparison.md) should not be used as compatibility certification.

## Scope and evidence limits

- Reviewed all **25 Python scripts**, including uppercase `.PY` files, and their C# counterparts, shared readers, visible Setup components, and additional metric helpers.
- Python contracts below come from referenced globals, `locals().get` / `globals().get`, assigned output variables, and execution logic. **The scripts do not serialize Grasshopper port order, nicknames, access settings, or type hints.** No `.gh`, `.ghx`, or `.3dm` reference definition was found by the repository file search. Python order below is a logical mapping, not a verified saved-canvas order. Variables assigned by scripts are identified as candidate outputs where canvas exposure cannot be established.
- Compiled port order, types, defaults, and optionality come from `RegisterInputParams` / `RegisterOutputParams`; behavior comes from `SolveInstance` and helpers. A C# `list` parameter is not proof of Python's explicit whole-tree flattening behavior.
- Existing compiled GUIDs support existing compiled definitions. They do not automatically replace arbitrary GhPython script instances. Same display name is not the same component identity.
- No application code was changed for this audit. New source probes isolate pure calculations; no dialogs, simulation launches, or external services are involved. The unrelated untracked `None/` directory was excluded.

Notation: `T[]` means a compiled list port; otherwise access is item. `?` means explicitly optional. `=value` means a registered default. Python defaults are stated only when the script provides them; missing saved-canvas defaults remain unknown.

## Findings and disposition

| ID | Priority / disposition | Evidence and user-visible consequence | Required follow-up |
| --- | --- | --- | --- |
| IO01 | High, open documentation/identity gap; relates to C06 | Visible **Working Directory**, **Simulation Paths**, and **Radiance Status** differ from same-named hidden legacy helpers. Existing migration tables describe old ports under new names. Python Working Directory also has **seven** toggles, while the hidden compiled version has six: `spectral_point_in_time_render` is absent. | Publish the visible workflow and identity-specific migration map; decide whether the missing legacy folder capability needs restoration. Preserve published compiled port indices. |
| IO02 | High, open result-affecting difference | Python annual inputs `_details` and `_custom_parameter` are independent. C# exposes one `Detail`. Python numeric level 4 uses `-lw 0.0015 -ab 3 -ad 1536`; C# `Detail="4"` uses mid diffuse settings while retaining high direct-sun settings. Numeric levels 1, 2, 4, 5 are not equivalent. C# custom strings select fallback direct-sun settings, so users cannot combine high direct sun with custom diffuse arguments as before. | Define one quality contract supporting levels 1–5 and a separate optional custom override appended after existing ports; coordinate with F08 validation. |
| IO03 | High, open numerical regression | All three compiled spectral components use `SpectralMath`: photons are summed over 380–780 nm without Python's 400–700 nm PAR mask, and Python's tabulated `OPN1_1NM` is replaced with a Gaussian expression. Identical CSV inputs therefore give different factors. Source probes below reproduce this. | Restore/reference-test the agreed spectral contract before relying on custom-CSV PPFD/DLI results. Treat predefined constant factors separately. |
| IO04 | Medium, open spectral UI/data-contract loss | Python Load Spectral Data has a modeless calculation table and `_calculated.csv` export. C# only opens a file picker; `File` returns the filename, not an export path. Python matches CSV columns by headers; C# always reads the first two columns, silently drops parse failures, and throws on duplicate rounded wavelengths. Python selection initially outputs `None`; C# immediately emits D65 `0.018043`. Manual numeric edits can be overridden by the still-selected standard source. | Restore needed inspection/export and robust CSV schema behavior; make default versus confirmed selection explicit; fix custom selection and persistence (F09). |
| IO05 | Medium, intentional annual compatibility break requiring explicit migration | Python cache builder expects four part files and writes merged `annualRfinal.ill`, `.f32`, and richer metadata. C# supports one/four manifest-declared parts, requires schema-2 run/state validation even with Build=False, and writes `.f32` + metadata without the merged `.ill`. Old flat result folders are rejected. | Keep isolation and strict validation; document regeneration/import policy and decide whether merged `.ill` export remains required. Existing binary readers can consume old valid `.f32` + dimension metadata directly; that does not validate run provenance. |
| IO06 | Medium, open reader output/access differences; relates to F11 | Python illuminance scripts assign `result`, `nrows`, `ncols`, `meta`, `status`; C# exposes only Lux and Status. Exact historical exposed ports need a saved definition. Python strips whitespace from mode; C# does not: `" hour "` falls back to sensor mode. C# hour offset still multiplies 32-bit integers before assigning stream position. | Confirm exposed legacy outputs, append any required dimension/metadata ports, normalize modes, and complete F11. |
| IO07 | High, open IES repeat-conversion/file-rewrite differences | On rerun Python falls back to files matching the requested stem; C# selects the latest `.rad`/`.dat` in the shared luminaire folder. It can therefore return/rewrite another luminaire. Python rewrites the first matching RGB line; C# rewrites every matching three-value line. Its DAT regex does not preserve already-quoted paths safely. | Bind output selection to the current conversion and validate Radiance primitive/file references before rewriting; add rerun/multiple-luminaire fixtures. |
| IO08 | Medium, intentional path redesign plus open geometry access loss | Both Python lighting scripts use `parent(_folder)/Luminaire_files`; both compiled scripts now use `Project/Luminaire_files` (C03/F05). Python placement/compiler/PAR sensor code explicitly flattens trees; C# uses branch-oriented list access. Python placement reports missing `.rad` paths; C# emits an OK status without checking existence. | Use one explicit project root for both lighting components; document flattening and sensor order; restore missing-file diagnostics. |
| IO09 | Medium, open selector capability/persistence gap; F06/F09/C05 | Opaque/glazing/IES selectors retain their principal selected identifier/path outputs, but lose preview UI. IES loses Size and actual angular-range displays. C# glazing counted-block parsing is still incorrect. Both Python material/IES selectors and C# emit no selection on Run=False; this is inherited behavior, not a new persistence feature. | Correct F06 and define deliberate selection retention, cancel, save/reopen, and trigger behavior under F09/C05. |
| IO10 | Medium, inherited calendar defect and open annual-time contract | Both date selectors compute `(DayOfYear-1)*24 + ((hour+23)%24)` using the selected year. Blocking Feb 29 does not remove the extra day after February in leap years. Both share global selection state. C# annual cache accepts actual EPW record count, but plots and DLI require 8,760 values. | Define hour-ending/timezone/leap-year conventions and reject or adapt unsupported time axes. Do not silently change indexing during migration. |
| IO11 | High workflow gap, not a newly removed Python component | Setup initializes folders/manifests but does not export Honeybee geometry/materials/sensors. Annual Simulation requires a specific ModelToRad source layout. The compiled lighting chain ends at `luminaries.rad`; Annual Simulation only uses `envelope.mat`, `envelope.rad`, `envelope.blk` and its daylight sky/sun commands, with no luminaire-file input. There is no automatic electric-light-to-daylight combination. | Provide an explicit Honeybee export/import boundary and a separate electric-light execution/combination contract. A successful Setup check is not an executable complete study. |
| IO12 | Medium, helper semantics and lifecycle gaps | Annual DLI accepts arbitrary positive `dt` but always groups 24 samples into a day. Lighting Energy's Hours counts all schedule samples, including zero-power periods. Annual Run=False prepares a new folder each solve; Run=True launches a new run each solve. A false→true transition does not launch the previously returned prepared folder. | Document the currently supported hourly case; clarify duration versus operating time; complete C05 before presenting buttons/timers as a stable annual controller. |

IO findings are a separate compatibility audit namespace. They do not reopen F01–F04's implemented fixes or mean Setup must be reverted. C06 remains open until the workflow and migration contract are reconciled and verified in saved definitions.

## Visible Setup: use these contracts for new definitions

| Component / implementation | Ordered compiled inputs | Ordered compiled outputs |
| --- | --- | --- |
| **Simulation Paths** / [SimulationPathsSetupComponent](../src/FlahaGrow.Grasshopper/Components/Setup/ProjectPathsComponent.cs) | `Location:text?`; `Mode:int=0` (0 Auto, 1 project-relative, 2 system, 3 custom); `Library:text?`; `Refresh:bool=false` | `Paths:ResolvedPaths`; `Folder:text`; `Library:text`; `Status:text` |
| **Working Directory** / [ProjectWorkspaceComponent](../src/FlahaGrow.Grasshopper/Components/Setup/ProjectWorkspaceComponent.cs) | `Paths:ResolvedPaths`; `Name:text="Greenhouse Study"`; `Analysis:text="baseline"`; `Workflow:int=0` (0 annual daylight, 1 electric preparation); `Initialize:bool=false`; `Adopt:bool=false`; `Refresh:bool=false` | `Project:ProjectContext`; `Analysis:AnalysisContext`; `Folder:text`; `Inputs:text`; `Runs:text`; `Library:text`; `Status:text` |
| **Radiance Status** / [RadianceSetupComponent](../src/FlahaGrow.Grasshopper/Components/Setup/RadianceSetupComponent.cs) | `Analysis:AnalysisContext?`; `Refresh:bool=false` | `Radiance:RadianceEnvironment`; `Ready:bool`; `Version:text`; `Bin:text`; `Lib:text`; `Found:text[]`; `Status:text` |

These three GUIDs are respectively `71ce89f2-1439-4730-915f-07436692926c`, `d236c57b-eab1-4329-8e4e-beb2285ba04d`, and `9cd39fc4-7c35-4aee-a247-1c8b980c4b17`.

Simulation Paths resolves locations without creating directories. Working Directory explicitly initializes/opens an analysis. Radiance Status automatically checks the selected analysis workflow; advanced installation/library overrides are in its menu. `Library` from Paths/Workspace means FlahaGrow assets; `Lib` from Radiance Status means Radiance calculation files. They are different inputs for different consumers.

Hidden identity distinctions:

| Hidden component / GUID | Actual contract |
| --- | --- |
| [WorkingDirectoryComponent](../src/FlahaGrow.Grasshopper/Components/WorkingDirectoryComponent.cs), `3bc3011e-2b2f-4c14-9344-dcb3554f3722` | Root + six toggles → root + six paths; no typed context. |
| [RadianceVersionComponent](../src/FlahaGrow.Grasshopper/Components/RadianceVersionComponent.cs), `272aa83d-9898-460d-8cbd-7f49374153ba` | Run=false; optional Bin → Version text. |
| [SimulationPathsComponent](../src/FlahaGrow.Grasshopper/Components/SimulationPathsComponent.cs), `71c6a045-9308-4a0c-9f72-cab76ceefa5c` | Project; optional Library → Project, Materials, Glazing, IES, Annual paths; creates directories. |
| [RadianceStatusComponent](../src/FlahaGrow.Grasshopper/Components/RadianceStatusComponent.cs), `f6f1d5d4-9a1a-4de7-a090-6299c94e0060` | Optional Bin → Available bool, rcontrib path; not the verified environment. |
| [ProjectPathsComponent](../src/FlahaGrow.Grasshopper/Components/Setup/ProjectPathsComponent.cs), `37c57f57-1be3-4eaa-aa88-12a20f0172ef` | Location?, Mode=0, Library?, Radiance?, Refresh=false → Paths, Folder, Library, Radiance location, Status. |
| [ProjectRadianceComponent](../src/FlahaGrow.Grasshopper/Components/Setup/ProjectRadianceComponent.cs), `59892a1c-7e97-46d5-989c-2283c9476ea4` | Project?, Location?, Workflow=0, Check=false, Lib? → Radiance, Ready, Version, Bin, Lib, Candidates, Status. |

## Complete legacy mapping: 25 scripts

### Setup and material selection

| # / Python source | Python inputs → assigned outputs / effects | Compiled counterpart: ordered ports | Assessment |
| --- | --- | --- | --- |
| 1 [Working Directory](<../src/Code/01 Basic/01 Working Directory.py>) | `_root_folder`; `_point_in_time_illuminance`, `_point_in_time_render`, `_annual_illuminance`, `_electric_illuminance`, `_spectral_point_in_time`, `_spectral_point_in_time_render`, `_spectral_annual` → `_root_folder_out`, `PIT_ill_folder`, `PIT_render_folder`, `ann_ill_folder`, `electric_ill_folder`, `spec_PIT_folder`, `spec_PIT_ren_folder`, `spec_ann_ill_folder`; creates enabled folders | Hidden WorkingDirectoryComponent: Root:text; PIT Ill/PIT Render/Annual Ill/Electric Ill/Spectral PIT/Spectral Annual:bool=false → Root and six text paths | Missing spectral PIT render. Visible Working Directory is the typed redesign above. IO01. |
| 2 [Radiance Version](<../src/Code/01 Basic/02 Radiance Version.py>) | `_radiance` trigger → `version` text/None; invokes `rcontrib -version` via PATH. No Python location input. | Hidden Radiance Version: Run:bool=false; Bin:text? → Version:text | Optional Bin added; Run=False emits guidance rather than None. Visible Radiance Status is a separate readiness contract. |
| 3 [Facade](<../src/Code/02 3 Channel Prep/Materials_Opaque/MS_Opaque Materials - facade .py>) | `run`; optional global `_rad_materials_folder` / environment fallback → `_modifier` string/None | [Facade Material](../src/FlahaGrow.Grasshopper/Components/MaterialSelectors.cs): Run:bool=false; Materials:text? → Modifier:text | Identifier retained; library root support added; Python preview omitted; no idle output. IO09. |
| 4 [Frame](<../src/Code/02 3 Channel Prep/Materials_Opaque/MS_Opaque Materials - Frame.py>) | Same inputs → `_modifier` | Frame Material: same ports as #3 | Same differences as #3; separate compiled identity. |
| 5 [Ground](<../src/Code/02 3 Channel Prep/Materials_Opaque/MS_Opaque Materials - Ground.py>) | Same inputs → `_modifier` | Ground Material: same ports as #3 | Same differences as #3; separate compiled identity. |
| 6 [Concrete](<../src/Code/02 3 Channel Prep/Materials_Opaque/MS_Opaque Materials_Concrete.py>) | Same inputs → `_modifier` | Concrete Material: same ports as #3 | Same differences as #3; separate compiled identity. |
| 7 [Glazing](<../src/Code/02 3 Channel Prep/Materials_Opaque/MS Glazing Materials - Glazing.py>) | `run`; optional global `_rad_glazing_folder` / environment fallback → `_glazing_modifier` | [Glazing Material](../src/FlahaGrow.Grasshopper/Components/GlazingMaterialComponent.cs): Run:bool=false; Glazing:text? → Modifier:text | Identifier retained; preview omitted; displayed optical quantities diverge because parser consumes argument counts as values. F06/IO09. |

Material outputs are **modifier names**, not complete Radiance material definitions, material file paths, or Honeybee material objects. The Honeybee/export side must know/load the corresponding definitions. The selectors do not establish that external link themselves. The compiled selectors use explicit/bundled locations rather than the Python material-specific environment-variable contract.

### Spectral and electric-light preparation

| # / Python source | Python inputs → assigned outputs / effects | Compiled counterpart: ordered ports | Assessment |
| --- | --- | --- | --- |
| 8 [Spectral Data Load](<../src/Code/02 3 Channel Prep/Conv. Factor ILLuminance to PPFD/CF_ILL_PPFD_Spectral Data Load.py>) | `_load_spectral_data`; `_wavelength_interval` (fallback 1) → `_conversion_factor`; modeless CSV calculation table and optional `_calculated.csv` export. CSV is selected in UI, not a script input. | [Load Spectral Data](../src/FlahaGrow.Grasshopper/Components/SpectralConversionFactorComponents.cs): Load:bool=false; nm:int=1 → Factor:number; PAR:number; Lux:number; File:text | Additional scalar outputs; removed table/export; different calculation and CSV schema handling. IO03/IO04. |
| 9 [Spectral Data Selection](<../src/Code/02 3 Channel Prep/Conv. Factor ILLuminance to PPFD/CF_ILL_PPFD_Spectral Data Selection.py>) | `_run`; `_wavelength_interval` → `_conversion_factor`, initially None; standards/custom numeric/custom CSV selection | Select Spectral Factor: Run:bool=false; nm:int=1 → Factor:number; Source:text | Seven predefined constants retained; default output, custom selection behavior and CSV numerics differ. IO03/IO04. |
| 10 [Spectral Data Selection2](<../src/Code/02 3 Channel Prep/Conv. Factor ILLuminance to PPFD/CF_ILL_PPFD_Spectral Data Selection2.PY>) | Same principal contract as #9 | Select Spectral Factor (Legacy): same compiled ports as #9 | Separate GUID, shared implementation and differences. |
| 11 [Select Grow Light](<../src/Code/02 3 Channel Prep/Electric Light/01 sELECT gROW lIGT.PY>) | `run`; hardcoded `C:\RadIES` → `_ies_path`, `_ies_name` | [Select IES Luminaire](../src/FlahaGrow.Grasshopper/Components/IesLuminaireSelectorComponent.cs): Run:bool=false; IES:text? → IES:text; Name:text | Portable library input added. Preview, Size and calculated angle ranges lost; current angle cells say “See IES”. IO09. |
| 12 [IES to Rad](<../src/Code/02 3 Channel Prep/Electric Light/02 IES to Rad.PY>) | `_ies_path`, `_ies_name`; `_r/_g/_b`=1; `_multiplier`; `_folder` (fallback `~/radiance_out`); `_datfile`; `_run`=false → `_rad_files`, `_dat_files`, `_log`, `_cmdline`, `out` list; normalized RGB and files | [IES to Radiance](../src/FlahaGrow.Grasshopper/Components/IesToRadianceComponent.cs): IES:text; Name:text?; R/G/B:number=1; M:number?; Project:text; DAT:text?; Run:bool=false; Bin:text?; Radiance:RadianceEnvironment? → Rad:text[]; DAT:text[]; Log:text | Project now required; diagnostics collapsed; environment appended; folder deliberately changed; normalization formula retained but rewrite/file selection differs. IO07/IO08. |
| 13 [Lighting Geometry](<../src/Code/02 3 Channel Prep/Electric Light/03 Lighting Geometry.PY>) | `_points`; `_rotationX/Y/Z` (empty→0, singleton broadcasts); `_rad_files` (singleton broadcasts) → `_lighting_geometry` string list; `out` diagnostics | [Lighting Geometry](../src/FlahaGrow.Grasshopper/Components/LightingGeometryComponent.cs): Pts:point[]; Rx/Ry/Rz:number[]=0; Rad:text[] → xform:text[]; Status:text | Placement order/degree units/broadcast retained; no explicit tree flattening or missing-path warning. IO08. |
| 14 [Compile Luminaries](<../src/Code/02 3 Channel Prep/Electric Light/04 Compile Luminaries.PY>) | `_lighting_geometry`; `_folder`; `_write` → `lum_rad_path`; `out`; writes `luminaries.rad` | [Compile Luminaires](../src/FlahaGrow.Grasshopper/Components/CompileLuminariesComponent.cs): xform:text[]; Project:text; Write:bool=false → Rad:text; Status:text | Same output role; deliberate parent-folder→project-folder migration; tree flattening differs. Folder must already exist. IO08. |

For #8–10, numeric output is intended as **PPFD per lux** and consumers multiply lux by it. The Python Load export footer itself uses `s_lx/s_par`, while its live output uses `s_par/(683*s_lx)`: an inherited inconsistency to resolve, not a formula to reproduce blindly. C# `Lux sum` is the unscaled weighting sum; factor calculation applies 683 separately. Neither label alone proves physically integrated SI quantities at arbitrary wavelength steps.

### Annual simulation and illuminance

| # / Python source | Python inputs → assigned outputs / effects | Compiled counterpart: ordered ports | Assessment |
| --- | --- | --- | --- |
| 15 [Annual Simulation](<../src/Code/03 RGB Simulation/Annual Simulation/01 Annual Simulation.py>) | `_folder`; `_weather_file`; `_sky_sub_div`; `_details`; `_custom_parameter`; `_run` → `_result_path`; file preparation/batch launches | [Annual Simulation](../src/FlahaGrow.Grasshopper/Components/AnnualSimulationComponent.cs): Project:text; EPW:text; Sky:int=1; Detail:text="mid"; Run:bool=false; Pts:point[]?; Bin:text?; Radiance:RadianceEnvironment?; Analysis:AnalysisContext?; Cancel:bool=false; Existing:text?; Keep:bool=false → Folder:text; BAT:text[]; Status:text | Custom input collapsed; optional ports added; sky validation/correction and isolated output intentional. Project is the Honeybee source; Folder is the new run, not Project. `Keep=False` cleans successful reproducible intermediates; the run drive is checked before launch. IO02/IO05/IO11/IO12. |
| 16 [Load Annual Result](<../src/Code/03 RGB Simulation/Annual Simulation/02 Load Annual Result.py>) | `_result_path`; `_build` → `_result` cache path, `sensors`, `hours`, `status`; merged `.ill`, `.f32`, metadata | [Load Annual Result](../src/FlahaGrow.Grasshopper/Components/AnnualResultCacheComponent.cs): Folder:text; Build:bool=false → F32:text; S:int; H:int; Status:text | Principal ports retained; requires validated schema-2 run, supports single part, removes merged `.ill` export and some old metadata keys. IO05. |
| 17 [selected_hour_index](<../src/Code/03 RGB Simulation/Annual Simulation/03 selected_hour_index.py>) | `_run` → `hour_index` integer/None via shared sticky state | [Select Date and Hour](../src/FlahaGrow.Grasshopper/Components/SelectHourIndexComponent.cs): Run:bool=false → Hour:int; Date:text | Readable output added; unusual hour-ending mapping and leap-year defect retained. IO10. |
| 18 [Illuminance Pointintime](<../src/Code/03 RGB Simulation/Annual Simulation/04 Illuminance Pointintime.py>) | `_result`; `_mode` (fallback sensor); `_index`; `_run` → assigned `result`, `nrows`, `ncols`, `meta`, `status` | [Illuminance Point in Time](../src/FlahaGrow.Grasshopper/Components/IlluminanceReaderComponents.cs): F32:text; Mode:text="sensor"; i:int; Run:bool=false → Lux:number[]; Status:text | Despite its name, default mode is sensor. Fewer candidate outputs; whitespace handling and overflow differ. C# rejects size mismatch; Python PIT may only warn. IO06. |
| 19 [Illuminance sensor](<../src/Code/03 RGB Simulation/Annual Simulation/05 Illuminance sensor.py>) | Same input variables and candidate output names as #18; memory-mapped reading | Illuminance Sensor: same compiled ports as #18, separate GUID | Both compiled readers share implementation. A sensor column has H values; an hour row has S values, not invariably 8,760. IO06. |
| 20 [Annual Plot](<../src/Code/03 RGB Simulation/Annual Simulation/06 Annual Plot.py>) | `_result_hourly`; `_range1..4` (fallback 0/10/20/50); `_grid_mode` (0); `_grid_color`; optional `_name_range1..5`; `_graph_title`; `_run` → modeless heatmap and manual PNG save, no explicit data output | [Annual Plot](../src/FlahaGrow.Grasshopper/Components/AnnualHeatmapComponents.cs): Data:number[]; R1..R4:number=0/10/20/50; Grid:int=0; Grid color:colour?=LightGray; Name 1..5:text; Title:text?; Run:bool=false → Status:text | Basic 365×24 display role retained; status added. Names default Imperceptible/Perceptible/Disturbing/Intolerable/Excessive. Typed colour port replaces Python custom string/tuple parser; cast parity requires Rhino checks. |
| 21 [Sensor Marker](<../src/Code/03 RGB Simulation/Annual Simulation/07 Sensor Marker.py>) | `_point`; `_grid_size`; `Up` (fallback Z) → `_marker` Brep/None | [Sensor Marker](../src/FlahaGrow.Grasshopper/Components/SensorMarkerComponent.cs): Point:point; Size:number; Up:vector?=Z → Marker:brep | Radius=size/2 upper-hemisphere role retained. Python coerces point objects/GUIDs explicitly; C# relies on GH point conversion. |

Annual preparation reads `model/scene/envelope.rad`, `envelope.mat`, `envelope.blk` and a grid under `model/grid`. Optional Pts replaces grid data in the run with upward normals `(0,0,1)`; it is not an oriented sensor-grid input. Without Pts the selected `.pts` preserves normals, but only one grid file is chosen (`0.pts` or the first sorted candidate), not all grids. Sensor ordering must match downstream point lists.

### PPFD results

| # / Python source | Python inputs → assigned outputs / effects | Compiled counterpart: ordered ports | Assessment |
| --- | --- | --- | --- |
| 22 [Select PIT to PPFD](<../src/Code/04 Result and Metrics/01 PPFD/01 Daylight Only/01 Select PIT to PPFD.py>) | `_run` → `hour_index`; same sticky key as #17 | Select PIT to PPFD: Run:bool=false → Hour:int; Date:text | Shares compiled static state with #17; not independent per sensor/document. Same IO10 caveat. |
| 23 [Hourly PAR](<../src/Code/04 Result and Metrics/01 PPFD/01 Daylight Only/02 Hourly PAR.py>) | `_result`; `_hour_index`; `_conversion_factor` (fallback .0185; numeric/preset) → `_PAR` list; errors yield empty list | [Hourly PAR](../src/FlahaGrow.Grasshopper/Components/LegacyPpfdComponents.cs): F32:text; Hour:int; Factor:generic? → PAR:number[]; Status:text | Cache-native per-hour conversion retained; status/runtime errors added. No Run input. |
| 24 [PAR Each Sensor](<../src/Code/04 Result and Metrics/01 PPFD/01 Daylight Only/03 PAR Each Sensor.py>) | `_result`; `_sensor_index`; `_conversion_factor`; `_sensor_pts`; `_mark`; `_marker_size`; `_marker_up` → `_PAR` list, `_sensor_pt`, `_marker` list | PAR Each Sensor: F32:text; Sensor:int; Factor:generic?; Pts:point[]?; Mark:bool=false; Size:number?; Up:vector?=Z → PAR:number[]; Point:point; Marker:brep[]; Status:text | Cache-native conversion/marker role retained; H values returned despite 8,760 tooltip; Python explicit tree flattening removed. |
| 25 [Annual Plot PPFD for sensor](<../src/Code/04 Result and Metrics/01 PPFD/01 Daylight Only/04 Annual Plot PPFD for sensor.py>) | Same plotting variables as #20, PPFD data → heatmap/PNG | Annual Plot PPFD for Sensor: same ports/defaults as #20, PPFD-specific default title → Status:text | Separate identity, shared display engine; exactly 8,760 values required. |

Numeric/preset factor behavior in #23–24 is retained: default `.0185`, electric `.015`, sunonly `.0205`, skyonly `.0135`, including aliases. This does not establish correctness of factors produced upstream by custom spectral CSV calculations.

## Additional compiled helpers: not Python replacements

| Component | Ordered inputs → ordered outputs | Workflow meaning / limitation |
| --- | --- | --- |
| [Annual Simulation Progress](../src/FlahaGrow.Grasshopper/Components/AnnualSimulationProgressComponent.cs) | Folder:text; Refresh:bool=true → Progress:text[]; Done:int; Status:text; %:number | Reads the selected run's states, validated final matrices, latest 1–8 batch stage, and stage coverage. Refresh is read but does not gate execution: every solve rereads results. Coverage is not an ETA. Does not launch/cancel jobs or schedule its own timer. |
| [Lux to PPFD](../src/FlahaGrow.Grasshopper/Components/LuxToPpfdComponent.cs) | Lux:number; Factor:number=.0185 → PPFD:number | Scalar multiplication; accepts no cache path or preset strings. |
| [Hourly PPFD](../src/FlahaGrow.Grasshopper/Components/PpfdCacheComponents.cs) | Lux:number[]; Factor:number=.0185 → PPFD:number[] | Converts an already-extracted list. Distinct from **Hourly PAR**, which reads a cache. |
| [PPFD Each Sensor](../src/FlahaGrow.Grasshopper/Components/PpfdCacheComponents.cs) | Lux:number[]; Factor:number=.0185 → PPFD:number[] | List conversion; does not choose a sensor or create a marker. Distinct from **PAR Each Sensor**. Warns on length other than 8,760 but still emits values. |
| [Annual DLI](../src/FlahaGrow.Grasshopper/Components/AnnualDliComponent.cs) | PPFD:number[]; dt:number=3600 → DLI:number[]; Mean:number | Exactly 8,760 samples → 365 groups of 24; use hourly data until IO12 is resolved. |
| [DLI Target](../src/FlahaGrow.Grasshopper/Components/DliTargetComponent.cs) | DLI:number[]; Target:number → OK:bool[]; Deficit:number[]; Days:number | Target is supplied by user; outputs sufficiency, missing mol/m²/day, and sufficient-day count. |
| [Lighting Energy](../src/FlahaGrow.Grasshopper/Components/AnnualLightingEnergyComponent.cs) | W:number[]; dt:number=3600 → kWh:number; Hours:number | Needs an independently supplied power schedule. Does not derive watts from illuminance, IES, or a DLI deficit; Hours is total represented duration. |

## Current wiring guide

### Annual daylight

| Producer output | Consumer input | Meaning |
| --- | --- | --- |
| Simulation Paths **Paths** | Working Directory **Paths** | Typed resolved locations; Initialize opens/creates the workspace. |
| Working Directory **Analysis** | Radiance Status **Analysis** and Annual Simulation **Analysis** | Use Workflow=0. The analysis owns run storage. |
| Radiance Status **Radiance** | Annual Simulation **Radiance** | Connect the typed environment, not Ready/Version text. |
| Paths/Working Directory **Library** | Material selectors **Materials/Glazing** | FlahaGrow library root. Radiance Status Lib is not a material library. |
| External Honeybee ModelToRad export root | Annual Simulation **Project** | Must contain the required model/scene and model/grid files. Working Directory Inputs or Folder alone does not create this model. |
| EPW file | Annual Simulation **EPW** | Source weather file; copied into the isolated run. |
| Annual Simulation **Folder** | Progress **Folder** and Load Annual Result **Folder** | Exact run directory, not Workspace Runs container or source Project. |
| Load Annual Result **F32** | Hourly PAR **F32** or PAR Each Sensor **F32** | Build only after validated completion. Cache preserves hours×sensors order. |
| Select Date and Hour / Select PIT to PPFD **Hour** | Hourly PAR **Hour** | Hour-ending convention and leap-year caveat in IO10. |
| Sensor integer + original ordered points | PAR Each Sensor **Sensor/Pts** | Select one cache column and optionally locate it. |
| Confirmed factor | Hourly PAR / PAR Each Sensor **Factor** | Custom CSV factors remain subject to IO03. |
| PAR Each Sensor **PAR** | Annual Plot PPFD for Sensor **Data**, Annual DLI **PPFD** | Only a full supported 8,760-hour series. |
| Annual DLI **DLI** | DLI Target **DLI** | Supply a plant target separately. |

Alternative lux route: F32 → Illuminance Point in Time (**Mode explicitly `hour`**, selected index, Run=True) → Hourly PPFD. For annual sensor lux: F32 → Illuminance Sensor (Mode=`sensor`, sensor index, Run=True) → Annual Plot or PPFD Each Sensor. Do not connect F32 path text directly to the list-conversion helpers.

Annual Simulation retains an unchanged prepared/completed run, launches only on a Run false→true edge, and can reopen a manifest-owned run through Existing. Keep the exact returned folder for later inspection. A timer should target Progress, not force Annual Simulation to recompute.

### Electric-light preparation

Use a separate analysis with Workflow=1 → Radiance Status Analysis → IES to Radiance Radiance. An annual-workflow environment is not interchangeable with the electric-preparation environment.

Library root → Select IES Luminaire IES folder; selected IES/Name → IES to Radiance IES/Name; Working Directory **Folder text** → both IES to Radiance Project and Compile Luminaires Project. IES to Radiance Rad → Lighting Geometry Rad, with placement points and rotations → Compile Luminaires xform → `Project/Luminaire_files/luminaries.rad`.

That is the end of the currently implemented electric preparation chain. It does not automatically feed the annual daylight runner or produce an electric illuminance/power schedule. Resolve IO07 before relying on repeat conversions in a shared folder.

## Validation performed for this audit

Run `python tools/Audit-ComponentContracts.py` from the repository root. The [probe](../tools/Audit-ComponentContracts.py) parses all 25 Python files, extracts the Python spectral selection functions/constants using AST, and builds an isolated .NET 9 harness from the **actual current C# SpectralMath and Detail source**. It writes fixtures/results under ignored `artifacts/component-contract-audit/`. Production files and the previous audit harness are not modified.

| Probe | Python result | Extracted C# result |
| --- | --- | --- |
| CSV with all integer wavelengths 380–780, power=1 only at 555 nm | Factor `0.006792734571240172` | `0.012984765381175033` |
| Same CSV layout, power=1 only at 750 nm | Factor `0` | `22.368198149370752` |
| Numeric annual quality 4, 8 threads | Source branch: `-lw 0.0015 -ab 3 -ad 1536 -n 8` | Executed Detail("4",8): `-lw .002 -ab 2 -ad 1024 -n 8` |
| Named annual quality high, 8 threads | Python diffuse text fallback is mid unless custom/numeric input; direct sun is high | Executed Detail("high",8): `-lw .0015 -ab 3 -ad 1536 -n 8` |

The numeric probe demonstrates migration drift, not scientific correctness of the old formulas. Python itself has inconsistencies (named versus numeric annual settings, export factor footer, leap-year indexing). Neither implementation should be accepted wholesale without an agreed contract.

The previously reported 153 Core tests and component smoke checks cover their existing scopes; they were not Python/C# parity certification. This audit does not claim a live Rhino canvas, original saved-wire comparison, dialog parity, complete Radiance study, or physical numerical reference validation.

## Recommended implementation sequence after this audit

1. Agree/document the visible workflow above and preserve an identity-specific legacy map (C06/IO01). Obtain a representative saved Python definition before claiming exact port-order/access parity.
2. Fix numerical and file-ownership regressions first: spectral IO03/IO04, annual quality IO02 together with F08, and IES IO07. Add paired fixtures against explicitly approved behavior, including corrections to known Python defects.
3. Complete reader/tree/selector contracts: F11/IO06, IO08, F06, F09 and C05. Append compatibility outputs/options instead of shifting existing compiled ports.
4. Provide a small saved annual study showing the external Honeybee export boundary and exact run/cache wiring; separately define electric simulation and any combination with daylight.
5. Validate time axes, spectral factors, PPFD/DLI/energy and save/reopen behavior before declaring the whole workflow integrated.
