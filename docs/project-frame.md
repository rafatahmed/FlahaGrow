# FlahaGrow project frame

## Purpose

FlahaGrow supports greenhouse lighting decisions by simulating the combined effects of daylight and electric grow lighting on plant-relevant metrics. It is designed for early design studies through operational assessments.

## Core outcomes

| Outcome | Resolution | Use |
| --- | --- | --- |
| Illuminance / lighting result | Point-in-time or annual | Foundation for conversion and visual checks |
| PPFD | Hourly, per sensor | Plant-usable light evaluation |
| DLI | Daily, per sensor | Plant target sufficiency or deficiency |
| Supplemental electric lighting | Hourly and annual | Meet target PPFD/DLI with daylight contribution |
| Energy implication | Annual | Compare operational lighting strategies |

## Workflow

```text
Geometry + materials + glazing + luminaire photometry
                         |
                         v
             Three-channel Radiance simulation
                         |
                         v
      Annual illuminance results (8,760 hours / sensors)
                         |
                         v
          Spectral conversion to PPFD and daily DLI
                         |
                         v
    Sufficiency/deficiency, supplementation, energy assessment
```

## Near-term roadmap

1. Maintain and validate the three-channel/RGB annual workflow.
2. Make library paths configurable rather than relying on machine-specific defaults.
3. Define a hyperspectral data model: wavelength sampling, source spectral power distributions, material spectral reflectance/transmittance, and sensor action spectra.
4. Add verification cases that compare PPFD and DLI results against controlled reference scenarios.
5. Standardise energy reporting assumptions: luminaire power, dimming/control logic, operating schedules, and units.

## Modelling assumptions to document per study

- Site, weather file, orientation, and simulation timestep.
- Greenhouse geometry and material/glazing assignments.
- Luminaire IES file, spectrum, wattage, placement, and control schedule.
- Sensor grid, measurement plane, and plant growth stage/species target.
- PPFD-to-DLI aggregation method and sufficiency thresholds.

## Definition of done for a simulation feature

A feature is complete when it runs in Grasshopper, produces traceable outputs with units, has a small reference scenario, and documents its input assumptions and known limitations.
