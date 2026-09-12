# Development and packaging

Use Rhino 8/Grasshopper for host testing, .NET 7 SDK for builds, and PowerShell. `yak` is required only to build a distributable package.

```powershell
dotnet restore FlahaGrow.sln
dotnet build FlahaGrow.sln --configuration Release
dotnet test tests/FlahaGrow.Core.Tests --configuration Release --no-restore -m:1
.\tools\Test-SetupComponents.ps1 -NoRestore
```

The add-on is `src/FlahaGrow.Grasshopper/bin/<configuration>/net7.0-windows/FlahaGrow.gha`. Deploy it with the matching `FlahaGrow.Core.dll` and, unless supplied explicitly, `shared/Library`. Do not deploy RhinoCommon, Grasshopper, or GH_IO binaries.

`tools/New-YakPackage.ps1 -Version <version>` stages the matching plugin/core pair and assets, then invokes `yak build --platform win` into untracked `artifacts/`.

New components inherit `FlahaGrowComponent`, have a permanent GUID, and require a `ComponentRevisionCatalog` entry. GUID and parameter order are compatibility contracts. Publish a new GUID for incompatible I/O; retain a hidden legacy component where practical. The smoke test checks registration, revision coverage, and archive round trips outside Rhino.
