# Annual run and cache contract

Updated 2026-09-11. This is the technical contract behind the [annual workflow](annual-workflow.md).

## Run identity

Every new annual simulation creates a GUID-named schema-2 folder under the connected Analysis `runs` folder, or under `<Honeybee-root>/runs` when Analysis is omitted. `flahagrow.run.json` records source identity, input hashes, sensor ordering, expected weather steps, and the one or four declared result parts.

The runner snapshots scene/weather inputs and sensor points into that folder. It never overwrites the Honeybee source root. A saved Annual Simulation component retains the run Folder and re-emits it after reopening; **Existing** can select another known run explicitly.

## Disk lifecycle

Before a launch, Annual Simulation requires a conservative amount of free space on the drive containing the run folder. A full drive is a launch failure, not a partially written “result.” The estimate is intentionally larger than the final cache because Radiance creates coefficient and sky matrices while it runs.

With `Keep intermediates=False` (the default), each successful part removes its reproducible pipeline staging, coefficient, sky, octree, and generated weather files after all commands succeed. It retains the manifest, input snapshots, point files, batches, logs/state files, `annualR_partN.ill`, `annualRd_partN.ill`, `annualRs_partN.ill`, and `annualRfinal_partN.ill` so validation and negative-result diagnosis remain possible. `Keep=True` disables that post-success cleanup for an intentional investigation. Failed and cancelled parts retain their intermediates for diagnosis; remove their terminal run folder only after preserving any evidence required for a bug report.

## Process states

Each declared part is one of:

- `Prepared`: batch file exists but has not launched.
- `Running`: the batch owns its execution lock.
- `CommandsSucceeded`: every checked command exited successfully.
- `Failed`: a checked command or launch failed.
- `Cancelled`: the component requested cancellation of a recorded batch process whose PID and UTC start time matched the run record.
- `Completed`: Progress validated CommandsSucceeded plus the final matrix.

Only Completed is cacheable. `Run` is a one-shot launch trigger. `Cancel` is explicit and can control a process after Rhino reopens when the run contains a matching recorded PID/start-time identity; legacy runs without that record cannot be safely attached.

## Cache acceptance

Load Annual Result accepts only declared files for its selected run. Each final matrix must have a valid Radiance ASCII header, expected rows/columns/component count, finite float32 values, and nonnegative illuminance. The cache binds the manifest and result hashes, then writes `annualRfinal.f32` and metadata atomically.

Flat legacy folders, undeclared files, malformed matrices, failed/cancelled jobs, altered inputs, and changed cache bytes are rejected. All direct illuminance and cache-native PPFD readers require the same manifest/signature/cache-hash provenance; legacy F32 files must be regenerated rather than treated as validated studies.

## Diagnostics

Progress reads only the selected Folder and reports each declared part's lifecycle state, latest `1/8`–`8/8` batch stage, and log-update time. Its `Stage coverage` output counts finished commands across declared parts; a run can therefore show 100% stage coverage and 0 validated parts when matrix validation fails. It is deliberately not an elapsed-time or ETA percentage because the stages have unequal costs. Attach a Grasshopper Timer to `Refresh` for live rereads. `annual_progress_partN.log`, `annual_errors_partN.log`, and `annual_state_partN.txt` remain in the run folder for investigation. Do not infer completion from CPU use, a nonempty result file, or an old log line.

The batch uses fail-fast command checks and preserves intermediate files for diagnosis. This protects file integrity; it does not certify numerical convergence or scientific validity.
