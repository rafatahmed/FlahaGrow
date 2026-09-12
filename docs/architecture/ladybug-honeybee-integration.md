# Model-export integration boundary

FlahaGrow consumes a pre-exported Radiance model root. It does not load Ladybug/Honeybee objects, parse HBJSON, or depend on a Python package. “Honeybee ModelToRad” describes a convenient export shape, not runtime integration.

**Annual Simulation** requires this source-root shape:

```text
model/
  scene/envelope.rad
  scene/envelope.mat
  scene/envelope.blk
  grid/0.pts                 # another .pts is used if 0.pts is absent
```

An optional `Pts` input replaces the source grid for that run only. The EPW input must contain exactly 8,760 records; leap-year and partial EPWs are rejected. The core writes a non-leap, hour-midpoint `weather.wea` in the run folder before `gendaymtx`.

Passing these checks does not certify scene correctness, material semantics, or sensor ordering. Retain exporter version, model, weather source, orientation, grid, materials, and Radiance settings with each study. Treat changed source inputs as a new run, not an edit to an existing snapshot.
