# 04 Electric Light: component reference

Select IES photometry, place luminaires, simulate electric illuminance and associate its schedule with verified weather. Photometric IES and spectral profiles are separate inputs to separate stages.

[Reference guide and shared contracts](README.md) · [All components](navigation.md)

Documentation reviewed: **2026-09-13**. This is the current working-tree source contract, not a deployment claim. Per-component versions below come from the revision ledger.

## Components

- [Compile Luminaires](#compileluminariescomponent)
- [Electric Annual Simulation](#electricannualsimulationcomponent)
- [IES to Radiance](#iestoradiancecomponent)
- [Lighting Geometry](#lightinggeometrycomponent)
- [Select IES Luminaire](#iesluminaireselectorcomponent)

<a id="compileluminariescomponent"></a>

## Compile Luminaires

<!-- component: 31b19b55-7384-4f37-bb1a-f436c3cbaa8b -->

- Short name / nickname: `Compile Lights`.
- Category: 04 Electric Light; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [CompileLuminariesComponent](../../src/FlahaGrow.Grasshopper/Components/CompileLuminariesComponent.cs); component ID: `31b19b55-7384-4f37-bb1a-f436c3cbaa8b`.

### Short description

Writes luminaries.rad in the project Luminaire_files folder.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | xform | Lighting geometry | Param_String | list | Required registration | None registered | Radiance transformation command strings; one fixture placement per item | — (not a physical quantity) | Radiance xform placement lines. | Lighting Geometry.xform |
| 1 | Project | Project folder | Param_String | item | Required registration | None registered | Project/export folder path (not typed Project context) | — (not a physical quantity) | FlahaGrow project folder. | Project folder chosen by user; see component workflow |
| 2 | Write | Write | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Write luminaries.rad. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Rad | Luminaire Radiance file | Param_String | item | Radiance .rad file path(s) | — (not a physical quantity) | Path to luminaries.rad. | Generated Radiance file(s) → placement or simulation (see workflow) |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Write status. | Component diagnostics → Panel |

### Workflow description

Connect Lighting Geometry.xform and the project folder; set Write to generate the assembled luminaire file; send Rad to Electric Annual Simulation.Lum.

### Notes

Writes/overwrites Luminaire_files/luminaries.rad; its directory must already exist. This assembles scene text, not a Radiance calculation. Write is level-triggered.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.0.1**. Last component update: **2026-09-11T12:48:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Canvas revision labels now include an exact update time.

<a id="electricannualsimulationcomponent"></a>

## Electric Annual Simulation

<!-- component: b87a6c40-49df-4aef-9ee4-99d5d806bb2d -->

- Short name / nickname: `Electric Annual`.
- Category: 04 Electric Light; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [ElectricAnnualSimulationComponent](../../src/FlahaGrow.Grasshopper/Components/ElectricAnnualSimulationComponent.cs); component ID: `b87a6c40-49df-4aef-9ee4-99d5d806bb2d`.

### Short description

Runs a full-output Radiance electric-light calculation and expands it by one validated 8,760-hour dimming schedule.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Project | Project folder | Param_String | item | Required registration | None registered | Project/export folder path (not typed Project context) | — (not a physical quantity) | Honeybee ModelToRad export root containing model/scene files. | Honeybee/Radiance exported model/grid and scene root |
| 1 | Lum | Luminaire Radiance file | Param_String | item | Required registration | None registered | Existing assembled Radiance luminaire scene file | — (not a physical quantity) | Compiled luminaries.rad or one Radiance luminaire file. | Compile Luminaires.Rad |
| 2 | Dim | Dimming schedule | Param_Number | list | Required registration | None registered | Exactly 8760 finite values in [0,1] | Fraction of full output | Exactly 8,760 hourly fractions in [0,1]. One common schedule controls the supplied luminaire set. | User-generated electrical-light dimming schedule on the selected weather axis |
| 3 | Pts | Sensor points | Param_Point | list | Optional registration; see workflow | None registered | Point3d(s); exact saved order for result sensors | Rhino model length units | Optional sensor points; otherwise the exported .pts grid is used. | GenPts/exported grid for sensors; fixture layout points for Lighting Geometry |
| 4 | Detail | Detail | Param_String | item | Required registration; default supplied | mid | low / mid / high / veryhigh, or custom Radiance parameters | — (not a physical quantity) | low, mid, high, or very high; controls Radiance indirect sampling. | User simulation quality setting |
| 5 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Launch the full-output Radiance calculation once on a false→true edge. | Button/toggle; triggering behavior in workflow |
| 6 | Bin | Radiance bin folder | Param_String | item | Optional registration; see workflow | None registered | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Optional Radiance bin folder. Leave blank for automatic detection. | User value or upstream output described below |
| 7 | Radiance | Radiance Environment | RadianceParameter | item | Optional registration; see workflow | None registered | Typed FlahaGrow object; do not substitute text | — (not a physical quantity) | Optional checked environment; when connected it must be ready for electric lighting and supplies exact executables. | Radiance Status.Radiance |
| 8 | Existing | Existing run folder | Param_String | item | Optional registration; see workflow | None registered | Existing run folder; optional inspection/reuse path | — (not a physical quantity) | Optional manifest-owned completed electric annual run to reopen. | Previously generated run Folder |
| 9 | Time Result | Schedule time reference | AnnualResultParameter | item | Optional registration; see workflow | None registered | LoadedAnnualResult typed object, with verified weather when available | — (not a physical quantity) | Optional Load Annual Result → Result from the daylight study. Connecting declares that the 8760 dimming values use that verified weather calendar/local standard time. Snapshot is retained for automatic timing; no manual UTC. | Load Annual Result.Result |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Folder | Result folder | Param_String | item | Text; see description | — (not a physical quantity) | Manifest-owned folder containing annualRfinal_part0.ill. | Produced by this component; see workflow |
| 1 | Full Lux | Full-output illuminance | Param_Number | list | Finite nonnegative illuminance values | lux | Radiance full-output illuminance at every sensor before schedule dimming. | Radiance/cache photopic illuminance → illuminance plot or numeric conversion |
| 2 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Preparation, execution, or validation status. | Component diagnostics → Panel |

### Workflow description

Connect scene, assembled Lum, a full 8760-value dimming schedule and Radiance; connect daylight Result to Time Result when that schedule uses the same weather axis. Run and load Folder separately.

### Notes

Calculates full-output illuminance and applies one shared annual dimming schedule. Independently controlled source/channel spectra require separate runs and contexts. Time Result copies verified EPW provenance; it does not prove that a user-authored schedule is aligned. One/four partitions are supported despite older part0-only tooltip wording. No Cancel port is exposed.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.6.0**. Last component update: **2026-09-13T15:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Shared bounded Radiance discovery; explicit locations never fall back to another installation; no fixed drive paths.

<a id="iestoradiancecomponent"></a>

## IES to Radiance

<!-- component: e64e15f4-7cee-48b2-a232-2064d3a9e602 -->

- Short name / nickname: `IES→Rad`.
- Category: 04 Electric Light; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [IesToRadianceComponent](../../src/FlahaGrow.Grasshopper/Components/IesToRadianceComponent.cs); component ID: `e64e15f4-7cee-48b2-a232-2064d3a9e602`.

### Short description

Converts an IES luminaire to Radiance files and applies normalized RGB channels.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | IES | IES path | Param_String | item | Required registration | None registered | Existing .ies photometry file or library folder, as described | — (not a physical quantity) | IES file path. | Select IES Luminaire.IES (conversion), or user/library folder (selector) |
| 1 | Name | Luminaire name | Param_String | item | Optional registration; see workflow | None registered | Text; see description | — (not a physical quantity) | Output luminaire name. Blank uses the IES filename. | User value or upstream output described below |
| 2 | R | Red | Param_Number | item | Required registration; default supplied | 1 | Finite ≥ 0; at least one RGB channel must be positive | Relative channel weight | Red channel multiplier. | User value or upstream output described below |
| 3 | G | Green | Param_Number | item | Required registration; default supplied | 1 | Finite ≥ 0; at least one RGB channel must be positive | Relative channel weight | Green channel multiplier. | User value or upstream output described below |
| 4 | B | Blue | Param_Number | item | Required registration; default supplied | 1 | Finite ≥ 0; at least one RGB channel must be positive | Relative channel weight | Blue channel multiplier. | User value or upstream output described below |
| 5 | M | Multiplier | Param_Number | item | Optional registration; see workflow | None registered | Finite positive multiplier; leave unwired for converter default | Multiplier | Optional finite positive ies2rad multiplier; leave unwired to use the converter default. | User value or upstream output described below |
| 6 | Project | Project folder | Param_String | item | Required registration | None registered | Project/export folder path (not typed Project context) | — (not a physical quantity) | Simulation project folder; Luminaire_files is created inside it. | Project folder chosen by user; see component workflow |
| 7 | DAT | DAT file | Param_String | item | Optional registration; see workflow | None registered | Optional existing/source or generated .dat file path(s), as described | — (not a physical quantity) | Optional replacement data-file path. | User value or upstream output described below |
| 8 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Connect a Button. False to True starts background conversion once; timeout is two minutes. Changed inputs or closing the document cancel pending work. | Button/toggle; triggering behavior in workflow |
| 9 | Bin | Radiance bin folder | Param_String | item | Optional registration; see workflow | None registered | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Optional folder containing ies2rad.exe. Leave empty for automatic detection. | User value or upstream output described below |
| 10 | Radiance | Radiance Environment | RadianceParameter | item | Optional registration; see workflow | None registered | Typed FlahaGrow object; do not substitute text | — (not a physical quantity) | Optional checked Radiance Status environment. When connected, it must be ready for electric-light preparation and determines the exact executable environment. | Radiance Status.Radiance |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Rad | Radiance files | Param_String | list | Radiance .rad file path(s) | — (not a physical quantity) | Generated .rad paths. | Generated Radiance file(s) → placement or simulation (see workflow) |
| 1 | DAT | Data files | Param_String | list | Optional existing/source or generated .dat file path(s), as described | — (not a physical quantity) | Generated .dat paths. | Produced by this component; see workflow |
| 2 | Log | Log | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Command and conversion log. | Component diagnostics → Panel |

### Workflow description

Connect selected IES, project folder and Radiance; choose optional RGB/multiplier settings and run conversion. Use Rad in Lighting Geometry.

### Notes

Idle evaluation writes no files. Pulse Run with a Button: conversion runs in the background once per false-to-true edge, with a two-minute timeout. Holding True reuses the completed result. Changed inputs or closing the document cancel pending work; pulse Run again after changing inputs. RGB must be finite and nonnegative with at least one positive channel; normalization uses 0.265/0.67/0.065 weights without overflow. RGB is not a spectral profile. A supplied multiplier must be finite and positive; a supplied DAT must exist. Conversion uses isolated staging and requires fresh RAD and referenced DAT output. Only light/illum emitter RGB arguments are rewritten; geometry is preserved. The named final files can be overwritten after validation. An explicit DAT override is preserved and returned instead of generated DAT files. Publication is atomic per file, not across the entire RAD/DAT set. An explicit Radiance location is authoritative and does not fall back to another installation.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.7.0**. Last component update: **2026-09-13T16:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Background edge-triggered IES conversion with bounded process I/O, isolated fresh outputs, input validation and primitive-aware RGB rewriting.

<a id="lightinggeometrycomponent"></a>

## Lighting Geometry

<!-- component: 2bb0d862-d310-4c90-8836-3760fd9870c5 -->

- Short name / nickname: `Light Geometry`.
- Category: 04 Electric Light; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [LightingGeometryComponent](../../src/FlahaGrow.Grasshopper/Components/LightingGeometryComponent.cs); component ID: `2bb0d862-d310-4c90-8836-3760fd9870c5`.

### Short description

Generates xform commands that position Radiance luminaire files at points.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Pts | Points | Param_Point | list | Required registration | None registered | Point3d(s); exact saved order for result sensors | Rhino model length units | Luminaire placement points. | GenPts/exported grid for sensors; fixture layout points for Lighting Geometry |
| 1 | Rx | X rotation | Param_Number | list | Required registration; default supplied | 0 | One rotation broadcasts; otherwise one per point | degrees | X-axis rotations in degrees; one value broadcasts. | User fixture orientation |
| 2 | Ry | Y rotation | Param_Number | list | Required registration; default supplied | 0 | One rotation broadcasts; otherwise one per point | degrees | Y-axis rotations in degrees; one value broadcasts. | User fixture orientation |
| 3 | Rz | Z rotation | Param_Number | list | Required registration; default supplied | 0 | One rotation broadcasts; otherwise one per point | degrees | Z-axis rotations in degrees; one value broadcasts. | User fixture orientation |
| 4 | Rad | Radiance files | Param_String | list | Required registration | None registered | Radiance .rad file path(s) | — (not a physical quantity) | One .rad path broadcasts, or provide one per point. | IES to Radiance.Rad |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | xform | Lighting geometry | Param_String | list | Radiance transformation command strings; one fixture placement per item | — (not a physical quantity) | Radiance xform placement lines. | Lighting Geometry → Compile Luminaires.xform |
| 1 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Validation status. | Component diagnostics → Panel |

### Workflow description

Connect fixture placement points, singleton-or-per-point rotations and Rad files; send xform to Compile Luminaires.

### Notes

Rotation order is X, Y, Z then translation; degrees and model coordinates are used. A single value broadcasts, otherwise list lengths must match points. Outputs command text, not simulated light. Points and rotations must be finite/valid, files must exist, and paths containing control characters, quotes, percent signs or exclamation marks are rejected before command generation.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.1.0**. Last component update: **2026-09-13T16:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Rejects invalid placement geometry, nonfinite rotations, missing files and unsafe command path characters.

<a id="iesluminaireselectorcomponent"></a>

## Select IES Luminaire

<!-- component: 492e14e7-163e-4c2a-a6d8-c44184da664d -->

- Short name / nickname: `IES Select`.
- Category: 04 Electric Light; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [IesLuminaireSelectorComponent](../../src/FlahaGrow.Grasshopper/Components/IesLuminaireSelectorComponent.cs); component ID: `492e14e7-163e-4c2a-a6d8-c44184da664d`.

### Short description

Selects an IES grow-light luminaire.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Open the IES luminaire selector. | Button/toggle; triggering behavior in workflow |
| 1 | IES | RadIES folder | Param_String | item | Optional registration; see workflow | None registered | Existing .ies photometry file or library folder, as described | — (not a physical quantity) | Optional RadIES folder, FlahaGrow library root, or its containing folder. Leave empty to use the bundled library. | Select IES Luminaire.IES (conversion), or user/library folder (selector) |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | IES | IES path | Param_String | item | Existing .ies photometry file or library folder, as described | — (not a physical quantity) | Selected IES file path. | IES library selection → IES to Radiance.IES |
| 1 | Name | Luminaire name | Param_String | item | Text; see description | — (not a physical quantity) | Selected luminaire identifier. | Produced by this component; see workflow |

### Workflow description

Trigger the library chooser, select an IES file and connect IES to IES to Radiance.

### Notes

Saved selection is reused. IES photometry does not identify or validate a spectral profile; match fixture/channel spectra separately. A Button avoids repeatedly opening a level-triggered dialog.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.0.2**. Last component update: **2026-09-11T17:45:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Persists the selected library item and re-emits it without reopening its dialog.
