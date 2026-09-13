# Loaded results, inherited weather and automatic plot attributes

This is the recommended scalar daylight workflow. It replaces manual UTC entry
and manually reconstructed plot labels. The calculation remains a lux-derived
PPFD estimate; the data contract does not make it spectral Radiance transport.

## Connect once

| Source output | Destination input | Responsibility |
|---|---|---|
| Annual Simulation.Folder | Load Annual Result.Folder | Select the manifest-owned completed run |
| Load Annual Result.Result | Select Date and Hour.Result | Inherit verified EPW location/calendar/UTC |
| Load Annual Result.Result | Plant Light Context.Result | Bind the same cache and annual time axis |
| Spectral Profile.Profile | Plant Light Context.Profile | Declare the source-specific conversion |
| Select Date and Hour.Hour | PPFD at Hour.Hour | Select one hourly sensor grid |
| Select Date and Hour.Day | DLI for Day.Day | Select one complete daily sensor grid |
| Plant Light Context.Context | Any of the four PPFD/DLI readers.Context | Independent readings from one source context |
| Annual PPFD/DLI at Sensor.PPFD/DLI | Annual Plot.Data | Annual numeric series |
| Same reader.Plot | Annual Plot.Plot | Matched quantity, units, selection and ranges |
| Read Illuminance.Lux + Plot | Annual Plot.Data + Plot | Annual illuminance series with attributes |

The existing illuminance readers retain their F32 inputs. Sensor inputs remain
zero-based indices into the saved simulation sensor order, not point coordinates.
No new component is required for the primary connection chain; existing components
gain typed outputs/optional inputs. Spectral Profile and Hour Index are selectors,
not additional annual simulations.

## Load Annual Result: responsive operation

The old implementation rebuilt and hashed files synchronously whenever Build
was True. The new loader runs validation/building on a background task. A folder
change starts loading; a False→True Build edge requests validation and, if needed,
cache construction. A valid cache is reused, even when Build is True. Holding
True does not repeatedly rebuild or hash on every component solve.

Status reports source validation, construction percentage and final verification.
Cancel stops pending work at cancellation checkpoints; an ongoing file hash is
not interrupted mid-read. Switching folders, closing or removing the component
invalidates its pending result. Operations for the same folder are serialized
within the plugin process. Only completed operations publish outputs.

The loaded descriptor is transient and reconstructed after reopening. It is not
an authority to skip downstream integrity checks. Click Build again after an
external result change. Plant-light readers still validate source/cache content
on reads, so corrupted files are not accepted merely because sizes/timestamps
match. This means large-grid downstream reading can still cost time; safe,
bounded cross-reader validation reuse remains a separate optimization. The
current change removes repeated rebuilding and blocking loader I/O, not every
possible downstream source of UI latency.

## Weather and timing

Daylight runs already retain `weather.epw`. Loading verifies it against the EPW
hash recorded in the run manifest, parses location/UTC from LOCATION, and checks
the consecutive 8,760-hour non-leap Jan–Dec calendar. Fractional-hour offsets
(for example UTC+05:30) are supported. No computer timezone is consulted.

Hour Index inherits Result weather; its old UTC-minutes port is retained only
for saved wires and is ignored with a migration warning. Disconnect that wire.
Connect Result instead. Hour and Day stay zero-based. Date is readable display
text; Alignment describes the entire annual time axis. With Result connected,
Plant Light Context inherits alignment itself: leave F32 and Alignment empty.
Conflicting legacy F32/Alignment inputs are rejected, not silently preferred.

Missing weather is reported explicitly. Numeric index-only single-source
analysis remains possible, but no automatic timezone/alignment is emitted.
A changed/malformed EPW is an error, not a reason to guess its timezone.
Weather records in typical-year EPWs may come from multiple source years;
the inherited axis is a 365-day typical calendar, not a historical-year claim.

For a new electric annual run, optionally connect the verified daylight Result
to Electric Annual Simulation.Time Result. This explicitly declares that the
8,760 dimming fractions follow that time axis; the EPW/hash are snapshotted in
the electric run. It does not independently verify how a user authored a
schedule. Existing weatherless electric runs are not retrospectively assigned
a timezone. Existing combined-lux runs cannot recover source-specific spectral
assumptions; use the separate source contexts and Combine Plant Light.

## Plot attributes and automatic ranges

Readers append Plot Attributes without changing their existing numeric outputs.
Attributes include quantity, units, annual/grid shape, selection, provenance,
count, value checksum and min/max. Annual Plot requires matching data: changing
the values or order on only one wire is rejected. A grid is rejected as an
annual series even if its sensor count happens to equal 365 or 8,760.

With attributes connected, the default title includes quantity, units and
selection. Four thresholds are derived from min, min + 25% of span, min + 50%,
and min + 75%; the final class extends to max. Constant-valued series produce
coincident thresholds and a single populated class. These are display bins,
not crop-optimal light thresholds. Automatic scales can differ between plots;
use common manual thresholds for visual comparisons.

Set Manual=True to use R1–R4. Title and class labels remain optional overrides.
Plots without attributes preserve their existing explicit ranges/title contract.
The renderer displays exact numeric inequalities and counts separately from
user labels; zero belongs to `x ≤ 0`, not `x < 0`. It rejects negative/nonfinite
light values. A Button opens a snapshot once; held True does not open repeated
windows. Open plots do not live-update: click again for the new data.

Annual Plot handles illuminance, PPFD and DLI through matched Plot Attributes. It does not infer units from list length.

## Migration and acceptance

- Load Annual Result: Cancel input and Result output appended.
- Select Date and Hour: inputs are Run and Result; no unused UTC port. Its new GUID requires a fresh instance in older definitions.
- Plant Light Context: Result input appended; F32 is now optional.
- Four plant-light readers and Read Illuminance: Plot output appended;
  DLI for Day retains its hourly-integral tree at its original output index.
- Annual plots: Plot and Manual inputs appended.
- Electric annual: Time Result input appended.

Saved-canvas port migration must be checked in Rhino. If an old instance does
not expose appended ports, place a fresh instance and reconnect by the names
above. Do not delete existing definitions before that check.

Automated tests cover cache reuse/cancellation, authenticated weather and a
fractional UTC offset, inherited Hour Index alignment, typed-context reading,
attribute/data mismatch, grid rejection, manual range override validation,
invalid values and component archive contracts. Physical PPFD accuracy, real
large-grid UI latency and saved-canvas migration remain host/study acceptance
tasks, not conclusions from screenshots or synthetic tests.
