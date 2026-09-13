# FlahaGrow documentation

This documentation describes the repository as implemented. Source code is the authority when a document and code disagree. FlahaGrow is a Windows Rhino 8 / Grasshopper add-on (`net7.0-windows`) for Radiance-oriented greenhouse-lighting workflows; it is not a general Honeybee or Ladybug integration.

## Read in this order

1. [System architecture](architecture/project-frame.md) — assemblies, assets, external programs, and ownership boundaries.
2. [Radiance methods](architecture/radiance-methods.md) — the implemented climate-based matrix calculation and the lux-to-PPFD/DLI boundary.
3. [Scientific PPFD/DLI assessment](architecture/ppfd-dli-scientific-assessment.md) — reliability limits and enhancement roadmap.
4. [Setup](getting-started/setup.md) — project contexts, assets, and Radiance readiness checks.
5. [Annual daylight workflow](workflows/annual-workflow.md) — manifest-backed annual results and the cache contract.
6. [Electric-light workflow](workflows/electric-light-workflow.md) — IES conversion, electric annual runs, and composition.
7. [Complete component reference](components/README.md) — all 30 components by category, exact ports, defaults, units, wiring, workflow and revisions; [toolbar navigation](components/navigation.md) provides the short overview.
8. [Validation](quality/current-status.md) and [plugin audit](quality/plugin-audit.md) — automated checks, cleanup scope and remaining host/reference acceptance.

## Documentation rules

Recommended current wiring: [Loaded Result → inherited weather → automatic plot attributes](workflows/result-weather-plot-contract.md).
This supersedes manual UTC entry and manual reconstruction of plot titles/units.

Implemented scalar pathway: [Plant-light workflow and component migration](workflows/plant-light-workflow.md)
lists new and modified components, exact wiring, compatibility rules and open
acceptance gates. [The task plan](architecture/plant-light-workflow-plan.md)
also includes remaining, unimplemented work.

Research evidence: [Daylight, LED and horticultural spectral profiles](research/plant-light/README.md)
contains the deeper scientific report, original datasets, reproducible calculations
and proposed profile contract. Core embeds the CIE photopic weighting data;
18 reference source profiles are available through the Spectral Profile table,
with explicit limitations acknowledgment; they are not certified fixture presets.

- Document observable inputs, outputs, units, files, ownership, and failures.
- Use the [plugin audit](quality/plugin-audit.md) for current evidence; approved obsolete archive documents have been removed.
- Update docs and tests whenever a component contract, manifest, cache format, or tool requirement changes.
