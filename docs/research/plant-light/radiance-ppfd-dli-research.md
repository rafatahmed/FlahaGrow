# Radiance, PPFD and DLI: scientific basis and FlahaGrow workflow

## Conclusions

Code observations in this report describe the pre-implementation research
snapshot. The subsequent [implementation and migration guide](../../workflows/plant-light-workflow.md)
identifies which findings have been addressed and which acceptance gates remain.

FlahaGrow's working example has a defensible workflow: simulate annual
illuminance once, load the result once, and expose independent illuminance,
PPFD and DLI readers. DLI must integrate photon flux mathematically, but it
does not need a visible wire from a PPFD component. The improvement needed is
one shared calculation contract and spectral assumption, not a mandatory
sequence of canvas components.

The current implementation estimates PPFD from lux. That is useful when the
assumed spectrum represents light at the sensor, but it cannot recover
spectral changes from a scalar illuminance cache. Reliable source data improves
traceability; it does not, by itself, validate greenhouse predictions.

The recommended first release scope is daylight, white LEDs and horticultural
LEDs. The accompanying data package contains three CIE daylight references,
nine CIE LED references, and six measured horticultural treatment spectra.
Five discharge-lamp references support an audit of existing presets. None is
enabled in the plugin by this research change.

A second path deserves a bounded feasibility study: **Radiance 6 supports
native spectral rendering**. The assumption that spectral work necessarily
requires a different engine or independent RGB runs is outdated. Availability
of the exact tools and matrix formats on FlahaGrow's Windows platform still
requires verification.[^1]

This report distinguishes current code, numerical research results and proposed
design. It does not approve component replacements or claim measured accuracy
for the supplied greenhouse example.

## 1. Quantities and dimensional correctness

| Quantity | Physical meaning | Unit | Required information |
| --- | --- | --- | --- |
| Illuminance | Incident light weighted for photopic human vision | lux = lm/m² | Photometric transport or spectrum and V(lambda) |
| PAR | Conventional photosynthetically active spectral region, 400–700 nm | A band, not a complete numerical quantity | Specify energy or photon quantity |
| PAR irradiance | Incident radiant power integrated over PAR | W/m² | Radiometric spectrum or an explicitly justified conversion |
| PPFD | Incident molar photon flux density integrated over PAR | µmol/m²/s | Photon spectrum or a justified lux-to-PPFD factor |
| DLI | Photon exposure accumulated over a complete day | mol/m²/day | PPFD time series and interval durations |
| Hourly photon integral | One interval's contribution to daily exposure | mol/m² per interval | Interval-mean PPFD and duration |

PPFD counts photons across the stated band; it is not weighted by the human
visual sensitivity curve or a crop action spectrum. Incident PPFD is not
absorbed PPFD, photosynthetic rate or biomass production. Far-red-inclusive
metrics must be named separately rather than changing the meaning of PPFD.
Quantum-sensor documentation explicitly distinguishes photon counting from
photometric quantities.[^2]

For spectral irradiance `E(lambda)` in **W/m²/nm**, with numerical wavelength
`lambda_nm` in nanometres:

```text
PAR irradiance = integral[400,700] E(lambda) d(lambda_nm)

PPFD = 10^-3 / (h c N_A)
       × integral[400,700] lambda_nm E(lambda) d(lambda_nm)

Illuminance = 683 × integral[360,830] V(lambda) E(lambda) d(lambda_nm)

DLI_day = 10^-6 × sum_i(PPFD_i × duration_i_seconds)
```

Here `h = 6.62607015e-34 J s`, `c = 299792458 m/s`, and
`N_A = 6.02214076e23 mol^-1`. The `10^-3` combines nanometres-to-metres
(`10^-9`) and moles-to-micromoles (`10^6`). The original assessment's compact
formula omitted these explicit unit conventions. This version removes that
ambiguity. The photopic weighting data comes directly from CIE.[^3]

For a relative **energy** spectrum `S(lambda)`, the same ratio gives
`f = PPFD(S)/lux(S)` because an overall normalization cancels. Absolute PPFD,
PAR irradiance or lux must not be reported from arbitrary relative units.
For a **photon** spectrum `q(lambda)` in µmol/m²/s/nm, first recover energy as
`E(lambda) = q(lambda)/(10^-3 lambda_nm/(h c N_A))`, or integrate photons
directly and derive only the lux denominator from energy. Applying the energy
formula directly to photon data weights wavelength twice.

For hourly interval means, `DLI = 0.0036 × sum(24 PPFD values)`. An hourly sky
calculation evaluated at a representative time remains an approximation to an
interval mean; multiplying by 3,600 seconds does not resolve within-hour moving
shadows. If input values are instantaneous measurements instead, their temporal
integration rule must be specified. Missing hours are not automatically zero.

## 2. The working example and current implementation

The six supplied output images demonstrate illuminance and PPFD spatial maps,
a daily DLI spatial map, and annual sensor heatmaps. `01 ILL.jpg` and
`02 PPFD.jpg` identify hour 6084 of 8760. Their different legend ranges and
clipping prevent deriving a conversion factor from matching colors.

`03 DLI.jpg` labels its day as `254.5`; daily output should identify an integer
day or calendar date independently of the selected hour. The annual DLI
heatmap's last legend label uses seconds where days are required. The annual
DLI image identifies sensor 500, while annual PPFD identifies sensor 428:
these are not a matched integration comparison. The labels may originate in
canvas text or plotting inputs; the images do not establish an arithmetic bug.

The visible results are accepted as a working FlahaGrow example. A saved GH/GHX
file would help preserve exact GUIDs, wires and legacy behavior during migration;
its absence does not prevent architectural planning. Numerical validation needs
matching raw sensor/time values, not additional screenshots.

### Source-code evidence

| Source | Observed behavior | Design consequence |
| --- | --- | --- |
| [AnnualSimulationComponent](../../../src/FlahaGrow.Grasshopper/Components/AnnualSimulationComponent.cs) | `gendaymtx`, daylight coefficients, total-minus-direct-plus-discrete-sun correction, then RGB reduction using `47.4 119.9 11.6` | Current result is photometric, not wavelength-resolved |
| [SpectralConversionFactorComponents](../../../src/FlahaGrow.Grasshopper/Components/SpectralConversionFactorComponents.cs) | Hardcoded references; embedded legacy OPN1 table; custom CSV factor calculation | Replace undocumented assumptions only after numerical and compatibility review |
| [LegacyPpfdComponents](../../../src/FlahaGrow.Grasshopper/Components/LegacyPpfdComponents.cs) | Hourly PAR and PAR Each Sensor are cache readers registered in 05 PPFD | The panel is not unused functionality; naming obscures what already exists |
| [PpfdCacheComponents](../../../src/FlahaGrow.Grasshopper/Components/PpfdCacheComponents.cs) | Alternative list-based PPFD conversion | Retain utilities, avoid duplicate recommended paths |
| [AnnualDliComponent](../../../src/FlahaGrow.Grasshopper/Components/AnnualDliComponent.cs) | Sum of PPFD × timestep / 10^6 with annual shape requirements | Integration is correct conditional on valid units, values and time axis |
| [AnnualHeatmapComponents](../../../src/FlahaGrow.Grasshopper/Components/AnnualHeatmapComponents.cs) | Generic annual plotting and supplied legend text | Add typed labels/provenance without diagnosing calculations from presentation |

The custom spectral importer rounds wavelengths to integers, overwrites
duplicates, skips invalid rows, and forward-fills the last exact match on its
sampling grid. This is not linear interpolation. A spectrum sampled at an
offset or an incompatible step can be missed. Negative samples can enter the
ratio, and a nonpositive photopic denominator is not robustly rejected.

Its intermediate sums do not multiply by the wavelength step. A constant step
can cancel in a ratio, but does not make the displayed PAR/lux sums correct
absolute integrals. The input also assumes a header and a spectral-power
column. The authentic CIE files are headerless and several contain multiple
profiles, so direct import is not a safe installation procedure.

The selector defaults to D65 `0.018043`; other conversion paths default to
`0.0185`. The latter is 2.535% larger. This alone can produce different PPFD
and DLI from the same lux if branches use different defaults. A numeric
override also needs a corresponding custom label rather than retaining the
name of a reference spectrum.

## 3. Reference data and reproducible factors

The [data inventory](profile-audit.json) retains source identifiers, exact
files, wavelength ranges, original spectral basis and local hashes. The
[audit script](audit_profiles.py) uses linear interpolation to a common 1 nm
grid, then trapezoidal integration over exact 400–700 nm and 360–830 nm bounds.
Unmeasured tails are zero-assumed and explicitly flagged. This method is a
documented research convention, not a claim that resampling adds resolution.

### Daylight references

CIE D55, D65 and D75 provide authoritative reference daylight distributions.
They are suitable named assumptions and regression inputs; they do not encode
a specific Riyadh weather hour, greenhouse material or sky direction.[^4][^5][^6]

| Profile | Calculated factor, µmol/m²/s per lux | Existing factor | Existing minus calculated |
| --- | ---: | ---: | ---: |
| D55 | 0.01780791 | 0.017833 | +0.141% |
| D65 | 0.01801872 | 0.018043 | +0.135% |
| D75 | 0.01831996 | 0.018345 | +0.137% |

These small differences are not evidence that existing presets are fundamentally
wrong. Endpoint conventions, reference tables, interpolation and range can
change a ratio. The research method and provenance should be frozen before
choosing replacement values. Scientific precision is not established by the
number of displayed decimal places.

ASTM G173 spectra are another useful benchmark: they describe one atmospheric
reference condition, with direct-normal and global irradiance on a specified
37-degree tilted plane. They are not a universal annual daylight spectrum.
Do not subtract direct-normal from global-tilted values to obtain diffuse
irradiance without the required geometric conversion.[^7] They remain a
candidate external benchmark, not a bundled profile in this package.

### White and reference LEDs

The official CIE 1 nm LED dataset supplies nine named distributions. Use these
as reference illuminants, not manufacturer fixtures or arbitrary CCT matches.
Matching CCT alone does not identify a spectrum.[^8]

| CIE label | Calculated factor | CIE label | Calculated factor |
| --- | ---: | --- | ---: |
| LED-B1 | 0.01490967 | LED-B2 | 0.01472502 |
| LED-B3 | 0.01433439 | LED-B4 | 0.01395089 |
| LED-B5 | 0.01440437 | LED-BH1 | 0.01364522 |
| LED-RGB1 | 0.01651946 | LED-V1 | 0.01884858 |
| LED-V2 | 0.01768529 | — | — |

The existing BH1 and V2 factors differ from these research calculations by
approximately −0.090% and +0.032%. Much larger uncertainty may come from
selecting the wrong spectrum for the actual fixture, not from this arithmetic.

### Horticultural measured spectra

Park and Runkle's open study provides six measured sole-source treatment
spectra and downloadable raw data. Measurements were taken at seedling-tray
height, with treatments targeted to 160 µmol/m²/s and an 18-hour photoperiod.
The dataset is scientifically relevant to horticultural lighting but is not
an accredited commercial-fixture library.[^9]

S1, sheet `Figure 1`, contains wavelengths 400–799 nm in `A4:A403` and six
photon-spectrum columns `B:G`. No image digitization was used. Lux below
400 nm and above 799 nm is unknown; the following estimates zero-assume those
tails. They are candidates for research demonstrations, not production
conversion presets with complete photopic coverage.

| Treatment | Calculated PPFD from source data | Calculated factor |
| --- | ---: | ---: |
| MW100 | 161.165 | 0.01185685 |
| MW75R25 | 159.425 | 0.01481861 |
| MW45R55 | 159.586 | 0.02205944 |
| MW25R75 | 158.986 | 0.03271706 |
| B15R85 | 162.168 | 0.08725328 |
| B20G40R40 | 159.531 | 0.01798897 |

These integrations are approximately −0.63% to +1.35% from the experimental
target of 160. That is a useful consistency check, not a measurement uncertainty
estimate. At exactly 160 for 18 hours, the independent arithmetic gives DLI
10.368 mol/m²/day, consistent with the study's rounded 10.4.

The B15R85 factor is approximately 4.84 times the D65 reference factor. Using
D65 for that treatment would predict roughly 79% too little PPFD at equal lux,
under the stated tail assumption. This is a source-specific demonstration,
not a factor recommendation for every red/blue product.

### Provenance, licensing and release readiness

All CIE downloads match their metadata MD5 values. Three published SHA-256
strings are malformed, and the photopic landing page has a conflicting MD5;
the [package notes](README.md#provenance-exceptions) preserve these exceptions.
Do not hide them behind a generic “verified” badge. Authentic source,
byte integrity, numerical reproducibility and scene representativeness are
four different checks.

CIE data metadata states CC BY-SA 4.0; the horticultural data states CC BY 4.0.
Keep originals, attribution and adaptations separable. Public availability is
not sufficient permission to redistribute every manufacturer's SPD. The LBNL
IGDB license page contains inconsistent license labels, so redistribution of
modified glazing data should remain on hold pending clarification.[^10]

## 4. Radiance method choices

### Existing scalar method

The current annual path generates RGB daylight coefficients and sky values,
combines the direct-sun correction, and reduces the result to one lux value
per sensor/hour. It is best described as a daylight-coefficient workflow with
direct-sun correction; the presence of three annual terms alone does not
establish a complete BSDF five-phase `V T D` implementation.

Given a known sensor spectrum, a scalar conversion is mathematically sound.
The approximation is assigning that spectrum to a spatially and temporally
varying field. Clear sun, diffuse sky, glazing, colored reflections and electric
fixtures may differ. One source factor assumes those differences are negligible
or already represented by the chosen received-light profile.

Retaining RGB channels may support some calibrated approximations, but three
numbers still do not uniquely recover a spectrum. A conversion table cannot
restore information discarded by the current lux reduction.

### Native spectral route

Radiance 6 introduced spectral sampling and spectral material patterns while
retaining three-channel behavior as the default. A typical build supports up
to 24 samples; exact options and effective bands must be checked, not inferred
from a version name.[^1] The official `genssky` source describes a spectral
atmospheric sky model and calibration options. Its precomputed spectral
resolution and atmospheric assumptions remain part of the error budget.[^11]

The spectral feasibility task should examine `genssky`, `gensdaymtx`, weather
inputs, material spectra, sensor irradiance output, wavelength metadata and
matrix multiplication together. It must preserve radiometric channels through
the full calculation and integrate photon flux only afterward. Increasing the
output sample count cannot recreate a narrow LED peak absent from the source
or recover detail lost by an unsuitable binning convention.

Do not assume the existing annual batch can accept spectral files unchanged.
The developers distinguish newer annual tools and spectral-picture handling;
some are Linux/macOS-only and may require WSL on Windows.[^12] This research
does not install WSL, replace Radiance, or assume a spectral executable is
available in the configured installation.

For pure electric lighting with spectrally neutral transport and a stable
fixture SPD, validated photometry plus a fixture factor can remain efficient.
For spectral glazing or different-color emitters with different beam patterns,
spectral transport or separately transported source/channel terms is the more
defensible model. Both paths still require independent validation.

## 5. Efficient target architecture

Keep the source lux cache immutable. Bind its validated descriptor to a profile
once, then let each reader request only its required data:

```text
Annual Simulation -> Load Annual Result -> Annual Illuminance Result
                                           |-- Illuminance at Hour
                                           |-- Illuminance at Sensor
                                           |
Spectral Profile --------------------------+-> Plant Light Context
                                                 |-- PPFD at Hour
                                                 |-- PPFD at Sensor
                                                 |-- DLI for Day
                                                 `-- DLI at Sensor
```

This is a proposed component contract. The context holds references, units,
time/sensor identity and profile assumptions; it does not duplicate the annual
matrix. DLI readers use the same internal conversion service as PPFD readers.
Changing a factor invalidates derived values, not the Radiance simulation.

The current hour-major F32 cache makes an hour or a 24-hour block inexpensive
to read contiguously. A single sensor's annual series is strided. Choose chunked
reads or a bounded cache from benchmarks before adding a second on-disk layout.
Repeated full provenance validation/hashing in every reader is another target:
validate on open/revalidation and reuse an immutable validated session with
explicit refresh and file-change handling. Size/mtime alone does not prove
content identity; optimization must preserve tamper and stale-file detection.

For daylight plus multiple luminaires or independently controlled channels:

```text
PPFD_total(s,t) = sum_j[f_j(s,t) × lux_j(s,t)]
DLI_total(s,d)  = 10^-6 × sum_t[PPFD_total(s,t) × duration_t]
```

Each term must share the same sensor order, orientation and time axis. A single
electric source may have a constant factor if its spectrum is stable. Different
channel schedules require separate contributions. A scalar factor attached
after combining all lux is valid only when its spatial/time-dependent mixture
is known or spectral equivalence is demonstrated.

The numeric Lux-to-PPFD and Annual DLI utilities remain useful for supplied
measurements and one-sensor lists. They should use the same core routines but
must distinguish a spatial grid from a time series. Existing saved definitions
retain their GUIDs and ports until tested migration adapters exist.

## 6. Fixture and greenhouse input requirements

For production white or horticultural fixture profiles, require a traceable
measured SPD matched to model, optics, drive/current state and test report.
DLC's March 2025 V4.0 requirements provide a useful acquisition template:
accredited-lab measurements, TM-33 spectral data at 5 nm or finer over
400–800 nm, in W/nm, and matching fixture identity. Its integrated SPD does
not contain color-over-angle information.[^13]

Consequently, the profile library needs distinct records for source spectrum,
photometric distribution and their association. An IES candela distribution
does not become a photon distribution by changing its label. Published PPF/PPE
and electrical watts also do not specify spatial PPFD at the crop.

For greenhouse transmission, obtain spectral transmittance/reflectance and
angular behavior appropriate to the glazing or film. A spectral transmittance
curve is dimensionless material data, not a light SPD. For a direct ray through
a suitable simple filter, `S_transmitted(lambda) = S_incident(lambda) ×
T(lambda, angle)` can derive a transmitted profile. It is not a general
substitute for multiple reflections or a scattering BSDF model.

Avoid applying glazing loss twice: if the lux calculation already includes
transmission, the post-processing factor should express the transmitted
spectrum's photon-to-lux ratio, not multiply the transmitted intensity again.
Wavelength-selective or diffuse films require a model-specific study.

## 7. Reliability and validation gates

Radiance has experimental validation for illuminance prediction under real
sky conditions. That evidence supports the engine/method family, not an
unmeasured FlahaGrow greenhouse's PPFD accuracy.[^14] The distinction matters:
a perfect DLI summation accumulates systematic PPFD bias rather than removing
it. No universal percentage accuracy is justified for the present plugin.

| Gate | Required comparison | Completion evidence |
| --- | --- | --- |
| Numerical | Known spectra, photon/energy equivalence, units, invalid inputs and daily sums | Independent expected values and automated tests |
| Profile | Published bytes, original basis, coverage, normalization and test identity | Licensed data inventory; explicit warnings; reproducible factors |
| Transport | Empty-sky/analytic cases, point-in-time versus annual, convergence with rays/bounces/sky basis | Lux errors separated from factor errors |
| Measurement | Matched lux and cosine-corrected PPFD or spectral measurements at identical sensor planes/times | Calibration records, measured weather, residuals by condition |
| Annual exposure | Daily integration of matching complete time series | Same sensor/date comparison, missing-data policy, daily bias/RMSE |
| Compatibility/performance | Old GH definitions and representative large grids | Save/reopen tests, preserved wires, measured I/O/memory/time |

Use contemporaneous weather for field comparison; a typical meteorological
year is not a record of the day measured. Include clear, overcast and partly
cloudy conditions, exposed and shaded sensors, and each fixture/channel state.
Record geometry, glazing condition, crop height and sensor orientation.

Report mean bias, RMSE and normalized errors with a declared normalization and
low-light policy. Percentage error near zero is unstable. Reserve independent
validation observations rather than deriving and testing a factor on the same
samples. Set decision-specific tolerances before examining residuals; this
report does not invent a generic acceptance threshold.

A measured library entry may be highly trustworthy yet unsuitable for another
fixture or a filtered sensor location. A spectral simulation can also be
inaccurate if material or sky spectra are wrong. Product wording should show
both **calculation method** and **validation status**, not a single
“research-grade” switch.

## 8. Recommended sequence and unresolved items

First freeze quantities, result shapes, time conventions, independent readers,
profile states and mixed-source rules. Next implement pure numerical services
and validated readers, followed by compatible Grasshopper adapters. Verify
the example with paired raw values and correct its labels. Profile curation
and numerical regression can proceed before plugin wiring.

Run a separate spectral feasibility gate early enough to inform the data model,
but do not block the safer scalar workflow on full annual spectral support.
The [task plan](../../architecture/plant-light-workflow-plan.md) records ordered
dependencies and acceptance evidence. The [profile specification](profile-library-specification.md)
defines what must be approved before any candidate becomes a bundled preset.

Unresolved release items are manufacturer-specific licensed measurements,
coverage review for truncated spectra, publisher checksum clarification,
Windows spectral-tool compatibility, actual greenhouse validation and exact
saved-definition migration. None is resolved by the supplied heatmaps or by
the passing research calculations.

## Sources

[^1]: Greg Ward / Radiance developers. [Official Radiance 6.0 release](https://discourse.radiance-online.org/t/official-radiance-6-0-release/6873), July 24, 2025. Spectral options, default compatibility and source/material support; verify installed build separately.
[^2]: Apogee Instruments. [SQ-212/215 quantum sensor manual](https://www.apogeeinstruments.com/content/SQ-212-215-manual.pdf), introduction and measurement definitions. Supports PPFD band/units, not FlahaGrow accuracy.
[^3]: CIE. [Spectral luminous efficiency for photopic vision](https://cie.co.at/datatable/cie-spectral-luminous-efficiency-photopic-vision), 2019, DOI 10.25039/CIE.DS.dktna2s3. CSV and original metadata retained locally.
[^4]: CIE. [Relative spectral power distribution of illuminant D55](https://cie.co.at/datatable/relative-spectral-power-distributions-cie-illuminant-d55), DOI 10.25039/CIE.DS.qewfb3kp. Data version/publication in adjacent metadata.
[^5]: CIE. [Standard illuminant D65](https://cie.co.at/datatable/cie-standard-illuminant-d65), DOI 10.25039/CIE.DS.hjfjmt59. Data and checksum discrepancy retained.
[^6]: CIE. [Relative spectral power distribution of illuminant D75](https://cie.co.at/datatable/relative-spectral-power-distributions-cie-illuminant-d75), DOI 10.25039/CIE.DS.9fvcmrk4.
[^7]: National Laboratory of the Rockies, formerly NREL. [Reference Air Mass 1.5 Spectra](https://www.nlr.gov/grid/solar-resource/spectra-am1.5), ASTM G173 reference conditions and field definitions. Accessed September 12, 2026.
[^8]: CIE. [Typical LED illuminants, 1 nm](https://cie.co.at/datatable/relative-spectral-power-distributions-illuminants-representing-typical-led-lamps-1nm), DOI 10.25039/CIE.DS.dhcw57sd. Original data and metadata retained.
[^9]: Yujin Park and Erik S. Runkle. [Spectral effects of light-emitting diodes on plant growth, visual color quality, and photosynthetic photon efficacy: White versus blue plus red radiation](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0202386), PLOS ONE 13(8), August 16, 2018. [Open data, v1](https://doi.org/10.6084/m9.figshare.6946136.v1), S1 Figure 1. Numerical factor table above is a new calculation, not a factor table published by the authors.
[^10]: Lawrence Berkeley National Laboratory. [IGDB license](https://windows.lbl.gov/igdb-license), accessed September 12, 2026. Inconsistent license descriptions; no glazing data redistributed here.
[^11]: LBNL-ETA/Radiance. [genssky manual source](https://raw.githubusercontent.com/LBNL-ETA/Radiance/master/doc/man/man1/genssky.1), accessed September 12, 2026. Living source, not an installed-binary capability guarantee.
[^12]: Greg Ward. [New/updated annual simulation tools](https://discourse.radiance-online.org/t/new-updated-annual-simulation-tools/6918), October 29, 2025. Newer annual tooling, spectral pictures and platform limitations.
[^13]: DesignLights Consortium. [Technical Requirements for LED-based Horticultural Lighting V4.0](https://designlights.org/wp-content/uploads/2025/03/DLC_HORT_Technical_Requirements_V4-0_finalpolicy_031225.pdf), released March 11, 2025, sections 9.1–9.2, printed pp. 34–36. Used as a documented measurement-data acquisition reference, not a claim that FlahaGrow is DLC compliant.
[^14]: J. Mardaljevic. [Validation of a lighting simulation program under real sky conditions](https://journals.sagepub.com/doi/10.1177/14771535950270040701), Lighting Research & Technology 27(4), 181–188, 1995. [Author's validation guidance and related primary studies](https://discourse.radiance-online.org/t/how-validated-is-radiance/5150/2), March 5, 2020.

Additional audit data: CIE, [High-pressure discharge illuminants](https://cie.co.at/datatable/relative-spectral-power-distributions-high-pressure-discharge-lamp-illuminants), DOI 10.25039/CIE.DS.f6rvvnev. These five references are retained to compare existing labels/factors, not prioritized as a new fixture library.
