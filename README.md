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
```

## Requirements

- Rhino with Grasshopper
- A Python component compatible with the scripts in `src/Code` (they use the Rhino/Grasshopper API and .NET Windows Forms)
- [Radiance](https://www.radiance-online.org/) available on the local machine

## Getting started

1. Open the intended Grasshopper definition in Rhino.
2. Set a writable simulation root with `01 Basic/01 Working Directory.py`.
3. Confirm Radiance is detected with `01 Basic/02 Radiance Version.py`.
4. Select the greenhouse materials, glazing, and electric luminaires from `02 3 Channel Prep`.
5. Run the annual RGB simulation workflow in `03 RGB Simulation`.
6. Evaluate annual PPFD/PAR and DLI outputs with `04 Result and Metrics`.

Generated simulation results are deliberately excluded from Git; retain only reusable code and library assets in commits.

## Current status

The current repository contains the three-channel/RGB workflow. Hyperspectral simulation is a planned extension; its interfaces and validation criteria are recorded in [the project frame](docs/project-frame.md).

## Compiled Grasshopper plugin

`src/FlahaGrow.Grasshopper` is the Rhino 8 / .NET 7 compiled plugin. It produces a `.gha` assembly with the **FlahaGrow** Grasshopper category and provides portable project/library setup, Radiance availability checks, Lux→PPFD conversion, annual DLI aggregation, DLI target assessment, and lighting-energy calculation.

See [the plugin development guide](docs/grasshopper-plugin.md) to build, install locally, debug, and package the add-on.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for development and validation guidance.
