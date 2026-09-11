# Annual daylight workflow

Updated 2026-09-11. This is the current operating guide for the compiled annual daylight workflow. **Release boundary:** the workflow may prepare, run, trace, and structurally validate a study, but it is not approved for design or scientific decisions until the numerical completion gate in [plugin-gap-audit-2026-09-11.md](plugin-gap-audit-2026-09-11.md) passes. A completed command sequence is not a validated annual result.

## Connect the workflow

```text
Simulation Paths → Working Directory → Radiance Status
                         |                   |
                         +-- Analysis --------+-- Radiance
                                  |                  |
Honeybee ModelToRad root + EPW ---+--> Annual Simulation → Folder
                                                       ├─→ Progress
                                                       └─→ Load Annual Result → F32
```

Annual Simulation **Project** is the Honeybee ModelToRad export root, not the Setup workspace. It must contain `model/scene/envelope.rad`, `envelope.mat`, `envelope.blk`, and a `.pts` grid in `model/grid` unless **Pts** is supplied. Connect typed **Analysis** and **Radiance** whenever the visible Setup components are used. The connected EPW must contain exactly 8,760 non-leap-year records; FlahaGrow snapshots it and writes a Ladybug-compatible `weather.wea` before invoking `gendaymtx`.

## Annual Simulation lifecycle

| Control | Meaning |
| --- | --- |
| `Run` | A false→true edge launches once. Leave it False after clicking. Holding True does not relaunch. |
| `Cancel` | A false→true edge cancels this run's recorded batch processes. The PID and UTC start time are verified, so the same build can cancel after Rhino reopens without touching a reused PID. Run=False is not cancellation. |
| `Existing` | Optional manifest-owned run Folder to reopen without preparing or launching work. Run is ignored while this is connected. |
| `Keep` | Default False. After all commands succeed, remove large reproducible coefficient, sky, octree, weather, and pipeline files. Keep it True only for a numerical investigation. Final/source-term `.ill` matrices, state, logs, batches, manifest, and snapshot inputs remain. |
| `Folder` | The retained run folder. Connect it to Progress and Load Annual Result, and keep/save it in the definition. |

The component prepares one isolated run for unchanged inputs and retains its Folder in the saved Grasshopper definition. Reopening the definition re-emits that Folder instead of preparing a duplicate run. To make a new run with the same inputs after completion, set Run False, then True. Changing a simulation input and launching also creates a new run. Before launch, the component checks free space on the actual run drive using a conservative run-size reserve; it refuses to start instead of filling the disk.

Runs created before process-identity recording have no safe cross-session cancellation record. For those legacy runs, use Progress to identify the job and stop only the matching process outside Grasshopper if necessary.

## Load a prior result

1. Use the saved Annual Simulation component; its Folder output restores the last run after reopening.
2. Or supply a known manifest-owned run folder to **Existing**.
3. Connect that Folder to **Annual Simulation Progress** and **Load Annual Result**.
4. Attach a Grasshopper Timer to Progress if live updates are needed. It reports the most recent stage (`1/8` through `8/8`) and log-update time for every declared part, plus stage coverage. Coverage is not an ETA: stages have very different durations.
5. Build the cache only after Progress reports every part Completed. The cache builder requires successful commands and finite, nonnegative final illuminance.

Load Annual Result writes `annualRfinal.f32` and `annualRfinal.meta.json`. It deliberately rejects flat legacy folders and does not create a merged `.ill`. Do not fabricate manifests or completion states for old studies.

If Progress reports `Running`, wait; the loader is correctly protecting an incomplete result. If it reports `Invalid`, preserve that run and inspect the reported matrix terms; do not clamp values or treat command success as a result. If it reports `Cancelled` or `Failed`, that run cannot be resumed or cached: set Run False, disconnect Existing, then set Run True to create a new isolated run. The retained `partN.execution-lock` folders are one-shot ownership markers and may remain after a run stops; they do not prove a process is still running.

## Read results

- **Illuminance Point in Time:** F32 + Mode=`hour` + zero-based hour index + Run=True returns every sensor for that hour.
- **Illuminance Sensor:** F32 + Mode=`sensor` + zero-based sensor index + Run=True returns the selected sensor through the year.
- **Hourly PAR** and **PAR Each Sensor** read an F32 path. **Hourly PPFD** and **PPFD Each Sensor** accept already-extracted numeric lux lists.
- **Annual Plot** and **Annual Plot PPFD for Sensor** infer their resolution from the supplied annual list: 8,760 hourly values (365 × 24), 365 daily values (one per day), or 12 monthly values (one per month). `DLI Each Sensor` emits the 365-daily form; `PAR Each Sensor` emits the 8,760-hourly form.

## Limits

Annual calculations are CPU and disk intensive. A valid result cache proves declared commands and matrices passed structural checks; it does not prove physical accuracy, convergence, spectral conversion, or an electric/daylight combined study. See [current status](current-status.md) and the gap audit before using results as scientific evidence.
