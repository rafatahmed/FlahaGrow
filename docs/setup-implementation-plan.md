# FlahaGrow Setup implementation plan

Status: proposed implementation contract, based on repository inspection on 2026-09-07.

Implementation has started with the shared core, schema-v1 project codec, read-only path resolver, and automated tests. See [Setup foundation](setup-foundation.md) for exact scope, compatibility inventory, and remaining host-validation gates.

The second increment adds explicit workspace creation/opening, schema-v1 analysis manifests, adoption, exclusive initialization locks, and recovery tests. See [workspace implementation notes](setup-workspaces.md) for behavior and validation boundaries.

The third increment implements [shared Radiance discovery and checks](setup-radiance.md), including bounded discovery, capabilities, cached version probes, cancellation, and a local IES diagnostic.

The fourth increment adds [project-based Setup components](setup-components.md), typed connections, persistent configuration, action lifecycle handling, standalone component checks, and Windows package updates. Live Rhino canvas/definition migration verification and downstream run integration remain pending.

## Objective and scope

The fifth increment simplifies the visible Setup group to Simulation Paths, Working Directory, and automatic Radiance Status. Standalone detection precedes Ladybug Tools and PATH; incomplete/failed automatic candidates can fall back with diagnostics. Radiance inherits the Analysis workflow, overrides live in its menu, and six previous components are hidden while retaining their saved interfaces. See the current [Setup guide](setup-components.md) for the implemented workflow; earlier interface proposals below are historical.

Create a predictable Setup category that resolves project locations, opens or creates workspaces, and verifies a consistent Radiance installation. Every downstream component should consume the same project and tool configuration.

Assumptions:

- Rhino 8 on Windows remains the initial supported host; this change does not include a runtime upgrade.
- A project contains named analyses, each with separate execution runs. Multiple projects may be open in the same Rhino session.
- Existing Grasshopper definitions must continue loading with their existing component identities and parameter order.
- Local disks are the initial performance baseline. Network and synchronized locations are supported on a best-effort basis with explicit diagnostics and cancellation.
- Project configuration is portable; machine-specific Radiance locations are optional overrides and must be revalidated on another machine.
- Setup prepares the environment. It does not certify numerical simulation accuracy or fix every finding in the repository audit.

No databases, credentials, deployment, or external services are required. Planning does not authorize installation or changes to machine environment variables.

## Current implementation constraints

| Current code | Design consequence |
| --- | --- |
| Simulation Paths creates folders while resolving inputs | Separate read-only resolution from explicit initialization |
| Working Directory creates six legacy output folders | Preserve its old interface; publish the new project contract with a new component identity |
| Status checks existence while Version runs a separate synchronous process | Share discovery and process execution; present both through the new Status component |
| Annual runner and IES converter implement different Radiance searches | Replace duplicate searches with a shared installation descriptor |
| IES conversion and compilation disagree on the luminaire folder | All project paths must come from one resolver |
| Annual result discovery uses unrestricted part-file globbing | Define run identity and expected outputs before downstream migration |
| Components combine dialogs, calculations, and filesystem operations | Keep new Grasshopper adapters thin and test core services independently |

## Intended user flow

1. Place Simulation Paths. Auto mode proposes a project location and resolves available library/tool locations. Optional inputs provide custom paths.
2. Connect Paths to Working Directory. Enter an analysis name, defaulting to `baseline`. Review the resolved location, then use an Initialize button to create or open it.
3. Connect the project context to Radiance Status. Use Check to verify the selected installation and required workflow capabilities.
4. Connect the analysis context and Radiance environment to simulation components. Explicit path outputs remain available for Honeybee and legacy components.
5. Run a simulation. The runner creates a unique run directory and records its inputs; Setup does not allocate runs during ordinary recomputation.

No modal picker opens during a normal solution. Optional Browse actions belong in component menus. Outputs remain available when action inputs return to False.

## Component contracts

### Simulation Paths — resolve and explain

Inputs, in proposed order:

1. Project location, optional: exact project root or existing `flahagrow.project.json`.
2. Location mode: Auto, Project-relative, System, or Custom; default Auto.
3. Library location, optional.
4. Radiance location, optional installation root or bin folder.
5. Refresh button, default False.

Outputs: typed Resolved Paths; Project folder; Library folder; candidate Radiance bin; Status with the source of each choice and any unresolved settings.

Rules:

- Resolving paths creates no directories or probe files and launches no processes.
- Resolve the project root first; only then read configuration at that root. Do not search unrelated directories for a project manifest.
- An explicit project input wins. Otherwise use a serialized project reference for this component, then `<saved-definition-directory>/<definition-name>.FlahaGrow`, then the user's Documents folder plus `FlahaGrow/Projects/<stable-draft-id>`.
- Obtain Documents through the operating system's known-folder API. If unavailable, request an explicit location rather than inventing a fallback.
- In forced Project-relative mode an unsaved definition is unresolved; Auto mode can propose the system fallback and explain why.
- Custom mode requires a custom project input. Invalid explicit paths produce an error; they never silently redirect to another project.
- Once initialized, a project remains bound to its identity. Saving or moving the Grasshopper definition does not relocate that project automatically.
- Library precedence: explicit input, project configuration, project-local library if present, bundled library. Accept either the library root or its `FlahaGrow_Library_Small` child and normalize to a single asset root.
- Radiance precedence: explicit input, project-configured override, PATH candidates, known installation locations. An invalid explicit override stays an error. Discovery reports alternatives without silently mixing installations.
- Relative external inputs are resolved against a documented base: project root for library/tool overrides and saved-definition directory for the project reference. Never use the process working directory implicitly.
- Distinguish syntax-valid, exists, and verified-writable. A read-only resolver must not claim to have tested write access.

### Working Directory — initialize or open a project

Inputs: Resolved Paths; Analysis name (`baseline`); Initialize button (False).

Outputs: Project Context; Analysis Context; Project folder; Model input folder; Results base folder; Library folder; Status.

Behavior:

- With Initialize=False, show the proposed structure or read an existing valid workspace. Do not mutate it.
- On an explicit initialization action, validate names, check existing manifests, verify write access using a uniquely named temporary probe in the intended location, and create only required folders.
- A project containing a valid supported manifest opens without resetting settings. An existing nonempty folder without a manifest requires an explicit Adopt action; never move its contents automatically.
- Initialization is idempotent. Repeating it does not overwrite inputs, results, or a valid manifest.
- Persist the selected project and analysis. Use stable project/analysis IDs distinct from display names and directory names.
- Changing an analysis input changes the proposed selection; it does not rename or mutate another analysis implicitly.
- Validate invalid Windows names, reserved names, trailing spaces/dots, path separators, and traversal in project-relative names. Keep all generated paths inside the resolved root, including checks for existing junctions/reparse points.
- Use manifest version checks. Reject unsupported newer schemas with a clear diagnostic; do not silently rewrite them.
- Write manifests through a temporary sibling file and replacement. Use a short exclusive operation lock to prevent simultaneous initialization from racing; report contention without blocking Rhino indefinitely.
- If initialization is interrupted, retain pre-existing user content and make a retry safe. No automatic recursive cleanup.

### Radiance Status — inspect one installation

Inputs: Project Context (optional); explicit Radiance location (optional override); Workflow (`Annual daylight` default); Check button (False); optional Diagnostic action in the component menu.

Outputs: typed Radiance Environment; Ready; Version; Bin folder; calculation-library search paths; installation candidates; detailed Status.

States: Not checked, Checking, Located, Executable, Ready, Incomplete, Failed, Cancelled. Ready means readiness for the selected workflow, not validated simulation accuracy.

- Discovery inspects a bounded list of candidate paths; it never recursively scans disks.
- Resolve all required executables from the selected installation and use absolute executable paths downstream.
- Annual capability includes the commands actually used by the runner: epw2wea, gendaymtx, oconv, rfluxmtx, dctimestep, rmtxop, cnt, rcalc, and rcontrib. Verify required calculation files, including reinsrc.cal and reinhart.cal.
- Electric-light preparation requires ies2rad and xform plus dependencies established by an executable fixture. Keep workflow requirements in one capability table.
- Read version output from both stdout and stderr; retain exit code and diagnostics. Do not infer success solely from one stream being empty.
- Run checks asynchronously with cancellation, bounded output capture, and a proposed five-second version-probe timeout per selected installation. Timeout means Unknown/Failed, not Not installed.
- Show all located candidate paths. Probe alternative versions only on explicit refresh/request; do not launch every candidate on each solution.
- Apply PATH/RAYPATH changes only to child processes. Do not change the user's environment or Rhino's process-wide environment.
- An optional tiny diagnostic validates actual execution in a disposable workspace; define and verify the fixture during implementation. It runs only on explicit request.
- Radiance Version remains available as a legacy component using the same underlying service.

## Project layout and data ownership

```text
<project>/
  flahagrow.project.json
  inputs/
    geometry/
    weather/
    sensors/
    lighting/
  library/                          Created only when project assets are added
  analyses/
    baseline/
      analysis.json
      runs/
        <utc-time>-<unique-id>/
          run.json
          model/
          commands/
          results/
          cache/
          logs/
```

Do not copy the bundled library into every project. Reference it by default. When reproducibility or export requires a snapshot, copy only used assets through an explicit operation and record content hashes. Project inputs remain editable; launched runs consume recorded snapshots. No simulation writes into the shared library.

Honeybee compatibility: preserve `model/scene` and `model/grid` inside each run's model staging contract. The staging service maps imported ModelToRad content to the exact paths expected by the runner; verify the nesting with a fixture before connecting real studies. Never assume a results directory is also a Honeybee project root.

Manifests:

| File | Required information |
| --- | --- |
| Project | Schema version, project ID, display name, creation time, relative managed paths, library configuration, optional machine tool override |
| Analysis | Schema version, analysis ID, project ID, name, workflow, input references and settings |
| Run | Schema version, run ID, analysis ID, input hashes/snapshot paths, ordered sensors, selected tool paths/version, command settings, expected parts/files, state and timestamps |

Run states: Prepared → Running → Succeeded, Failed, or Cancelled. An interrupted Running state is reported as interrupted/unknown on reopening until explicitly reconciled. Success requires validated outputs, never just a nonempty file. Readers select a specific completed run; they do not glob results across runs. Store dimensions, part ordering, and source-run ID with caches.

## Internal architecture

Proposed additions:

```text
src/FlahaGrow.Core/
  Projects/       Models, path resolution, manifest validation, workspace/run services
  Radiance/       Installation discovery, capability checks, process runner
  Diagnostics/    Structured status and errors
src/FlahaGrow.Grasshopper/
  Parameters/     Typed context parameters and Grasshopper serialization adapters
  Components/    Thin Setup components and compatibility wrappers
tests/FlahaGrow.Core.Tests/
  Projects/
  Radiance/
  Fixtures/
```

Use one small core assembly, not a service framework. It must not reference Rhino, Grasshopper, or WinForms. Use immutable context snapshots and explicit operation results. Introduce filesystem/process seams only where needed for deterministic tests. Share descriptors and discovery caches, but never use a static global current project or analysis.

Adding the core assembly changes distribution: the manual install and Yak staging must include it while continuing to exclude host assemblies. This is a release acceptance requirement, not a later documentation task.

Grasshopper serialization stores configuration and project references, not live processes or a permanent Ready=True result. On reload, contexts rebind to manifests and runtime readiness becomes unverified. Changes in one document must not affect another document's selection.

## Execution and performance contract

These are proposed acceptance targets, not measurements of the current code:

| Operation | Target and strategy |
| --- | --- |
| Unchanged warm Setup solution | p95 under 20 ms per component on the recorded local SSD test machine; zero writes, process launches, or full directory scans |
| Discovery | Bounded candidates, deduplicated normalized paths; no recursive search or registry-wide enumeration |
| Version check | Outside the UI thread; five-second per-probe timeout, cancellation, concurrent stdout/stderr draining |
| Initialization | Work proportional to the fixed folder/manifest count, independent of library or historical result size |
| Manifest reads | Reuse parsed snapshots; refresh when configuration/file identity changes or user requests it |
| Input hashing | At explicit run preparation, streamed with cancellation; never hash weather/geometry during ordinary Setup recomputation |
| Duplicate checks | Coalesce concurrent checks for the same installation fingerprint; bounded cache, explicit refresh bypass |

Treat file timestamps/lengths as discovery cache hints, not proof that content has not changed. Revalidate executable existence and run prerequisites immediately before launch. Use full content hashes when capturing run provenance.

Network filesystem calls can stall even when executed asynchronously. The UI must remain responsive, but hard cancellation of every operating-system file call is not promised. Bound active operations, discard obsolete results, and prevent abandoned checks from flooding the thread pool.

Action semantics: Initialize/Check use rising-edge button behavior and an operation key. Keep outputs on release; do not repeat while True remains connected. On document reload, require the action to be rearmed before any write/process action. For asynchronous work, capture an input revision, publish results only if it still matches, and marshal completion back to Grasshopper's solution lifecycle. Cancel or detach safely when a component/document is removed.

## Compatibility strategy

- Preserve existing component GUIDs, parameter order, and legacy output meanings.
- Publish new Setup interfaces under new permanent GUIDs. After parity checks, show the new versions in Setup and hide legacy versions from ordinary placement while keeping them loadable.
- Old components can delegate to shared discovery/path helpers where behavior is equivalent. Do not quietly reinterpret legacy folder inputs.
- Provide ordinary path outputs as well as typed context outputs during migration.
- Migrate IES conversion, luminaire compilation, and annual execution to context-based variants in separate reviewable steps. A new context input must not replace an old path input in-place under the same GUID.
- Validate a saved legacy definition before and after loading the new plugin. Record any intentional behavioral differences.

## Delivery sequence and completion gates

| Phase | Deliverable | Gate |
| --- | --- | --- |
| 1. Contracts and fixtures | Context models, schema v1, path precedence tests, saved legacy component fixture | Deterministic paths; unresolved/invalid inputs explicit; existing GUID/parameter inventory captured |
| 2. Project services | Read-only resolution, manifest store, idempotent initialization, analysis selection | Repeated initialization leaves inputs/results unchanged; traversal/collision/concurrent access tests pass |
| 3. Radiance services | Unified discovery, capabilities, cancellable process runner, cached diagnostics | Missing/partial/multiple installations, timeout, stderr, cancellation, and duplicate-request tests pass |
| 4. Setup UI | Three new components, typed parameters, persistent settings, legacy wrappers | New/open/unsaved/moved project cases and document save/reopen pass in Rhino; UI stays responsive |
| 5. Downstream integration | Shared luminaire paths, run staging/manifests, explicit completed-run cache selection | Four-part → single-part rerun cannot mix results; failed jobs never report success; old definitions still load |
| 6. Release verification | Benchmarks, local diagnostic fixture, install/package updates, user guide | Clean Release build; package includes core/library; fresh-host smoke test and measured performance report |

Each phase is a reviewable change with its tests and documentation. Do not combine runtime upgrades, broad UI restyling, spectral-math changes, and Setup migration in one change.

Phase 5 must also address the audited sky-basis mismatch and strict matrix validation before claiming the new annual workflow is ready. Setup alone does not resolve those calculation/result-integrity defects.

## Validation matrix

- Paths: custom, saved project, saved definition, unsaved definition, spaces, Unicode, relative paths, missing roots, unavailable network paths, invalid explicit overrides.
- Projects: create/open, repeated action, adoption, malformed/newer manifest, moved project, read-only root, interrupted initialization, concurrent initialization, junction escape attempts.
- Persistence: save/reopen with action True; deleted project; renamed definition; two documents and two components using different projects; unchanged outputs after button release.
- Radiance: none/one/multiple installations; missing executable/calculation file; custom bin/lib; output on stderr; nonzero exit; timeout; large output; cancellation; input changes during a check.
- Integration: same project for IES conversion and compilation; Honeybee model import; isolated reruns; expected part validation; cache provenance; no changes to shared assets or global environment.
- Performance: record OS, storage, host versions, candidate count, cold/warm timings, p50/p95, file operations, and child-process count. Run repeated unchanged solutions and compare against the proposed targets.
- Release: warnings-as-errors build, core tests, clean-machine-style package staging, manual Rhino load/save/reopen and tiny Radiance diagnostic. No publishing or installation into the user's active host without specific approval.

## Definition of done

The foundation is complete when a user can create or reopen a project with the three Setup components, retain settings across sessions, identify exactly which Radiance installation is selected, and run a migrated analysis without path guessing or stale-result reuse. Existing definitions remain loadable; normal recomputation performs no writes or process launches; tests and measured host checks support the behavior. Numerical correctness outside the migrated workflow remains separately tracked in the repository audit.
