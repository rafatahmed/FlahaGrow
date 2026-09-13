# System architecture

```text
Grasshopper definition
        │
        ▼
FlahaGrow.Grasshopper     component UI, persistence, typed GH connections
        │
        ▼
FlahaGrow.Core            workspace, Radiance discovery, annual contracts
        │                         │
        │                         ├── Radiance executables and calculation library
        │                         └── filesystem run folders and caches
        ▼
src/Library               bundled material, glazing, and IES assets
```

`src/FlahaGrow.Grasshopper` builds `FlahaGrow.gha` for Rhino 8 on Windows. It references `FlahaGrow.Core` and Grasshopper; spectral calculations run in Core. `src/FlahaGrow.Core` targets plain `net7.0` and has no Grasshopper dependency. `src/Code` is historical GhPython source outside the compiled build; it is intentionally preserved at the user’s request and is not registered or packaged as plugin components.

| Concern | Owner / contract |
| --- | --- |
| Grasshopper state | Component GUIDs identify saved nodes; revisions are persisted per node. |
| Project metadata | Core workspace services validate `flahagrow.project.json` and `analysis.json`. |
| Annual provenance | A schema-2 `flahagrow.run.json` declares input hashes, sensor order, and parts. |
| Radiance installation | User-selected/discovered installation must pass file, version, and small workflow checks. |
| Model translation | External exporter; FlahaGrow accepts a filesystem source root only. |
| Assets | `FlahaGrow_Library_Small` contains `RadMaterials`, `RadGlazing`, and `RadIES`. |

The plugin invokes Radiance directly with argument lists or generated batch files. It does not invoke Ladybug, Honeybee, `pip`, or an arbitrary Python environment. A checked `Radiance Environment` wire pins execution to that ready installation. Annual runs are new GUID folders, separate from the model-export root; the runner snapshots inputs before execution.

## Daylight-to-plant-metric data flow

Radiance is the daylight engine, but it does not produce PPFD or DLI directly
in this plugin. The compiled annual path has three distinct stages:

```text
EPW + exported Radiance scene + sensor points
                   │
                   ▼
Radiance annual calculation ───────────► hourly sensor illuminance [lux]
                   │                                  │
                   │                                  ▼
                   │                     spectral conversion factor [μmol·m⁻²·s⁻¹/lux]
                   │                                  │
                   │                                  ▼
                   └──────────────────────────────► PPFD [μmol·m⁻²·s⁻¹]
                                                      │
                                                      ▼
                          sum each day's PPFD × sample duration / 1,000,000
                                                      │
                                                      ▼
                                              DLI [mol·m⁻²·day⁻¹]
```

This separation is deliberate. The annual Radiance result is a traceable,
validated illuminance field. The PPFD result is only as applicable as the
factor chosen for the active light spectrum. DLI is arithmetic aggregation of
the chosen PPFD values, not a separate Radiance simulation. This means users
must record the spectral source/factor, sensor plane, weather/model inputs, and
time convention along with any plant-light result.

See [Radiance methods](radiance-methods.md), [the model boundary](ladybug-honeybee-integration.md), and [run/cache contract](../workflows/annual-run-isolation.md).
