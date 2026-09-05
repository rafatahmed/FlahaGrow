# FlahaGrow

FlahaGrow is a Grasshopper/Rhino workflow for annual greenhouse-lighting simulation. It brings greenhouse geometry, glazing and opaque materials, daylight, and electric grow lights into a plant-centred design process.

## What it supports

- Precise modelling of greenhouse geometry, material properties, daylight, and electric lighting.
- Plant-specific assessment through PPFD (photosynthetic photon flux density) and DLI (daily light integral).
- Standard three-channel lighting simulation, with a planned hyperspectral workflow.
- Annual evaluation across 8,760 hours, including daylight contribution, electric-light supplementation, DLI sufficiency/deficiency, and lighting-energy implications.

The intent is to improve greenhouse design and operations through better lighting performance, energy efficiency, and plant-health outcomes.

## Repository layout

```text
src/
  Code/
    01 Basic/                         Working-directory and Radiance checks
    02 3 Channel Prep/                Materials, luminaires, spectral conversion
    03 RGB Simulation/                Annual and point-in-time simulation workflow
    04 Result and Metrics/            PPFD/PAR metrics and visualisation
  Library/
    FlahaGrow_Library_Small/          Radiance materials, glazing, IES files, textures
    Materials/                        Tabular material data
docs/
  project-frame.md                    Scope, workflow, conventions, roadmap
  annual-workflow.md                  Tested annual Radiance workflow and troubleshooting
  legacy-compiled-comparison.md       Legacy/compiled input-output comparison matrix
```

## Requirements

- Rhino with Grasshopper
- [Radiance](https://www.radiance-online.org/) available locally. The tested Windows installation is Ladybug Tools Radiance.
- Ladybug Tools / Honeybee when creating the Radiance ModelToRad project and sensor grid.
- The legacy Python components remain in `src/Code` as an executable reference for ports that are not yet tested in the compiled plugin.

## Getting started

1. Build and install the compiled plugin, then open Grasshopper in Rhino.
2. Set a writable simulation root with **FlahaGrow → Setup → Simulation Paths**.
3. Confirm Radiance with **Radiance Status**.
4. Select materials, glazing, and luminaires from the **Materials** and **Electric Light** tabs.
5. Use the **Annual** workflow to run Radiance, build the annual cache, and inspect hourly or per-sensor illuminance.
6. Convert illuminance to PPFD and calculate DLI with the Metrics components.

For the required ModelToRad folder structure, annual command sequence, cache format, progress monitoring, visualisation guidance, and tested error resolutions, see [the annual workflow guide](docs/annual-workflow.md).
For legacy script names alongside their compiled components and exact input/output comparison, see [the comparison matrix](docs/legacy-compiled-comparison.md).

Generated simulation results are deliberately excluded from Git; retain only reusable code and library assets in commits.

## Current status

The current repository contains the three-channel/RGB workflow. Hyperspectral simulation is a planned extension; its interfaces and validation criteria are recorded in [the project frame](docs/project-frame.md).

## Compiled Grasshopper plugin

`src/FlahaGrow.Grasshopper` is the Rhino 8 / .NET 7 compiled plugin. It produces a `.gha` assembly with the **FlahaGrow** Grasshopper category and provides setup, material and luminaire selection, annual Radiance execution, cache creation, point-in-time and sensor illuminance readers, sensor/date-hour tools, PPFD conversion, annual DLI aggregation, DLI target assessment, and lighting-energy calculation.

See [the plugin development guide](docs/grasshopper-plugin.md) to build, install locally, debug, and package the add-on.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development and validation guidance.
