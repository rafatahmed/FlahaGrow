# Grasshopper plugin development

## Scope and target

The compiled add-on is in `src/FlahaGrow.Grasshopper`. It targets **Rhino 8 on Windows** and `net7.0-windows`, and builds a Grasshopper assembly named `FlahaGrow.gha`.

The existing `src/Code` files are Grasshopper Python/IronPython components. They are retained as the working reference implementation; they are not yet compiled-plugin components. Port each feature deliberately so its inputs, outputs, units, and existing definitions remain traceable.

## Prerequisites

- Rhino 8 with Grasshopper installed.
- .NET 7 SDK or later with the .NET 7 targeting pack.
- PowerShell for package staging.
- The `yak` command-line tool from Rhino when creating a distributable package.

The project references McNeel's `Grasshopper` NuGet package, which supplies Grasshopper, GH_IO, and matching RhinoCommon assemblies. Host assemblies are not copied beside the `.gha` output.

## Build

From the repository root:

```powershell
dotnet restore FlahaGrow.sln
dotnet build FlahaGrow.sln --configuration Debug
```

The resulting add-on is `src/FlahaGrow.Grasshopper/bin/Debug/net7.0-windows/FlahaGrow.gha`.

## Local install and debugging

1. Close Rhino before replacing a loaded `.gha` file.
2. Copy the generated `FlahaGrow.gha` into the Grasshopper Libraries folder, normally `%AppData%\Grasshopper\Libraries`.
3. Start Rhino, open Grasshopper, and find the **FlahaGrow** category. The scaffold exposes **Metrics → Lux to PPFD**.
4. For debugging, configure Visual Studio to start `Rhino.exe`, build the Debug configuration, then attach/run Rhino before opening Grasshopper.

Do not copy `Grasshopper.dll`, `GH_IO.dll`, or `RhinoCommon.dll` into the Libraries folder; Rhino supplies those host assemblies.

## Adding components

Create one public class per component under `Components/`, inherit from `GH_Component`, assign a permanent `ComponentGuid`, and define explicit units in parameter descriptions. Never change a published component GUID. For breaking input/output changes, preserve the old component as hidden/legacy and publish a new GUID.

## Packaging

`tools/New-YakPackage.ps1` builds the Release `.gha`, stages it with the material library under `shared/Library`, and invokes `yak build`.

```powershell
.\tools\New-YakPackage.ps1 -Version 0.1.0
```

The generated `.yak` file remains under `artifacts/`, which is intentionally untracked. Review the staged `manifest.yml`, plugin assembly, and bundled library before publishing through Rhino's Package Manager or `yak push`.

## Verification checklist

1. Build without warnings or errors.
2. Load `FlahaGrow.gha` in Grasshopper with no assembly-load messages.
3. Confirm the component appears under **FlahaGrow → Metrics**.
4. Test the reference component with 1,000 lux and a 0.0185 factor; expected PPFD is 18.5 μmol/m²/s.
5. For each ported simulation feature, execute a small point-in-time Radiance case and an annual test containing 8,760 hourly values and 365 DLI results.
