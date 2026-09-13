# Validation and release evidence

The automated suites establish the contracts they execute; they do not replace a Rhino-host or numerical-reference validation. See the [systematic plugin audit](plugin-audit.md) for scope, results and the remaining host and study validation limits.

Component cleanup (2026-09-13): the compiled plugin contains 30 visible executable
components and 206 registered ports. Category documents, README inventory, revision
ledger and 23 component icons plus the logo match the built registrations. Eight
typed parameter classes support registered wires; they are not executable components.
The unused UTC port has been removed with a new selector GUID. This build was installed locally with Rhino closed; see the [deployment receipt](../releases/plugin-audit-deployment-2026-09-13.md). The 27 historical src/Code files are preserved at the user’s request, outside the compiled plugin. The 10 approved docs/archive files have been removed.

Current validation (2026-09-13): 198 Core tests pass; the dedicated
component runner checks registration for 30 components and the expanded
plant-light, timing and plot interface/archive cases. Builds have zero warnings/errors. These
checks also include the 13-sensor Load Result/profile/context/four-reader
pipeline with hourly-integral tree ordering and shared spectral calculations.
These checks do not establish physical accuracy, actual example wire migration or
large-grid performance. See the [current workflow and limits](../workflows/plant-light-workflow.md).

Deep-audit fixes cover process input/output deadlocks, fresh staged IES conversion, strict shared illuminance reads, finite numeric/geometry inputs and invalid plot attributes. Packaging now gates staging on Core tests, smoke checks and a fresh documentation/inventory audit. A local Yak package was built successfully; the matching assemblies were subsequently installed locally, with verified backups and checksums. The package was not published.

Current source adds background cache loading/reuse, verified run-EPW timing,
and data-bound plot attributes. See the [contract and migration guide](../workflows/result-weather-plot-contract.md).
Local deployment is recorded in the receipt above; live Rhino acceptance remains outstanding. A synthetic 13-sensor held-True test ran 100 warm loader
solves in about 1 ms without rewriting the cache; this is not a full-model or
Rhino UI benchmark. Downstream readers retain full content validation.

```powershell
dotnet build FlahaGrow.sln --configuration Release --no-restore -m:1
dotnet test tests/FlahaGrow.Core.Tests --configuration Release --no-restore -m:1
.\tools\Test-SetupComponents.ps1 -NoRestore
```

Core tests cover workspace/path behavior, Radiance discovery/process behavior, annual manifests/matrices/weather conversion, and composition. The smoke program checks component registration, revisions, parameter contracts, and archive round trips without Rhino's canvas scheduler.

Before relying on a study, retain evidence of a Rhino 8 host check, numerical reference for the intended Radiance/weather convention/model, spectral-factor verification, electric/combined-run validation when used, and representative cache performance on target storage. Historical release notes in `docs/releases/` are not proof that a current binary or new study is valid.
