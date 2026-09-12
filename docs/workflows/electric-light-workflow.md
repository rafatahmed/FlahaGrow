# Electric-light workflow

1. **Select IES Luminaire** chooses an IES file from `RadIES`.
2. **IES to Radiance** runs `ies2rad`, writes its output under the supplied project's `Luminaire_files`, and applies configured RGB channels.
3. **Lighting Geometry** creates placement transforms; **Compile Luminaires** writes `luminaries.rad`.
4. **Electric Annual Simulation** validates/snapshots one non-leap 8,760-hour dimming schedule, runs a full-output electric calculation, and writes a manifest-owned annual illuminance result.
5. **Combine Annual Lighting** accepts only completed daylight/electric runs with the same hour count, sensor count, and sensor-order hash. It creates a new run with summed illuminance. Build that run's cache before cache-native readers consume it.

Combination adds illuminance values. It does not infer lighting controls, spectrum, crop requirements, or energy. Select an appropriate factor for PPFD conversion and use **Lighting Energy** with the real watt schedule for energy reporting.
