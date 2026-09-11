# FlahaGrow documentation

Updated 2026-09-11. This page separates current operating guidance from dated evidence. Read the current guides first; dated audits record what was observed on their stated date and do not by themselves describe the present release.

## Start here

| Need | Current document |
| --- | --- |
| Install, connect, or migrate the Grasshopper workflow | [Component migration and wiring](component-migration.md) |
| Run an annual daylight study | [Annual workflow](annual-workflow.md) |
| Configure paths, an analysis, and Radiance | [Setup guide](setup-components.md) |
| Build, test, or package the plugin | [Grasshopper plugin development](grasshopper-plugin.md) |
| Connect Ladybug/Honeybee exports safely | [Ladybug Tools integration boundary](ladybug-honeybee-integration.md) |
| Check current implementation limits and validation evidence | [Current status](current-status.md) |

## Reference and evidence

| Document | Role |
| --- | --- |
| [Component I/O audit](component-io-audit-2026-09-09.md) | Source-derived comparison of the 25 legacy Python scripts and compiled components. |
| [Example definition audit](example-definition-audit-2026-09-10.md) | Latest source-backed audit of the supplied example definition; Rhino is still required for saved-node and wire proof. |
| [Component revision ledger](component-revisions.md) | Per-component canvas version/date tracking and update procedure. |
| [Annual run isolation](annual-run-isolation.md) | Manifest, result-validation, and cache ownership contract. |
| [Annual result audit](annual-result-audit-2026-09-10.md) | Read-only investigation of the legacy FlahGrow01 result set. |
| [Plugin gap audit](plugin-gap-audit-2026-09-11.md) | Current confirmed defects, validation boundaries, and implementation priorities. |
| [Rhino annual acceptance](rhino-acceptance-2026-09-11.md) | Evidence from the successful 980-sensor annual daylight Grasshopper run. |
| [Repository audit](repository-audit-2026-09-07.md) | Historical findings and implementation tracker. |
| [Setup consistency review](setup-consistency-review-2026-09-07.md) | Historical producer/consumer review, with follow-up status. |
| [Setup implementation plan](setup-implementation-plan.md) | Superseded planning record; use the Setup guide and current status for active behavior. |

The remaining design/background documents—project frame, legacy comparison, and the dated Setup increment notes—are reference material. They must not override the current guides or source.
