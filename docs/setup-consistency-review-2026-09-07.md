# Component consistency after Setup

Original review revision: `dd57f5866d85abecf3585cefbf23e6fd899e6954` (2026-09-07). Implementation status updated 2026-09-10; see [current status](current-status.md).

## Scope and conclusion

This review compares the latest Setup implementation with downstream compiled components and [the preceding repository audit](repository-audit-2026-09-07.md), which describes revision `254d720`. The previous conversation was unavailable; the latest commit and checked-in Setup contracts are the baseline. This is a verification and integration review, not authorization to redesign the simulation pipeline. The original review made no application or installation changes. Subsequent implementation and the authorized September 10 plugin replacement are recorded separately; existing studies remain unchanged. The pre-existing untracked `None/` directory was excluded.

**Setup is a tested foundation, but the complete component workflow is not yet consistent with it.** Optional typed environment/analysis integration is implemented. Preserved compiled GUIDs do not certify numerical correctness or original Python canvas migration.

## Connection matrix

| Producer → consumer | Current contract | Assessment |
| --- | --- | --- |
| Simulation Paths `Paths` → Working Directory `Paths` | `PathsGoo` / `PathsParameter` | Matching typed connection. |
| Working Directory `Analysis` → Radiance Status `Analysis` | `AnalysisGoo`; workflow comes from the stored analysis manifest | Matching typed connection. |
| Working Directory `Project` → existing runners' `Project` | Typed `ProjectGoo` versus text folder inputs | Not a supported direct connection. Use text `Folder` where a project root is required. |
| Working Directory `Folder` → Annual Simulation `Project` | FlahaGrow root versus Honeybee root containing `model/grid` and `model/scene` | Only usable if that root separately contains the required Honeybee export. Initialization does not create it. |
| Working Directory `Analysis` → Annual Simulation `Analysis` | Optional typed analysis ownership | Run snapshots are created in the analysis's Runs folder. The text Project input remains a separate Honeybee source root. |
| Setup `Library` → material, glazing, IES selectors | Asset root or legacy selector folder | Shared resolver accepts both; bundled paths also receive missing-folder validation. |
| Radiance Status `Radiance` → annual / IES runners | Optional typed verified environment | Enforces ready/matching workflow. Annual uses quoted absolute executable paths and checks the environment before writes. |
| Radiance Status `Bin` → annual / IES runners `Bin` | Text executable folder | Partial bridge; does not transfer readiness, workflow, or calculation-library override. |
| Radiance Status `Lib` → annual / IES runners | Selected calculation library | Carried through the typed Radiance input; no separate Lib wire is needed. |
| IES to Radiance → Lighting Geometry → Compile Luminaires | `.rad` paths → xform lines → output file | Shared project-local Luminaire_files folder. |
| Annual Simulation `Folder` → progress / cache | Isolated run folder containing a manifest | Consumers use only declared parts. Cache verifies manifest/source identity; progress uses the declared denominator. |
| Cache → illuminance / PPFD / metrics | Existing `.f32`, metadata, numeric lists | Cache layout retained; final matrices and selected reader values now reject negative/nonfinite illuminance. This does not prove scientific accuracy. |

## Findings and required follow-up

### C01 — Implemented; host execution validation pending

`Components/Setup/RadianceSetupComponent.cs` publishes workflow, readiness, bin, and library. Annual Simulation and IES to Radiance now expose an appended optional `Radiance Environment` input. When it is connected, the shared `RadianceExecutionEnvironment` requires `Ready=True` and the matching workflow; a supplied Bin must match the checked installation. Annual uses its exact bin and calculation library, while IES uses the checked `ies2rad` executable and `PATH`/`RAYPATH` child environment. A missing or invalid connected environment cannot fall back to ambient discovery.

Existing definitions without the new optional input retain the legacy Bin/automatic discovery path. They remain outside the Setup readiness contract. Annual still creates batch files and IES still invokes its process synchronously, so C05 and the annual execution findings remain open.

Validation: Core tests cover ready, not-ready, workflow-mismatched, selected-installation, separate-library, and mismatched-Bin gating. Component smoke checks verify the appended optional typed input on both consumers. A live Rhino run remains required to prove the runners use the checked environment during actual execution.

### C02 — Run ownership and result validation implemented; full annual host study pending

Annual Simulation accepts an optional Analysis context and snapshots the Honeybee source into a unique analysis-owned run folder. Without Analysis it uses a unique folder under the source root's `runs` container. The manifest declares identity, input/sensor hashes, and contiguous result parts. Progress and cache read only declared files. Source grids are preserved, including grids with names other than `0.pts`.

The F02 run-isolation contract is implemented and tested. Cache requires the full declared result set and rejects changed source signatures when reopening. Legacy flat results without manifests are rejected with regeneration guidance. See [annual run isolation](annual-run-isolation.md).

F03/F04 are now implemented: schema-2 runs record expected EPW steps, strict scalar ASCII parsing validates all values and dimensions, and each command is checked individually. Cache/progress require successful command states plus validated final matrices. Producer/consumer failures, negative exit codes, equally truncated parts, and invalid final output are covered. Full annual study validation in Rhino remains separate.

### C03 — Resolved: electric preparation uses one project-local luminaire folder

`LuminairePathResolver` now defines `<project>/Luminaire_files` as the one location for generated luminaire assets. Both `IesToRadianceComponent` and `CompileLuminariesComponent` use it, and the converter tooltip now describes the same location.

Validation: Core tests verify project-local resolution and reject an ambiguous relative project root. The plugin build and Setup smoke checks pass with existing component identities and ports. Live conversion → placement → compilation with `ies2rad` remains required before treating the electric preparation workflow as host-validated.

### C04 — Resolved: selectors accept the Setup Library output and legacy folder inputs

`LibraryPathResolver` now resolves a selector folder from the Setup asset root, its containing folder, or a legacy direct selector folder. `MaterialSelectors.cs`, `GlazingMaterialComponent.cs`, and `IesLuminaireSelectorComponent.cs` use it for explicit inputs while retaining their existing parameter names, order, and direct-folder behavior.

Validation: Core tests cover all three sections from an asset root, an asset-root parent, a legacy direct folder, and a missing folder. The Setup smoke build confirms the plugin compiles and retained its checked Setup interfaces. Selector dialogs still require live Rhino validation.

### C05 — Medium: lifecycle guarantees differ across Setup and consumers

Setup uses asynchronous operations, invalidates obsolete results, and gates initialization with an action latch. IES creates `Luminaire_files` before checking Run, then blocks in synchronous process reads without a timeout. Annual prepares and writes files when Run=False and launches again on each solve with Run=True. Selectors return without restoring selected output when Run=False and lack persisted selection state.

These are existing behaviors, not proof that legacy compatibility was broken. They nevertheless prevent applying Setup's lifecycle expectations to the entire workflow.

Acceptance: specify preparation versus execution actions explicitly; introduce bounded asynchronous execution and deliberate trigger semantics with migration coverage. Test idle solves, held True, save/reopen, changed inputs during work, and document closure in Rhino.

### C06 — Documentation mappings corrected; saved-definition verification pending

The original migration table incorrectly mapped legacy folder toggles to Simulation Paths and version execution to Radiance Status. Those interfaces belong to the hidden legacy Working Directory and Radiance Version components. The rewritten guide now distinguishes them from the visible components that reuse familiar names with different contracts.

Documentation acceptance addressed: [migration guide](component-migration.md) distinguishes hidden legacy/visible Setup by class/GUID and links the complete I/O audit. Original saved-definition access/wire verification remains pending; matching display names do not imply compatible ports.

## Implemented changes

- Added Core services, automated tests, workspace manifests, and shared Setup path resolution. The original audit's repository inventory is historical and should not be read as the current inventory.
- Preserved six hidden Setup identities and introduced a visible trio with documented transient typed outputs and explicit initialization.
- Added workflow-specific Radiance checks and component-local selection persistence.
- Added the native build exit-code guard in `tools/New-YakPackage.ps1`, addressing F10's stale-package-after-build-failure mechanism; this review does not repeat a packaging failure-injection test.
- Documented Core DLL and library requirements for installation, and clarified the new Library resolver's accepted layouts. Existing hidden component behavior remains separate.
- Added `LibraryPathResolver` and updated the material, glazing, and IES selectors to consume the Setup Library output directly while supporting legacy direct folder inputs (C04).

The original dd57f58 Setup commit did not close the annual/selector findings. Subsequent work is recorded in the repository tracker and live-result audit; do not interpret that historical scope as the current source state.

Subsequent implementation addresses C01–C04, F01–F04, and F07 as recorded in the repository audit. The review regressions are corrected, with direct component integration checks. Selector persistence, remaining option validation, and full annual Rhino study validation remain outstanding.

## Validation

| Check | Result |
| --- | --- |
| `dotnet test tests/FlahaGrow.Core.Tests --configuration Release --no-restore -m:1` | 156 passed, zero failed/skipped (2026-09-10). |
| `tools/Test-SetupComponents.ps1 -NoRestore` | Nine component GUID/interface/archive checks passed; exactly three visible Setup components. Direct solve, initialization, retained identity, automatic check, explicit-selection isolation, and analysis-workflow checks passed. |
| Plugin and smoke-project Release build through the component script | Passed with zero warnings/errors. |

The old live annual study was inspected and its cache verified, revealing negative simulation values. No corrected full annual study or live Rhino canvas lifecycle validation is included. The expanded smoke checks exercise annual preparation, manifest-owned cache/progress, strict matrices, failed-part rejection, selector error handling, luminaire path wiring, and invariant RGB/xform writing through compiled methods. Optional `-RadianceBin` checks passed against installed Ladybug `rmtxop` for a real calculation and missing-input failure. User-shared Rhino Setup outputs confirm readiness; real wire conversion, menus, and the Rhino UI scheduler are not covered by the adapter.

## Recommended implementation sequence

1. Unify library subfolder and luminaire path resolution; preserve old input forms and cover the actual producer/consumer chain.
2. Connect the verified Radiance environment to execution, including custom calculation libraries and strict explicit selection.
3. Integrate analysis/run identity across annual preparation, progress, and cache while addressing the audit's result-integrity defects.
4. Reconcile trigger/persistence behavior and migration documentation, then exercise saved legacy and new definitions in Rhino.

Completion requires downstream integration tests as well as the existing Setup checks. A passing Setup suite alone is not sufficient evidence of an integrated simulation workflow.
