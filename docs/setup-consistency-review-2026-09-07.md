# Component consistency after Setup

Reviewed revision: `dd57f5866d85abecf3585cefbf23e6fd899e6954` (2026-09-07).

## Scope and conclusion

This review compares the latest Setup implementation with downstream compiled components and [the preceding repository audit](repository-audit-2026-09-07.md), which describes revision `254d720`. The previous conversation was unavailable; the latest commit and checked-in Setup contracts are the baseline. This is a verification and integration review, not authorization to redesign the simulation pipeline. No application source, installations, deployments, credentials, or existing studies were changed. The pre-existing untracked `None/` directory was excluded.

**Setup is a tested foundation, but the complete component workflow is not yet consistent with it.** Preserving old component GUIDs and ports allows definitions to load; it does not make the old runners consume the new project, analysis, or Radiance contexts.

## Connection matrix

| Producer → consumer | Current contract | Assessment |
| --- | --- | --- |
| Simulation Paths `Paths` → Working Directory `Paths` | `PathsGoo` / `PathsParameter` | Matching typed connection. |
| Working Directory `Analysis` → Radiance Status `Analysis` | `AnalysisGoo`; workflow comes from the stored analysis manifest | Matching typed connection. |
| Working Directory `Project` → existing runners' `Project` | Typed `ProjectGoo` versus text folder inputs | Not a supported direct connection. Use text `Folder` where a project root is required. |
| Working Directory `Folder` → Annual Simulation `Project` | FlahaGrow root versus Honeybee root containing `model/grid` and `model/scene` | Only usable if that root separately contains the required Honeybee export. Initialization does not create it. |
| Working Directory `Inputs` / `Runs` → Annual Simulation | Shared input folder / analysis run container versus Honeybee root | Not interchangeable; no import or isolated-run adapter exists. |
| Setup `Library` → material, glazing, IES selectors | Asset root versus `RadMaterials`, `RadGlazing`, or `RadIES` subfolder | Requires appending the appropriate subfolder. Direct root wiring is not supported by selector lookup. |
| Radiance Status `Radiance` → annual / IES runners | Typed verified environment versus text `Bin` only | No typed consumer exists in either runner. |
| Radiance Status `Bin` → annual / IES runners `Bin` | Text executable folder | Partial bridge; does not transfer readiness, workflow, or calculation-library override. |
| Radiance Status `Lib` → annual / IES runners | Selected calculation library | Neither runner has a corresponding input. Annual derives sibling `lib`; IES inherits the ambient process environment. |
| IES to Radiance → Lighting Geometry → Compile Luminaires | `.rad` paths → xform lines → output file | Ports match, but conversion and compilation disagree on the output directory for the same project. |
| Annual Simulation `Folder` → progress / cache | Flat result folder | Ports match, but consumers do not use analysis identity or an isolated-run manifest. |
| Cache → illuminance / PPFD / metrics | Existing `.f32`, metadata, numeric lists | Unchanged by Setup; new Setup checks do not validate result correctness. |

## Findings and required follow-up

### C01 — Implemented; host execution validation pending

`Components/Setup/RadianceSetupComponent.cs` publishes workflow, readiness, bin, and library. Annual Simulation and IES to Radiance now expose an appended optional `Radiance Environment` input. When it is connected, the shared `RadianceExecutionEnvironment` requires `Ready=True` and the matching workflow; a supplied Bin must match the checked installation. Annual uses its exact bin and calculation library, while IES uses the checked `ies2rad` executable and `PATH`/`RAYPATH` child environment. A missing or invalid connected environment cannot fall back to ambient discovery.

Existing definitions without the new optional input retain the legacy Bin/automatic discovery path. They remain outside the Setup readiness contract. Annual still creates batch files and IES still invokes its process synchronously, so C05 and the annual execution findings remain open.

Validation: Core tests cover ready, not-ready, workflow-mismatched, selected-installation, separate-library, and mismatched-Bin gating. Component smoke checks verify the appended optional typed input on both consumers. A live Rhino run remains required to prove the runners use the checked environment during actual execution.

### C02 — High: workspace identity and run isolation stop at Setup

`Core/Projects/WorkspaceService.cs` creates project-level `inputs/{geometry,weather,sensors,lighting}` and `analyses/<name>/runs`. Annual Simulation still reads `model/grid` and `model/scene` and writes all prepared and result files into its supplied root. Progress and cache scan that folder by wildcard. Neither consumes `AnalysisGoo` or its identity.

Consequence: separate analysis manifests do not isolate simulation results. Audit F02–F04 remain applicable; naming an analysis alone cannot prevent stale or partial files from being accepted.

Acceptance: define an explicit Honeybee import/preparation boundary and allocate a unique run directory with declared parts and input/sensor identity. Runner, progress, and cache must agree on that manifest. Test one/four-part runs, reruns, missing parts, command failure, and truncated output.

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

### C06 — Medium: migration documentation uses ambiguous old component names

`component-migration.md` rows 1–2 map legacy folder toggles to Simulation Paths and version execution to Radiance Status. Those interfaces belong to the hidden legacy Working Directory and Radiance Version components. The visible new components reuse familiar display names with different contracts.

Acceptance: distinguish hidden legacy and visible Setup components using class/GUID references, update the migration table, and link the new connection guide. Do not interpret matching display names as port compatibility.

## Implemented changes

- Added Core services, automated tests, workspace manifests, and shared Setup path resolution. The original audit's repository inventory is historical and should not be read as the current inventory.
- Preserved six hidden Setup identities and introduced a visible trio with documented transient typed outputs and explicit initialization.
- Added workflow-specific Radiance checks and component-local selection persistence.
- Added the native build exit-code guard in `tools/New-YakPackage.ps1`, addressing F10's stale-package-after-build-failure mechanism; this review does not repeat a packaging failure-injection test.
- Documented Core DLL and library requirements for installation, and clarified the new Library resolver's accepted layouts. Existing hidden component behavior remains separate.
- Added `LibraryPathResolver` and updated the material, glazing, and IES selectors to consume the Setup Library output directly while supporting legacy direct folder inputs (C04).

The latest commit did not modify the annual runner, result cache, luminaire pipeline, glazing parser, spectral selection implementation, or illuminance readers. Consequently F01–F09 and F11–F12 are not closed by this commit. This review does not claim a fresh numerical reproduction of every historical finding.

Subsequent working-tree implementation resolved C01, C03, C04, and F07 as recorded in the repository audit. Annual run isolation, cache validation, selector persistence, and Rhino-host validation remain outstanding.

## Validation

| Check | Result |
| --- | --- |
| `dotnet test tests/FlahaGrow.Core.Tests --configuration Release --no-restore -m:1` | 106 passed, zero failed/skipped. |
| `tools/Test-SetupComponents.ps1 -NoRestore` | Nine component GUID/interface/archive checks passed; exactly three visible Setup components. Direct solve, initialization, retained identity, automatic check, explicit-selection isolation, and analysis-workflow checks passed. |
| Plugin and smoke-project Release build through the component script | Passed with zero warnings/errors. |

No full annual simulation or live Rhino canvas lifecycle test is included. The standalone smoke checks do not exercise real wire conversion, menus, or the Rhino UI scheduler. The downstream findings above are based on source inspection; the passing tests do not cover those integrations.

## Recommended implementation sequence

1. Unify library subfolder and luminaire path resolution; preserve old input forms and cover the actual producer/consumer chain.
2. Connect the verified Radiance environment to execution, including custom calculation libraries and strict explicit selection.
3. Integrate analysis/run identity across annual preparation, progress, and cache while addressing the audit's result-integrity defects.
4. Reconcile trigger/persistence behavior and migration documentation, then exercise saved legacy and new definitions in Rhino.

Completion requires downstream integration tests as well as the existing Setup checks. A passing Setup suite alone is not sufficient evidence of an integrated simulation workflow.
