# 07 Energy: component reference

Integrate an electrical power schedule. This category does not infer electrical watts from lux, PPFD or an IES file.

[Reference guide and shared contracts](README.md) · [All components](navigation.md)

Documentation reviewed: **2026-09-13**. This is the current working-tree source contract, not a deployment claim. Per-component versions below come from the revision ledger.

## Components

- [Lighting Energy](#annuallightingenergycomponent)

<a id="annuallightingenergycomponent"></a>

## Lighting Energy

<!-- component: 5f1d58a4-064f-4fc9-b79b-640a380a3e43 -->

- Short name / nickname: `Energy`.
- Category: 07 Energy; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [AnnualLightingEnergyComponent](../../src/FlahaGrow.Grasshopper/Components/AnnualLightingEnergyComponent.cs); component ID: `5f1d58a4-064f-4fc9-b79b-640a380a3e43`.

### Short description

Calculates lighting energy use from a power schedule.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | W | Power schedule | Param_Number | list | Required registration | None registered | Finite nonnegative power samples | W | Lighting power for each timestep in watts. | Installation electrical power schedule, not lux or dimming fractions |
| 1 | dt | Timestep | Param_Number | item | Required registration; default supplied | 3600 | Finite positive uniform timestep | s | Duration of each power sample in seconds. | Sampling interval of the supplied numeric series (annual cache = 3600) |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | kWh | Energy | Param_Number | item | Sum(W) × dt / 3,600,000 | kWh | Total lighting energy in kilowatt-hours. | Integrated electrical power |
| 1 | Hours | Operating hours | Param_Number | item | Count(W) × dt / 3600, including zero-power samples | h | Number of schedule hours represented. | Represented sample duration |

### Workflow description

Construct the installation's electrical power series in watts, connect W and the uniform timestep dt, and read kWh.

### Notes

kWh = sum(W) × dt / 3,600,000. Hours is represented duration including zero-power intervals, not actual on-time. Any series length is accepted. Finite positive dt and finite nonnegative watts are required; nonfinite energy or duration is rejected before outputs are published. IES/lux/PPFD and dim fractions alone are not watts.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.1.0**. Last component update: **2026-09-13T16:00:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Rejects nonfinite power/timestep and arithmetic overflow before publishing energy or duration.
