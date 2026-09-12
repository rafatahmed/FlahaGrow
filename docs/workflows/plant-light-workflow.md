# Plant-light workflow and component migration

Implementation status: 2026-09-12. The scalar lux-derived pathway is implemented
and Core-tested. Host acceptance, full example migration, physical validation
and large-grid performance acceptance remain open. This document distinguishes
those gates from compiled functionality; it does not claim a gap-free release.

## Recommended daylight or single-spectrum electric workflow

```text
Annual Simulation (or Electric Annual Simulation)
  Folder -> Load Annual Result
              F32 -------------------+-> existing illuminance readers -> lux plots
                                     |
Spectral Profile --------------------+-> Plant Light Context
                                          Context -> PPFD at Hour
                                                  -> Annual PPFD at Sensor
                                                  -> DLI for Day
                                                  -> Annual DLI at Sensor
```

Load Annual Result still has **Folder, Build** inputs and **F32, S, H, Status**
outputs in their existing order. No port was appended and no old wire needs
to move. The new context constructs its validated descriptor from F32; adding
another public result type to Load Annual Result was unnecessary for this
increment. Illuminance readers remain independent and do not need a profile.

1. Run the simulation and pass its manifest-owned Folder to Load Annual Result.
   Build the cache, then reuse its F32 output.
2. Add **Spectral Profile** in 02 Spectral. Provide a required Label and exactly
   one of Factor or CSV. There is no universal default in this new component.
3. For compatibility, wire **Select Spectral Factor: Factor → Spectral Profile:
   Factor** and **Source → Label**. This is recorded as an explicit numeric
   assumption, not as an independently verified dataset import. Inspect the
   selector's warnings about retained or changed CSV sources.
4. Prefer Spectral Profile's own CSV input for explicit energy/photon basis and
   coverage handling. Its source hash is recalculated when it solves.
5. Connect F32 and Profile to **Plant Light Context**. Connect that one Context
   independently to whichever of the four readers you need. DLI integrates
   the same converted values internally; no PPFD wire into DLI is needed.
6. Keep plotting and sensor markers separate. Connect values to plots and
   retain Status beside the output to preserve quantity/selection/provenance.

The profile must describe received light sufficiently well for the study.
An unfiltered source SPD is not automatically representative after colored
glazing/reflections. Results are explicitly **estimated incident PPFD/DLI**,
not absorbed photons or crop-growth predictions.

## Seven new components

| Panel | Component | Inputs, in order | Outputs, in order |
| --- | --- | --- | --- |
| 02 Spectral | Spectral Profile | Label; Factor optional; CSV optional; Basis=0; Tails=false | Profile; Factor; Status |
| 05 PPFD | Plant Light Context | F32; Profile; Time optional | Context; Status |
| 05 PPFD | Combine Plant Light | Sources, a list of contexts | Context; Status |
| 05 PPFD | PPFD at Hour | Context; Hour | PPFD[S]; Status |
| 05 PPFD | Annual PPFD at Sensor | Context; Sensor | PPFD[8760]; Status |
| 06 DLI | DLI for Day | Context; Day | DLI[S]; Status; Hourly tree |
| 06 DLI | Annual DLI at Sensor | Context; Sensor | DLI[365]; Status |

Hour indices are 0–8759; day indices are 0–364; sensor indices are 0–S−1.
Out-of-range selections fail, without clamping. To select the day containing
an hour, explicitly use `floor(hour / 24)`. DLI for Day also reports day-of-year
`Day + 1`; it never displays a fractional day as a daily total's identity.

PPFD is µmol/m²/s. DLI is mol/m²/day. The hourly tree uses branch `{sensor}`
with 24 ordered **mol/m² per hourly interval** values. Those values sum to that
sensor's daily DLI. They are neither instantaneous PPFD nor a separate daily
quantity named “hourly DLI.” The annual result must contain 8,760 intervals;
this implementation does not silently accept partial/leap-year caches.

No actual calendar is inferred from an opaque cache. The single-source path
can use indices without a Time declaration; Status warns that the calendar is
unspecified. Representative hour labels follow the existing midpoint convention.

## Mixed daylight and electric lighting

```text
Daylight run -> Load Result -> Context + daylight profile -----+
Electric run -> Load Result -> Context + fixture profile ------+-> Combine Plant Light
Other channel -> Load Result -> Context + channel profile ----+       |
                                                        same four readers
```

Create one source context for each independently transported spectral source
or source group. Different fixture/channel spectra require separate runs or
contributions; one electric run still uses one common dimming schedule for its
supplied luminaire set. Do not treat different-color emitters as one constant
spectrum merely because they share a fixture enclosure.

Combine Plant Light requires at least two contexts, identical sensor count and
sensor-record hash, 8,760 hourly intervals, no duplicate source RunId, and an
identical nonblank **Time-axis declaration** on all contexts. For example:
`non-leap Jan 1–Dec 31; UTC+03 local standard time; hourly intervals; no DST`.
This is a user declaration, **not an automatic calendar/timezone verification**:
the current manifests do not supply enough information for that. Confirm the
electric schedule and weather time convention before using mixed results.

Each source is converted before summation. Recognized combined-lux manifests
are rejected by Plant Light Context; reconnect the original source runs.
**Combine Annual Lighting** remains available for combined illuminance, not
as the input to a one-factor plant-light calculation. Arbitrary mixed spectra
within an original run cannot be detected from scalar output; source grouping
remains an explicit modeling responsibility.

## Changes to existing components

Existing GUIDs, port order/count, names and persisted numeric defaults remain
intact. Toolbar removals and automatic GUID migration are not part of this
increment. Changes to numerical validation can intentionally reject previously
accepted invalid data.

| Existing component | Change | Migration consequence |
| --- | --- | --- |
| Annual Simulation | No change for the new photon workflow | Continue producing the existing lux run |
| Load Annual Result | No interface change | F32 supplies either old readers or new Context |
| Illuminance readers | No interface change | Keep existing lux branch |
| Select Spectral Factor / Legacy variant | Custom CSV now uses shared CIE weighting and trapezoidal calculation; numeric override labels are corrected | Existing preset constants retained; reconnect/reload CSV to recompute saved results |
| Load Spectral Data | Same shared CSV calculation; stricter input; corrected integrated lux includes 683 | PAR/Lux results can change; only absolute if input has absolute energy units |
| Lux to PPFD, Hourly PPFD, PPFD Each Sensor | Shared conversion, finite/nonnegative/overflow checks | Numeric utilities remain usable; old 0.0185 defaults preserved |
| Hourly PAR, PAR Each Sensor | Shared photon conversion; strict factor parsing via common legacy helper | Compatibility readers; use new context readers for new definitions |
| DLI Hourly, DLI Each Sensor | Shared photon exposure arithmetic and finite checks | Legacy shapes/selection behavior retained; new DLI for Day uses strict day indices |
| Annual DLI | Shared complete-year integration and timestep validation | NaN/infinity/overflow and invalid temporal shapes rejected |
| Electric Annual Simulation | Writes all manifest partitions, not an assumed single part; checks every part's completion | Fixes grids above ten sensors |
| Combine Annual Lighting | Composes matching electric/daylight partitions | Retained as lux-only composition |

Legacy omitted factors still use 0.0185. Unknown factor strings now fail
instead of silently falling back. Recognized legacy strings retain their
values. This prevents spelling errors from silently changing an analysis but
is a deliberate behavior change for invalid old inputs.

Legacy day readers still retain their earlier hour-selection/partial-series
behavior. The new primary path is strict; do not mix branches with different
factor defaults or different selection conventions in one comparison.

## CSV contract and scientific boundaries

Use an explicit header and one spectrum:

```csv
wavelength_nm,value
360,0.001
361,0.002
```

The snippet illustrates format only; it is not a complete usable spectrum.
Supply all necessary samples covering at least 400–700 nm. Basis `0` means
energy; `1` means photon. A shared relative normalization is acceptable for
the ratio; absolute output quantities are not inferred from it. Recognized
explicit spectral-power/photon headers must agree with Basis.

The new component uses 1 nm linear resampling and trapezoidal integration with
explicit band endpoints. Duplicate/unordered wavelengths, negative/nonfinite
values, malformed rows, missing PAR coverage and zero/nonfinite photopic
denominators fail. Fractional wavelengths are preserved. Resampling does not
recover narrow features absent from the measurements.

Incomplete 360–830 nm photopic coverage fails unless **Tails=true** explicitly
accepts zero unmeasured tails; the output retains that warning. Do not set it
merely to suppress an error. The old CSV components retain an energy-basis,
zero-tail compatibility assumption and warn about it; their sampling interval
is now restricted to 1–10 nm.

The CIE photopic weighting function is embedded with checked source bytes,
metadata and an attribution notice. No daylight/LED/horticultural research
candidate has been silently promoted to a certified fixture preset. The
[reference package](../research/plant-light/README.md) remains a separate
curation resource. Native spectral Radiance transport is not implemented here.

## Integrity, persistence and performance

Core owns profile calculation, conversion, integration, result descriptors and
source combination. Grasshopper owns typed wires, selections and status. A
context contains immutable references/assumptions, not an annual PPFD matrix,
open stream or trusted state serialized into a saved definition.

The new reader locks manifest, metadata, cache, declared result parts and state
files during each operation on Windows. It verifies dimensions/order, command
completion, source hashes and cache hash. A context's recorded identity must
still match. Same-size/same-timestamp tampering is tested. Rebuild/recompute
the context after changing results; changed data is not silently accepted.

Hours and days are contiguous cache reads; a sensor series uses strided reads.
Full validation/hashing is still repeated for each query. This prioritizes
integrity but is **not yet the planned shared-validation performance
optimization**, and these new readers solve synchronously. Large-grid latency,
cancellation and safe validation reuse are outstanding acceptance work. No
speedup is claimed from the passing numerical tests.

## Validation and next acceptance step

- Core suite: 183 tests passed, including new spectral, mixed-source,
  partitioned electric, invalid-number, tamper and daily-integration checks.
- Grasshopper plus compatibility-test project: builds with zero warnings/errors.
- The dedicated smoke runner passes 18 new/compatibility component
  interface/archive contracts, typed ports and explicit-profile solving, and
  verifies all 50 registered components. Use the repository runner: direct
  launch without its test-only `.gha` to `.dll` adaptation can fail during
  CoreCLR initialization (`0x80070057`). This was not a plugin deployment.
- The end-to-end component check passes a 13-sensor, four-part fixture through
  Load Annual Result, explicit/CSV profiles, typed context and all four readers;
  it verifies tree order, integration agreement, invalid-day/ambiguous-input
  rejection and agreement with the updated legacy spectral calculator.
- Live Rhino canvas save/reopen, wire preservation in the actual saved example,
  large-grid responsiveness and matched physical measurements are not passed.
- No Rhino plugin installation/deployment was performed.

Run `dotnet test tests/FlahaGrow.Core.Tests/FlahaGrow.Core.Tests.csproj --no-restore`
and run `./tools/Test-SetupComponents.ps1 -NoRestore` for component compatibility.
Then verify the real example in Rhino using one matched sensor/day:
sum its 24 PPFD values × 0.0036 and compare with DLI for Day. Keep profile, run,
sensor and interval identical and use raw values, not heatmap colors.
