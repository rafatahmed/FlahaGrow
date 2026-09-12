# FlahaGrow

FlahaGrow is a Rhino/Grasshopper plugin for annual greenhouse-lighting workflows using Radiance. It supports daylight and electric-light studies, validated annual-result caches, PPFD/DLI analysis, material selection, and lighting-energy calculation.

## Start here

1. Read the [setup guide](docs/getting-started/setup.md) to configure project paths, a workspace, and Radiance.
2. Follow the [annual workflow](docs/workflows/annual-workflow.md) to prepare a Honeybee ModelToRad export, launch an annual run, monitor it, and load validated results.
3. Use the [component navigation](docs/components/navigation.md) to find the correct Grasshopper panel and distinguish similarly named PPFD/DLI components.
4. Check [current status](docs/quality/current-status.md) before treating any simulation result as validated for design or scientific use.

## Grasshopper panels

`00 Setup → 01 Materials → 02 Spectral → 03 Annual → 04 Electric Light → 05 PPFD → 06 DLI → 07 Energy`

The numbered order is intentional. Component GUIDs and ports are stable; moving a component between panels does not break a saved definition.

## Complete component catalog

This catalog lists the 44 placeable FlahaGrow components in the current toolbar. Use the ordered panels rather than the old mixed **Metrics** panel. For new PPFD/DLI definitions, follow the [typed plant-light workflow](docs/workflows/plant-light-workflow.md).

| Panel | Component | Role | Icon |
|---|---|---|---|
| 00 Setup | Simulation Paths | Resolve project and bundled-library paths. | Not implemented |
| 00 Setup | Working Directory | Open or initialize a project and named analysis. | Not implemented |
| 00 Setup | Radiance Status | Discover and check the selected Radiance installation. | Not implemented |
| 01 Materials | Facade Material | Select an opaque façade modifier. | Not implemented |
| 01 Materials | Frame Material | Select an opaque frame modifier. | Not implemented |
| 01 Materials | Ground Material | Select an opaque ground modifier. | Not implemented |
| 01 Materials | Concrete Material | Select an opaque concrete modifier. | Not implemented |
| 01 Materials | Glazing Material | Select a glazing modifier. | Not implemented |
| 02 Spectral | Select Spectral Factor | Choose a standard or custom spectral conversion factor. | Not implemented |
| 02 Spectral | Load Spectral Data | Open a spectral-data table and calculate its conversion factor. | Not implemented |
| 02 Spectral | Spectral Profile | Declare a conversion factor or derive one from an explicit spectral CSV. | Not implemented |
| 02 Spectral | Select Spectral Factor (Legacy) | Preserve older spectral definitions; do not use for new work. | Not implemented |
| 03 Annual | Annual Simulation | Prepare and launch an annual daylight Radiance run. | Not implemented |
| 03 Annual | Annual Simulation Progress | Read per-part run progress and batch stages. | Not implemented |
| 03 Annual | Load Annual Result | Validate, merge, and cache an annual result. | Not implemented |
| 03 Annual | Combine Annual Lighting | Combine compatible completed daylight and electric runs. | Not implemented |
| 03 Annual | Illuminance Point in Time | Read one point-in-time illuminance result. | Not implemented |
| 03 Annual | Illuminance Sensor | Read one sensor's annual illuminance series. | Not implemented |
| 03 Annual | Select Date and Hour | Convert a date and hour to an annual hour index. | Not implemented |
| 03 Annual | Select PIT to PPFD | Select an hour index for point-in-time PPFD. | Not implemented |
| 03 Annual | Sensor Marker | Create a viewport marker for a sensor. | Not implemented |
| 03 Annual | Annual Plot | Plot annual illuminance data. | Not implemented |
| 03 Annual | Annual Plot PPFD for Sensor | Plot annual PPFD or DLI data for one sensor. | Not implemented |
| 04 Electric Light | Select IES Luminaire | Choose an IES grow-light luminaire. | Not implemented |
| 04 Electric Light | IES to Radiance | Convert an IES luminaire to Radiance files. | Not implemented |
| 04 Electric Light | Lighting Geometry | Position Radiance luminaires at points. | Not implemented |
| 04 Electric Light | Compile Luminaires | Compile luminaires into a Radiance file. | Not implemented |
| 04 Electric Light | Electric Annual Simulation | Run an annual electric-light Radiance calculation. | Not implemented |
| 05 PPFD | Lux to PPFD | Convert illuminance to PPFD. | Not implemented |
| 05 PPFD | Plant Light Context | Bind validated annual illuminance to its source-specific spectral profile. | Not implemented |
| 05 PPFD | Combine Plant Light | Combine compatible contexts after source-specific conversion. | Not implemented |
| 05 PPFD | PPFD at Hour | Read one hour of PPFD for all sensors from a context. | Not implemented |
| 05 PPFD | Annual PPFD at Sensor | Read annual PPFD for one sensor from a context. | Not implemented |
| 05 PPFD | Hourly PPFD | Convert selected-hour illuminance values to PPFD. | Not implemented |
| 05 PPFD | PPFD Each Sensor | Convert one sensor's annual illuminance series to PPFD. | Not implemented |
| 05 PPFD | Hourly PAR | Read and convert one annual-cache hour for all sensors. | Not implemented |
| 05 PPFD | PAR Each Sensor | Read and convert one annual-cache sensor series. | Not implemented |
| 06 DLI | Annual DLI | Aggregate annual PPFD into 365 daily DLI values. | Not implemented |
| 06 DLI | DLI for Day | Read daily DLI and hourly photon integrals for all sensors. | Not implemented |
| 06 DLI | Annual DLI at Sensor | Read 365 daily DLI values for one context sensor. | Not implemented |
| 06 DLI | DLI Hourly | Read a selected day's hourly DLI for all sensors. | Not implemented |
| 06 DLI | DLI Each Sensor | Read daily DLI for one annual-cache sensor. | Not implemented |
| 06 DLI | DLI Target | Compare daily DLI values with a crop target. | Not implemented |
| 07 Energy | Lighting Energy | Integrate a power schedule into energy and operating hours. | Not implemented |

### Compatibility components

Six registered components are deliberately hidden from the toolbar so existing definitions can still open: **Simulation Paths** (legacy), **Working Directory** (legacy), **Radiance Status** (legacy), **Radiance Version**, **Simulation Paths (Project)**, and **Radiance Status (Project)**. **Select Spectral Factor (Legacy)** remains placeable only to preserve older spectral definitions; use **Select Spectral Factor** for new work.

### Component icons

There is currently no implemented per-component icon set in the plugin source. This catalog is therefore the complete list of components, not an icon inventory. Each component does already expose its version and exact update time on the canvas and in its right-click menu. A visual icon system should be added as a separate, deliberate design task so that the icon meaning is consistent across all 44 placeable components.

## Documentation

- [Getting started](docs/getting-started/setup.md) — operator setup and workspace use.
- [Architecture](docs/architecture/project-frame.md) — project scope, plugin development, and Ladybug/Honeybee boundary.
- [Components](docs/components/navigation.md) — navigation, I/O migration, and revision policy.
- [Workflows](docs/workflows/annual-workflow.md) — annual simulation, result ownership, cache, and troubleshooting.
- [Quality](docs/quality/current-status.md) — release status, acceptance evidence, delivery gate, and known boundaries.
- [Releases](docs/releases/v0.1.0-preliminary.1.md) — preliminary milestone notes.
- [Archive](docs/archive/) — dated audits and superseded implementation reports retained as evidence.

## Development

The compiled plugin is in `src/FlahaGrow.Grasshopper`; shared logic is in `src/FlahaGrow.Core`; legacy GhPython scripts remain under `src/Code` as behavioral references. See [plugin development](docs/architecture/plugin-development.md) and [CONTRIBUTING.md](CONTRIBUTING.md).

## Release boundary

The current preliminary release has passed source and automated checks, but several Rhino-host, spectral-parity, electric-integration, and broader numerical-reference gates remain open. See [current status](docs/quality/current-status.md) and the [delivery gate](docs/quality/delivery-gate.md).
