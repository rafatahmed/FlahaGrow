# 02 Spectral: component reference

Choose a referenced spectral profile for source-specific lux conversion. Built-in daylight, white LED and horticultural references need no user CSV; custom CSVs remain available for measured/project-specific spectra.

[Reference guide and shared contracts](README.md) · [All components](navigation.md)

Documentation reviewed: **2026-09-13**. This is the current working-tree source contract, not a deployment claim. Per-component versions below come from the revision ledger.

## Components

- [Custom Spectral Profile](#customspectralprofilecomponent)
- [Spectral Profile](#spectralprofilecomponent)

<a id="customspectralprofilecomponent"></a>

## Custom Spectral Profile

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa410 -->

- Short name / nickname: `Custom Profile`.
- Category: 02 Spectral; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [CustomSpectralProfileComponent](../../src/FlahaGrow.Grasshopper/Components/PlantLightComponents.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa410`.

### Short description

Advanced custom factor/CSV entry. For bundled references use Spectral Profile and a Button.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Label | Source label | Param_String | item | Required registration | None registered | Nonblank descriptive text | — (not a physical quantity) | Required source/state label. A label does not certify a fixture. | User value or upstream output described below |
| 1 | Factor | Explicit factor | Param_Number | item | Optional registration; see workflow | None registered | Finite number ≥ 0 | µmol/m²/s per lux | µmol/m²/s per lux. Connect this OR CSV, never both. Existing spectral selector's Factor can connect here. | Spectral Profile.Factor / Custom Spectral Profile.Factor |
| 2 | CSV | Spectral CSV | Param_String | item | Optional registration; see workflow | None registered | Existing CSV file: wavelength_nm,value; basis/coverage rules in Notes | — (not a physical quantity) | Headered wavelength_nm,value CSV. Explicit basis below; no raw multi-profile CIE files. | User spectral measurements or referenced source CSV |
| 3 | Basis | Spectral basis | Param_Integer | item | Required registration; default supplied | 0 | 0 energy; 1 photon | — (not a physical quantity) | 0 = energy spectrum; 1 = photon spectrum. Applies to CSV only; relative normalization yields a ratio only. | Source dataset's declared spectral basis |
| 4 | Tails | Accept zero tails | Param_Boolean | item | Required registration; default supplied | False | False requires photopic coverage; True explicitly accepts unmeasured tails as zero | — (not a physical quantity) | Explicitly assume unmeasured wavelengths outside CSV coverage are zero. PAR coverage remains mandatory. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Profile | Spectral Profile | SpectralProfileParameter | item | Typed spectral profile with factor, identity and assumptions | — (not a physical quantity) | Typed assumption for Plant Light Context. | Spectral Profile → Plant Light Context.Profile |
| 1 | Factor | Factor | Param_Number | item | Finite number ≥ 0 | µmol/m²/s per lux | µmol/m²/s per lux. | Spectral calculation/preset → numeric Factor inputs |
| 2 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Method, provenance and limitations. | Component diagnostics → Panel |

### Workflow description

Provide Label and exactly one of Factor or CSV; for CSV declare energy/photon basis and any acknowledged missing photopic tails.

### Notes

CSV uses wavelength_nm,value, strictly increasing positive wavelengths and finite nonnegative samples. PAR coverage 400–700 nm is mandatory; photopic coverage is 360–830 nm unless zero tails are acknowledged. Conversion uses CIE V(lambda) and 1 nm trapezoidal integration; provenance includes a source hash.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.2.1**. Last component update: **2026-09-12T14:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Renamed Custom Spectral Profile; existing GUID and five inputs retained.

<a id="spectralprofilecomponent"></a>

## Spectral Profile

<!-- component: a9c4973b-acb7-45be-96ee-a6d8a35fa417 -->

- Short name / nickname: `Profile`.
- Category: 02 Spectral; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [SpectralProfileComponent](../../src/FlahaGrow.Grasshopper/Components/SpectralProfileComponent.cs); component ID: `a9c4973b-acb7-45be-96ee-a6d8a35fa417`.

### Short description

Choose a bundled, referenced spectrum. No CSV required. Selection acknowledges the displayed research assumptions.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Select | Select profile | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Connect a Grasshopper Button. Click to open the reference table; choose a row and accept its assumptions. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Profile | Spectral Profile | SpectralProfileParameter | item | Typed spectral profile with factor, identity and assumptions | — (not a physical quantity) | Connect to Plant Light Context → Profile. | Spectral Profile → Plant Light Context.Profile |
| 1 | Factor | Conversion factor | Param_Number | item | Finite number ≥ 0 | µmol/m²/s per lux | µmol/m²/s per lux. For numeric conversion inputs; the typed Profile already includes this value. | Spectral calculation/preset → numeric Factor inputs |
| 2 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Selected reference, source DOI, method and limitations. Connect to a Panel to inspect. | Component diagnostics → Panel |

### Workflow description

Pulse Select, choose a built-in reference, acknowledge its assumptions, and connect Profile to Plant Light Context.

### Notes

18 built-in references: daylight, white LEDs and horticultural treatments. No CSV is required. Selection persists by profile ID/library revision; changed revisions require reselection. Reference spectra do not certify a commercial fixture or the spectrum received at the sensor.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.0.1**. Last component update: **2026-09-12T15:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Acknowledged research limitations remain in Status without repeated runtime warning; readable wavelength coverage.
