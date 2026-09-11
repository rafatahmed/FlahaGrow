# Current implementation and release status

Updated 2026-09-11. This is the source-backed status index. The code and automated validation are authoritative; dated audits preserve evidence and do not overrule this page.

## What is implemented and checked

- Annual daylight creates isolated manifest-owned runs, persists the last run, supports `Existing`, records stages, rejects invalid/non-finite illuminance, validates cache provenance, and records process identity for cross-session cancellation.
- Annual daylight converts strict 8,760-record non-leap EPW input to an inspectable WEA and uses a 146-column ground-plus-sky receiver basis. A one-sensor 8,760-hour installed-Radiance fixture completed with finite, nonnegative values.
- Electric Annual Simulation performs one full-output `oconv`/`rtrace` calculation in the background, snapshots one validated 8,760-hour dimming schedule, and writes a manifest-owned annual illuminance matrix. A real Radiance 5.4 one-sensor reference produced 624.5204 lux. Combine Annual Lighting validates matching completed runs and creates a separately provenance-owned daylight-plus-electric result for the normal cache/readers.
- Component revisions are persisted per node. The canvas shows version and update time; a saved node reports a review marker when the installed component version changes.
- Selector state, deterministic IES conversion, DLI schedule validation, and provenance-bound direct readers are implemented and covered by automated checks.

`dotnet build FlahaGrow.sln --configuration Release --no-restore -m:1`, 165 Core tests, and `tools/Test-SetupComponents.ps1 -NoRestore` passed on 2026-09-11. The smoke check verifies all 40 concrete components have revision entries and archive persistence. It is not Rhino host acceptance.

## Release gates still open

| Priority | Gate | What is required to close it |
| --- | --- | --- |
| Closed (observed) | Fresh Rhino annual daylight acceptance | The supplied canvas completed a 980-sensor, 8,760-hour, four-part manifest-owned run at `FlahGrow02`; all parts, cache metadata, and reader wiring were observed. See [Rhino acceptance](rhino-acceptance-2026-09-11.md). The old FlahGrow01 run remains invalid and cannot be reused. |
| High | Rhino lifecycle acceptance | Verify canvas revision labels, save/reopen, `Existing`, selector persistence, real Timer refresh, and cancel after restarting Rhino while a job is running. |
| High | Annual electric integration | Exercise Electric Annual and Combine Annual Lighting with a real exported luminaire and a real Rhino definition. The source-level combination contract is implemented and tested. |
| High | Spectral parity | Compare custom CSV conversion against a locked legacy/reference case, then correct any wavelength, weighting, and export differences. |
| Medium | Calendar convention reference | Compare EPW/WEA hour semantics and a known annual reference result; leap years are intentionally rejected today. |
| Medium | Large-cache stress | Run a multi-gigabyte cache performance/seek test on a drive with sufficient free space. |

## Installed plugin state

The installed plugin is the matching tested Release build, including Electric Annual Simulation and Combine Annual Lighting. Rhino was confirmed closed before replacement; the preceding matched pair is retained in `FlahaGrow.backup-20260911-2120`. Its hashes are:

| File | SHA-256 |
| --- | --- |
| `FlahaGrow.gha` | `9E6E6456E484BB6651319D49A778E252073EE82D24F07EF5D0F555F4ECE62BF0` |
| `FlahaGrow.Core.dll` | `BED447B708ECA69A07C49FBAFF737C153DA8C86CCBDEA9D6549915FBA3144D43` |

The remaining release gates are host/numerical acceptance gates, not a source-versus-installed-plugin mismatch. See [component revisions](component-revisions.md), [annual workflow](annual-workflow.md), [Ladybug/Honeybee boundary](ladybug-honeybee-integration.md), and the [delivery gate](plugin-gap-audit-2026-09-11.md).
