# Spectral profile library specification

Target contract; not all fields and release gates are implemented. The current
Spectral Profile table exposes 18 bundled audited references with explicit
assumption acknowledgment, not production-validated fixture records. See the
[implemented selector workflow](../../workflows/plant-light-workflow.md#built-in-spectral-selection).
Scientific justification
and sources are in the [research report](radiance-ppfd-dli-research.md).
First-release scope: daylight, white LEDs and horticultural LEDs.

## 1. Library categories and readiness

| Category | Available evidence | Initial treatment |
| --- | --- | --- |
| Reference daylight | CIE D55/D65/D75 data | Named reference candidates; review coverage and publisher metadata exceptions |
| Reference LEDs | Nine CIE 1 nm illuminants | Reference candidates, not manufacturer products |
| Horticultural research | Six Park–Runkle measured treatment spectra | Research-only candidates; 400–799 nm coverage, photon basis |
| Measured fixtures | No matched commercial fixture files curated here | Import specification only; no invented presets |
| Discharge references | Five CIE HP distributions | Existing-preset audit, outside first-release priority |
| Material optics | No approved glazing dataset bundled | Separate material-data type, never a source SPD |

Preserve official labels. In particular, do not automatically relabel every CIE
HP distribution as HPS. Resolve lamp-type metadata before changing the existing
`CIE-HPS-1` and `CIE-HPS-5` names.

Each record has a lifecycle: `candidate`, `reviewed-reference`,
`reviewed-measured-source`, `project-validated`, or `deprecated`. A numerical
factor is not promoted because it matches another calculator. Project
validation is scoped to a declared configuration and does not become a global
property of the reference dataset.

## 2. Required record fields

| Group | Required fields / rules |
| --- | --- |
| Identity | Stable profile ID, revision, display name, category, status, schema version |
| Attribution | Author/publisher, title, publication/test date when available, DOI/source URL, retrieval date, license and redistribution status |
| Original data | Relative asset path, original filename, SHA-256 of exact bytes, original columns/sheet/cells, original units and spectral basis |
| Spectral definition | Energy/photon/relative-energy/relative-photon basis; wavelength units, bounds, spacing/resolution, normalization and measurement geometry |
| Calculation | Algorithm version, PAR bounds, photopic dataset ID/hash, interpolation/integration and out-of-range policies, factor and factor unit |
| Provenance checks | Published checksum(s), local checksums, match status, explicit exceptions; never overwrite malformed publisher values |
| Applicability | Reference versus measured, source versus received-light spectrum, expected source state, known limitations and uncertainty evidence |
| Fixture association | Manufacturer, model, channel/drive state, optical configuration, report ID/lab, matched photometry hash when applicable |
| Validation | Test/reference IDs, assessed configuration, error metrics, date and review status; no inferred accuracy percentage |

The existing [research inventory](profile-audit.json) contains a subset for
reproducibility. It deliberately does not pretend to satisfy this production
schema. Licenses remain attached to individual datasets and their adaptations.

## 3. Import and calculation policy

1. Require explicit spectral basis and wavelength units. Do not infer watts
   from any numeric second column or accept a photon spectrum as energy.
2. Recognize headerless CIE data through a source-specific adapter; select a
   named column explicitly. Retain the original multi-column file unchanged.
3. Reject nonfinite values, negative physical spectra, duplicate wavelengths,
   unordered wavelengths, empty data, incomplete PAR coverage and nonpositive
   photopic denominators. If background-correction processing is necessary,
   it is a separate recorded operation, not silent clipping.
4. Preserve fractional wavelengths. Use documented interpolation/integration
   with exact band endpoints. Never silently forward-fill gaps. Flag large
   gaps or resolution inadequate for narrow features.
5. Require photopic coverage or an explicit, reviewed tail assumption. A
   400–700 nm dataset can describe PPFD yet be incomplete for the lux
   denominator. The research zero-tail rule is not automatic production consent.
6. Keep relative-data calculations to ratios. Absolute output requires a
   physical normalization and geometry. Return energy and photon integrals
   with their correct units only when those conditions are satisfied.
7. Persist method and data hashes with the result. A custom numeric override
   creates a custom-assumption profile; it must not keep the CIE source label.
8. Show source authenticity and scene representativeness separately. A
   publisher metadata exception or unknown fixture match must remain visible.

The first algorithm implementation should have independently calculated tests
for energy/photon equivalence, normalization invariance, endpoint handling,
irregular spacing and nonfinite/negative input. Compare against the research
snapshot, but do not use self-consistency with one script as the only oracle.

## 4. Runtime architecture

The source library belongs in a versioned data layer. Pure unit conversion,
profile validation and integration belong in Core without a Rhino dependency.
Grasshopper selects profiles and displays warnings; it should not contain a
second independent formula or parse a Python file to obtain reference tables.

`PlantLightContext` binds a validated result descriptor and immutable profile
revision. A future spectral result descriptor needs wavelength bounds/bins,
radiometric units and method metadata; leave room for it without making the
current scalar cache masquerade as spectral data.

For a multi-source context, associate a profile with every source contribution.
Validate sensor coordinates/order/normals, time intervals and units before
addition. Independent channel controls require separate schedules or an
explicitly validated varying-spectrum model. Keep illuminance combination
available separately from photon combination.

Cache derived slices by result content identity, profile revision/hash, method,
selection and timestep. Reopening a definition reconstructs and revalidates
descriptors; it must not deserialize trusted open-file state. Invalidation,
cancellation and bounded memory are acceptance requirements, not optional
cleanup after performance work.

## 5. Presentation contract

New output labels state quantity, unit, sensor/grid identity, interval/date,
method (`lux-derived estimate` or validated spectral method), profile ID and
run identity. Plotting receives these descriptors rather than guessing from
list length. Numeric ports remain possible for compatibility.

An hour is an interval in the annual time axis, not a new point-in-time solve.
A DLI grid identifies one complete day. “Hourly DLI” is either renamed
`Hourly photon integral` or documented as the hour's contribution to DLI.
Crop threshold bands are separate user assumptions, not universal PPFD/DLI
quality classes. Never compare different sensor heatmaps as an integration test.

## 6. Release gates

- Reviewed profile record, clear attribution and permission to distribute.
- Original and derived data reproducible; checksum exceptions resolved or
  explicitly accepted and documented by the release review.
- Numerical tests and independent reference values pass.
- Same-context PPFD and DLI agree for matching sensor/time selections.
- Mixed-source reference case detects the incorrect combined-lux shortcut.
- Old definition save/reopen and factor-change behavior pass.
- Performance is measured on realistic grids with no stale validated results.
- Any accuracy claim has matched measurements and a stated domain.

Do not publish the horticultural treatments as measured commercial fixtures.
The bounded next acquisition task is to curate licensed LM-79/TM-33 data for
specific white and grow-light models, including independently controlled
channels where relevant. Final fixture choices remain open; this does not
block the shared-context design or the reference-data tests.
