# Annual run isolation

2026-09-10 follow-up: [live-result audit](annual-result-audit-2026-09-10.md). Final illuminance must now be nonnegative as well as structurally valid; invalid results cannot count as completed or publish a cache. Header parsing accepts copied Radiance provenance with internal blank lines before FORMAT. Generic matrix parsing still supports signed intermediate data.

Implemented 2026-09-08 for audit F02–F04 and the run-ownership/result-validation portions of C02.

## Connections

Annual Simulation keeps its original inputs and appends optional `Radiance Environment` and `Analysis` inputs. Connect Radiance Status's typed `Radiance` output and Working Directory's typed `Analysis` output. Both must describe annual daylight. The text `Project` input remains the Honeybee ModelToRad export root with `model/scene` and a sensor grid (or supplied sensor points).

Each preparation creates a new GUID-named folder. With Analysis connected it lives under `<project>/analyses/<analysis>/runs/<run-id>`; otherwise it lives under `<Honeybee-root>/runs/<run-id>`. Connect Annual Simulation's `Folder` output to Load Annual Result and Annual Simulation Progress. Do not connect the Runs container itself.

The runner snapshots scene and weather files into the new folder and writes the sensor grid there. It does not rename or overwrite the source grid. A weather file already inside the Honeybee root is supported. Failed environment, workflow, Bin, and sky checks occur before run preparation writes. Failed copying can leave an incomplete isolated folder for diagnosis; it cannot be mistaken for a complete result set.

## Identity and result membership

`flahagrow.run.json` schema 2 records the run ID, optional project/analysis IDs, source root, sky subdivision, input file hashes, ordered sensor-grid hash, total sensor count, expected time steps (`Hours`, counted from the EPW weather records), and contiguous sensor ranges for each part. Ten or fewer sensors produce one part; larger grids produce four. Generated result and log names are derived from validated part indices, not arbitrary manifest paths.

Load Annual Result requires all declared parts to have successful command states and validated matrices. It requires `#?RADIANCE`, one each of `NROWS`, `NCOLS`, `NCOMP`, and `FORMAT`, a blank header terminator, `FORMAT=ascii`, and `NCOMP=1`. Dimensions must match the run's EPW step count and part sensor count. Every data token must be finite and fit float32. Missing, extra, malformed, and nonfinite data is rejected; numeric rows are never silently skipped. Whitespace wrapping is allowed because dimensions define logical rows. These requirements follow the [official rmtxop matrix contract](https://floyd.lbl.gov/radiance/man_html/rmtxop.1.html).

Cache writing streams logical rows from each part in sensor order. Metadata retains `sensors`, `hours`, `ncomp`, and `order` and adds `runId`, `sourceSignature`, `validationVersion`, and `cacheHash`. The source signature covers the manifest and declared result files; the cache hash checks the binary itself. Reopening revalidates completion/matrices and rejects changed sources, an invalid validation version, or damaged cache bytes. Binary and metadata writes are staged; old metadata is invalidated before publishing a replacement. Invalid input leaves a previously published binary untouched and cannot produce a valid new cache.

Progress reports `Prepared`, `Running`, `Failed`, `Invalid`, or `Completed` for declared parts. Command-state files include the run ID. `Completed` requires `CommandsSucceeded` plus a successful full matrix validation; nonempty files and old completion log text are insufficient. Both progress and cache use the same validator. Large result validation can take time; host scheduling/performance remains a separate lifecycle concern.

## Command failure handling

Jobs launch in the background, after every batch has been prepared. Launch errors identify the failed part. Each batch takes a one-shot execution lock, sets `Running`, and checks every command exit code. Pipelines are split into sequential commands with intermediate files so a failed producer cannot be hidden by a successful consumer. The first nonzero exit (including negative codes) records the step and exit code, retains stderr in `annual_errors_partN.log`, and stops later commands. Intermediate files are retained for diagnosis and require additional disk space.

The batch publishes `CommandsSucceeded` only after all commands succeed; final matrix validation happens when progress/cache reads the result. A zero-exit command that writes malformed output is therefore `Invalid`, never `Completed`. Interrupted jobs may remain `Running`; they are not accepted by cache. Do not rerun the same batch folder: prepare a new run. The execution lock prevents stale state/output reuse during retries.

## Compatibility and remaining limits

Legacy flat result folders and schema-1 runs require regeneration because they lack the expected-step/checked-command contract. Existing files are not adopted, moved, or deleted. Re-run the Honeybee source through Annual Simulation, then connect its new Folder output. Existing `.f32` readers retain their interfaces and can still read an explicitly supplied old cache; they do not certify its identity.

Run=False continues to prepare a new run on each solve, and Run=True prepares and launches a new run on each solve. Trigger latching and asynchronous lifecycle work remain C05. Detail shell metacharacters are now rejected to protect checked command sequencing; a complete Radiance option/value allowlist remains F08. Validation establishes file integrity and command success, not scientific accuracy.

When a checked Radiance environment is connected, generated annual commands use its quoted absolute executable paths, including both former pipeline sides. The selected calculation library is used without ambient RAYPATH fallback. Paths containing batch-expansion characters are rejected. Legacy definitions without the typed environment continue using legacy discovery.

## Validation

- Core tests: unique run IDs, project/analysis identity, contiguous partitions, declared-only results, missing-part rejection, malformed ordering, and legacy-folder rejection.
- Standalone component integration: actual annual preparation for Sky 1/4, ready/workflow/Bin rejection before writes, source preservation, custom library/absolute commands, Setup ownership, one/four-part caching, stale result/cache detection, and progress with extra unrelated files.
- Review regression checks: missing selector libraries report errors, luminaire preparation/placement/compilation shares the project folder without touching a sibling, and RGB/xform output under `de-DE` uses decimal dots.

The Core suite now includes malformed-header/data cases and actual Windows batch failure injection. Component checks cover rejection of malformed results, preservation of a prior cache on failure, damaged cache bytes, and failed-part progress.

Optional real-engine validation:

```powershell
.\tools\Test-SetupComponents.ps1 -NoRestore -RadianceBin 'C:\Program Files\ladybug_tools\radiance\bin'
```

On 2026-09-08 this passed against installed Ladybug Radiance: a two-row `rmtxop` calculation produced the expected values and a missing-input failure stopped subsequent batch commands. User-shared live Rhino Setup outputs also confirmed Ladybug Radiance 5.4 readiness and the sun-direction check. Neither establishes a full annual study result. The smoke adapter does not exercise Rhino's canvas scheduler or selector dialogs; the luminaire chain uses a stand-in `.rad` asset after Run=False IES preparation.
