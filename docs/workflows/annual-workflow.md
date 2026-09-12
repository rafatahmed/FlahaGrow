# Annual daylight workflow

This workflow creates and structurally validates a Radiance annual illuminance result. It does not by itself certify a model, spectral factor, or design outcome.

```text
model-export root + 8,760-record EPW + ready annual Radiance environment
                              │
                              ▼
                      Annual Simulation
                       │             │
                       ▼             ▼
                    Progress    Load Annual Result ──► checked .f32 cache
```

Connect a source root, EPW, sky subdivision (`1` Tregenza or `4` Reinhart), and detail setting to **Annual Simulation**. `Run` launches only on a false-to-true edge; `Run=False` does not cancel. `Cancel` is a separate edge-triggered action and terminates only recorded processes with a matching PID/start time. `Existing` opens a manifest-owned run and ignores `Run`.

The runner snapshots scene files, EPW, and sensor data; it writes one result part for ten or fewer sensors, otherwise four contiguous parts. Sensor contiguity is part of the cache contract. It checks free space before launch. With `Keep=False`, reproducible large intermediates are removed after success, while snapshots, logs, states, and matrices remain.

Use **Annual Simulation Progress** (optionally with a Timer). Its stage coverage is command-stage coverage, not elapsed-time or validated-completion percentage. Connect the run folder to **Load Annual Result** and trigger `Build`. The cache builder accepts only declared, completed parts with valid Radiance ASCII headers, expected dimensions, one component, finite non-negative values, and matching provenance. It writes `annualRfinal.f32` and metadata atomically. Readers recheck manifest, result signature, and cache hash; copied, edited, and legacy flat caches are rejected.

## How Radiance produces annual daylight illuminance

For each declared sensor part, the generated batch records eight stages. The
commands are retained in the run's batch file and logs, so this is inspectable
for a specific study:

1. `gendaymtx` converts the generated WEA into an annual sky matrix using the
   requested Tregenza (`Sky=1`) or Reinhart (`Sky=4`) subdivision.
2. `oconv` compiles the complete envelope scene. `rfluxmtx -I+` calculates
   total daylight coefficients from the sensor points to the ground-plus-sky
   receiver basis.
3. `dctimestep` multiplies those coefficients by the annual sky matrix.
   `rmtxop -c 47.4 119.9 11.6` reduces Radiance RGB values to the single
   illuminance channel written as `annualR_part*.ill`.
4. The workflow repeats the coefficient/matrix calculation with the black
   scene and direct-only sky to obtain a direct-daylight term
   (`annualRd_part*.ill`).
5. It builds a discrete-sun scene and uses `rcontrib`, `gendaymtx`, and
   `dctimestep` for the direct-sun term (`annualRs_part*.ill`).
6. `rmtxop` calculates the retained final matrix:

```text
annualRfinal = annualR − annualRd + annualRs
```

The final result has one row per annual hour and one column per sensor. It is
therefore an annual illuminance result in lux, before any plant-spectrum
conversion. A successful batch is not enough: Load Annual Result checks the
matrix header, 8,760-row/hour contract, declared sensor counts, single
component, finite values, non-negative illuminance, and provenance before it
creates the cache.

## Convert the validated result to PPFD

Choose one consistent factor in μmol/m²/s per lux for the result being
evaluated. The practical cache-native route is:

```text
Load Annual Result → annualRfinal.f32
       ├─ Hourly PAR (one hour, all sensors) → PPFD
       └─ PAR Each Sensor (one sensor, 8,760 hours) → PPFD
```

Both readers multiply each lux value by the supplied factor. For a factor
derived from a custom spectrum, **Load Spectral Data** samples 380–780 nm,
treats 400–700 nm as PAR, and returns the computed PPFD-per-lux factor from
its photopic and photon-weighted sums. This is a factor calculation; Radiance's
daylight matrix remains photometric. Document the spectral data, factor, and
why it represents the study's daylight or combined-light condition.

## Aggregate PPFD to DLI

Send an 8,760-value PPFD series to **Annual DLI** with `dt=3600` seconds. It
requires exactly 365 days and calculates each daily value as:

```text
DLI [mol/m²/day] = Σhourly(PPFD [μmol/m²/s] × 3,600 s) / 1,000,000
```

Alternatively, **DLI Each Sensor** reads the validated cache, applies its
factor, and returns daily values for one zero-based sensor. **DLI Hourly**
returns a selected 24-hour day's per-sensor total and hourly contributions.
Use **DLI Target** only after DLI exists; it compares values with a target and
does not simulate or convert light.

See [run and cache contract](annual-run-isolation.md).

For the underlying command-level method, weather convention, parameter presets,
and calculation boundaries, see [Radiance methods](../architecture/radiance-methods.md).
