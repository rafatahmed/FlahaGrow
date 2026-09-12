# Scientific assessment: PPFD and DLI in FlahaGrow

This assessment compares the implementation with scientific definitions of
PPFD/DLI and the Radiance matrix method. It distinguishes code-level integrity
from physical accuracy: a passing run or cache is not experimental validation.

Expanded evidence, authentic reference spectra, reproducible factor calculations
and proposed contracts are in the
[deep research package](../research/plant-light/README.md). Its explicit unit
and data conventions take precedence over the earlier compact descriptions.

## Scientific basis

PPFD is the incident photon flux density traditionally counted over the
400–700 nm PAR waveband, in μmol/m²/s. DLI is the 24-hour integral of PPFD, in
mol/m²/day. See the quantum definition in the
[Apogee sensor manual](https://www.apogeeinstruments.com/content/SQ-212-215-manual.pdf)
and the PPFD/photoperiod/DLI relationship in
[Park and Runkle's measured horticultural study](https://journals.plos.org/plosone/article?id=10.1371/journal.pone.0202386).

For spectral irradiance `Eₑ,λ` in W/m²/nm and numerical wavelength in nm,
PPFD in µmol/m²/s is calculated as:

```text
PPFD = 10^-3 × ∫400–700 λ_nm Eₑ,λ / (Nₐ h c) dλ_nm
```

This follows from photon energy `hc/λ`, with explicit conversion to micromoles.
Lux is instead weighted by human photopic sensitivity, represented by the
[CIE V(lambda) dataset](https://cie.co.at/datatable/cie-spectral-luminous-efficiency-photopic-vision).
There is consequently no universal lux-to-PPFD conversion: it depends on
spectral power distribution (SPD). The
[reproducible profile calculations](../research/plant-light/profile-audit.json)
demonstrate the difference using official reference and measured research data.

## What FlahaGrow calculates

```text
EPW → Perez sky matrix → Radiance matrix method → annual illuminance [lux]
                                                        │
                             user/CSV-derived factor ───┤
                                                        ▼
                                               estimated PPFD [μmol/m²/s]
                                                        │
                                               numerical daily integration
                                                        ▼
                                                   estimated DLI [mol/m²/day]
```

The first half is a climate-based Radiance calculation. `gendaymtx` generates
an annual Perez sky matrix; `rfluxmtx` produces daylight coefficients; and
`dctimestep` multiplies coefficient and sky matrices. These roles are defined
in the official [gendaymtx](https://radsite.lbl.gov/radiance/man_html/gendaymtx.1.html),
[rfluxmtx](https://floyd.lbl.gov/radiance/man_html/rfluxmtx.1.html), and
[dctimestep](https://radsite.lbl.gov/radiance/man_html/dctimestep.1.html)
manuals. FlahaGrow's total-minus-direct-plus-discrete-sun combination is in
[Radiance methods](radiance-methods.md).

The second half is not spectral Radiance. The plugin calculates:

```text
estimated PPFD = illuminance × factor
daily DLI = Σ(estimated PPFD × timestep) / 1,000,000
```

`Annual DLI` enforces a complete 365-day series and a timestep that divides a
day exactly. Its arithmetic is scientifically correct conditional on PPFD input.
Cache/provenance validation makes illuminance traceable; it does not establish
spectral accuracy.

## Reliability assessment

| Layer | Assessment | Main limitation |
| --- | --- | --- |
| Annual lux | A sound method family; validate each application | No field-validation dataset or stated accuracy for this plugin/model configuration |
| DLI integration | Reliable arithmetic when PPFD and timestep are valid | It integrates any PPFD bias |
| One fixed lux→PPFD factor | Practical estimate for a known homogeneous SPD | Cannot resolve changing sky, glazing, reflection, or mixed-source spectra |
| Default `0.0185` | Daylight starting estimate only | Not a greenhouse-wide or all-weather constant; it has been used as an approximate sunlight factor, e.g. [Hassanein et al.](https://pmc.ncbi.nlm.nih.gov/articles/PMC5298785/) |
| Combined lux with one factor | Not generally robust | Different daylight/electric SPDs need separate conversion before summation |

The current workflow is appropriate for **traceable preliminary daylight
PPFD/DLI estimates**, when the factor is explicit and locally checked. It is
not sufficient alone for high-confidence predictions in spectrally selective or
mixed-light greenhouses.

## Recommended enhancements

### Priority 1 — make the current method safer

1. Call lux-derived outputs **estimated PPFD** and **estimated DLI**.
2. Persist factor value/source, SPD-file hash, wavelength range, sampling
   interval, and calculation mode with PPFD/DLI outputs. The current manifest
   establishes illuminance provenance, not factor-assumption provenance.
3. Convert source terms separately for mixed light:

   ```text
   PPFD_total = lux_daylight × f_daylight + lux_electric × f_electric
   ```

   Do not add daylight and electric lux first and apply one factor unless their
   SPDs are demonstrably equivalent.
4. Warn when a default factor is used, an SPD changes, or a spectral-glazing/
   luminaire study has no SPD basis.

### Priority 2 — validate against measurements

Create a reference-case harness before claiming accuracy:

1. Use a calibrated cosine-corrected quantum sensor or spectroradiometer at the
   same canopy plane, with calibration/orientation/sampling recorded.
2. Measure a grid through clear, overcast, and partly cloudy periods; use local
   observed weather rather than a typical-year EPW for the validation case.
3. Match geometry, glazing, sensor coordinates, and timestamps in the model.
4. Compare lux and PPFD separately; report bias, RMSE/CVRMSE, slope, and sky
   condition rather than only annual totals.
5. Compare direct sun with point-in-time Radiance as well as annual matrices.

Prior validation of dynamic Radiance daylight-coefficient/Perez-sky predictions
against a full-scale test office supports this method family, not this plugin:
[Reinhart & Walkenhorst (2001)](https://doi.org/10.1016/S0378-7788%2801%2900058-5).

### Priority 3 — evaluate spectral transport with independent validation

For spectral glazing, coloured surfaces, canopies, or mixed daylight/LEDs, add
an optional spectral PPFD mode instead of inferring spectrum from final lux. It
requires wavelength-banded sky/daylight and luminaire SPDs; matching glazing,
material, and optionally canopy optical data; one radiometric transport
calculation resolved by wavelength; and photon
integration over 400–700 nm. Treat extended PPFD over 400–750 nm as a separate,
explicit metric rather than silently mixing it with traditional PPFD. Far-red
band quantities also require their own declared bounds and units.

Radiance 6 introduced native spectral rendering, while retaining RGB as the
default: see the developers' [release announcement](https://discourse.radiance-online.org/t/official-radiance-6-0-release/6873).
Evaluate this route before assuming another engine or repeated RGB runs are
necessary. Installed Windows executables, spectral sky data, matrix formats and
integration require a separate feasibility check. Spectral mode alone does not
establish research-grade accuracy.

This is a new data/model pipeline, not a small factor tweak. FlahaGrow's current
annual matrices are RGB; the official `gendaymtx` manual says each entry has
RGB radiance values. RGB/lux output cannot uniquely reconstruct SPD. Start with
separate daylight/electric factors and measurements; pursue banded transport
only where the decision requires it.

## Decision rule

Use the current workflow for feasibility studies only when reports state
“lux-derived PPFD/DLI,” name the factor and SPD basis, and include a local
check. Require Priority 1 plus measurement validation for operational lighting
or crop-target decisions. Require validated spectral transport where spectral
glazing, strongly coloured surfaces, or multi-spectrum lighting affects the
decision.
