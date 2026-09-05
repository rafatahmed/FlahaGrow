# Legacy script and compiled component comparison

This matrix is the maintenance reference for the 25 legacy Grasshopper Python scripts in `src/Code`. “Legacy inputs” use the script's Grasshopper contract; underscores are omitted for readability. “Compiled inputs/outputs” use the parameter names shown in the FlahaGrow tab.

`Match` means the compiled component preserves the workflow role. `Extension` means the compiled version adds diagnostics, optional portable paths, or an explicit status output without removing the legacy capability. Both versions should retain the same units and annual ordering.

## Setup and library selection

| # | Legacy script | Compiled component | Legacy inputs → outputs | Compiled inputs → outputs | Comparison |
| ---: | --- | --- | --- | --- | --- |
| 1 | `01 Working Directory.py` | **Working Directory** | Root folder; six folder toggles → root and enabled subfolder paths | Root folder; six folder toggles → root and enabled subfolder paths | Match. **Simulation Paths** is an additional portable-path helper. |
| 2 | `02 Radiance Version.py` | **Radiance Version** | Run; optional Radiance location → version text | Run; optional `Bin` → version/diagnostic text | Match. **Radiance Status** is an additional executable-discovery helper. |
| 3 | `MS_Opaque Materials - facade .py` | **Facade Material** | Run; material library → selected modifier | Run; optional `RadMaterials folder` → modifier | Extension: bundled library fallback. |
| 4 | `MS_Opaque Materials - Frame.py` | **Frame Material** | Run; material library → selected modifier | Run; optional `RadMaterials folder` → modifier | Extension: bundled library fallback. |
| 5 | `MS_Opaque Materials - Ground.py` | **Ground Material** | Run; material library → selected modifier | Run; optional `RadMaterials folder` → modifier | Extension: bundled library fallback. |
| 6 | `MS_Opaque Materials_Concrete.py` | **Concrete Material** | Run; material library → selected modifier | Run; optional `RadMaterials folder` → modifier | Extension: bundled library fallback. |
| 7 | `MS Glazing Materials - Glazing.py` | **Glazing Material** | Run; glazing library → selected modifier | Run; optional `RadGlazing folder` → modifier | Extension: bundled library fallback. |

## Spectral conversion and electric lighting

| # | Legacy script | Compiled component | Legacy inputs → outputs | Compiled inputs → outputs | Comparison |
| ---: | --- | --- | --- | --- | --- |
| 8 | `CF_ILL_PPFD_Spectral Data Load.py` | **Load Spectral Data** | Load trigger; wavelength interval; spectral CSV → factor and calculated spectral quantities | `Load spectral data`; wavelength interval → conversion factor; PAR sum; lux sum; file | Match with explicit intermediate outputs. |
| 9 | `CF_ILL_PPFD_Spectral Data Selection.py` | **Select Spectral Factor** | Run trigger; wavelength interval → standard/custom factor | Run; wavelength interval → factor; source | Match. Both standard and custom CSV selection are exposed through the Boolean-triggered UI. |
| 10 | `CF_ILL_PPFD_Spectral Data Selection2.PY` | **Select Spectral Factor (Legacy)** | Run trigger; wavelength interval → standard/custom factor | Run; wavelength interval → factor; source | Match. Separate identity retained for legacy definitions. |
| 11 | `01 sELECT gROW lIGT.PY` | **Select IES Luminaire** | Run; IES library → IES path; luminaire name | Run; optional `RadIES folder` → IES path; luminaire name | Extension: bundled library fallback. |
| 12 | `02 IES to Rad.PY` | **IES to Radiance** | IES path/name; RGB; multiplier; project; DAT; run → Radiance/DAT files | IES path; name; R/G/B; multiplier; project; DAT; run; optional `Bin` → `.rad` paths; `.dat` paths; log | Extension: automatic `ies2rad` discovery, safe output names, conversion log. |
| 13 | `03 Lighting Geometry.PY` | **Lighting Geometry** | Placement points; rotations; Radiance luminaire files → `xform` lines | Points; X/Y/Z rotation lists; Radiance files → lighting geometry; status | Extension: list broadcasting and validation status. |
| 14 | `04 Compile Luminaries.PY` | **Compile Luminaires** | `xform` lines; project folder; write → `luminaries.rad` | Lighting geometry; project; write → luminaire Radiance file; status | Match with explicit status. |

## Annual Radiance calculation and illuminance results

| # | Legacy script | Compiled component | Legacy inputs → outputs | Compiled inputs → outputs | Comparison |
| ---: | --- | --- | --- | --- | --- |
| 15 | `01 Annual Simulation.py` | **Annual Simulation** | Project folder; EPW; sky subdivision; detail/custom parameters; run → result folder; generated batch jobs | Project; EPW; Sky; Detail; Run; optional Pts; optional Bin → result folder; batch files; status | Extension: ModelToRad `.pts` discovery, LB point-grid input, automatic PATH/RAYPATH, progress logs, and contiguous part order. Calculation remains `annualR - annualRd + annualRs`. |
| 16 | `02 Load Annual Result.py` | **Load Annual Result** | Result folder; build → `.f32` cache; sensors; hours; status | Folder; Build → result cache; sensors; hours; status | Match. Reads complete Radiance headers and writes Python-compatible lowercase metadata keys. |
| 17 | `03 selected_hour_index.py` | **Select Date and Hour** | Run → selected non-leap-year hour index | Run → selected hour index; readable date/hour | Extension: readable selection output. |
| 18 | `04 Illuminance Pointintime.py` | **Illuminance Point in Time** | Cache; mode; sensor/hour index; run → illuminance list; status | Result cache; Mode; Index; Run → illuminance; status | Match. `hour` returns all sensors at one hour; `sensor` returns 8,760 values. |
| 19 | `05 Illuminance sensor.py` | **Illuminance Sensor** | Cache; mode; sensor/hour index; run → illuminance list; status | Result cache; Mode; Index; Run → illuminance; status | Match. Separate identity retained, sharing the same validated cache reader. |
| 20 | `06 Annual Plot.py` | **Annual Plot** | 8,760 values; four ranges; grid mode/color; range names; title; run → interactive heatmap / PNG | Hourly results; R1–R4; Grid; Grid color; Name 1–5; Title; Run → status, interactive heatmap / PNG | Match. Uses the legacy 365 × 24 classification and five default buckets. |
| 21 | `07 Sensor Marker.py` | **Sensor Marker** | Point; grid size; Up → upper hemisphere Brep | Sensor point; Grid size; optional Up → Marker | Match. |

## PPFD and annual PPFD review

| # | Legacy script | Compiled component | Legacy inputs → outputs | Compiled inputs → outputs | Comparison |
| ---: | --- | --- | --- | --- | --- |
| 22 | `01 Select PIT to PPFD.py` | **Select PIT to PPFD** | Run → selected non-leap-year hour index | Run → selected hour index; readable date/hour | Match. Shares selection state and hour convention with Select Date and Hour, as the Python scripts share the same sticky key. |
| 23 | `02 Hourly PAR.py` | **Hourly PAR** | Result cache; hour index; numeric/preset factor → per-sensor PPFD | Result cache; Hour; numeric/preset Factor → PPFD; status | Match. Reads the annual cache directly and supports `electric`, `sunonly`, and `skyonly` factor presets. |
| 24 | `03 PAR Each Sensor.py` | **PAR Each Sensor** | Result cache; sensor index; factor; optional points/marker settings → 8,760 PPFD values; sensor point; marker | Result cache; Sensor; numeric/preset Factor; optional Pts/Mark/Size/Up → PPFD; sensor point; marker; status | Match. The generic **PPFD Each Sensor** remains a separate list-conversion helper. |
| 25 | `04 Annual Plot PPFD for sensor.py` | **Annual Plot PPFD for Sensor** | 8,760 PPFD values; ranges; grid/display options; title; run → interactive heatmap / PNG | Hourly results; R1–R4; Grid; Grid color; Name 1–5; Title; Run → status, interactive heatmap / PNG | Match. Reuses the annual-plot engine with PPFD naming and title. |

## Extra compiled helper components

These do not replace a specific single legacy script; they make the compiled workflow more portable or provide downstream greenhouse metrics.

| Component | Inputs → outputs |
| --- | --- |
| **Simulation Paths** | Project/library folders → project; materials; glazing; IES; annual-result paths. |
| **Radiance Status** | Optional Radiance bin → availability; `rcontrib` path. |
| **Annual Simulation Progress** | Result folder; refresh → part progress; completed part count; status. |
| **Lux to PPFD** | One illuminance value; factor → PPFD. |
| **Hourly PPFD** | Illuminance list; numeric factor → PPFD list; helper for already-extracted point-in-time values. |
| **PPFD Each Sensor** | Annual illuminance list; numeric factor → annual PPFD list; helper for already-extracted sensor values. |
| **Annual DLI** | 8,760 PPFD values; timestep → 365 DLI values; mean DLI. |
| **DLI Target** | Daily DLI; target → sufficient flags; deficit; sufficient-day count. |
| **Lighting Energy** | Power schedule; timestep → kWh; operating hours. |

## Maintenance rules

1. Keep the script filename, compiled component name, permanent GUID, and this row aligned when a component changes.
2. Do not change the annual cache contract: little-endian `float32`, row-major `hours × sensors`, and sibling metadata keys `sensors`, `hours`, `ncomp`.
3. Preserve contiguous sensor splitting and part-order merge. Changing either breaks spatial visualisation.
4. Add optional inputs only at the end of a published component to avoid shifting existing Grasshopper wires.
5. Test both the compiled and Python cache paths whenever changing annual results, PPFD, or DLI components.
