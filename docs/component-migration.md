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
| 15 | Annual Simulation | Annual Simulation | ModelToRad project folder, EPW, sky subdivision, detail, run, optional points/bin folder | Four/single Radiance batch jobs, progress logs, and result folder; preserves sensor order across parts |
| 16 | Load Annual Result | Load Annual Result | Result folder, build | Merged `.ill`, Python-compatible `.f32` and lowercase-key `.meta.json` |
| 17 | selected_hour_index | Select Date and Hour | Boolean run | Hour index, non-leap-year calendar mapping |
| 18 | Illuminance Pointintime | Illuminance Point in Time | Cache path, `hour`/`sensor` mode, index, run | Hourly sensor series or one hour across all sensors |
| 19 | Illuminance sensor | Illuminance Sensor | Same as #18 | Same data contract and bounds validation as #18 |
| 20 | Annual Plot | Annual Plot | 8,760 values, ranges, grid/display options, run | Interactive hourly annual heatmap and PNG export |
| 21 | Sensor Marker | Sensor Marker | Point, grid size, up vector | Upper hemisphere marker Brep |
| 22 | Select PIT to PPFD | Select PIT to PPFD | Boolean run | Hour index, non-leap-year calendar mapping |
| 23 | Hourly PAR | Hourly PPFD | Cache path, hour index, conversion factor | Per-sensor PPFD at selected hour |
| 24 | PAR Each Sensor | Sensor Annual PPFD | Cache path, sensor index, conversion factor, optional point/marker | 8,760 PPFD values and optional marker |
| 25 | Annual Plot PPFD for sensor | Annual Plot PPFD for Sensor | 8,760 PPFD values, ranges, grid/display options, run | Interactive annual PPFD heatmap and PNG export |

## Migration rules

- Preserve units, data ordering, and file naming before changing a component UI.
- Keep each legacy component identity, even when code is shared internally, so existing definitions can be migrated predictably.
- Use portable paths returned by **Simulation Paths**. Do not reintroduce fixed `C:\` library locations.
- The annual cache format is little-endian `float32`, row-major **hours × sensors**, with `sensors`, `hours`, and `ncomp` in the sibling metadata JSON.
- Annual execution must support both a single `annualRfinal_part0.ill` result and the four-part result set.
- For split annual jobs, point blocks must be contiguous and merge in part order. Round-robin splitting breaks the relationship between cache columns and sensor-grid positions.
- Radiance result parsing must skip all nonnumeric header lines, not only lines above the first blank line.

## Additional compiled support components

| Component | Purpose |
| --- | --- |
| Annual Simulation Progress | Reads the latest stage from `annual_progress_partN.log` files and counts final annual-result files. Use a Grasshopper Timer for live updates. |
