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

## Read illuminance, PPFD and DLI

Connect the validated F32 cache to Read Illuminance and choose hour or sensor mode. For plant-light quantities, connect Load Annual Result.Result and a source-specific Profile to Plant Light Context. Use PPFD at Hour or Annual PPFD at Sensor to read PPFD; DLI for Day and Annual DLI at Sensor integrate the same context independently.

Annual Plot displays an annual sensor series together with that reader's matching Plot attributes. A grid of sensors at one hour is not an annual temporal series.

For externally supplied numeric data, Lux to PPFD applies `PPFD = lux × factor`. Annual DLI integrates a complete PPFD series; for hourly data each day's total is `Σ(PPFD × 3600) / 1,000,000` mol/m²/day.

See the [plant-light workflow](plant-light-workflow.md) for source composition and the [component reference](../components/README.md) for exact ports. Source spectra and receiving-light assumptions determine conversion validity; a successful Radiance run alone does not validate estimated PPFD.
