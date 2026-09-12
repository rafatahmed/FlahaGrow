# Setup implementation plan — superseded record

Original plan: 2026-09-07. Consolidated 2026-09-11.

This file is retained because dated audits link to it. It is not the active Setup specification: its proposed interfaces, phased milestones, and early test counts were superseded by implementation and later audits.

## Current source of truth

- [Setup guide](../getting-started/setup.md): visible components, connections, installation requirements, and operator actions.
- [Component migration and wiring](../components/migration.md): supported downstream connections and annual-run migration.
- [Current status](../quality/current-status.md): installed-build record, validation evidence, and open work.
- [Annual run isolation](../workflows/annual-run-isolation.md): current run/result/cache ownership contract.

## What the plan established

The plan introduced the now-implemented direction: explicit project/analysis contexts, a checked Radiance environment, read-only resolution before initialization, project-local run ownership, and preservation of published component identities. It also recorded important constraints that remain valid:

- Rhino 8 on Windows is the supported host.
- Setup configuration is portable; machine-specific Radiance locations must be rechecked on another machine.
- A workspace folder is not a Honeybee ModelToRad export root.
- Existing component GUIDs and parameter order must be preserved; compatible inputs are appended.
- Setup does not certify physical accuracy of a simulation.

## Superseded items

Do not use the old proposed port lists, acceptance targets, increment sequencing, or early test totals to make an implementation or release decision. Current behavior is defined by the compiled source and the guides above. Historical work recorded in the original plan is retained in the repository and Setup-consistency audits.
