# FlahaGrow

FlahaGrow is a Rhino/Grasshopper plugin for annual greenhouse-lighting workflows using Radiance. It supports daylight and electric-light studies, validated annual-result caches, PPFD/DLI analysis, material selection, and lighting-energy calculation.

## Start here

1. Read the [setup guide](docs/getting-started/setup.md) to configure project paths, a workspace, and Radiance.
2. Follow the [annual workflow](docs/workflows/annual-workflow.md) to prepare a Honeybee ModelToRad export, launch an annual run, monitor it, and load validated results.
3. Use the [component navigation](docs/components/navigation.md) to find the correct Grasshopper panel and distinguish similarly named PPFD/DLI components.
4. Check [current status](docs/quality/current-status.md) before treating any simulation result as validated for design or scientific use.
5. Use the [loaded-result and automatic-plot workflow](docs/workflows/result-weather-plot-contract.md): no manual UTC input is needed for verified daylight results.

## Grasshopper panels

`00 Setup → 01 Materials → 02 Spectral → 03 Annual → 04 Electric Light → 05 PPFD → 06 DLI → 07 Energy`

The numbered order is intentional. Component GUIDs and ports are stable; moving a component between panels does not break a saved definition.

## Complete component catalog

This catalog lists the 45 placeable FlahaGrow components in the current toolbar. Use the ordered panels rather than the old mixed **Metrics** panel. For new PPFD/DLI definitions, follow the [typed plant-light workflow](docs/workflows/plant-light-workflow.md).

| Panel | Component | Role | Icon |
|---|---|---|---|
| 00 Setup | Simulation Paths | Resolve project and bundled-library paths. | Embedded PNG |
| 00 Setup | Working Directory | Open or initialize a project and named analysis. | Embedded PNG |
| 00 Setup | Radiance Status | Discover and check the selected Radiance installation. | Embedded PNG |
| 01 Materials | Opaque Material | Select an opaque façade modifier. | Embedded PNG |
| 01 Materials | Glazing Material | Select a glazing modifier. | Embedded PNG |
| 02 Spectral | Spectral Profile | Select one of 18 bundled references using a Button and table; no CSV required. | Embedded PNG |
| 02 Spectral | Custom Spectral Profile | Declare a custom factor or derive one from an explicit spectral CSV; preserves the original five-input profile component. | Embedded PNG |
| 03 Annual | Annual Simulation | Prepare and launch an annual daylight Radiance run. | Embedded PNG |
| 03 Annual | Annual Simulation Progress | Read per-part run progress and batch stages. | Embedded PNG |
| 03 Annual | Load Annual Result | Validate, merge, and cache an annual result. | Embedded PNG |
| 03 Annual | Combine Annual Lighting | Combine compatible completed daylight and electric runs. | Not supplied |
| 03 Annual | Read Illuminance | Read one sensor's annual illuminance series. | Not supplied |
| 03 Annual | Select Date and Hour | Convert a date and hour to an annual hour index. | Not supplied |
| 03 Annual | Sensor Marker | Create a viewport marker for a sensor. | Not supplied |
| 03 Annual | Annual Plot | Plot annual illuminance data. | Not supplied |
| 04 Electric Light | Select IES Luminaire | Choose an IES grow-light luminaire. | Embedded PNG |
| 04 Electric Light | IES to Radiance | Convert an IES luminaire to Radiance files. | Embedded PNG |
| 04 Electric Light | Lighting Geometry | Position Radiance luminaires at points. | Embedded PNG |
| 04 Electric Light | Compile Luminaires | Compile luminaires into a Radiance file. | Embedded PNG |
| 04 Electric Light | Electric Annual Simulation | Run an annual electric-light Radiance calculation. | Embedded PNG |
| 05 PPFD | Lux to PPFD | Convert illuminance to PPFD. | Embedded PNG |
| 05 PPFD | Plant Light Context | Bind validated annual illuminance to its source-specific spectral profile. | Embedded PNG |
| 05 PPFD | Combine Plant Light | Combine compatible contexts after source-specific conversion. | Embedded PNG |
| 05 PPFD | PPFD at Hour | Read one hour of PPFD for all sensors from a context. | Embedded PNG |
| 05 PPFD | Annual PPFD at Sensor | Read annual PPFD for one sensor from a context. | Not supplied |
| 06 DLI | Annual DLI | Aggregate annual PPFD into 365 daily DLI values. | Embedded PNG |
| 06 DLI | DLI for Day | Read daily DLI and hourly photon integrals for all sensors. | Embedded PNG |
| 06 DLI | Annual DLI at Sensor | Read 365 daily DLI values for one context sensor. | Embedded PNG |
| 06 DLI | DLI Target | Compare daily DLI values with a crop target. | Not supplied |
| 07 Energy | Lighting Energy | Integrate a power schedule into energy and operating hours. | Embedded PNG |

### Compatibility components

The plugin registers 30 visible components. Superseded and duplicate component implementations have been removed. Older definitions using those identities require the replacements in the [migration guide](docs/components/migration.md).

### Component icons

The plugin embeds 23 named component icons and the FlahaGrow logo from the supplied PNG artwork. Icons are rendered and cached at 24 × 24 pixels; the 267 × 267 originals remain unchanged. Components without matching artwork retain the existing fallback. See the [icon mapping](docs/components/icons.md).

## Documentation

- [Getting started](docs/getting-started/setup.md) — operator setup and workspace use.
- [Architecture](docs/architecture/project-frame.md) — project scope, plugin development, and Ladybug/Honeybee boundary.
- [Components](docs/components/navigation.md) — navigation, I/O migration, and revision policy.
- [Workflows](docs/workflows/annual-workflow.md) — annual simulation, result ownership, cache, and troubleshooting.
- [Quality](docs/quality/current-status.md) — release status, acceptance evidence, delivery gate, and known boundaries.
- [Releases](docs/releases/v0.1.0-preliminary.1.md) — preliminary milestone notes.
- [Plugin audit](docs/quality/plugin-audit.md) — current cleanup scope, fixes and validation evidence.

## Development

The compiled plugin is in `src/FlahaGrow.Grasshopper`; shared logic is in `src/FlahaGrow.Core`; legacy GhPython scripts remain under `src/Code` as behavioral references. See [plugin development](docs/architecture/plugin-development.md) and [CONTRIBUTING.md](CONTRIBUTING.md).

## Release boundary

The current preliminary release has passed source and automated checks, but several Rhino-host, spectral-parity, electric-integration, and broader numerical-reference gates remain open. See [current status](docs/quality/current-status.md) and the [delivery gate](docs/quality/delivery-gate.md).
