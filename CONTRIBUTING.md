# Contributing to FlahaGrow

Implement Grasshopper components in `src/FlahaGrow.Grasshopper` and reusable calculations in `src/FlahaGrow.Core`. Store Radiance assets in `src/Library`. Avoid adding separate component classes for identical behavior; use instance inputs or shared purpose where appropriate.

Every executable component must be visible, have a unique identity and purpose, and appear exactly once in the revision ledger, README catalog and category reference. Typed wire parameters must be used by registered ports. Every runtime icon must match a current component or the plugin logo.

Before merging, run:

```powershell
dotnet test tests/FlahaGrow.Core.Tests/FlahaGrow.Core.Tests.csproj --configuration Release
.\tools\Test-SetupComponents.ps1 -NoRestore
.\tools\Test-ComponentDocumentation.ps1
.\tools\Test-PluginAudit.ps1
```

Validate changed Radiance commands with the relevant reference fixture. Live dialogs and canvas wiring require Rhino acceptance; CLR smoke tests do not replace it. Record source spectra for photon calculations and verify 8,760 hourly / 365 daily shapes where required.

Update contracts and documentation with implementation changes. Incompatible port changes need a new GUID and explicit update guidance. Do not commit generated simulation outputs, local caches or machine-specific credentials. PRs should describe behavior, validation and any saved-definition impact.
