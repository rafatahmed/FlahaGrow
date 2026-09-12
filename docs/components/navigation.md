# Component catalog

Panels are a workflow aid. Source under `src/FlahaGrow.Grasshopper/Components` is authoritative for component names, parameters, and GUIDs.

| Panel | Components | Responsibility |
| --- | --- | --- |
| 00 Setup | Simulation Paths; Working Directory; Radiance Status | Resolve/create contexts and check Radiance. |
| 01 Materials | Facade, Frame, Ground, Concrete, Glazing | Select Radiance modifiers. |
| 02 Spectral | Spectral Profile; Select Spectral Factor; Load Spectral Data | Explicit source assumptions and calculated lux-to-PPFD ratios. |
| 03 Annual | Simulation; Progress; Load Result; Combine; readers; selectors; marker; plots | Create, validate, read, combine, display annual illuminance. |
| 04 Electric Light | IES selector/conversion; geometry; compilation; Electric Annual | Prepare fixtures and electric annual results. |
| 05 PPFD | Plant Light Context; Combine Plant Light; PPFD at Hour; Annual PPFD at Sensor; compatibility converters/readers | Bind per-source profiles and read estimated PPFD. |
| 06 DLI | DLI for Day; Annual DLI at Sensor; Annual DLI; DLI Hourly; DLI Each Sensor; DLI Target | Integrate photons with explicit daily/hourly distinction. |
| 07 Energy | Lighting Energy | Integrate watt schedules into kWh and hours. |

## Daylight PPFD and DLI components

Radiance-facing Annual components emit illuminance in lux. The following
components make plant-light metrics from that result; none of them changes the
underlying Radiance run.

| Need | Use | Input/output contract |
| --- | --- | --- |
| One calculated lux value | **Lux to PPFD** | `PPFD = lux × factor`; output is μmol/m²/s. |
| All sensors at a selected hour | **Hourly PAR** | Reads a validated `.f32` cache row by zero-based hour, then applies the factor. |
| One sensor through the year | **PAR Each Sensor** | Reads a validated cache column by zero-based sensor index, then applies the factor. |
| Lux already in Grasshopper | **Hourly PPFD** / **PPFD Each Sensor** | Applies the same multiplication to a supplied list. The latter expects 8,760 values and warns otherwise. |
| 365 daily values from PPFD | **Annual DLI** | Requires exactly `365 × (86,400 / timestep)` non-negative PPFD samples and returns daily DLI plus its annual mean. |
| Cache-native daily result | **DLI Hourly** / **DLI Each Sensor** | Reads validated cache values, converts with its factor, and aggregates 24-hour blocks. |

The factor unit is **μmol/m²/s per lux**. It is spectrum-specific: FlahaGrow
ships a default factor of `0.0185`, legacy text presets, standard selectable
sources, and a custom spectral-CSV calculator. These are alternative factor
sources, not a proof that one value represents mixed daylight and electric
lighting. Use the factor matching the light being evaluated and retain its
source with results.

For samples with duration `dt` seconds, **Annual DLI** computes, for each day:

```text
DLI = Σ(PPFD sample × dt) / 1,000,000
```

With hourly samples, `dt = 3,600` and each daily sum contains 24 samples. This
converts micromoles per square metre to moles per square metre per day. It does
not fill missing hours, accept leap years, or apply a crop target; **DLI
Target** only compares a completed 365-value series to a target.

## Plant-light implementation update

The new recommended scalar-analysis components and their exact ports are listed
in [Plant-light workflow and component migration](../workflows/plant-light-workflow.md).
They coexist with compatibility components; existing GUIDs and port order are
retained. Native spectral transport and fixture-profile promotion remain separate
work, not implied by the new component names.
