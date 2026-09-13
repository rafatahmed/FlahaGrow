# 03 Annual: component reference

Run and load annual illuminance; select weather-aligned date/hour indices; inspect sensor/grid slices and plot temporal data. The typed Result is the preferred weather/provenance connection.

[Reference guide and shared contracts](README.md) · [All components](navigation.md)

Documentation reviewed: **2026-09-13**. This is the current working-tree source contract, not a deployment claim. Per-component versions below come from the revision ledger.

## Components

- [Annual Plot](#annualplotcomponent)
- [Annual Simulation](#annualsimulationcomponent)
- [Annual Simulation Progress](#annualsimulationprogresscomponent)
- [Combine Annual Lighting](#combineannuallightingcomponent)
- [Read Illuminance](#readilluminancecomponent)
- [Load Annual Result](#annualresultcachecomponent)
- [Select Date and Hour](#selecthourindexcomponent)
- [Sensor Marker](#sensormarkercomponent)

<a id="annualplotcomponent"></a>

## Annual Plot

<!-- component: 5747b67c-4aec-4117-83a2-5e30a7308920 -->

- Short name / nickname: `Annual Plot`.
- Category: 03 Annual; visibility: placeable (primary).
- Icon available: No — no name-matched component bitmap.
- Implementation: [AnnualPlotComponent](../../src/FlahaGrow.Grasshopper/Components/AnnualHeatmapComponents.cs); component ID: `5747b67c-4aec-4117-83a2-5e30a7308920`.

### Short description

Displays annual illuminance, PPFD or DLI with matching Plot Attributes and exports PNG.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Data | Annual results | Param_Number | list | Required registration | None registered | 8760 hourly, 365 daily, or 12 monthly finite nonnegative samples | From Plot metadata; numeric-only mode requires correct user labeling | Annual values: 8,760 hourly, 365 daily, or 12 monthly values. | Annual sensor reader's Lux / PPFD / DLI, paired with its Plot |
| 1 | R1 | Range 1 | Param_Number | item | Required registration; default supplied | 0 | Four finite thresholds in nondecreasing order; used in manual/numeric-only mode | Same as Data | First inclusive threshold. | Automatic from Plot/Data, or user numeric thresholds |
| 2 | R2 | Range 2 | Param_Number | item | Required registration; default supplied | 10 | Four finite thresholds in nondecreasing order; used in manual/numeric-only mode | Same as Data | Second inclusive threshold. | Automatic from Plot/Data, or user numeric thresholds |
| 3 | R3 | Range 3 | Param_Number | item | Required registration; default supplied | 20 | Four finite thresholds in nondecreasing order; used in manual/numeric-only mode | Same as Data | Third inclusive threshold. | Automatic from Plot/Data, or user numeric thresholds |
| 4 | R4 | Range 4 | Param_Number | item | Required registration; default supplied | 50 | Four finite thresholds in nondecreasing order; used in manual/numeric-only mode | Same as Data | Fourth inclusive threshold. | Automatic from Plot/Data, or user numeric thresholds |
| 5 | Grid | Grid mode | Param_Integer | item | Required registration; default supplied | 0 | 0 plain; 1 inset; 2 grid; 3 dark grid (clamped) | — (not a physical quantity) | 0 plain, 1 inset, 2 grid, 3 dark grid. | User value or upstream output described below |
| 6 | Grid color | Grid color | Param_Colour | item | Optional registration; see workflow | LightGray | Grasshopper colour | — (not a physical quantity) | Grid color for mode 2. | Colour Swatch |
| 7 | Name 1 | Range 1 name | Param_String | item | Required registration; default supplied | Bin 1 | Text; see description | — (not a physical quantity) | Optional user label, not a crop-quality claim. Exact numeric interval is always shown. | User value or upstream output described below |
| 8 | Name 2 | Range 2 name | Param_String | item | Required registration; default supplied | Bin 2 | Text; see description | — (not a physical quantity) | Optional user label; exact numeric interval is always shown. | User value or upstream output described below |
| 9 | Name 3 | Range 3 name | Param_String | item | Required registration; default supplied | Bin 3 | Text; see description | — (not a physical quantity) | Optional user label; exact numeric interval is always shown. | User value or upstream output described below |
| 10 | Name 4 | Range 4 name | Param_String | item | Required registration; default supplied | Bin 4 | Text; see description | — (not a physical quantity) | Optional user label; exact numeric interval is always shown. | User value or upstream output described below |
| 11 | Name 5 | Range 5 name | Param_String | item | Required registration; default supplied | Bin 5 | Text; see description | — (not a physical quantity) | Optional user label; exact numeric interval is always shown. | User value or upstream output described below |
| 12 | Title | Graph title | Param_String | item | Optional registration; see workflow | Annual results — connect Plot Attributes | Optional title text; metadata or component fallback if empty | — (not a physical quantity) | Optional heatmap title. | User value or upstream output described below |
| 13 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Connect a Button: False → True opens a snapshot. Changes do not update an already open plot; click again to open the current data. | Button/toggle; triggering behavior in workflow |
| 14 | Plot | Plot Attributes | PlotAttributesParameter | item | Optional registration; see workflow | None registered | Typed, data-bound PlotAttributes; matching data checksum/shape | — (not a physical quantity) | Connect the same reader's Plot output as the values connected to Data. Automatically supplies title, units and ranges; mismatched data is rejected. | The same reader's Plot output as the connected Data |
| 15 | Manual | Use manual ranges | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | False: automatic min-to-max ranges with Plot attributes. True: use R1–R4. Legacy numeric-only plots retain their existing ranges. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Heatmap status. | Component diagnostics → Panel |

### Workflow description

Connect a temporal Data series and the matching reader's Plot output. Leave Manual=False for data-derived ranges, or set Manual=True and provide R1–R4. Pulse Run to open the plot.

### Notes

Accepts finite nonnegative 8760-hour, 365-day or 12-month series. Data-bound attributes check shape/count/checksum and reject spatial grids, even when a grid has 365 sensors. Automatic bins use min plus 0%, 25%, 50%, 75% of the span; they are not crop thresholds. Numeric-only compatibility uses explicit ranges when Plot is unwired. Supplied or connected invalid Plot attributes produce an error instead of falling back to numeric-only mode. Ranges must be nondecreasing. The nonmodal window is a snapshot; rerun for changed data. Save As exports PNG. Title can override metadata; units and selection come from Plot.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.6.0**. Last component update: **2026-09-13T16:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Rejects connected invalid or unresolved Plot Attributes instead of falling back to numeric-only plotting.

<a id="annualsimulationcomponent"></a>

## Annual Simulation

<!-- component: ca2ce6ef-a0c8-4d98-87a3-2adf2a91ca45 -->

- Short name / nickname: `Annual Sim`.
- Category: 03 Annual; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [AnnualSimulationComponent](../../src/FlahaGrow.Grasshopper/Components/AnnualSimulationComponent.cs); component ID: `ca2ce6ef-a0c8-4d98-87a3-2adf2a91ca45`.

### Short description

Prepares and launches the FlahaGrow annual Radiance daylight simulation.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Project | Project folder | Param_String | item | Required registration | None registered | Project/export folder path (not typed Project context) | — (not a physical quantity) | Simulation project root containing model/grid and model/scene. | Honeybee/Radiance exported model/grid and scene root |
| 1 | EPW | EPW weather file | Param_String | item | Required registration | None registered | Existing EPW file for supported non-leap 8760-hour simulation | — (not a physical quantity) | Weather file for the annual simulation. | Project weather EPW; simulation snapshots and hashes it |
| 2 | Sky | Sky subdivision | Param_Integer | item | Required registration; default supplied | 1 | 1 or 4 (supported sky subdivisions) | — (not a physical quantity) | 1 for Tregenza or 4 for Reinhart subdivision. | User value or upstream output described below |
| 3 | Detail | Detail | Param_String | item | Required registration; default supplied | mid | low / mid / high / veryhigh, or custom Radiance parameters | — (not a physical quantity) | low, mid, high, very high, or a custom Radiance parameter string. | User simulation quality setting |
| 4 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Launch isolated annual jobs in the background. Use Annual Simulation Progress for states and diagnostics. | Button/toggle; triggering behavior in workflow |
| 5 | Pts | Sensor points | Param_Point | list | Optional registration; see workflow | None registered | Point3d(s); exact saved order for result sensors | Rhino model length units | Sensor points written into the isolated run's 0.pts using upward-facing normals. The source grid is preserved. | GenPts/exported grid for sensors; fixture layout points for Lighting Geometry |
| 6 | Bin | Radiance bin folder | Param_String | item | Optional registration; see workflow | None registered | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Optional folder containing Radiance executables. Leave blank for automatic detection. | User value or upstream output described below |
| 7 | Radiance | Radiance Environment | RadianceParameter | item | Optional registration; see workflow | None registered | Typed FlahaGrow object; do not substitute text | — (not a physical quantity) | Optional checked Radiance Status environment. When connected, it must be ready for annual daylight and determines the exact bin and library. | Radiance Status.Radiance |
| 8 | Analysis | Analysis | AnalysisParameter | item | Optional registration; see workflow | None registered | Typed analysis context | — (not a physical quantity) | Optional annual Setup analysis owning the isolated run. Project remains the Honeybee export source. | Working Directory.Analysis |
| 9 | Cancel | Cancel | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Button: cancel this run's recorded batch processes, including after reopening Rhino. PID and start time are verified to prevent cancelling a reused PID. Run=False does not cancel a launched simulation. | Button/toggle; triggering behavior in workflow |
| 10 | Existing | Existing run folder | Param_String | item | Optional registration; see workflow | None registered | Existing run folder; optional inspection/reuse path | — (not a physical quantity) | Optional manifest-owned run folder to reopen without preparing or launching a new simulation. | Previously generated run Folder |
| 11 | Keep | Keep intermediates | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Keep large reproducible Radiance coefficient, sky, octree, and weather intermediates after successful commands. False keeps final/source-term matrices and frees disk space. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Folder | Result folder | Param_String | item | Text; see description | — (not a physical quantity) | Folder containing annualRfinal_part*.ill results. | Produced by this component; see workflow |
| 1 | BAT | Batch files | Param_String | list | Text; see description | — (not a physical quantity) | Generated batch-file paths. | Produced by this component; see workflow |
| 2 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Preparation or launch status. | Component diagnostics → Panel |

### Workflow description

Connect an exported Honeybee/Radiance project, EPW and verified Radiance context; pulse Run; connect Folder to progress and Load Annual Result.

### Notes

Run snapshots scene, sensors and EPW into an isolated run. One partition is used for up to ten sensors, otherwise four. Current contract is 8760 non-leap hours. Run=False does not cancel; Cancel verifies the owned process. Keep controls reproducible intermediates, not final results. Existing supports run inspection; Project/EPW remain registered required ports.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.6.0**. Last component update: **2026-09-13T15:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Shared bounded Radiance discovery; explicit locations never fall back to another installation; no fixed drive paths.

<a id="annualsimulationprogresscomponent"></a>

## Annual Simulation Progress

<!-- component: 1a2d08d7-6d3e-459e-a2c2-62636cbbaf24 -->

- Short name / nickname: `Annual Progress`.
- Category: 03 Annual; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [AnnualSimulationProgressComponent](../../src/FlahaGrow.Grasshopper/Components/AnnualSimulationProgressComponent.cs); component ID: `1a2d08d7-6d3e-459e-a2c2-62636cbbaf24`.

### Short description

Reads the progress logs written by Annual Simulation. Attach a Grasshopper Timer to Refresh for live updates.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Folder | Result folder | Param_String | item | Required registration | None registered | Existing run folder containing manifest/results | — (not a physical quantity) | Annual Simulation result folder. | Annual Simulation.Folder / Electric Annual Simulation.Folder |
| 1 | Refresh | Refresh | Param_Boolean | item | Required registration; default supplied | True | True / False | — (not a physical quantity) | Use with a Grasshopper Timer to update while batch jobs run. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Progress | Part progress | Param_String | list | Text; see description | — (not a physical quantity) | Lifecycle state, latest batch stage, and log update time for each declared part. | Produced by this component; see workflow |
| 1 | Done | Completed parts | Param_Integer | item | Completed partitions: 0–1 or 0–4 | Partitions | Declared parts whose commands succeeded and final matrix passed validation. | Produced by this component; see workflow |
| 2 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Overall annual-simulation progress. | Component diagnostics → Panel |
| 3 | % | Stage coverage | Param_Number | item | 0–100; stage-based progress | % | Completed stage coverage across declared parts. This is stage-based, not an elapsed-time estimate. | Produced by this component; see workflow |

### Workflow description

Connect the simulation Folder and recompute to inspect partition and stage progress.

### Notes

Refresh is registered but is not a Boolean gate in the current solver. Percent is stage coverage, not elapsed-time prediction or proof that results pass validation.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.1.0**. Last component update: **2026-09-11T16:10:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Reports each part's latest 1–8 batch stage and stage coverage instead of only its lifecycle state.

<a id="combineannuallightingcomponent"></a>

## Combine Annual Lighting

<!-- component: 9f5f9a71-0ec2-4fb7-a481-49625f0871f2 -->

- Short name / nickname: `Daylight + Electric`.
- Category: 03 Annual; visibility: placeable (primary).
- Icon available: No — no name-matched component bitmap.
- Implementation: [CombineAnnualLightingComponent](../../src/FlahaGrow.Grasshopper/Components/CombineAnnualLightingComponent.cs); component ID: `9f5f9a71-0ec2-4fb7-a481-49625f0871f2`.

### Short description

Adds matching completed daylight and electric annual runs into a new manifest-owned result.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Daylight | Daylight run folder | Param_String | item | Required registration | None registered | Text; see description | — (not a physical quantity) | Completed manifest-owned annual daylight result. | User value or upstream output described below |
| 1 | Electric | Electric run folder | Param_String | item | Required registration | None registered | Text; see description | — (not a physical quantity) | Completed manifest-owned electric annual result with identical sensors/hours. | User value or upstream output described below |
| 2 | Run | Combine | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Create the combined result once on a false→true edge. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Folder | Combined run folder | Param_String | item | Text; see description | — (not a physical quantity) | New provenance-owned annual result; connect it to Load Annual Result. | Produced by this component; see workflow |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Validation or composition status. | Component diagnostics → Panel |

### Workflow description

Connect daylight and electric run folders and pulse Run to create a combined illuminance run.

### Notes

Adds lux, not source-specific photons. Matching dimensions/order do not prove time alignment. Mixed-lux results are rejected by the single-profile Plant Light Context; use separate contexts and Combine Plant Light for PPFD/DLI. Combined lux currently does not inherit weather metadata.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.2.0**. Last component update: **2026-09-12T00:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Adds explicit profile/context readers and shared finite-value photon integration; existing ports and defaults retained. Spectral CSV uses CIE weighting and trapezoidal integration.

<a id="readilluminancecomponent"></a>

## Read Illuminance

<!-- component: 3d38a66d-b381-45f2-ad70-57e6be84a6cc -->

- Short name / nickname: `Read Lux`.
- Category: 03 Annual; visibility: placeable (primary).
- Icon available: No — no name-matched component bitmap.
- Implementation: [ReadIlluminanceComponent](../../src/FlahaGrow.Grasshopper/Components/ReadIlluminanceComponent.cs); component ID: `3d38a66d-b381-45f2-ad70-57e6be84a6cc`.

### Short description

Reads annual illuminance from a FlahaGrow .f32 cache by sensor or hour.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | F32 | Result cache | Param_String | item | Required registration | None registered | Path to validated .f32 cache with associated metadata/manifest | Path; stored values are lux | Annual .f32 cache. | Load Annual Result.F32 |
| 1 | Mode | Mode | Param_String | item | Required registration; default supplied | sensor | 'hour' selects a grid; everything else uses sensor mode | — (not a physical quantity) | sensor or hour. | User value or upstream output described below |
| 2 | i | Index | Param_Integer | item | Required registration | None registered | Mode=hour: 0–H−1; otherwise 0–S−1 | Zero-based index | Sensor or hour index. | Integer slider; use original grid List Item at the same index |
| 3 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Read the cache. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Lux | Illuminance | Param_Number | list | Mode=hour: S sensor values; otherwise H hourly values at sensor i | lux | Selected illuminance values. | Radiance/cache photopic illuminance → illuminance plot or numeric conversion |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Read status. | Component diagnostics → Panel |
| 2 | Plot | Plot Attributes | PlotAttributesParameter | item | Typed, data-bound PlotAttributes; matching data checksum/shape | — (not a physical quantity) | Connect with Lux to Annual Plot for sensor series; hour selections describe a sensor grid. | Reader.Plot → Annual Plot.Plot, only for supported temporal series |

### Workflow description

Connect Load Annual Result.F32, use Mode=sensor, set i to a zero-based sensor index and enable Run. Connect Lux and Plot together to Annual Plot.

### Notes

One reader supports Mode=hour (all sensors at one hour) and Mode=sensor (all hours at one sensor). Other modes are rejected. Shared Core reads lock and validate cache content, dimensions, manifest provenance and sensor order before publishing values or plot attributes.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.6.0**. Last component update: **2026-09-13T16:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Rejects unknown modes; uses shared locked cache validation including manifest dimensions and order.

<a id="annualresultcachecomponent"></a>

## Load Annual Result

<!-- component: 0e5f7114-fbb9-4a77-a3f4-40ccd0c0c258 -->

- Short name / nickname: `Load Result`.
- Category: 03 Annual; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [AnnualResultCacheComponent](../../src/FlahaGrow.Grasshopper/Components/AnnualResultCacheComponent.cs); component ID: `0e5f7114-fbb9-4a77-a3f4-40ccd0c0c258`.

### Short description

Validates or builds annual caches in the background. Reuses valid caches; exposes verified run weather.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Folder | Result folder | Param_String | item | Required registration | None registered | Existing run folder containing manifest/results | — (not a physical quantity) | Annual Simulation → Folder. | Annual Simulation.Folder / Electric Annual Simulation.Folder |
| 1 | Build | Build | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Button: validate/reuse a cache, or build if missing/invalid. Held True does not rebuild each solve. | Button/toggle; triggering behavior in workflow |
| 2 | Cancel | Cancel | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Cancel pending work. Click Build again to retry. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | F32 | Result cache | Param_String | item | Little-endian float32 cache, hour-major × sensor; requires metadata and run manifest | Path; stored values are lux | Compatibility cache path. | Load Annual Result → legacy F32/cache inputs |
| 1 | S | Sensors | Param_Integer | item | Positive count from validated cache | Sensors | Sensor count. | Produced by this component; see workflow |
| 2 | H | Hours | Param_Integer | item | Positive count from validated cache | Hourly samples | Hour count. | Produced by this component; see workflow |
| 3 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Background progress, completion and weather status. | Component diagnostics → Panel |
| 4 | Result | Annual Result | AnnualResultParameter | item | LoadedAnnualResult typed object, with verified weather when available | — (not a physical quantity) | Connect to Hour Index.Result and Plant Light Context.Result. Includes weather; no manual UTC needed. | Load Annual Result → Hour Index.Result / Context.Result / electric Time Result |

### Workflow description

Connect a run Folder; allow the background probe, then pulse Build to validate/reuse/build the cache. Connect Result to Hour Index and Plant Light Context.

### Notes

Folder changes start background probing; stale operations cannot publish. Build is edge-triggered. Cancel works at checkpoints, not during an indivisible hash operation. Missing weather permits numeric indices only; malformed or hash-mismatched weather fails validation. Downstream full provenance checks can still take time.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.4.0**. Last component update: **2026-09-12T18:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports.

<a id="selecthourindexcomponent"></a>

## Select Date and Hour

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa418 -->

- Short name / nickname: `Hour Index`.
- Category: 03 Annual; visibility: placeable (primary).
- Icon available: No — no name-matched component bitmap.
- Implementation: [SelectHourIndexComponent](../../src/FlahaGrow.Grasshopper/Components/SelectHourIndexComponent.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa418`.

### Short description

Selects a date and AM/PM hour and returns its annual 0-based hour index (0–8759).

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Connect a Button. Opens once on False → True; selected clock hour is the interval start. | Button/toggle; triggering behavior in workflow |
| 1 | Result | Annual Result | AnnualResultParameter | item | Optional registration; see workflow | None registered | LoadedAnnualResult typed object, with verified weather when available | — (not a physical quantity) | Connect Load Annual Result → Result. Location and UTC come from the verified run EPW automatically. | Load Annual Result.Result |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Hour | Selected hour index | Param_Integer | item | 0–8759 for the supported annual axis | Zero-based index | Connect to PPFD at Hour.Hour or an illuminance Hour input. 0–8759; interval start, local standard time. | Date/hour selection → corresponding Hour or Day reader input |
| 1 | Date | Selected date and hour | Param_String | item | Human-readable date and hour/interval label; display only | — (not a physical quantity) | Display only: connect to a Panel. NOT Plant Light Context.Alignment. | Selector → Panel (never Context.Alignment) |
| 2 | Day | Selected day index | Param_Integer | item | 0–364; Jan 1 = 0 | Zero-based index | Connect to DLI for Day.Day. 0–364; Jan 1 = 0. Equals floor(Hour / 24). | Date/hour selection → corresponding Hour or Day reader input |
| 3 | Alignment | Annual weather alignment | Param_String | item | Annual time-axis declaration; not a date/hour value | — (not a physical quantity) | Inherited from Result weather. Normally Context inherits it directly from Result too. Does not certify unrelated electric schedules. | Verified Result weather → legacy Context.Alignment if needed |

### Workflow description

Connect Load Annual Result.Result and pulse Run to choose a date/hour. Wire Hour to hourly readers and Day to DLI for Day; Date is display only.

### Notes

Uses a 365-day local-standard-time axis: Jan 1 00:00 is Hour=0 and Day=0. The chosen hour is the interval start; no daylight-saving adjustment. UTC/location are inherited from verified EPW. The selector has only Run and Result inputs. Without Result only numeric selection is available; no manual UTC input is needed.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **2.0.0**. Last component update: **2026-09-13T12:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Removed unused UTC port; Result now follows Run. New GUID prevents misreading older saved wires.

<a id="sensormarkercomponent"></a>

## Sensor Marker

<!-- component: e0c7494d-bf04-4bd1-a9ed-9184fd2b9b53 -->

- Short name / nickname: `Marker`.
- Category: 03 Annual; visibility: placeable (primary).
- Icon available: No — no name-matched component bitmap.
- Implementation: [SensorMarkerComponent](../../src/FlahaGrow.Grasshopper/Components/SensorMarkerComponent.cs); component ID: `e0c7494d-bf04-4bd1-a9ed-9184fd2b9b53`.

### Short description

Creates an upper-hemisphere marker at a sensor point, scaled from the sensor-grid size.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Point | Sensor point | Param_Point | item | Required registration | None registered | Point3d(s); exact saved order for result sensors | Rhino model length units | Selected sensor point. | GenPts/exported grid for sensors; fixture layout points for Lighting Geometry |
| 1 | Size | Grid size | Param_Number | item | Required registration | None registered | Positive marker diameter | Rhino model length units | Sensor-grid spacing; marker radius is half this value. | User display size, not grid spacing |
| 2 | Up | Up | Param_Vector | item | Optional registration; see workflow | {0,0,1} | Vector3d; nonzero direction | Direction, dimensionless | Marker orientation. Defaults to world Z. | Vector parameter |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Marker | Marker | Param_Brep | item | Rhino Brep geometry | — (not a physical quantity) | Upper-hemisphere sensor marker. | Produced by this component; see workflow |

### Workflow description

Select one point from the original ordered sensor grid, connect Point, and choose a positive Size for the marker.

### Notes

Size is a finite positive diameter in model units, limited to double.MaxValue / 10 to prevent construction overflow. Up defaults to Z; invalid directions fall back to Z. This creates preview geometry, not a new analysis sensor or a result-index mapping.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.1.0**. Last component update: **2026-09-13T16:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Rejects nonfinite or overflowing marker sizes before geometry operations.
