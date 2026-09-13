# 05 PPFD: component reference

Convert source-specific illuminance into estimated PPFD. Recommended: one loaded Result plus one Profile per source → Context → independent hourly/sensor readers; combine contexts only after source-specific conversion.

[Reference guide and shared contracts](README.md) · [All components](navigation.md)

Documentation reviewed: **2026-09-13**. This is the current working-tree source contract, not a deployment claim. Per-component versions below come from the revision ledger.

## Components

- [Annual PPFD at Sensor](#annualppfdatsensorcomponent)
- [Combine Plant Light](#combineplantlightcomponent)
- [Lux to PPFD](#luxtoppfdcomponent)
- [Plant Light Context](#plantlightcontextcomponent)
- [PPFD at Hour](#ppfdathourcomponent)

<a id="annualppfdatsensorcomponent"></a>

## Annual PPFD at Sensor

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa414 -->

- Short name / nickname: `PPFD Sensor`.
- Category: 05 PPFD; visibility: placeable (primary).
- Icon available: No — no name-matched component bitmap.
- Implementation: [AnnualPpfdAtSensorComponent](../../src/FlahaGrow.Grasshopper/Components/PlantLightComponents.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa414`.

### Short description

Reads estimated incident plant light using one shared context; PAR 400–700 nm.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Context | Plant Light Context | PlantLightContextParameter | item | Required registration | None registered | Typed plant-light context | — (not a physical quantity) | Connect Plant Light Context → Context or Combine Plant Light → Context. No separate factor needed. | Plant Light Context.Context / Combine Plant Light.Context |
| 1 | Sensor | Sensor index | Param_Integer | item | Required registration | None registered | 0–S−1; saved sensor ordering | Zero-based index | Integer slider: 0 to sensor count minus 1. Index into the exact saved simulation sensor order (0 = first point), not a Point or grid label. GenPts order matches only if exported unchanged. | Integer slider; use original grid List Item at the same index |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | PPFD | Estimated PPFD | Param_Number | list | 8760 hourly values at the selected sensor; finite ≥ 0 | µmol/m²/s | µmol/m²/s. Hour grid or 8760-hour sensor series, as named. | Lux × source factor → compatible plot/integration |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Quantity, selection and method provenance. Indices do not imply a known calendar date. | Component diagnostics → Panel |
| 2 | Plot | Plot Attributes | PlotAttributesParameter | item | Typed, data-bound PlotAttributes; matching data checksum/shape | — (not a physical quantity) | Connect to Annual Plot.Plot with this component's matching PPFD/DLI values. Grid outputs describe a grid, not an annual series. | Reader.Plot → Annual Plot.Plot, only for supported temporal series |

### Workflow description

Connect Context and a zero-based Sensor; connect PPFD plus Plot to Annual Plot.

### Notes

Produces the annual hourly series at one saved grid point. Sensor is an index, not a point coordinate or human one-based label.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.4.0**. Last component update: **2026-09-12T18:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports.

<a id="combineplantlightcomponent"></a>

## Combine Plant Light

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa412 -->

- Short name / nickname: `Mix Plant Light`.
- Category: 05 PPFD; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [CombinePlantLightComponent](../../src/FlahaGrow.Grasshopper/Components/PlantLightComponents.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa412`.

### Short description

Combines source contexts: convert each source separately, then add photons. Does not create a combined lux cache.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Sources | Source contexts | PlantLightContextParameter | list | Required registration | None registered | List of ≥2 independent, compatible typed contexts | — (not a physical quantity) | Connect two or more Plant Light Context → Context outputs. Sensors and declared time axes must match. | Plant Light Context.Context / Combine Plant Light.Context |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Context | Combined context | PlantLightContextParameter | item | Typed plant-light context | — (not a physical quantity) | Connect to the same PPFD/DLI readers. | Plant Light Context / Combine Plant Light → PPFD and DLI readers |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Source-specific conversion and alignment declaration. | Component diagnostics → Panel |

### Workflow description

Build one context per independent source/run with its own profile, then combine Sources before PPFD/DLI reading.

### Notes

Requires at least two distinct run IDs, identical sensor identity/order and 8760-hour aligned axes. Each source is converted before photon addition. Duplicate runs are rejected to prevent double counting. Equal alignment declarations alone do not validate schedule authorship.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.3.0**. Last component update: **2026-09-12T15:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Strict Hour Index alignment contract, sensor-order guidance; acknowledged assumptions retained in Status.

<a id="luxtoppfdcomponent"></a>

## Lux to PPFD

<!-- component: ac8f8d0f-c1d7-480c-8f37-4fe4c76247aa -->

- Short name / nickname: `Lux→PPFD`.
- Category: 05 PPFD; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [LuxToPpfdComponent](../../src/FlahaGrow.Grasshopper/Components/LuxToPpfdComponent.cs); component ID: `ac8f8d0f-c1d7-480c-8f37-4fe4c76247aa`.

### Short description

Converts illuminance (lux) to PPFD using a spectrum-specific conversion factor.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Lux | Illuminance | Param_Number | item | Required registration | None registered | Finite nonnegative illuminance values | lux | Illuminance in lux. | Illuminance reader.Lux |
| 1 | Factor | Conversion factor | Param_Number | item | Required registration | None registered | Finite number ≥ 0 | µmol/m²/s per lux | Micromoles per square metre per second per lux. Use a value derived from the active light spectrum. | Spectral Profile.Factor / Custom Spectral Profile.Factor |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | PPFD | PPFD | Param_Number | item | One finite nonnegative value; Lux × Factor | µmol/m²/s | Photosynthetic photon flux density in μmol/m²/s. | Lux × source factor → compatible plot/integration |

### Workflow description

Connect one illuminance value and a justified numeric factor; read their product as PPFD.

### Notes

PPFD = lux × factor. Inputs must be finite and nonnegative. Factor is required. Connect the selected profile’s Factor or supply an explicit source-specific value; no universal factor is assumed.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **2.0.0**. Last component update: **2026-09-13T15:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Requires an explicit spectrum-specific factor; no implicit numeric default.

<a id="plantlightcontextcomponent"></a>

## Plant Light Context

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa411 -->

- Short name / nickname: `Plant Context`.
- Category: 05 PPFD; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [PlantLightContextComponent](../../src/FlahaGrow.Grasshopper/Components/PlantLightComponents.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa411`.

### Short description

Binds a manifest-owned lux cache to one spectral profile. Readers remain independent.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | F32 | Result cache | Param_String | item | Optional registration; see workflow | None registered | Path to validated .f32 cache with associated metadata/manifest | Path; stored values are lux | F32 output of Load Annual Result, before mixed-lux composition. | Load Annual Result.F32 |
| 1 | Profile | Spectral Profile | SpectralProfileParameter | item | Required registration | None registered | Typed spectral profile with factor, identity and assumptions | — (not a physical quantity) | Connect Spectral Profile → Profile (or Custom Spectral Profile → Profile). Use the spectrum of this source. | Spectral Profile.Profile / Custom Spectral Profile.Profile |
| 2 | Alignment | Annual alignment declaration | Param_String | item | Optional registration; see workflow | None registered | Annual time-axis declaration; not a date/hour value | — (not a physical quantity) | Legacy optional declaration. With Result connected, leave empty: weather alignment is inherited automatically. Never connect Date, Hour or Day. | Normally inherited from Result; leave unwired |
| 3 | Result | Annual Result | AnnualResultParameter | item | Optional registration; see workflow | None registered | LoadedAnnualResult typed object, with verified weather when available | — (not a physical quantity) | Recommended: Load Annual Result → Result. Supplies cache and weather alignment automatically; leave F32 and Alignment empty. | Load Annual Result.Result |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Context | Plant Light Context | PlantLightContextParameter | item | Typed plant-light context | — (not a physical quantity) | Connect independently to PPFD and DLI readers. | Plant Light Context / Combine Plant Light → PPFD and DLI readers |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Run, assumptions and time convention. | Component diagnostics → Panel |

### Workflow description

Connect Load Annual Result.Result and the corresponding Spectral Profile.Profile. Feed Context independently to PPFD and DLI readers.

### Notes

Result inherits verified weather automatically; normally leave legacy F32/Alignment unwired. At least Result or F32 is needed. Conflicting paths/alignment are rejected. A context does not select an hour. Mixed-source lux is rejected; results remain lux-derived estimates, with profile assumptions preserved.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.4.0**. Last component update: **2026-09-12T18:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports.

<a id="ppfdathourcomponent"></a>

## PPFD at Hour

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa413 -->

- Short name / nickname: `PPFD Hour`.
- Category: 05 PPFD; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [PpfdAtHourComponent](../../src/FlahaGrow.Grasshopper/Components/PlantLightComponents.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa413`.

### Short description

Reads estimated incident plant light using one shared context; PAR 400–700 nm.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Context | Plant Light Context | PlantLightContextParameter | item | Required registration | None registered | Typed plant-light context | — (not a physical quantity) | Connect Plant Light Context → Context or Combine Plant Light → Context. No separate factor needed. | Plant Light Context.Context / Combine Plant Light.Context |
| 1 | Hour | Hour index | Param_Integer | item | Required registration | None registered | 0–8759 for the supported annual axis | Zero-based index | Connect Select Date and Hour → Hour. Zero-based 0–8759; Jan 1 00:00–01:00 = 0. | Select Date and Hour.Hour |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | PPFD | Estimated PPFD | Param_Number | list | S values in saved sensor order; finite ≥ 0 | µmol/m²/s | µmol/m²/s. Hour grid or 8760-hour sensor series, as named. | Lux × source factor → compatible plot/integration |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Quantity, selection and method provenance. Indices do not imply a known calendar date. | Component diagnostics → Panel |
| 2 | Plot | Plot Attributes | PlotAttributesParameter | item | Typed, data-bound PlotAttributes; matching data checksum/shape | — (not a physical quantity) | Connect to Annual Plot.Plot with this component's matching PPFD/DLI values. Grid outputs describe a grid, not an annual series. | Reader.Plot → Annual Plot.Plot, only for supported temporal series |

### Workflow description

Connect Context and Hour Index.Hour; use PPFD for the grid at that interval.

### Notes

Output follows saved sensor order. Plot attributes identify a spatial grid and must not be used as an annual heatmap series.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.4.0**. Last component update: **2026-09-12T18:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports.
