# Plant-light output and component audit — 2026-09-12

## Evidence and limits

Visually inspected the four user-supplied files in Downloads: Annual PPFD At
sensor #5, DLI at day, PPFD At hour, and Annual DLI At sensor #5. Titles and
legend wording were entered manually by the user; they are not evidence of
automatic metadata generation or a calculation defect. No raw result matrices,
source profile selection or saved definition accompanied these images.

The grid labels show hour index 6109 and day index 254. Those selections agree
under the current zero-based non-leap interval-start convention: September 12,
13:00–14:00. The two annual images show hourly and daily resolutions. This is
visual workflow evidence, not numerical or physical validation.

The grids' displayed scales (roughly 17–1518 PPFD and 18–36 DLI) do not establish
their raw extrema, spectral accuracy, or correctness for an individual sensor.
An annual plot for one sensor need not share extrema with the whole-grid plot.
The title “sensor #5” alone does not establish whether the user means index 5
(sixth saved point) or the fifth point. Export metadata must make this explicit.

## Findings and disposition

| Finding | Disposition |
|---|---|
| Load Result rebuilds and hashes on the solve thread | Background task, progress/cancel and valid-cache reuse implemented |
| Manual UTC is detached from the actual run | Result descriptor inherits authenticated run EPW; UTC entry deprecated |
| Numeric lists lose quantity/unit/selection identity | Readers append matching Plot Attributes |
| Annual PPFD plot name also covers DLI | Use the unified Annual Plot with paired values and attributes |
| Manually entered first-bin “< 0” can disagree with zero-inclusive classification | Numeric inequalities/counts now rendered separately from user labels |
| Rounded percentages alone can conceal rare nonzero bins | Exact counts accompany percentages |
| Generic subjective default labels imply quality classes | Neutral Bin labels; automatic ranges are display-only, not crop thresholds |
| Hold-True plotting can open repeated windows | Edge-triggered plot snapshots, disarmed on archive restore |
| Plot data can contain invalid values or a wrong shape | Finite/nonnegative checks, attributes/data checksum and annual/grid shape checks |

## Required components at this stage

Use Load Annual Result, Spectral Profile, Plant Light Context, Hour Index, and
only the needed independent readers: PPFD at Hour, DLI for Day, Annual PPFD at
Sensor, Annual DLI at Sensor. Use Annual Plot for annual series.
Use Combine Plant Light only for source-specific mixed studies; Custom Spectral
Profile only for custom assumptions. Numeric conversion and integration components support independently supplied data,
not additional steps needed by the typed workflow. None were removed or hidden
as part of this change.

## Acceptance still needed

Retain raw values and status/provenance for the same run, profile, sensor and
day. Verify `sum(24 hourly PPFD values) × 0.0036` equals that day's DLI in both
the grid and annual sensor outputs within a stated floating-point tolerance.
Verify saved sensor order against the exported sensor file and viewport mapping.
Verify the EPW snapshot identity, source spectrum applicability, and separate
electric schedule association if used. Finally measure cold loading, cancellation,
and all connected readers on the real grid in Rhino. Screenshots cannot close
those acceptance items.

Implementation and wiring: [Result/weather/plot contract](../workflows/result-weather-plot-contract.md).
