# Ladybug Tools integration boundary

Updated 2026-09-11. FlahaGrow does not load Ladybug or Honeybee Python objects inside Rhino/.NET. It interoperates at explicit, versioned filesystem boundaries so a Python-environment change cannot silently alter a saved Grasshopper definition.

## Responsibility boundary

| System | Authoritative responsibility | FlahaGrow contract |
| --- | --- | --- |
| Ladybug Core | EPW parsing, weather data and WEA semantics | FlahaGrow snapshots the EPW and writes a strict non-leap-year WEA with the same midpoint timestamp, DNI/DHI, longitude and time-zone conventions before `gendaymtx`. |
| Ladybug Radiance | Climate-based sky/radiation utilities | FlahaGrow uses the same `gendaymtx` WEA contract but owns its own declared run, commands, state and result validation. |
| Honeybee Core | Building-model representation | The Honeybee model is exported before entering FlahaGrow. FlahaGrow does not deserialize HBJSON or infer model identity from Rhino geometry. |
| Honeybee Radiance | Radiance translation and daylight workflows | FlahaGrow accepts its ModelToRad-style source root only when the required scene and sensor files are present, then snapshots them into a manifest-owned run. |

## Required source-root contract

`Annual Simulation` accepts a Honeybee-exported root only if it contains:

```text
model/scene/envelope.rad
model/scene/envelope.mat
model/scene/envelope.blk
model/grid/*.pts
```

The returned FlahaGrow run folder is a separate immutable calculation snapshot. Never point Honeybee at a FlahaGrow run folder as an export destination, and never edit a snapshot to “refresh” a Honeybee model.

## Weather and time semantics

The annual daylight workflow accepts exactly 8,760 non-leap-year EPW records. It rejects leap-day or partial EPWs rather than shifting annual indices. It converts EPW hour-ending records to WEA midpoint values (`hour - 0.5`) and writes the WEA into the run before Radiance starts. This matches the Ladybug climate-based-sky input convention and makes the exact weather input inspectable without depending on `epw2wea` behavior.

## Security and reproducibility

- Radiance executable paths are checked as files in one selected installation; FlahaGrow does not search arbitrary scripts at launch time.
- The run manifest hashes scene and weather inputs, declares expected parts and sensor ordering, and cache readers require that provenance.
- Commands use fixed tools and generated arguments. Detail rejects shell metacharacters; run folders are isolated and cannot traverse links.
- FlahaGrow does not run `honeybee`, `ladybug`, `pip`, or Python from Grasshopper. Updating those tools therefore requires a new Honeybee export and a new FlahaGrow run, not an invisible mutation of a previous study.

## Verification boundary

FlahaGrow has source tests for WEA conversion, run ownership, command structure and result provenance. A real Rhino definition still needs host acceptance after a Ladybug Tools or Honeybee update: export a fresh model, verify the source-root contract, run annual daylight, save/reopen, then compare the manifest and cache status.
