# Shared Radiance discovery and checks — historical implementation record

Original implementation evidence: 2026-09-07. This document describes the discovery service and local observations from that date. The current operator contract is [Setup guide](../getting-started/setup.md); current limitations and validation are in [current status](../quality/current-status.md).

The visible [Setup components](../getting-started/setup.md) use shared installation discovery, workflow capability checks, cancellable process execution, and cached readiness probes. Hidden legacy components remain loadable for saved definitions.

## Discovery contract

`RadianceRequest` contains an explicit installation override, an optional project-configured Radiance location, an optional absolute library override, the analysis workflow, ordered search paths, and known installation locations. `FromSystem()` snapshots PATH and supplies the known Windows Radiance locations; it does not modify the environment.

`RadianceDiscovery.Discover()` checks locations without directory enumeration or process execution. It inspects at most 128 unique normalized supplied locations, deduplicates before filesystem inspection, normalizes installation-root/bin inputs, deduplicates bin paths case-insensitively, and returns immutable candidate descriptors with executable paths, library location, missing requirements, and a file fingerprint.

Selection rules:

- An explicit override takes priority over the project Radiance override. A configured override is inspected alone; an invalid override never silently falls back.
- Without an override, candidates follow standalone, Ladybug Tools, other supplied known locations, then PATH. The first complete installation is selected, with diagnostics for skipped incomplete candidates. Tools are never borrowed from another installation. If none is complete, the first recognized candidate supplies the missing-file diagnostics.
- Automatic checks try complete candidates in preference order until both the version and workflow execution checks succeed, with at most four installation attempts. Failed attempts are reported. Explicit selections probe only that installation; return to automatic detection to inspect alternatives.
- Relative installation/library overrides are rejected here. Project-relative overrides must first be resolved against the project by the path resolver.

| Workflow | Required executables | Required library files |
| --- | --- | --- |
| Annual daylight | rcontrib, gendaymtx, oconv, rfluxmtx, dctimestep, rmtxop, cnt, rcalc | reinsrc.cal, reinhart.cal |
| Electric-light preparation | rcontrib, ies2rad, xform, oconv | source.cal, lamp.tab |

The electric-light requirements were exercised with the bundled `ULHB-70W.IES` fixture. These capability lists cover the current preparation paths, not every possible custom Radiance model, photometry variant, or auxiliary dependency.

## Status and version probes

`RadianceStatusService.CheckAsync()` returns NotFound, Incomplete, Ready, Failed, or TimedOut, with the selected descriptor, combined version output, raw process report, and diagnostics. Discovery alone supplies the located descriptor; it does not assert executable readiness.

Ready requires all listed workflow files, a successful `rcontrib -version`, and a workflow execution fixture. Annual daylight runs `rcalc` with the selected absolute `reinsrc.cal` and validates a finite unit vector. Electric preparation runs IES conversion, transformation, and compilation of the transformed scene. Failure prevents readiness and allows automatic fallback; explicit selections stay fixed. Full simulation accuracy and all annual pipeline stages remain outside this check.

Execution fixtures use unique temporary directories and a ten-second shared cancellation deadline, with five-second per-process deadlines. Directories are cleaned after checking. No installations or project files are changed. Execution results share the existing fingerprint cache and Refresh lifecycle with version checks.

- Discovery is dispatched away from the caller thread, with at most four filesystem inspections active per service instance. Operating-system filesystem calls on unavailable network storage are not hard-cancellable.
- Version checks use a five-second deadline, direct absolute executable invocation, and concurrent stdout/stderr draining. There is no command-shell interpolation.
- Output capture is bounded per stream (16,384 characters by default), while excess output is still drained to avoid pipe deadlocks.
- The process runner supports explicit cancellation, attempts process-tree termination on timeout/cancellation, and allows two seconds for cleanup. Process-start latency and blocked operating-system calls are outside a strict wall-clock guarantee.
- Child PATH starts with the selected bin folder. Child RAYPATH includes the working directory and selected calculation library only. Rhino/system environment values are never changed.

## Cache and cancellation semantics

Each service instance holds at most 32 probe tasks/results. Equal installation fingerprints share a probe. A fingerprint includes normalized bin/library locations, workflow, and required-file timestamps/lengths. Every Check repeats the bounded existence/fingerprint inspection; unchanged checks reuse the version result. Completed checks are evicted when the cache fills. If all 32 entries are active, an additional distinct probe receives a busy error.

Refresh bypasses completed results but shares an already-active matching probe. Failed probes are removed after observation so a later check can retry. Cancelling a caller abandons that caller's wait without terminating a probe shared by other callers; the underlying probe still has its own five-second deadline. Direct use of `RadianceProcessRunner` supports cancellation of the active process itself.

Fingerprints are cache hints, not cryptographic identity. Simulation runners must recheck prerequisites and capture input/tool provenance before launch. The Grasshopper adapters cache unchanged inputs, discard obsolete asynchronous results, and map errors to component status. The visible Radiance Status checks automatically and inherits workflow from its optional Analysis input.

## Validation

Readiness tightening: 106 core tests and nine standalone component compatibility checks pass. Local execution checks pass for annual daylight with Ladybug 5.4 and electric preparation with standalone 6.1a. The local PATH no longer triggers the discovery-limit warning after deduplication and the 128-location budget. Standalone's missing `reinsrc.cal` remains correctly reported. These checks do not change the installed Radiance files.

Automatic selection confirmed on 2026-09-07:

| Workflow | Selected bin | Version | Execution result |
| --- | --- | --- | --- |
| Annual daylight | `C:\Program Files\ladybug_tools\radiance\bin` | `5.4 2023-11-05 LBNL (5.4.4ee32974b1)` | Sun-direction calculation passed |
| Electric-light preparation | `C:\Radiance\bin` | `6.1a 2026-05-05 LBNL (6.1.39b9966033)` | IES conversion, transformation, and scene compilation passed |

Annual daylight skips standalone because `C:\Radiance\lib\reinsrc.cal` is absent. All executable tools in the current checklist are present in both installations. Roaming's Ladybug standards contain material/construction data and do not supply the missing calculation file. Selection is workflow-specific and remains automatic; Ladybug is not globally pinned. Installing the missing file through an appropriate installation update may change future automatic selection after Refresh.

Coverage includes standalone priority, automatic fallback, explicit override isolation, duplicate candidates, missing commands/calculation files, workflow differences, search/probe bounds, stderr versions, failed/empty probes, cache refresh/invalidation/eviction, shared requests, caller cancellation, large pipe output, process timeout, launch failure, and child-only environment overrides.

Local diagnostic on 2026-09-07:

- Installation: `C:\Program Files\ladybug_tools\radiance`.
- Version reported: `RADIANCE 5.4 2023-11-05 LBNL (5.4.4ee32974b1)`.
- Both annual-daylight and electric-preparation capability/version checks returned Ready.
- Using `src/Library/FlahaGrow_Library_Small/RadIES/ULHB-70W.IES`, `ies2rad -t default`, a zero-translation `xform`, and `oconv` all exited zero with no stderr diagnostics.
- The diagnostic ran through the new process service and wrote only to a unique ignored `artifacts/radiance-check/` subdirectory. It did not install anything or change the active Rhino environment.

The generated local diagnostic harness is retained under ignored `artifacts/radiance-check/`; it is not part of the automated test suite. No annual weather/matrix simulation or live Rhino UI test was performed in this increment.

```powershell
dotnet test tests/FlahaGrow.Core.Tests/FlahaGrow.Core.Tests.csproj --configuration Release --no-restore -m:1
dotnet build FlahaGrow.sln --configuration Release --no-restore -m:1
```

The subsequent [typed Setup component increment](../getting-started/setup.md) connects these services and updates distribution for the core assembly. Live host validation and downstream simulation migration remain outstanding.
