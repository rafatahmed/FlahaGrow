# Component navigation

The plugin has 30 executable components. The [complete reference](README.md) records every registered port and GUID.

| Panel | Count | Purpose |
| --- | --- | --- |
| [00 Setup](00-setup.md) | 3 | Resolve paths, initialize a workspace and verify Radiance |
| [01 Materials](01-materials.md) | 2 | Select opaque or glazing modifiers |
| [02 Spectral](02-spectral.md) | 2 | Select a reference profile or declare a custom profile |
| [03 Annual](03-annual.md) | 8 | Simulate, monitor, load, combine, read, select time, mark sensors and plot |
| [04 Electric Light](04-electric-light.md) | 5 | Select IES, convert, position, compile and simulate luminaires |
| [05 PPFD](05-ppfd.md) | 5 | Bind/compose source contexts, read hourly/sensor PPFD and convert numeric lux |
| [06 DLI](06-dli.md) | 4 | Read daily/sensor DLI, integrate supplied PPFD and compare a target |
| [07 Energy](07-energy.md) | 1 | Integrate an electrical power schedule |

Use one Opaque Material component per independently selected surface material. Each instance opens the same library; surface labels do not need separate component implementations. Glazing uses a distinct optical model.

Read Illuminance has a Mode input: `hour` returns one value per sensor; `sensor` returns an annual series. Annual Plot accepts illuminance, PPFD or DLI with matching Plot Attributes. Connect the values and attributes from the same annual sensor reader.

For plant-light results, connect Load Annual Result.Result and a source-specific Profile to Plant Light Context. Its four independent readers provide PPFD at Hour, Annual PPFD at Sensor, DLI for Day and Annual DLI at Sensor. Combine Plant Light adds compatible source contexts after per-source conversion.

Lux to PPFD handles numeric lux values, including lists through Grasshopper item matching. Annual DLI integrates a supplied complete numeric PPFD series with an explicit timestep. These numeric tools support data outside the annual-cache workflow.

See the [plant-light workflow](../workflows/plant-light-workflow.md) for wiring and the [update contract](migration.md) before opening older definitions.
