# Plant-light workflow and implementation plan

Status: first scalar implementation added with authorization, 2026-09-12.
See [implemented components and current wiring](../workflows/plant-light-workflow.md)
for the actual contract; the original proposed contracts below are retained as
design context. Not all backlog items are complete.

PL-04/07 have Core services and readers with tests; PL-06 has typed adapters;
PL-09 has source-specific combination with user-declared time alignment.
PL-05 implements conservative content validation, not cross-reader validation
reuse or cancellation. PL-08 host migration, PL-10 physical validation,
PL-12 spectral transport and PL-13 profile promotion remain open. PL-11/14 have
initial documentation and status labels, not a rebuilt/accepted Rhino example.

## 1. What the example establishes

Annual Simulation produces annual illuminance. Load Annual Result builds or
opens the validated lux cache. Illuminance, PPFD, and DLI readers can consume
that cache independently. DLI readers may calculate PPFD internally; a visible
wire from a PPFD output to a DLI component is not mathematically necessary.

The canvas and six supplied result images are a working FlahaGrow example.
They establish the three result branches, not independent physical accuracy.
Exact GUIDs and saved wires remain a migration check when the GH/GHX file is
available; this is not a prerequisite for agreeing the architecture.

The [deep research and reference-data package](../research/plant-light/README.md)
extends the scientific assessment. First-release profile scope is daylight,
white LEDs and horticultural LEDs. It contains research candidates, not newly
enabled plugin presets or validated commercial fixtures.

In current compiled source, Hourly PAR and PAR Each Sensor already appear in
05 PPFD. They return PPFD despite the PAR names. Hourly PPFD and PPFD Each
Sensor are alternative numeric-list converters. The example does not need both
the cache readers and the list converters for the same result.

## 2. Vocabulary and result shapes

| Quantity | Meaning in this design | Unit / shape |
| --- | --- | --- |
| Illuminance | Photometric result from the annual simulation | lux; one value per sensor per hour |
| PAR | Traditional 400–700 nm spectral band; requires a stated quantity to give a numeric result | Not a standalone numeric output name |
| PAR irradiance | Radiant power within PAR, if implemented later | W/m²; not currently obtained from the scalar lux cache |
| PPFD | Incident photon flux density within PAR, currently estimated using a factor | μmol/m²/s |
| DLI | Integral of PPFD across a full day | mol/m²/day |
| Hourly photon integral | Contribution of one hourly interval to DLI | mol/m² over that interval; not instantaneous PPFD |

For a scalar factor f, PPFD = lux × f. For hourly samples, DLI =
sum(PPFD × 3600) / 1,000,000. DLI's conceptual dependence on PPFD does not
require materializing an annual PPFD matrix on the canvas.

An hour selection returns S sensor values; a sensor selection returns 8,760
hour values. A day selection returns S daily totals plus optional 24-value
branches per sensor; an annual sensor DLI selection returns 365 daily values.
Do not feed the S values of one hour into Annual DLI as if they were a time
series. Spatial and temporal lists must have explicit descriptions.

## 3. Recommended target workflow

Preserve the independent result branches. Bind the annual result and the
conversion assumption once, using a lightweight analysis context:

```text
Annual Simulation → Load Annual Result → Annual Illuminance Result
                                           ├─ Illuminance at Hour
                                           ├─ Annual Illuminance at Sensor
                                           │
Spectral Factor Profile ────────────────────┤
                                           ▼
                                  Plant Light Context
                                   ├─ PPFD at Hour
                                   ├─ Annual PPFD at Sensor
                                   ├─ DLI for Day
                                   └─ Annual DLI at Sensor
```

All names and types above are proposed. Plant Light Context is a descriptor of
a validated lux source and an explicit factor profile, not a precomputed
sensor-by-hour PPFD matrix. Its readers request only the needed slices through
one shared calculation service. DLI readers apply the same profile internally.
This preserves the example's branches while preventing accidental use of
different factors for PPFD and DLI in the same analysis.

Alternatives considered:

| Option | Benefit | Cost / decision |
| --- | --- | --- |
| Raw cache + separate factor on every reader | Fewest changes; familiar canvas | Repeated assumptions and validation; keep as compatibility path |
| Lux list → PPFD list → DLI list | Transparent arithmetic | Useful for one sensor or supplied data; poor primary path for an entire annual grid |
| Shared context + independent readers | One factor binding, provenance, slice-based reading | Adds one setup component/type; recommended target |

## 4. Proposed component contracts

Inputs/outputs below are conceptual contracts to freeze before implementation.
Existing ports remain unchanged until migration rules are agreed.

| Panel / component | Required inputs | Main outputs |
| --- | --- | --- |
| 03 Annual / Load Annual Result | Existing Folder and Build contract | Keep F32, S, H, Status; consider appending typed Result after archive tests |
| 03 Annual / Illuminance at Hour | Result, Hour index | Lux[S], time, status |
| 03 Annual / Annual Illuminance at Sensor | Result, Sensor index | Lux[8760], sensor identity, status |
| 02 Spectral / Spectral Factor Profile | Explicit preset, CSV, or numeric factor with source label | Typed Profile, numeric factor, provenance/status |
| 05 PPFD / Plant Light Context | Result, Profile | Typed Context, method/status |
| 05 PPFD / PPFD at Hour | Context, Hour index | Estimated PPFD[S], time, status |
| 05 PPFD / Annual PPFD at Sensor | Context, Sensor index | Estimated PPFD[8760], sensor identity, status |
| 06 DLI / DLI for Day | Context, Day index | Estimated DLI[S], optional hourly-integral tree, date/status |
| 06 DLI / Annual DLI at Sensor | Context, Sensor index | Estimated DLI[365], mean, sensor identity, status |

Keep Lux to PPFD and Annual DLI as general numeric utilities. Retain old list
converters and cache readers for saved definitions; choose toolbar visibility
after the example audit. Do not automatically remove all 05 PPFD components.
Keep sensor markers and plotting as separate presentation operations for new
readers; preserve old combined marker ports in compatibility components.

New readers reject invalid indices. Day index is explicitly 0–364; an adapter
can map an hour index with floor(hour / 24). Hour selection is an annual hourly
interval, not a new instantaneous Radiance solve. Time labels should make the
EPW midpoint convention visible.

## 5. Shared services and scientific contracts

- Annual result descriptor: run/cache identity, dimensions, units, sensor-order
  identity, time axis/timestep, source kind, and validation state. Existing
  manifests may not supply all fields; do not invent missing metadata.
- Factor profile: finite non-negative numeric factor, explicit selection,
  source label, method/version, PAR bounds, and CSV hash/sampling details when
  applicable. No silent unknown-text fallback in new components.
- Plant light context: immutable binding of result identity and profile
  identity. Changing a factor updates metrics without rerunning Radiance or
  overwriting the lux cache. Record derived-result provenance separately.
- Core calculation service: common finite-value validation, lux conversion,
  slice selection and daily integration used by all new components.
- Shared result reader: full validation when opening/revalidating, controlled
  reuse across readers, bounded memory and cancellation for long reads.
  Specify file-change detection, refresh, and consistency guarantees before
  optimizing. Modification-time/size checks alone cannot prove content identity.
- Persistence: save descriptors/configuration, reconstruct and revalidate after
  reopening; do not serialize live streams, tasks, or trusted validation state.

Code issues that must be covered: AnnualCacheData.Factor silently defaults for
unknown text; finite/negative checks differ across numeric converters; Day24
clamps out-of-range selection; each reader invokes full matrix validation and
file hashing. These are verified code behaviors, not yet changed.

Mixed lighting requires source-specific conversion before addition:
PPFD_total = f_daylight × lux_daylight + f_electric × lux_electric.
Matching sensor counts alone is insufficient: verify ordering, time axis,
units and source identity. Preserve existing combined-lux output for
illuminance. A new mixed context should reference separate source contexts;
do not infer spectral equivalence from an already combined lux cache.

The existing scalar cache cannot reconstruct spectral irradiance, canopy
absorption, or photosynthesis. Radiance 6 offers native spectral rendering;
evaluate actual tool/build/platform compatibility before committing to a new
pipeline. The [research report](../research/plant-light/radiance-ppfd-dli-research.md)
specifies explicit units, data limitations and the distinction between the
current RGB/lux implementation and that spectral route.

## 6. Ordered task backlog

Dependencies are task IDs. The status summary above records the initial
implementation; each row's full acceptance evidence is still required before
claiming that task complete.

| ID | Task / deliverable | Depends on | Acceptance evidence |
| --- | --- | --- | --- |
| PL-01 | Audit example node identities and all affected source ports/GUIDs; record current-to-target mapping | — | Actual GH/GHX inspection when available; screenshot-only uncertainties retained |
| PL-02 | Freeze vocabulary, proposed ports, context design, indices and profile policy | Source inventory in PL-01; saved GH not blocking design | Reviewed daylight, LED and mixed-source wiring specifications |
| PL-03 | Review scientific specification and spectral CSV algorithm | — | Correct units; verified sources; interpolation, band coverage, denominator and invalid-data rules defined |
| PL-04 | Build pure Core metric/profile services | PL-02, PL-03 | Known-value, invalid-factor, nonfinite, timestep and list-shape tests |
| PL-05 | Build shared annual-result reader and descriptors | PL-02 | Provenance/tamper tests, cancellation and stale-file behavior; measured baseline vs new read costs |
| PL-06 | Add profile and context Grasshopper adapters with persistence | PL-04, PL-05 | Save/reopen, changed CSV, missing source, factor-change and undo checks |
| PL-07 | Add hour/sensor PPFD and day/sensor DLI readers | PL-06 | Independent branches agree with explicit lux→PPFD→DLI arithmetic; correct tree/list order |
| PL-08 | Apply compatibility/toolbar migration and rebuild example | PL-07 | Old definitions preserve ports/wires; new sample uses only the agreed primary path |
| PL-09 | Add source-specific mixed-light context | PL-07 | Different-factor reference case; rejection of sensor/time/source mismatches |
| PL-10 | Validate Radiance-to-metric pipeline against independent cases and measurements | PL-08; PL-09 for mixed use | Lux and PPFD errors separated; DLI checked by date; domain and tolerances declared in advance |
| PL-11 | Update active guides and release evidence from implemented contracts | PL-08, PL-10 | Docs match ports and sample; no unvalidated accuracy claims |
| PL-12 | Evaluate native Radiance spectral feasibility; prototype only after approval | PL-03 for feasibility; PL-10 before production claim | Windows/tool inventory, wavelength and matrix contract, narrowband tests, measured reference and cost estimate |
| PL-13 | Review and promote reference profiles; curate licensed measured white/horticultural fixtures | PL-03; research package available | Approved schema, energy/photon basis, coverage, license notices and checksum-exception review; fixture data matched to test/photometry |
| PL-14 | Correct result presentation and create matched numerical example | PL-02; adapters after PL-07 | Integer date for DLI, correct units, same sensor/time/profile comparison and no unclipped-value inference from colors |

Suggested increments: design and profile review (PL-01–03, PL-13); shared
implementation for daylight and LEDs (PL-04–08); mixed light (PL-09);
validation/presentation/documentation (PL-10–11, PL-14). PL-12 feasibility can
run early without blocking scalar-method improvements. Production profile
release depends on PL-13; mixed-source release depends on PL-09 and PL-10.
Capture reference arithmetic during PL-04 rather than waiting for host acceptance.

## 7. Concrete acceptance cases

- 1,000 lux × 0.0185 = 18.5 μmol/m²/s. Constant for 24 hours gives
  1.5984 mol/m²/day; for 12 lit hours gives 0.7992.
- 8,760 hourly values of 100 PPFD produce 365 daily values of 8.64.
- DLI for a day agrees with the sum of its 24 PPFD-at-hour results multiplied
  by 0.0036, within an explicit floating-point tolerance.
- Annual DLI at sensor agrees with its annual PPFD series passed to Annual
  DLI, using the same context/time axis.
- Changing the factor changes PPFD/DLI only; lux and run identity remain stable.
- Different daylight/electric factors are applied separately; using a combined
  scalar lux field does not silently claim source-specific PPFD.
- Reopen, changed cache bytes, deleted files and modified CSV cases cannot emit
  stale values marked as verified. Large grids are benchmarked before calling
  the workflow efficient.

## 8. Decisions for review

Recommended: shared Plant Light Context; explicit factor selection; clear hour,
day and sensor readers; numeric utilities retained; old GUIDs/ports preserved;
mixed sources converted before summation; spectral transport deferred to a
separate validated model. Exact example migration remains contingent on
inspecting the saved definition. No estimate of physical accuracy is implied
by agreeing this software design.
