# Setup user guide

**FlahaGrow → Setup** exposes three components. Standalone Radiance is preferred; an existing Ladybug Tools installation is supported. You do not need both, and normally do not need to browse for a bin folder.

## Connect the workflow

1. Place **Simulation Paths**. Choose a project location or leave Auto enabled. Connect `Paths` to **Working Directory**.
2. Enter project and analysis names and select `Workflow`: `0 Annual daylight` or `1 Electric-light preparation`. Press a Button connected to `Initialize` to create the workspace. Existing workspaces open automatically.
3. Connect Working Directory's `Analysis` output to **Radiance Status**. It automatically detects Radiance and inherits the analysis workflow.
4. Connect Radiance Status's `Radiance` output to the optional **Radiance Environment** input on Annual Simulation or IES to Radiance. The runner then uses that exact checked installation. Existing definitions may continue using the optional Bin input.

Radiance Status also works independently with no Analysis connection. It defaults to annual daylight; its right-click menu offers an electric-light check. A connected analysis determines the workflow.

| Component | Inputs in order | Outputs in order |
| --- | --- | --- |
| Simulation Paths | Location, Mode, Library, Refresh | Paths, Folder, Library, Status |
| Working Directory | Paths, Project name, Analysis name, Workflow, Initialize, Adopt existing, Refresh | Project, Analysis, Folder, Inputs, Runs, Library, Status |
| Radiance Status | Analysis (optional), Refresh | Radiance, Ready, Version, Bin, Lib, Found, Status |

Location modes are `0 Auto`, `1 Project-relative`, `2 System`, and `3 Custom`. Library is the FlahaGrow material/photometry library, separate from Radiance's calculation library. Packaged assets are discovered automatically; development builds can use `src/Library`. Connect its Library output directly to a material, glazing, or IES selector; each selector resolves its required subfolder. Existing definitions that supply `RadMaterials`, `RadGlazing`, or `RadIES` directly remain supported.

## Automatic Radiance detection

Detection checks standalone locations (`C:\Radiance` and Radiance under Program Files), then common Ladybug Tools locations, then PATH. It uses bounded direct checks without scanning the drive. Other locations are supported through **Choose custom Radiance folder…** in the right-click menu.

The first complete installation whose version probe succeeds is selected. Status explains skipped incomplete installations and failed probes. Found lists recognized installations and missing files. Right-click to select a discovered installation or custom calculation library. Explicit selections never silently fall back. Choose **Automatic detection (prefer standalone)** to reset overrides and discover alternatives.

Checks run on placement, configuration change, and reopening. Unchanged solutions reuse the result. Use Refresh after updating/installing Radiance or changing PATH. Each check allows at most four automatic version attempts, each with a five-second process deadline. See [the service contract](setup-radiance.md) for filesystem and process-cleanup limits.

Ready requires workflow files, a successful version probe, and a small execution check. Annual daylight evaluates a sun direction with `rcalc` and the selected `reinsrc.cal`, validating a finite unit vector. Electric-light preparation converts a tiny IES fixture, transforms it, and compiles the transformed scene with `oconv`. These checks do not certify a full simulation. Checks use a unique temporary folder, cleaned afterward, and never modify installations, project folders, or system/Rhino environment variables.

Discovery checks at most 128 unique normalized locations; duplicate PATH entries do not consume the budget. If the limit is reached, Status explains that it limits further discovery rather than invalidating the selected installation. Each candidate's execution check has a ten-second cancellation deadline in addition to the five-second version deadline. Successful results are cached with the installation fingerprint; Refresh repeats them.

## Persistence and compatibility

Paths resolves without creating folders. Auto remembers its selected root even when an unsaved definition is later saved; use **Reset automatic project location** to derive another location. Radiance configuration belongs to Radiance Status; simplified Paths ignores legacy project Radiance settings.

Workspace creation requires an explicit Initialize action. Held True does not repeat creation; restored actions must observe False before accepting True. `Adopt existing=True` must accompany Initialize to adopt a nonempty unrecognized folder. Existing files are preserved. Radiance deliberately rechecks automatically after reopening.

Six previous Setup components, including the earlier `(Project)` Paths and Radiance components, are hidden from placement. Their GUIDs and parameter interfaces remain loadable. Existing definitions retain their behavior. Working Directory retains its project-component GUID and ports with a simplified display name. Place the visible trio to use the new workflow; legacy definitions are not automatically rewired.

Typed outputs are transient connections; keep them connected to Setup components so they can be rebuilt on reopening. Filesystem/process work runs asynchronously, obsolete results are discarded, and warm solutions reuse results.

## Installation and validation

On the development machine, Annual daylight selects `C:\Program Files\ladybug_tools\radiance\bin` (Radiance 5.4), because standalone lacks `C:\Radiance\lib\reinsrc.cal`. Electric-light preparation selects standalone 6.1a. Both pass their execution fixtures. This is the observed automatic result, not a machine-independent promise or a global Ladybug override. See [the verification record](setup-radiance.md#validation).

Install the locally generated Windows Yak package, or copy **both** `FlahaGrow.gha` and `FlahaGrow.Core.dll` together. Manual installation also needs `shared/Library` beside the assemblies unless Library is supplied explicitly. Restart Rhino after replacing assemblies. Do not distribute RhinoCommon, Grasshopper, or GH_IO assemblies.

```powershell
dotnet test tests/FlahaGrow.Core.Tests --configuration Release --no-restore -m:1
.\tools\Test-SetupComponents.ps1 -NoRestore
.\tools\New-YakPackage.ps1 -NoRestore
```

Core tests cover discovery priority, fallback, override isolation, probe limits, caching, and workspace behavior. Standalone component checks cover nine component identities/interfaces and archive round trips, three visible names, automatic checking, and direct solves. They do not exercise Rhino's canvas scheduler. Live checks still include opening a saved legacy definition, saving/reopening the new trio, using menus, changing inputs during a check, and closing a document during work.

The current annual runner still expects a Honeybee ModelToRad root containing `model/scene` and `model/grid`. Setup's Inputs and Runs are not drop-in replacements. Downstream simulation integration remains a separate stage.

See the [post-Setup component consistency review](setup-consistency-review-2026-09-07.md) for the connection matrix, remaining library/luminaire/Radiance integration gaps, and validation results at revision `dd57f58`.
