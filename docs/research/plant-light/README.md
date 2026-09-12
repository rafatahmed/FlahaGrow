# Plant-light scientific reference package

Research and design material for FlahaGrow. The CIE photopic weighting CSV and
metadata are embedded by Core with attribution. The Spectral Profile table now
offers 18 pre-integrated references (three daylight, nine LEDs, six horticultural
treatments), with explicit acknowledgment of research limitations. The archived
audit's candidate status describes its research snapshot, not the current UI.
These are not project-validated fixture presets. Scope: daylight, white LEDs and horticultural LEDs.
Source retrieval and code assessment: 2026-09-12.

Start with the [research report](radiance-ppfd-dli-research.md), then the
[profile contract](profile-library-specification.md) and
[workflow/task plan](../../architecture/plant-light-workflow-plan.md).

## Contents and reproduction

- `data/cie/`: six unchanged official CSV datasets and their JSON metadata.
- `data/horticulture/`: unchanged Park–Runkle S1 spectrum workbook and Figshare
  metadata. Other workbooks named in that metadata are not downloaded.
- [profile-audit.json](profile-audit.json): derived factors, source ranges,
  original units/basis, column/cell mappings, checksums and comparison with
  seven existing hardcoded factors. This is a research inventory, not a
  production manifest or a list of scientifically validated fixture presets.
- [audit_profiles.py](audit_profiles.py): standalone, read-only numerical audit.
  Uses Python 3.10+ standard library, no package installation or network access.

From the repository root:

```powershell
python docs/research/plant-light/audit_profiles.py --check
```

Without `--check`, the script prints the full JSON calculation to stdout. It
never rewrites the inputs. Checks cover source hashes, snapshot consistency,
interpolation, exact PAR endpoints, scale invariance, DLI arithmetic, and six
invalid-input cases. They do not execute the C# importer or a Radiance model.

The inventory contains 23 spectrum calculations: 18 in the requested priority
scope, plus five discharge-lamp references to audit existing selections. The
photopic sensitivity dataset is a weighting function, not a light profile.

## Attribution and redistribution

The CIE datasets retain their original filenames, values and metadata. Credit:
International Commission on Illumination (CIE), Vienna, Austria. Each adjacent
metadata file states **CC BY-SA 4.0**, identifies its DOI/publication and links
the [license](https://creativecommons.org/licenses/by-sa/4.0/).
The CIE-derived factor records in `profile-audit.json` are adaptations by the
FlahaGrow research documentation: linear resampling, band integration and
unit conversion as specified in the report; these records are provided under
the same CC BY-SA 4.0 terms. No CIE endorsement is implied. Review the final
distribution notices before bundling datasets with a plugin release; this
notice does not relicense unrelated repository code.

The horticultural workbook is by Yujin Park and Erik S. Runkle, supporting
their 2018 paper, DOI
[10.1371/journal.pone.0202386](https://doi.org/10.1371/journal.pone.0202386).
Dataset: [10.6084/m9.figshare.6946136.v1](https://doi.org/10.6084/m9.figshare.6946136.v1),
**CC BY 4.0**. File ID 12739562, original name
`S1 File. Raw data supporting Fig 1..xlsx`; stored locally as
`park-runkle-2018-s1.xlsx` without changing file bytes. Its derived records
retain that attribution and license. No manufacturer endorsement or commercial
fixture certification is implied.

## Provenance exceptions

All six CIE CSVs match the MD5 values in their downloaded metadata. Three also
match their published SHA-256 values. D65, D75 and HP metadata SHA-256 strings
have respectively 63, 62 and 63 characters instead of 64; they do not match the
downloaded files. Original metadata is retained, not silently corrected.
Locally computed full SHA-256 values are recorded for reproducibility. This
records a publisher-metadata discrepancy; it does not establish why it occurred.

The V(lambda) landing page displays MD5 `272297df28120479b5a1ffb9a0388b02`,
whereas the downloaded metadata and file agree on
`f389958555461a7d9a7562145e8ca9c0` and also agree on SHA-256. Record both when
seeking publisher clarification. MD5 agreement is a transfer cross-check, not
a substitute for authenticated provenance or a modern release-integrity policy.

Some landing-page `_metadata_v2.json` links returned 404 during retrieval;
the retained metadata came from the corresponding official
`https://files.cie.co.at/<filename>.csv_metadata.json` endpoints.

Do not load the raw files directly into the existing spectral component:
CIE CSVs are headerless, several are multi-column, and the horticultural
workbook contains **photon** rather than **energy** spectra. Import adapters,
coverage review and approved profile states are still required.
