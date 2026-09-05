# Annual greenhouse daylight workflow

This guide documents the compiled **FlahaGrow** Grasshopper annual workflow. It calculates hourly illuminance for a sensor grid, stores 8,760 annual values per sensor, and provides the data required by the PPFD, DLI, electric-light, and visualisation stages.

The current implementation is a three-channel/RGB Radiance workflow. Hyperspectral simulation is planned and is not represented by these annual illuminance results.

## What the workflow produces

For every sensor point, the workflow produces a row-major annual cache:

```text
hour 0:    sensor 0, sensor 1, ... sensor N
hour 1:    sensor 0, sensor 1, ... sensor N
...
hour 8759: sensor 0, sensor 1, ... sensor N
```

The cache is little-endian `float32` illuminance in lux. Its metadata records the number of sensors, 8,760 hours, and one illuminance component. Use the cache as the source for point-in-time illuminance, sensor annual illuminance, PPFD conversion, and DLI aggregation.

## Prerequisites

- Rhino 8 and Grasshopper on Windows.
- The compiled `FlahaGrow.gha` installed in `%AppData%\Grasshopper\Libraries`.
- Ladybug Tools / Honeybee components used to create the Radiance model and sensor grid.
- Radiance commands available from Ladybug Tools, normally:

  `C:\Program Files\ladybug_tools\radiance\bin`

  The annual runner detects this path automatically. Its `Bin` input can specify another Radiance `bin` folder.
- An EPW weather file and a writable project folder.

The runner also sets `RAYPATH` to the sibling Radiance `lib` folder so that `reinsrc.cal` and `reinhart.cal` are available for direct-sun calculations.

## Input project contract

The Annual Simulation `Project folder` is the Honeybee ModelToRad project root. Before the annual run it must contain:

```text
<project>/
  model/
    scene/
      envelope.rad
      envelope.mat
      envelope.blk
    grid/
      <sensor-grid>.pts   # any .pts name is accepted
```

`envelope.rad` is the visible scene, `envelope.mat` supplies the normal material modifiers, and `envelope.blk` is the blackened scene used to isolate direct daylight and direct sun.

### Sensor-point source

Annual Simulation supports two compatible routes:

1. Connect `LB Generate Point Grid` to `Sensor points (Pts)`. FlahaGrow writes `model\grid\0.pts` with upward normals.
2. Leave `Pts` empty when Honeybee ModelToRad has already written a `.pts` file under `model\grid`. FlahaGrow uses that file as `0.pts`, matching the legacy Python workflow.

For a horizontal plant canopy, upward normals (`0 0 1`) are appropriate. For a non-horizontal analysis plane, use the Honeybee-written `.pts` file if its sensor normals must be retained.

## Grasshopper workflow

```text
Honeybee ModelToRad project + sensor grid + EPW
                       |
                       v
             FlahaGrow / Annual / Annual Simulation
                       |
                       +--> batch files and result folder
                       |
                       v
          FlahaGrow / Annual / Load Annual Result  (Build = True)
                       |
                       +--> annualRfinal.f32 + annualRfinal.meta.json
                       |
          +------------+-------------+
          v                          v
Illuminance Point in Time      Illuminance Sensor
          |                          |
          v                          v
  hourly spatial values       8,760 values for one sensor
```

### 1. Prepare the annual run

Use **FlahaGrow → Annual → Annual Simulation**.

| Input | Meaning |
| --- | --- |
| `Project` | Honeybee ModelToRad project folder. |
| `EPW` | Valid EPW weather-file path. |
| `Sky` | `1` for Tregenza; `4` for the higher Reinhart subdivision. |
| `Detail` | `low`, `mid`, `high`, `very high`, or a custom Radiance parameter string. |
| `Run` | Boolean trigger that writes and launches the jobs. |
| `Pts` | Optional Ladybug sensor points. |
| `Bin` | Optional Radiance `bin` folder; blank uses automatic detection. |

With more than ten sensors, the runner makes four jobs: `run_part0.bat` through `run_part3.bat`. Sensor points are split into **contiguous** blocks, preserving their original grid order when results are merged. With ten or fewer sensors it makes one job.

The runner performs the same three-term Radiance calculation as the legacy Python component:

```text
annualR   = total daylight
annualRd  = direct daylight through the blackened scene
annualRs  = direct sun using Reinhart sky subdivisions
final     = annualR - annualRd + annualRs
```

Each part writes `annualRfinal_partN.ill` after it completes.

### 2. Monitor progress

Radiance matrix commands do not provide a reliable percent-complete value. They are intentionally quiet while `rfluxmtx` and `rcontrib` are working.

The generated batch windows now show stages `1/8` through `8/8` and write `annual_progress_partN.log` files. Use **FlahaGrow → Annual → Annual Simulation Progress** with the same result folder to read the latest stage for each part. Attach a Grasshopper Timer when live refresh is wanted.

The calculation is still active when a Radiance process is consuming CPU or when its intermediate `.mtx`, `.ill`, or `.oct` files are changing. Do not build the cache until all parts show `Completed` and every final `.ill` file has a nonzero size.

### 3. Build the annual cache

Use **FlahaGrow → Annual → Load Annual Result**.

Connect Annual Simulation `Result folder` to `Folder`, then set `Build` True only after the run completes. It reads the Radiance headers safely, merges the part columns in original sensor order, and writes:

```text
annualRfinal.ill
annualRfinal.f32
annualRfinal.meta.json
```

The metadata is compatible with both legacy Python and compiled components:

```json
{
  "sensors": 1716,
  "hours": 8760,
  "ncomp": 1,
  "order": "row-major hours x sensors"
}
```

The numbers above are an example from the tested Qatar project, not a requirement for another project.

### 4. Inspect results

Use **Select Date and Hour** to select a non-leap-year date and AM/PM hour. Its `Selected hour index` output is zero-based (`0` to `8759`) and follows the legacy convention used by EPW annual data.

Connect the cache to either illuminance reader:

| Component | Mode | Output |
| --- | --- | --- |
| `Illuminance Point in Time` | `hour` | One lux value for every sensor at the selected hour. |
| `Illuminance Sensor` | `sensor` | 8,760 lux values for one selected sensor. |

Use **Sensor Marker** to create an upper-hemisphere marker at a selected sensor point. Its inputs are point, grid size, and optional `Up` vector.

## Visualisation guidance

- A point-in-time result is a discrete sensor-grid sample, not a smooth daylight field. Cell-to-cell changes are expected near framing members and direct-sun patches.
- Disable preview on the upstream point-grid or Sensor Marker component when red crosses obscure the heat map.
- Set a legend range that matches the selected hour. A fixed range of 20,000–120,000 lux clips lower values to dark blue. For the tested mid-afternoon Qatar example, a range near 0–65,000 lux makes variation more visible.
- If an older run was made before the contiguous sensor-order correction, rerun Annual Simulation and rebuild the cache before judging the spatial pattern.
- The direct-sun combination can contain a small number of negative lux values from numerical subtraction. The workflow preserves the legacy result; clamp values to zero only in a downstream visualisation or metric stage if that is the study convention.

## Troubleshooting

| Symptom | Cause | Resolution |
| --- | --- | --- |
| `Required annual-simulation file ... model\grid\0.pts` | No sensor grid was found. | Connect `Pts`, or ensure ModelToRad wrote a `.pts` file inside `model\grid`. |
| Empty `.oct`, `.mtx`, or `.ill` files | Batch process could not resolve Radiance commands. | Leave `Bin` blank for automatic Ladybug Tools discovery, or provide the correct `bin` folder. |
| `rcalc: cannot find file 'reinsrc.cal'` | Radiance library path was unavailable. | Use the current plugin build; it sets `RAYPATH` to the sibling `lib` directory. |
| `rcontrib: warning - no light sources found` | The preceding `rcalc` direct-sun source generation failed. | Resolve the `reinsrc.cal` error, then rerun all parts. |
| `Non-numeric annual-result value ... rmtxop` | A loader attempted to parse Radiance header text. | Use the current Load Annual Result component, which accepts valid Radiance headers. |
| `Index was outside the bounds of the array` in an illuminance reader | Legacy lowercase metadata was not being read. | Use the current reader; it accepts Python-compatible `sensors`, `hours`, and `ncomp` keys and reports useful bounds errors. |
| Heat map appears checkerboarded or spatially scrambled | Result order and point-grid order do not match. | Run the current Annual Simulation component, then rebuild the cache. |

## Validation record

The workflow has been exercised with an EPW-driven Qatar greenhouse case containing 1,716 sensors and 8,760 annual hours. The generated cache contained exactly 15,032,160 float values (`1716 × 8760`) and was read successfully by the compatible annual reader contract.

This is a workflow validation, not a claim of experimental calibration. Each study should still document its weather file, greenhouse geometry, materials, sensor plane, luminaire assumptions, spectral conversion factor, and plant targets.
