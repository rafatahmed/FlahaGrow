# Radiance methods in FlahaGrow

This page describes the annual daylight method implemented by the compiled
plugin. It is an implementation record, not a claim that every model or
configuration has been numerically benchmarked. For how to operate it in
Grasshopper, see [the annual daylight workflow](../workflows/annual-workflow.md).

## Method at a glance

FlahaGrow uses a climate-based Radiance matrix workflow to calculate annual
daylight **illuminance** at sensor points. It then derives PPFD and DLI outside
Radiance:

```text
EPW ──► WEA ──► annual sky / sun matrices ───────────────────────┐
                                                                    ▼
exported Radiance scene ──► coefficient matrices ──► annual lux per sensor/hour
                                                                    │
                       PPFD factor [μmol·m⁻²·s⁻¹/lux] ─────────────┘
                                                                    ▼
                                                         PPFD per sensor/hour
                                                                    │
                                                        daily time integration
                                                                    ▼
                                                          DLI per sensor/day
```

The source scene represents geometry and Radiance modifiers. An EPW represents
weather. Sensor points define the evaluation plane. The result is a scalar,
photometric matrix: 8,760 rows (annual hours) by the number of sensor points.

## Inputs and time convention

**Annual Simulation** requires `envelope.rad`, `envelope.mat`, and
`envelope.blk` under `model/scene`, plus a source point grid under `model/grid`
unless points are connected directly. It snapshots these files into a new run
folder; the exporter’s source root is not modified.

The EPW reader accepts exactly 8,760 non-leap-year records. It rejects February
29, partial years, invalid dates, and negative DNI/DHI. It writes a WEA with:

- EPW direct normal irradiance and diffuse horizontal irradiance;
- each EPW hour converted from hour-ending to an `hour - 0.5` midpoint;
- longitude and time zone written with the sign convention expected by the
  generated WEA/Radiance path.

Consequently, the hourly index in the cache is zero-based (`0` through `8759`)
and describes this non-leap-year sequence. Do not use this workflow for a leap
year or a partial-period calculation.

## Matrix calculation

For each sensor part, the plugin generates an eight-stage Windows batch file.
It uses only the checked Radiance installation's executables when a typed
Radiance Environment is connected.

| Stage | Radiance tools | Produced term | Purpose |
| --- | --- | --- | --- |
| 1 | `gendaymtx -m <sky>` | `Weather_*.smx` | Annual sky matrix. Sky is Tregenza (`1`) or Reinhart (`4`). |
| 2 | `oconv`, `rfluxmtx -I+` | `illum_*.mtx` | Total scene-to-sensor daylight coefficients. |
| 3 | `dctimestep`, `rmtxop` | `annualR_*.ill` | Total annual daylight illuminance. |
| 4 | `oconv`, `rfluxmtx -I+` | `billum_*.mtx` | Black-scene, direct-only coefficients. |
| 5 | `gendaymtx -d`, `dctimestep`, `rmtxop` | `annualRd_*.ill` | Direct-daylight term that will be removed. |
| 6 | `cnt`, `rcalc`, `oconv`, `rcontrib` | `cdsDDS_*.mtx` | Discrete-sun coefficients. |
| 7 | `gendaymtx -5 0.533 -d`, `dctimestep`, `rmtxop` | `annualRs_*.ill` | Discrete direct-sun term. |
| 8 | `rmtxop` | `annualRfinal_*.ill` | Final annual illuminance matrix. |

The receiver scene passed to `rfluxmtx` contains one ground receiver and an
upper-hemisphere sky receiver (`h=r1` for Tregenza or `h=r4` for Reinhart).
The direct-daylight calculation sets `-ab 0`, so it represents direct light
without inheriting interreflection from the total calculation. The direct-sun
calculation uses a discrete sun source set from `reinsrc.cal` / `reinhart.cal`.

The final value is calculated for every hour and sensor as:

```text
annualRfinal = annualR − annualRd + annualRs
```

This total-minus-direct-plus-discrete-sun formulation preserves the total sky
and interreflected contribution while substituting the discrete-sun result for
the coarser direct term. It is the behavior of this plugin's generated batch;
do not replace or hand-edit a run's intermediate matrices and expect its
provenance validation to still apply.

## Radiance settings exposed by FlahaGrow

`Sky` accepts only `1` (Tregenza) and `4` (Reinhart). `Detail` maps the named
presets below to Radiance ambient parameters; a string containing a hyphen is
passed through as custom Radiance arguments after basic batch-syntax rejection.

| Detail | Total coefficient parameters |
| --- | --- |
| very low / `1` | `-lw .01 -ab 1 -ad 256` |
| low / `2` | `-lw .005 -ab 2 -ad 512` |
| mid / `3` | `-lw .002 -ab 2 -ad 1024` |
| high / `4` | `-lw .0015 -ab 3 -ad 1536` |
| very high / `5` | `-lw .001 -ab 3 -ad 2048` |

The plugin adds `-n` based on available processors, splitting that count across
four parts when the grid contains more than ten sensors. This split is an
execution optimization only: parts retain contiguous global sensor ordering and
are merged logically by the manifest/cache reader.

For the RGB-to-photometric reduction, each annual matrix calculation invokes:

```text
rmtxop -fa -t -c 47.4 119.9 11.6
```

The resulting one-component values are treated throughout the plugin as
illuminance in lux. The cache builder refuses matrices with a different header
shape, component count, expected hour/sensor dimensions, non-finite values, or
negative final illuminance.

## From illuminance to plant-light metrics

FlahaGrow's scalar photometric result does not retain the spectral information
needed to establish PPFD directly. Radiance 6 supports native spectral rendering,
but this implemented annual path does not use it. FlahaGrow uses a factor:

```text
PPFD [μmol/m²/s] = illuminance [lux] × factor [μmol/m²/s per lux]
```

Several readers/converters default to `0.0185`; the spectral selector instead
defaults to CIE-D65 `0.018043`. These are not interchangeable defaults.
Standard selections, legacy presets, or a custom spectral CSV can
provide a factor. The shared custom calculator now integrates photons over
400–700 nm and CIE photopic weighting over 360–830 nm, using linear resampling
and trapezoidal integration. The legacy table view remains 380–780 nm and
assumes energy input/zero tails with warnings; the new Spectral Profile requires
an explicit basis and coverage policy. The user must decide whether the factor represents the actual
daylight, glazing-transmitted light, electric light, or a combined condition.

DLI is then calculated per sensor and day:

```text
DLI [mol/m²/day] = Σ(PPFD sample [μmol/m²/s] × timestep [s]) / 1,000,000
```

At the normal 3,600-second timestep this sums 24 samples per day. **Annual
DLI** requires a complete non-leap year; cache-native DLI readers also require
a provenance-validated annual cache.

## Implementation map and limits

| Code area | Responsibility |
| --- | --- |
| `AnnualSimulationComponent` | Validates source inputs, writes the receiver/sun scenes and batch commands, launches/cancels parts. |
| `LadybugWea` | Strict EPW-to-WEA conversion. |
| `AnnualRun`, `AnnualPartStatus`, `AnnualMatrix` | Run ownership, declared-part state, matrix/header/value validation. |
| `AnnualResultCacheComponent`, `AnnualCacheData` | Atomic float32 cache creation and provenance checks before reading. |
| PPFD/DLI components | Lux-to-PPFD multiplication and daily integration. |

The methods above do not validate model geometry, Radiance material assignments,
sensor placement, spectral representativeness, crop targets, numerical
convergence, or a particular installed Radiance version. Address those with a
reference case and a Rhino-host study review before using results for design or
research.

For the research basis, reliability assessment, and an improvement roadmap, see
[Scientific PPFD/DLI assessment](ppfd-dli-scientific-assessment.md).
