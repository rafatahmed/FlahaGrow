# Timing and sensor connections

Use **Select Date and Hour**, nicknamed **Hour Index**, as the single timing helper.
Connect a Button to Run; select the start of the hourly interval in local standard
time. The simulation has 365 days, starts January 1, and has no daylight-saving
clock shifts or February 29. Choosing 13:00 means the interval 13:00–14:00.

| Hour Index output | Destination | Meaning |
|---|---|---|
| Hour | PPFD at Hour.Hour; annual illuminance Hour inputs | 0–8759; first interval = 0 |
| Day | DLI for Day.Day; compatible daily-index inputs | 0–364; January 1 = 0 |
| Date | Panel only | Readable interval label, not a machine alignment token |
| Alignment | Plant Light Context.Alignment | Entire annual time axis, not a selected hour/day |

Alignment is optional for a single-source context. For mixed sources, enter the
actual simulation/weather-file UTC offset in **Hour Index.UTC min**, for example
180 for UTC+03:00. Its Alignment output supplies the required format automatically.
Use it for each source only after verifying that their Jan–Dec calendars and
schedules really match. This declares alignment; it does not validate or resample
run metadata. Do not use the computer's current time or connect Date to Alignment.

Plant Light Context binds the whole annual result and spectrum. It has no Hour
selection input. PPFD at Hour selects one hour; DLI for Day integrates all 24 hours
of one day. Both read the same Context independently.

## Sensor indices

PPFD Sensor and DLI Sensor require an integer index into the exact saved simulation
sensor order: 0 is the first point, 428 is the 429th point. They do not accept a Point,
coordinate, tree path or arbitrary grid label. Context Status reports the valid range.
GenPts points correspond only when their order is unchanged in the exported sensor
file. Flattening, sorting or merging grids later can invalidate that correspondence.
To visualize a sensor, use the same index with List Item on that exact ordered point
list, then connect the selected point to Sensor Marker.Point. If export order is not
known, inspect the saved run's sensor file rather than guessing from the viewport.

## Messages and migration

The library's coverage and research limitations are assumptions, not execution errors.
They remain in Profile and Context Status after acknowledgment. Malformed input,
invalid indices and failed provenance checks still produce real runtime errors.
An authenticated source spectrum is not automatically representative of received
light after greenhouse glazing, reflections, or mixed lighting.

The old selector shifted clock hours backward and depended on a globally shared
selection. The corrected selector uses interval starts and saves state per component.
Review old definitions: regenerate selected hours instead of trusting old exported
indices. Existing Hour and Date output positions remain; Day and Alignment are appended,
and UTC min is appended as an optional input. Recreate the selector if an old saved
instance does not expose the appended ports. Existing Context.Time wires remain in
the same port position, now named Alignment, but date strings must be disconnected.

September 12 at 13:00 gives Hour 6109 and Day 254, independent of a leap calendar year
shown in the picker. DLI is the sum of that day's 24 PPFD values multiplied by 0.0036.
