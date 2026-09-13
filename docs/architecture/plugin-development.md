# Development and packaging

Use Rhino 8/Grasshopper for host testing, .NET 7 SDK for builds, and PowerShell. `yak` is required only to build a distributable package.

```powershell
dotnet restore FlahaGrow.sln
dotnet build FlahaGrow.sln --configuration Release
dotnet test tests/FlahaGrow.Core.Tests --configuration Release --no-restore -m:1
.\tools\Test-SetupComponents.ps1 -NoRestore
.\tools\Test-PluginAudit.ps1
```

The add-on is `src/FlahaGrow.Grasshopper/bin/<configuration>/net7.0-windows/FlahaGrow.gha`. Deploy it with the matching `FlahaGrow.Core.dll` and, unless supplied explicitly, `shared/Library`. Do not deploy RhinoCommon, Grasshopper, or GH_IO binaries.

`tools/New-YakPackage.ps1 -Version <version>` first runs Core tests, the component smoke suite and the plugin/documentation audit. Only after those gates pass does it stage the matching plugin/core pair and assets and invoke `yak build --platform win` into untracked `artifacts/`.

New components inherit `FlahaGrowComponent`, have a permanent GUID, and require a `ComponentRevisionCatalog` entry. GUID and parameter order are compatibility contracts. Publish a new GUID for incompatible I/O and document rebuilding affected definitions; do not retain hidden executable aliases. Typed wire parameters must be used by registered ports. The smoke test checks registration, revision coverage, and archive round trips for every component outside Rhino.

The plugin audit writes `artifacts/plugin-audit.json`. It rejects inventory/documentation drift, orphaned runtime artwork, hidden executable components, fixed machine paths and obsolete docs/archive content. The 27 historical src/Code files are explicitly preserved outside the compiled and packaged plugin. Documentation checks require a successful smoke stamp matching current source and runtime hashes; stale builds cannot certify current documentation. For local real ies2rad and rmtxop fixtures, supply -RadianceBin to Test-SetupComponents.ps1. See the [audit scope and evidence](../quality/plugin-audit.md).

Radiance consumers share Core discovery. Configure an explicit installation or use the system-derived standalone/Ladybug/PATH candidates; a bad explicit path never selects another installation. The annual and electric numerical-reference scripts require `-RadianceBin` explicitly. Known installation subdirectory names, component GUIDs, format contracts, scientific constants and documented numerical presets are intentional code values, not machine-specific configuration.
