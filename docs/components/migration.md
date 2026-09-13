# Updating Grasshopper definitions

The current catalog contains 30 executable components. Only current implementations are registered; there are no hidden executable aliases. The eight typed wire parameter classes are used by registered input/output ports and are excluded from toolbar exposure.

Component consolidation is a breaking update for saved definitions that reference removed identities. The date/hour selector also has a new GUID because its unused UTC input was removed: its inputs are now Run and Result. There is no automatic GUID or wire migration.

Use the [current component reference](README.md) to rebuild affected definitions. Connect ports by name and meaning rather than their previous positions. Keep a copy of the original definition until the rebuilt workflow has been checked.

| Task | Current component |
| --- | --- |
| Resolve paths and initialize a study | Simulation Paths → Working Directory |
| Verify engine readiness and read its version | Radiance Status |
| Select any opaque surface modifier | Opaque Material |
| Select transparent glazing | Glazing Material |
| Choose a referenced source or custom spectral assumption | Spectral Profile / Custom Spectral Profile |
| Read illuminance by hour or sensor | Read Illuminance |
| Select weather-aligned time | Select Date and Hour |
| Read source-aware PPFD/DLI | Plant Light Context and its four readers |
| Convert supplied numeric lux or integrate supplied numeric PPFD | Lux to PPFD / Annual DLI |
| Display any supported annual quantity | Annual Plot with matching Data and Plot |
| Mark a selected sensor | Sensor Marker |

Unmanifested annual data cannot be adopted as validated caches; regenerate it through the current simulation and loader workflow. A rebuilt assembly does not update an installed Rhino plugin automatically.

Lux to PPFD now requires a supplied Factor. A saved component may retain its previous persistent numeric value; verify it against the intended source spectrum. Explicit Radiance paths are authoritative: correct an invalid path or remove the override instead of relying on fallback to another installation.

## Conversion and validation hardening (2026-09-13)

IES to Radiance retains its GUID and port order, but Run now uses a false-to-true edge and background execution. Use a Button. Idle evaluation creates no folders. Leave Multiplier unwired for the converter default; zero is rejected. RGB must be finite, nonnegative and not all zero. A supplied DAT must exist. Changed inputs cancel pending conversion and require a new Run pulse. Illuminance modes are strictly hour/sensor; invalid plot attributes, nonfinite DLI/energy inputs and invalid placement geometry now report errors.
