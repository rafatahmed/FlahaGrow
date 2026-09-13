# 06 DLI: component reference

Integrate photon flux over a day. Recommended typed DLI readers consume Context directly; they do not require a PPFD component wired before them. They nevertheless use the same source conversion and integration rules.

[Reference guide and shared contracts](README.md) · [All components](navigation.md)

Documentation reviewed: **2026-09-13**. This is the current working-tree source contract, not a deployment claim. Per-component versions below come from the revision ledger.

## Components

- [Annual DLI](#annualdlicomponent)
- [Annual DLI at Sensor](#annualdliatsensorcomponent)
- [DLI for Day](#dlifordaycomponent)
- [DLI Target](#dlitargetcomponent)

<a id="annualdlicomponent"></a>

## Annual DLI

<!-- component: f32f1cbd-04b5-42ed-9fdf-c194851011b2 -->

- Short name / nickname: `DLI`.
- Category: 06 DLI; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [AnnualDliComponent](../../src/FlahaGrow.Grasshopper/Components/AnnualDliComponent.cs); component ID: `f32f1cbd-04b5-42ed-9fdf-c194851011b2`.

### Short description

Converts an hourly annual PPFD series into 365 daily light integral values.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | PPFD | Annual PPFD | Param_Number | list | Required registration | None registered | Finite nonnegative photon-flux values | µmol/m²/s | A complete non-leap-year PPFD series in μmol/m²/s; sample count must match Timestep. | PPFD reader.PPFD with matching order/timestep |
| 1 | dt | Timestep | Param_Number | item | Required registration; default supplied | 3600 | Finite &gt; 0; must divide 86400 s; complete 365-day series | s | Duration of each sample in seconds. | Sampling interval of the supplied numeric series (annual cache = 3600) |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | DLI | Daily DLI | Param_Number | list | Exactly 365 daily totals; finite ≥ 0 | mol/m²/day | 365 daily light integral values in mol/m²/day. | Daily integration / target comparison → plot, grid preview or assessment |
| 1 | Mean | Annual mean DLI | Param_Number | item | Arithmetic mean of the 365 daily totals | mol/m²/day | Mean daily light integral in mol/m²/day. | Daily integration / target comparison → plot, grid preview or assessment |

### Workflow description

Connect a complete annual PPFD series with its timestep dt; read 365 daily integrals and their mean.

### Notes

dt must be finite, positive and divide 86400 seconds exactly within tolerance; count must be 365 × (86400/dt). DLI = sum(PPFD × dt)/1e6. The annual mean divides each daily value before summing to avoid overflow from a large finite annual total. No crop suitability is inferred.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.3.0**. Last component update: **2026-09-13T16:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Computes the annual mean without overflowing the sum of finite daily values.

<a id="annualdliatsensorcomponent"></a>

## Annual DLI at Sensor

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa416 -->

- Short name / nickname: `DLI Sensor`.
- Category: 06 DLI; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [AnnualDliAtSensorComponent](../../src/FlahaGrow.Grasshopper/Components/PlantLightComponents.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa416`.

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
| 0 | DLI | Estimated DLI | Param_Number | list | 365 daily totals at the selected sensor; finite ≥ 0 | mol/m²/day | mol/m²/day. Day grid or 365-day sensor series, as named. | Daily integration / target comparison → plot, grid preview or assessment |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Quantity, selection and method provenance. Indices do not imply a known calendar date. | Component diagnostics → Panel |
| 2 | Plot | Plot Attributes | PlotAttributesParameter | item | Typed, data-bound PlotAttributes; matching data checksum/shape | — (not a physical quantity) | Connect to Annual Plot.Plot with this component's matching PPFD/DLI values. Grid outputs describe a grid, not an annual series. | Reader.Plot → Annual Plot.Plot, only for supported temporal series |

### Workflow description

Connect Context and Sensor; send DLI together with Plot to Annual Plot.

### Notes

Returns 365 daily totals for a single saved sensor; uses 24 one-hour contributions per day.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.4.0**. Last component update: **2026-09-12T18:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports.

<a id="dlifordaycomponent"></a>

## DLI for Day

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa415 -->

- Short name / nickname: `DLI Day`.
- Category: 06 DLI; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [DliForDayComponent](../../src/FlahaGrow.Grasshopper/Components/PlantLightComponents.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa415`.

### Short description

Reads estimated incident plant light using one shared context; PAR 400–700 nm.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Context | Plant Light Context | PlantLightContextParameter | item | Required registration | None registered | Typed plant-light context | — (not a physical quantity) | Connect Plant Light Context → Context or Combine Plant Light → Context. No separate factor needed. | Plant Light Context.Context / Combine Plant Light.Context |
| 1 | Day | Day index | Param_Integer | item | Required registration | None registered | 0–364; Jan 1 = 0 | Zero-based index | Connect Select Date and Hour → Day. Zero-based 0–364; Jan 1 = 0. Do not connect Hour or Date. | Select Date and Hour.Day |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | DLI | Estimated DLI | Param_Number | list | S daily totals in saved sensor order; finite ≥ 0 | mol/m²/day | mol/m²/day. Day grid or 365-day sensor series, as named. | Daily integration / target comparison → plot, grid preview or assessment |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Quantity, selection and method provenance. Indices do not imply a known calendar date. | Component diagnostics → Panel |
| 2 | Hourly | Hourly photon integral | Param_Number | tree | Tree {sensor}; 24 contributions per branch | mol/m² per 1-hour interval | 24 values per sensor branch {sensor}: mol/m² per hourly interval, not PPFD. | PPFD × 3600 / 1,000,000 for the selected day |
| 3 | Plot | Plot Attributes | PlotAttributesParameter | item | Typed, data-bound PlotAttributes; matching data checksum/shape | — (not a physical quantity) | Connect to Annual Plot.Plot with this component's matching PPFD/DLI values. Grid outputs describe a grid, not an annual series. | Reader.Plot → Annual Plot.Plot, only for supported temporal series |

### Workflow description

Connect Context and Hour Index.Day; use DLI for the spatial grid and Hourly to inspect its 24 contributions per sensor.

### Notes

Day is zero-based. Hourly is a tree with branch {sensor}; values are photon amounts per hourly interval, not instantaneous PPFD and not a daily integral computed from one hour.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.4.0**. Last component update: **2026-09-12T18:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Background cache loading, run-derived weather/time, typed result and data-bound plot attributes; appended compatibility ports.

<a id="dlitargetcomponent"></a>

## DLI Target

<!-- component: 0f9f53a1-6fe6-4a4f-ab14-ee19f53c4fdd -->

- Short name / nickname: `DLI Target`.
- Category: 06 DLI; visibility: placeable (primary).
- Icon available: No — no name-matched component bitmap.
- Implementation: [DliTargetComponent](../../src/FlahaGrow.Grasshopper/Components/DliTargetComponent.cs); component ID: `0f9f53a1-6fe6-4a4f-ab14-ee19f53c4fdd`.

### Short description

Reports daily DLI sufficiency and deficiency against a plant-specific target.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | DLI | Daily DLI | Param_Number | list | Required registration | None registered | Finite nonnegative daily photon amount (see validation notes) | mol/m²/day | Daily light integral values in mol/m²/day. | Daily DLI reader.DLI |
| 1 | Target | Target DLI | Param_Number | item | Required registration | None registered | Finite nonnegative daily photon amount (see validation notes) | mol/m²/day | Plant-specific daily light integral target in mol/m²/day. | User's referenced crop/stage requirement |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | OK | Sufficient | Param_Boolean | list | One Boolean per DLI item; True when value ≥ Target | — (not a physical quantity) | True where the target DLI is met. | Validation/comparison result → Panel or downstream Boolean filter |
| 1 | Deficit | Deficiency | Param_Number | list | One value per DLI item: max(0, Target − DLI) | mol/m²/day | Additional DLI required in mol/m²/day. | Daily integration / target comparison → plot, grid preview or assessment |
| 2 | Days | Sufficient days | Param_Number | item | Numeric count of items meeting Target; not inherently calendar days | Count | Number of days meeting the target. | Produced by this component; see workflow |

### Workflow description

Connect daily DLI values and a justified crop/stage target; inspect OK, Deficit and Days.

### Notes

Any list length is accepted: Days counts qualifying items, which are days only when input is a temporal daily series. On a spatial grid it counts sensors. Targets and DLI values must be finite and nonnegative; NaN and infinity are rejected. Crop targets are user-supplied, not a built-in crop database.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.1.0**. Last component update: **2026-09-13T16:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Rejects nonfinite DLI values and targets.
