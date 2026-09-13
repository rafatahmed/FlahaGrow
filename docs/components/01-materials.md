# 01 Materials: component reference

Choose opaque/glazing library modifiers for subsequent model/scene assignment. These selectors do not assign materials to geometry automatically.

[Reference guide and shared contracts](README.md) · [All components](navigation.md)

Documentation reviewed: **2026-09-13**. This is the current working-tree source contract, not a deployment claim. Per-component versions below come from the revision ledger.

## Components

- [Opaque Material](#opaquematerialcomponent)
- [Glazing Material](#glazingmaterialcomponent)

<a id="opaquematerialcomponent"></a>

## Opaque Material

<!-- component: 29e2836f-5da0-4e4c-bdac-990365a0471e -->

- Short name / nickname: `Opaque Mat`.
- Category: 01 Materials; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [OpaqueMaterialComponent](../../src/FlahaGrow.Grasshopper/Components/MaterialSelectors.cs); component ID: `29e2836f-5da0-4e4c-bdac-990365a0471e`.

### Short description

Selects a Radiance opaque material for any opaque surface.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Open the material selector. | Button/toggle; triggering behavior in workflow |
| 1 | Materials | RadMaterials folder | Param_String | item | Optional registration; see workflow | None registered | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Optional RadMaterials folder, FlahaGrow library root, or its containing folder. Leave empty to use the bundled library. | User value or upstream output described below |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Modifier | Modifier | Param_String | item | Selected Radiance material identifier | — (not a physical quantity) | Selected Radiance material modifier. | Library selection → model/scene material assignment |

### Workflow description

Trigger Run, choose an opaque material from the library, and apply Modifier in the scene/model export workflow.

### Notes

Output is the selected material identifier, not geometry or full Radiance material text; assignment is not automatic. Saved selection can be re-emitted. Use a Button for the level-triggered chooser. Library photopic optical properties are not a source spectral profile.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.5.0**. Last component update: **2026-09-13T12:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Consolidated duplicate components into one tool with shared behavior; canonical GUID and port order retained.

<a id="glazingmaterialcomponent"></a>

## Glazing Material

<!-- component: 53402415-6620-4ecd-bfa3-593e7a148f29 -->

- Short name / nickname: `Glazing Mat`.
- Category: 01 Materials; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [GlazingMaterialComponent](../../src/FlahaGrow.Grasshopper/Components/GlazingMaterialComponent.cs); component ID: `53402415-6620-4ecd-bfa3-593e7a148f29`.

### Short description

Selects a Radiance glazing modifier and reports its visual properties.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Run | Run | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Open the glazing selector. | Button/toggle; triggering behavior in workflow |
| 1 | Glazing | RadGlazing folder | Param_String | item | Optional registration; see workflow | None registered | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Optional RadGlazing folder, FlahaGrow library root, or its containing folder. Leave empty to use the bundled library. | User value or upstream output described below |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Modifier | Modifier | Param_String | item | Selected Radiance material identifier | — (not a physical quantity) | Selected Radiance glazing modifier. | Library selection → model/scene material assignment |

### Workflow description

Trigger Run, choose a glazing entry from the library, and apply Modifier in the scene/model export workflow.

### Notes

Output is the selected material identifier, not geometry or full Radiance material text; assignment is not automatic. Saved selection can be re-emitted. Use a Button for the level-triggered chooser. Library photopic optical properties are not a source spectral profile.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.1.0**. Last component update: **2026-09-11T22:57:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Restores the legacy material-table workflow: preview bitmap, display name, RGB/VLR or RGB/VLT/VLR summary, Python ordering, Select, and Reload.
