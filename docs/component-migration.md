# Legacy-to-plugin component migration contract

This document is the implementation contract for the compiled FlahaGrow Grasshopper plugin. Each legacy Grasshopper Python component remains the behavioural reference until the corresponding compiled component is tested in Rhino.

| # | Legacy script | Compiled component | Required inputs | Required outputs / effects |
| ---: | --- | --- | --- | --- |
| 1 | `01 Working Directory` | Simulation Paths | Root folder; six output-folder toggles | Root and selected PIT/annual/spectral folders; creates missing folders |
| 2 | `02 Radiance Version` | Radiance Status | Run | `rcontrib` version/status |
| 3 | Opaque Materials – façade | Facade Material | Run; RadMaterials folder | Selected Radiance modifier |
| 4 | Opaque Materials – frame | Frame Material | Run; RadMaterials folder | Selected Radiance modifier |
| 5 | Opaque Materials – ground | Ground Material | Run; RadMaterials folder | Selected Radiance modifier |
| 6 | Opaque Materials – concrete | Concrete Material | Run; RadMaterials folder | Selected Radiance modifier |
| 7 | Glazing Materials | Glazing Material | Run; RadGlazing folder | Selected glazing modifier |
| 8 | Spectral Data Load | Load Spectral Data | Open UI; wavelength step | Conversion factor; calculated spectral CSV |
| 9 | Spectral Data Selection | Select Spectral Factor | Run; wavelength step | Standard/custom conversion factor |
| 10 | Spectral Data Selection2 | Select Spectral Factor (legacy) | Run; wavelength step | Same contract as #9 |
| 11 | Select Grow Light | Select IES Luminaire | Run; RadIES folder | IES file path and luminaire name |
| 12 | IES to Rad | Convert IES to Radiance | IES path/name, RGB, multiplier, project folder, run | `.rad`/`.dat` paths, log; runs `ies2rad` |
| 13 | Lighting Geometry | Place Luminaires | Points, rotations, `.rad` paths | `xform` lines |
| 14 | Compile Luminaries | Compile Luminaires | xform lines, project folder, write | `luminaries.rad` path |
| 15 | Annual Simulation | Run Annual Simulation | Folder, EPW, sky subdivision, quality, run | Radiance batch files and result folder |
| 16 | Load Annual Result | Build Annual Cache | Result folder, build | Merged `.ill`, `.f32`, `.meta.json` |
| 17 | selected_hour_index | Select Annual Hour | Run | Hour index, non-leap-year calendar mapping |
| 18 | Illuminance Pointintime | Read Illuminance | Cache path, mode, sensor/hour index, run | Hourly sensor series or sensor row |
| 19 | Illuminance sensor | Read Illuminance (legacy) | Same as #18 | Same contract as #18 |
| 20 | Annual Plot | Annual Heatmap | 8,760 values, ranges, display options, run | Interactive hourly annual heatmap / PNG export |
| 21 | Sensor Marker | Sensor Marker | Point, grid size, up vector | Upper hemisphere marker Brep |
| 22 | Select PIT to PPFD | Select PPFD Hour | Run | Hour index, non-leap-year calendar mapping |
| 23 | Hourly PAR | Hourly PPFD | Cache path, hour index, conversion factor | Per-sensor PPFD at selected hour |
| 24 | PAR Each Sensor | Sensor Annual PPFD | Cache path, sensor index, conversion factor, optional point/marker | 8,760 PPFD values and optional marker |
| 25 | Annual Plot PPFD for sensor | Annual PPFD Heatmap | 8,760 PPFD values, ranges, display options, run | Interactive annual PPFD heatmap / PNG export |

## Migration rules

- Preserve units, data ordering, and file naming before changing a component UI.
- Keep each legacy component identity, even when code is shared internally, so existing definitions can be migrated predictably.
- Use portable paths returned by **Simulation Paths**. Do not reintroduce fixed `C:\` library locations.
- The annual cache format is little-endian `float32`, row-major **hours × sensors**, with `sensors`, `hours`, and `ncomp` in the sibling metadata JSON.
- Annual execution must support both a single `annualRfinal_part0.ill` result and the four-part result set.
