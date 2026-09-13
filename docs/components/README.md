# FlahaGrow component reference

Complete reference for the current source: **30 components** (all visible), across eight toolbar categories. Reviewed **2026-09-13**. This documents the working tree, including changes that may not yet be installed in Rhino; it does not assert that a build was deployed. Component version/date entries are taken from the code revision ledger, not inferred from this review date.

## Category references

| Category | Components | Scope |
| --- | --- | --- |
| [00 Setup](00-setup.md) | 3 | Paths, workspace and Radiance readiness |
| [01 Materials](01-materials.md) | 2 | Opaque and glazing library selectors |
| [02 Spectral](02-spectral.md) | 2 | Built-in/custom profiles and explicit custom assumptions |
| [03 Annual](03-annual.md) | 8 | Simulation, loading, illuminance, time selection, plots and markers |
| [04 Electric Light](04-electric-light.md) | 5 | IES conversion, placement, scene assembly and annual electric simulation |
| [05 PPFD](05-ppfd.md) | 5 | Source contexts, photon composition and PPFD readers/converters |
| [06 DLI](06-dli.md) | 4 | Daily integration, sensor/day readers and target comparison |
| [07 Energy](07-energy.md) | 1 | Electrical power integration |

Every component section records its actual name/nickname, icon availability, GUID, implementation source, description, all registered input/output ports, workflow, caveats and revision. Each registered component has a unique display name and GUID. The hidden typed parameters used on wires are data containers, not additional executable components in this count.

## Reading port tables

- **Port letter / nickname** is the actual short label on the component; it is not always a single letter. `#` is its zero-based registration order. Do not reorder existing wires based on an abbreviated tutorial name.
- **Data type** is the registered Grasshopper parameter class. `Param_Number` carries numbers, `Param_Integer` integers, `Param_String` text/path values, `Param_Boolean` Booleans, `Param_GenericObject` generic values, and `Param_Point`/`Param_Vector`/`Param_Brep` geometry. Named FlahaGrow `…Parameter` classes carry typed objects, not interchangeable strings.
- **Access** is `item` (one value per solve), `list` (an ordered list per solve) or `tree` (branched Grasshopper data). A list of sensors and a list of hours may have the same numeric type but different meanings. Preserve branches and ordering unless the component explicitly flattens them.
- **Required status** reports Grasshopper registration. A required input with a supplied default does not require a wire. Optional registration does not mean optional for every workflow: for example, Context needs Result or legacy F32, and Custom Spectral Profile needs exactly one of Factor or CSV.
- **Default value** is persistent data registered by the component. `None registered` is not necessarily the solver fallback: fallbacks such as optional marker size 0 are identified in range/notes. Output cells are not guaranteed to contain values before successful execution.
- **Valid range / format** gives the supported contract. Where code does not fully enforce it, notes distinguish recommended finite/physical values from checks actually performed. Registered descriptions are retained even where a label needs qualification; notes explain the actual quantity.
- **Units** describe the data, not the parameter's storage type. Paths, context objects and diagnostics do not themselves have physical units. A relative spectral integral must not be treated as an absolute measurement.
- **Source / connection** identifies the intended upstream producer or computation/downstream consumer. Generic Grasshopper Buttons, Panels, sliders, List Item and geometry parameters are host components, not missing FlahaGrow toolbar components.
- Boolean behavior is component-specific. Prefer a **Button** for one-shot selection/start operations; do not assume every Run port is edge-triggered or that setting Run=False cancels a process.

## Quantities: illuminance, PAR, PPFD and DLI

| Quantity | Meaning | Unit | FlahaGrow boundary |
| --- | --- | --- | --- |
| Illuminance | Light weighted for photopic human vision | lux | Annual Radiance result and its cache |
| PAR | The photosynthetically active spectral band, conventionally 400–700 nm here; not itself a unique unit | W/m² when expressed as PAR irradiance | Must not be confused with photon flux; the plant-light readers output PPFD or DLI, not PAR irradiance |
| PPFD | Photon arrival rate integrated over that band per area | µmol/m²/s | Estimated from lux and a source-specific spectral factor |
| DLI | PPFD integrated over one day | mol/m²/day | Sum of interval photon amounts across the day |

For the scalar pathway, `PPFD = lux × factor`. For uniform samples, `DLI = Σ(PPFD × dt_seconds) / 1,000,000`. With hourly data, each contribution is `PPFD × 0.0036` mol/m² per interval; a daily total contains 24 such contributions. An hourly contribution is neither instantaneous PPFD nor the full DLI for a day.

Different spectra can yield the same lux but different PPFD. The built-in reference library does not turn photometric Radiance simulation into wavelength-resolved transport. Spectrum at the receiving sensor, glazing/filtering, source mix and schedule alignment still matter. See the [scientific assessment](../architecture/ppfd-dli-scientific-assessment.md), [Radiance methods](../architecture/radiance-methods.md) and [referenced spectral research](../research/plant-light/README.md) for evidence, assumptions and limitations. This reference does not introduce new research claims or validated crop thresholds.

## Recommended connection contract

| Producer | Consumer | Contract |
| --- | --- | --- |
| Simulation Paths.Paths | Working Directory.Paths | Typed resolved paths |
| Working Directory.Analysis | Radiance Status.Analysis; Annual Simulation.Analysis | Typed workspace analysis; not exported scene geometry |
| Radiance Status.Radiance | Simulation / IES to Radiance.Radiance | Verified runtime context |
| Annual Simulation.Folder or Electric Annual Simulation.Folder | Load Annual Result.Folder | Run folder, not arbitrary `.ill` or `.f32` file |
| Load Annual Result.Result | Select Date and Hour.Result; Plant Light Context.Result | Typed cache plus verified weather when available |
| Spectral Profile.Profile | Plant Light Context.Profile | Profile for that individual source's received-light assumption |
| Plant Light Context.Context | PPFD at Hour.Context; Annual PPFD at Sensor.Context; DLI for Day.Context; Annual DLI at Sensor.Context | Independent readers of one consistent context; DLI does not require a PPFD wire |
| Independent source Context outputs | Combine Plant Light.Sources | Convert per source before photon addition; same sensors/time axis |
| Select Date and Hour.Hour | PPFD at Hour.Hour; Read Illuminance in hour mode | Zero-based hour index |
| Select Date and Hour.Day | DLI for Day.Day | Zero-based day index |
| Annual sensor reader's data + its Plot | Annual Plot.Data + Plot | Matched temporal values and attributes; no cross-wiring from another selection |

The Honeybee/Radiance exported scene root still has to be supplied to simulation Project inputs; a workspace folder or its Inputs path is not automatically an exported model. File paths named Project and typed Project/Analysis contexts are different contracts.

### Time and weather

Use **Select Date and Hour** (nickname **HourIndex**) for date/hour selection. The current annual axis is 365 days / 8760 one-hour intervals in EPW local standard time. Jan 1 at 00:00 is Hour 0; Dec 31 at 23:00 is Hour 8759. Day is `floor(Hour / 24)`, from 0 to 364. The selected clock hour denotes interval start. Date is display text, not an alignment token. Leap-day and daylight-saving reinterpretation are not supported by this contract.

Connect the loaded Result to both the selector and Context. UTC offset and location come from the verified run EPW: **no manual UTC minutes are required**. A missing-weather result may still support numeric index access, but it must not silently acquire a fabricated location/UTC axis. The selector has no manual UTC input; legacy Alignment is normally unwired. Associating an electric schedule with Time Result declares its axis, but does not independently verify the schedule's authorship.

### Sensors and plotting

Sensor means the **zero-based index of a point in the saved simulation grid**, not a Point3d input. Sensor 5 is the sixth point. The same integer used with Grasshopper List Item on the original ordered grid identifies its geometry. Never sort/cull/rebuild the point list independently of result ordering. Optional markers only visualize supplied points; they do not repair mismatched identity.

An hour/day grid contains one value per sensor. An annual sensor series contains one value per hour/day. Only the latter is appropriate for an annual temporal heatmap. Matching count alone is insufficient: a 365-sensor grid is not a 365-day series. Plot attributes bind quantity, units, shape, title, selection and data identity. Automatic ranges describe the observed data distribution, not plant suitability. Set Manual=True only when explicit comparable thresholds are wanted. Open plot windows are snapshots; pulse Run again after changes.

## Maintenance and verification

Primary authority: each component's constructor, input/output registration, solver, shared Core calculation and `ComponentRevisionCatalog`. The catalog can be inspected reproducibly after preparing the smoke-test runtime:

```powershell
.\tools\Test-SetupComponents.ps1 -NoRestore
dotnet tests/FlahaGrow.SetupSmoke/bin/Release/net7.0-windows/FlahaGrow.SetupSmoke.dll --component-catalog 05
.\tools\Test-ComponentDocumentation.ps1
```

Use panel prefixes 00 through 07; omit the prefix to export all registrations. This reports names, GUIDs, types, access, optional flags, defaults, icons and revision metadata. Workflow and scientific notes still require human/source review; registration extraction alone cannot prove calculation correctness.

See also [toolbar navigation](navigation.md), [icon inventory](icons.md), [migration](migration.md), [result/weather/plot workflow](../workflows/result-weather-plot-contract.md), and [validation status](../quality/current-status.md).

Documentation validation requires a successful smoke-test stamp matching current source and runtime hashes. Run `tools/Test-SetupComponents.ps1` before `tools/Test-PluginAudit.ps1`; the local Yak packaging script runs Core tests and both gates before staging. See the [deep audit](../quality/plugin-audit.md) for regression coverage and remaining limits.
