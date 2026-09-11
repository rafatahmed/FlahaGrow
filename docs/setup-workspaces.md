# Workspace creation and analysis management — historical increment record

Original increment: 2026-09-07. The API notes and test evidence below are retained for reference. For current operator behavior, use the [Setup guide](setup-components.md); for current run ownership, use [annual run isolation](annual-run-isolation.md).

Implemented as the second Setup core increment. Subsequent increments added [Radiance services](setup-radiance.md) and [Grasshopper Setup components](setup-components.md).

## API and behavior

`WorkspaceService.Open(ResolvedPaths, analysisName, cancellationToken)` reads and validates project/analysis manifests and returns their contexts. It never creates directories, takes a write lock, repairs layout, or runs a process. Opening confirms identity and metadata, not simulation readiness or completeness of the input folders.

`WorkspaceService.Initialize(WorkspaceRequest, cancellationToken)` explicitly creates or opens a workspace. Its request contains resolved paths, a project display name, analysis folder name (default `baseline`), workflow, and an `AdoptExisting` flag (default False).

Initialization:

1. Validates names, workflow, and existing path ancestors.
2. Creates the root if necessary and immediately acquires an exclusive `.flahagrow.workspace.lock` handle. Contention returns an I/O error rather than waiting indefinitely.
3. Reads any existing manifests and checks project identity, analysis ownership, folder name, and workflow. It rejects unsupported schema versions and conflicting ownership without rewriting manifests.
4. Requires explicit adoption for nonempty project or analysis directories without manifests. Adoption preserves existing content; conflicting files are never replaced to make the layout fit.
5. Tests write access with a unique, delete-on-close probe file.
6. Publishes new manifests through unique sibling temporary files, durable file flushes, and non-overwriting rename. Existing valid manifest bytes remain unchanged.
7. Creates the fixed input folders and selected analysis's run container. It does not allocate a simulation run or copy the shared library.

```text
<project>/
  flahagrow.project.json
  .flahagrow.workspace.lock
  inputs/
    geometry/
    weather/
    sensors/
    lighting/
  analyses/
    baseline/
      analysis.json
      runs/
```

The retained lock file is intentional: the handle provides exclusivity, and deleting the file when releasing the lock could permit competing processes to lock different files. Read-only Open does not acquire this lock. An unsuccessful initialization can leave the root and lock file present.

Changing the requested project display name does not rename an existing project. Selecting another analysis creates/opens a separate identity and run container. Reusing an analysis name with a different workflow fails; use a new name. No rename, delete, settings-update, or simulation execution API is introduced here.

## Manifest contract

Project manifests retain the schema-v1 contract from the first increment. Locations inside the project are persisted relative to its root, external overrides remain absolute, and the bundled library is not pinned to a machine-specific installation path.

`analysis.json` has schema version 1, analysis ID, project ID, validated name, named workflow (`AnnualDaylight` or `ElectricLighting`), and creation timestamp. Missing/unsupported workflow values and inconsistent ownership are rejected. Simulation input references and command settings remain the responsibility of later analysis/run configuration work; this increment does not invent values for them.

New manifest writes and reads are limited to 64 KiB. Existing project and analysis IDs survive repeated initialization. A resolved context carrying a different project ID, or whose known manifest has disappeared, cannot silently initialize a replacement project.

## Recovery and filesystem boundaries

- Identity manifests are published before the corresponding remaining layout is created. If a conflicting file interrupts layout creation, correcting that conflict and retrying resumes the same committed identity.
- Cancellation is checked before mutation and between operations. There is no recursive rollback or cleanup of user files. A cancelled/failed operation can leave a partially initialized workspace.
- This is not a transaction across all files. A hard crash before a manifest is committed may leave a temporary file; an unrecognized nonempty directory still requires explicit adoption on retry.
- Existing symbolic links/junctions in managed paths or their ancestors are rejected, including manifest and lock paths. This deliberately also rejects otherwise legitimate project roots accessed through a junction. External shared asset paths are only recorded, not modified.
- Ancestors are rechecked before writes. These checks prevent ordinary accidental traversal; they are not an operating-system sandbox against another process replacing filesystem entries between checks and writes.
- Services are synchronous and expose cancellation. Future Grasshopper adapters must run initialization outside the UI thread. Cancellation does not promise to interrupt a blocked operating-system filesystem call.

## Validation

The core suite now contains 66 passing tests, including 22 new workspace/analysis cases. It checks creation/opening, idempotence, input/result preservation, read-only opening, explicit adoption, separate analyses, project and analysis recovery, exclusive lock contention, concurrent initializers, cancellation, invalid names, newer schemas, foreign IDs, portability, and a real Windows junction escape fixture. Test filesystem changes are confined to unique temporary test directories.

```powershell
dotnet test tests/FlahaGrow.Core.Tests/FlahaGrow.Core.Tests.csproj --configuration Release --no-restore -m:1
dotnet build FlahaGrow.sln --configuration Release --no-restore -m:1
```

The read-only-manifest test verifies preservation; it is not a test of an ACL-denied directory. ACL denial, network storage, power-loss durability, and the host UI lifecycle are not validated by this increment.

Unified Radiance checks and typed Grasshopper Setup interfaces are now implemented in subsequent increments. The plugin references the shared core, which must be included in installations and packages; see [the current Setup guide](setup-components.md).
