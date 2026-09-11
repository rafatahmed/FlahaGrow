# Plugin delivery gate

Updated 2026-09-11 against the current compiled source and automated checks. This is the single active completion gate. Dated repository and component-I/O audits are evidence only; they cannot close an item here.

## Release decision

The recorded four-part FlahGrow01 run was invalid and remains retired. The later `FlahGrow02` 980-sensor/8,760-hour Grasshopper run completed all four declared parts, produced a provenance-bound cache, and exercised result-reader wiring. See [the Rhino acceptance record](rhino-acceptance-2026-09-11.md). This closes annual daylight workflow acceptance for that observed case; it does not certify a different scene, numerical convergence, electric workflow, or spectral conversion.

The failure is reproduced from the run's own matrices, without modifying them:

| Part | Hour | Local sensor | Total lux | Direct lux | Sun lux | Final lux |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| 0 | 37 | 34 | 23,862.58 | 25,471.78 | 0 | -1,609.20 |
| 1 | 35 | 105 | 27,326.57 | 27,403.19 | 0 | -76.61 |
| 2 | 35 | 138 | 22,782.89 | 22,857.70 | 0 | -74.81 |
| 3 | 38 | 29 | 12,592.74 | 13,220.71 | 0 | -627.97 |

This is a matrix-method/numerical-validation failure, not a Progress or cache-loading failure. Clamping would hide it and is prohibited.

## Fixed in this update

| Finding | Resolution | Evidence |
| --- | --- | --- |
| Annual Run launched again on every Grasshopper solve and Run=False could not affect detached jobs. | Annual Simulation `1.5.0` uses a rising-edge Run trigger, retains a run Folder, and adds an explicit Cancel action. | Component smoke integration covers retained folders and archive reopen. |
| Reopening a definition lost the run Folder, so cache/progress could not be reconnected to the previous study. | Last run folder/key are serialized; Existing accepts an explicit manifest-owned run. | Component smoke archive round trip. |
| Missing Working Directory manifest exposed a raw filesystem path error. | Working Directory `1.0.2` gives initialization/adoption guidance and permits explicit legacy-folder adoption. | Core workspace test. |
| Progress reduced a multi-part run to `Running`. | Annual Simulation Progress `1.1.0` reads each part's recorded `1/8`–`8/8` stage and log-update time, and exposes stage coverage. | Annual smoke test asserts a live `7/8` report and 75% coverage. |
| Terminal annual runs filled the run drive with pipeline and Radiance intermediates. | Annual Simulation checks run-drive capacity before launch and removes successful reproducible intermediates by default; `Keep` preserves them only for investigation. | Core tests cover the disk estimate and successful pipeline cleanup. |
| Direct `.f32` readers could bypass the result-loader provenance check. | Illuminance and cache-native PPFD readers now require the manifest, validated declared parts, source signature, and cache hash before emitting values. | Setup smoke test modifies a valid cache and asserts all reader paths reject it. |
| A reopened Rhino session could not cancel a still-running annual batch. | Each launched batch PID and UTC start time are persisted in its run folder; Cancel verifies both before killing the process tree. | Core test covers run-bound identity parsing. Real Rhino restart/cancel remains host acceptance work. |
| Selector choices were lost after saving a definition. | Opaque, glazing, IES, and spectral selections serialize their values; custom spectral CSV selections also serialize absolute path and content hash. | Component archive smoke coverage; manual dialog selection remains host acceptance work. |
| IES rerun could select a newer unrelated output file. | `ies2rad` accepts only the requested output stem and rejects ambiguous/missing outputs. | Source and annual smoke coverage. |
| DLI treated every series as hourly. | DLI requires a complete 365-day series whose timestep divides a day exactly. | Component source validation. |

## Open findings

| Priority | Gap | Current boundary |
| --- | --- | --- |
| High | Annual numerical validation scope | The former run was invalid. The source now uses a verified 146-column ground-plus-sky receiver basis; the executable reference and the successful 980-sensor Rhino run contain no rejected final values. Scene-specific convergence/reference tolerances remain required before design/scientific use. |
| High | Cancellation across sessions | New runs record and verify `cmd.exe` PID + start time. A real child-process test proves a mismatched identity is not killed and the matching identity is terminated; older runs have no identity record. Real Rhino restart/cancel acceptance is still required. |
| High | Spectral CSV parity | Custom spectral factor calculation, wavelength mask/weighting, CSV handling, and export differ from the legacy Python workflow. |
| High | Electric annual workflow | Electric Annual Simulation performs a background full-output `oconv`/`rtrace` calculation expanded by a validated, snapshotted 8,760-hour dimming schedule. Combine Annual Lighting requires matching completed manifest-owned runs and produces a separately provenance-owned result. A real Radiance 5.4 one-sensor fixture passed at 624.5204 lux; a real exported-luminaire/Rhino acceptance run remains required. |
| Medium | Calendar/timestep semantics | DLI validates 365-day timestep count, but leap-year and annual hour-ending convention need a reference fixture. |
| Medium | Host validation | Selector dialog/reopen and persisted cancellation need a real Rhino acceptance test. |
| Medium | Host validation | Automated tests do not exercise a real Rhino canvas, UI scheduler, saved legacy definition wires, or live cancellation. |
| Low | Large-cache performance | Offset arithmetic is checked, but multi-gigabyte cache stress testing remains outstanding. |

## Test boundary

The current automated evidence is the Core test suite and `tools/Test-SetupComponents.ps1 -NoRestore`. They cover source-level contracts, archives, preparation, cache/progress validation, and controlled batch failures. They do not replace a full annual Radiance study in Rhino.

## Completion sequence

1. Define one small, versioned Radiance reference scene/EPW/sensor set with expected annual values and convergence tolerance. This must be an executable test fixture, not another narrative audit.
2. Compare the total, direct, and sun coefficient paths against that fixture at the reported failing sensor/hours. Change settings or scene construction only when the reference demonstrates the correction.
3. Run the complete manifest-owned workflow through Rhino: launch, live stage reporting, cross-session cancel, save/reopen, Existing, cache build, readers, selector persistence, and a deliberately invalid case. Record the installed binary hashes.
4. Install the matched `.gha` and Core DLL only after steps 1–3 pass; archive the previous pair for rollback.
5. Close the blocker only with the test fixture, a successful fresh run, and a concise release record. Then address spectral parity and electric annual workflow as separate delivery items.
