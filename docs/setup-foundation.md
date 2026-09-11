# Setup foundation: historical first-increment record

Original increment: 2026-09-07. This document preserves its implementation evidence; it is not the active Setup or annual-run specification. Use the [documentation index](README.md), [Setup guide](setup-components.md), and [current status](current-status.md) for current behavior.

These notes describe the first shared-core increment. Subsequent increments implemented [workspaces](setup-workspaces.md), [Radiance checks](setup-radiance.md), and [Grasshopper Setup components](setup-components.md). The annual-run contract is now documented in [annual run isolation](annual-run-isolation.md); its outstanding validation is tracked in [current status](current-status.md).

## Implemented

- `src/FlahaGrow.Core` targets .NET 7 with no Rhino, Grasshopper, UI, or third-party dependencies.
- Immutable project/context records and a camel-case schema-v1 project manifest codec preserve identity and reject missing required values or unsupported schema versions.
- `ProjectPathResolver` resolves explicit, saved, definition-relative, and stable system project locations, recording the source and observed existence of each location.
- Library resolution accepts the asset root or its parent and respects explicit/project/bundled precedence. Configured Radiance locations are resolved as locations only; executable readiness is not inferred.
- Relative paths require an explicit base. Invalid explicit overrides, missing saved project manifests, Windows reserved folder names, and ambiguous drive-relative paths fail with diagnostics.
- `ProjectLayout` supplies analysis/run locations without creating them. These are lexical checks; future filesystem writers must also enforce junction/reparse-point containment.
- `IProjectPathReader` exposes only existence checks and manifest reads. Resolution has no write, enumeration, process, or environment-mutation operation. The physical reader limits manifest size to 64 KiB.

The host must supply the OS Documents path, saved definition path, serialized project reference, stable draft ID, and bundled asset location. An omitted library or Radiance location remains unresolved (`null`); a successful project resolution is not a claim that simulation prerequisites are ready. Unchanged-solution caching belongs in the future host adapter, not in a global current-project singleton.

Schema v1 uses a fixed managed directory layout. At this first increment, analysis/run serialization and plugin integration were deferred. Analysis manifests and the plugin reference have since been added; the current package requires `FlahaGrow.Core.dll` beside `FlahaGrow.gha` as described in the Setup component guide.

## Validation

Run from the repository root:

```powershell
dotnet restore FlahaGrow.sln
dotnet test tests/FlahaGrow.Core.Tests/FlahaGrow.Core.Tests.csproj --configuration Release --no-restore
dotnet build FlahaGrow.sln --configuration Release --no-restore
```

The test suite targets .NET 7 and uses xUnit. It includes a committed manifest fixture and tests path precedence, invalid inputs, library normalization, portable relative references, missing/malformed/newer manifests, identity round trips, analysis containment, and physical read-only resolution. Windows path cases require Windows.

Validated on 2026-09-07: 44 tests passed under .NET 7. The full Release solution build passed with zero warnings/errors using `-m:1`. The initial default parallel build exited unsuccessfully without compiler diagnostics in this execution environment; its cause is not established. Dependency restore required network access for the missing test-host package; the restore command disabled NuGet vulnerability auditing for that operation, so these checks do not establish dependency vulnerability status.

No new components have been installed into Rhino. A saved legacy Grasshopper definition and host save/reopen checks remain required before the Setup UI migration; this increment does not claim the complete Phase 1 host fixture gate or the overall Setup definition of done.

## Existing Setup compatibility inventory

These interfaces were inspected before implementation. Their source files remain unchanged. The lists below retain parameter order; all listed parameters use item access.

| Component and permanent GUID | Inputs | Outputs |
| --- | --- | --- |
| Simulation Paths — `71c6a045-9308-4a0c-9f72-cab76ceefa5c` | Project folder (text), Library folder (optional text) | Project folder, Materials, Glazing, IES, Annual results (text) |
| Working Directory — `3bc3011e-2b2f-4c14-9344-dcb3554f3722` | Root folder (text); Point-in-time illuminance, Point-in-time render, Annual illuminance, Electric illuminance, Spectral point-in-time, Spectral annual illuminance (booleans, default False) | Root folder followed by the same six folder names (text) |
| Radiance Status — `f6f1d5d4-9a1a-4de7-a090-6299c94e0060` | Radiance bin folder (optional text) | Available (boolean), rcontrib path (text) |
| Radiance Version — `272aa83d-9898-460d-8cbd-7f49374153ba` | Run (boolean, default False), Radiance bin folder (optional text) | Version (text) |

The next increment described when this foundation was introduced is now implemented: see [workspace initialization and analysis management](setup-workspaces.md). Unified Radiance services precede the new typed Grasshopper interfaces. Distribution must be updated when the plugin references the core assembly.
