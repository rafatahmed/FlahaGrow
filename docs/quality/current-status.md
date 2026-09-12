# Validation and release evidence

The automated suites establish the contracts they execute; they do not replace a Rhino-host or numerical-reference validation.

Plant-light implementation (2026-09-12): 183 Core tests pass; the dedicated
component runner passes registration for 50 components and 18 new/compatibility
plant-light interface/archive cases. Builds have zero warnings/errors. These
checks also include the 13-sensor Load Result/profile/context/four-reader
pipeline with hourly-integral tree ordering and shared spectral calculations.
These checks do not establish physical accuracy, actual example wire migration or
large-grid performance. See the [current workflow and limits](../workflows/plant-light-workflow.md).

```powershell
dotnet build FlahaGrow.sln --configuration Release --no-restore -m:1
dotnet test tests/FlahaGrow.Core.Tests --configuration Release --no-restore -m:1
.\tools\Test-SetupComponents.ps1 -NoRestore
```

Core tests cover workspace/path behavior, Radiance discovery/process behavior, annual manifests/matrices/weather conversion, and composition. The smoke program checks component registration, revisions, parameter contracts, and archive round trips without Rhino's canvas scheduler.

Before relying on a study, retain evidence of a Rhino 8 host check, numerical reference for the intended Radiance/weather convention/model, spectral-factor verification, electric/combined-run validation when used, and representative cache performance on target storage. Historical findings and release notes in `docs/archive/` and `docs/releases/` are not proof that a current binary or new study is valid.
