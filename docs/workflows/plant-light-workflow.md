# Plant-light workflow

Use a source-specific spectral profile to estimate PPFD and DLI from validated annual illuminance. This is scalar photometric conversion, not wavelength-resolved light transport or a crop-growth prediction.

1. Run Annual Simulation or Electric Annual Simulation and connect Folder to Load Annual Result.Folder.
2. Choose a reference using Spectral Profile, or provide a named numeric factor/measured CSV through Custom Spectral Profile. The custom component requires exactly one of Factor and CSV.
3. Connect Load Annual Result.Result and the chosen Profile to Plant Light Context. Verified run weather supplies the time axis; leave the optional F32 and Alignment inputs unwired for this path.
4. Connect Result to Select Date and Hour.Result. Its Run Button opens the selector; Hour is 0–8759 and Day is 0–364. Both use interval-start local standard time.
5. Read Context independently using PPFD at Hour, Annual PPFD at Sensor, DLI for Day or Annual DLI at Sensor. DLI readers do not require a PPFD wire.
6. Connect an annual sensor reader's values and matching Plot output to Annual Plot. A selected-hour/day sensor grid is not an annual temporal series. Use Sensor Marker with the original ordered sensor points to locate an index.

For multiple spectral sources, load each run separately, apply its own Profile and combine the resulting contexts with Combine Plant Light. Sensors, ordering and time axes must agree. Source conversion occurs before photon addition. Combine Annual Lighting instead combines illuminance and is not a substitute for per-source spectral treatment.

PPFD uses µmol/m²/s. DLI uses mol/m²/day. DLI for Day also returns a tree with one 24-value branch per sensor, in mol/m² per hourly interval; summing a branch yields that sensor's daily DLI. Missing or inconsistent data produces diagnostics rather than silent clamping.

Lux to PPFD converts numeric lux independently of caches. Grasshopper applies its item inputs across lists. Annual DLI integrates a complete numeric PPFD series using its timestep; DLI Target compares a completed daily series to an explicitly supplied target.

See the [component reference](../components/README.md) for exact ports, defaults and units, [weather/plot contract](result-weather-plot-contract.md) for timing, and [scientific assessment](../architecture/ppfd-dli-scientific-assessment.md) for limitations.
