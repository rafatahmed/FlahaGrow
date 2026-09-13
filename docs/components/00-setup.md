# 00 Setup: component reference

Resolve paths, initialize a project/analysis workspace, and check Radiance. The three registered setup components cover the complete setup workflow.

[Reference guide and shared contracts](README.md) · [All components](navigation.md)

Documentation reviewed: **2026-09-13**. This is the current working-tree source contract, not a deployment claim. Per-component versions below come from the revision ledger.

## Components

- [Radiance Status](#radiancesetupcomponent)
- [Simulation Paths](#simulationpathssetupcomponent)
- [Working Directory](#projectworkspacecomponent)

<a id="radiancesetupcomponent"></a>

## Radiance Status

<!-- component: 9cd39fc4-7c35-4aee-a247-1c8b980c4b17 -->

- Short name / nickname: `Radiance`.
- Category: 00 Setup; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [RadianceSetupComponent](../../src/FlahaGrow.Grasshopper/Components/Setup/RadianceSetupComponent.cs); component ID: `9cd39fc4-7c35-4aee-a247-1c8b980c4b17`.

### Short description

Automatically locates Radiance and checks its version and requirements. Standalone Radiance is preferred; Ladybug Tools is supported. Right-click for advanced overrides.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Analysis | Analysis | AnalysisParameter | item | Optional registration; see workflow | None registered | Typed analysis context | — (not a physical quantity) | Optional Working Directory analysis. Its workflow determines required Radiance tools. Leave empty to check independently. | Working Directory.Analysis |
| 1 | Refresh | Refresh | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Optional Button to repeat discovery and the version check. The initial check is automatic. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Radiance | Radiance Environment | RadianceParameter | item | Typed FlahaGrow object; do not substitute text | — (not a physical quantity) | Selected installation, workflow, and check result. | Radiance Status → simulations / IES to Radiance.Radiance |
| 1 | Ready | Ready | Param_Boolean | item | True / False | — (not a physical quantity) | Required files, version probe, and small workflow execution check passed. Full simulation has not been tested. | Produced by this component; see workflow |
| 2 | Version | Version | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Version reported by the selected installation. | Component diagnostics → Panel |
| 3 | Bin | Bin folder | Param_String | item | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Automatically resolved executable folder. | Produced by this component; see workflow |
| 4 | Lib | Library folder | Param_String | item | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Resolved calculation-library folder. | Produced by this component; see workflow |
| 5 | Found | Installations | Param_String | list | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Discovered installations and missing requirements; right-click to choose an alternative. | Component diagnostics → Panel |
| 6 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Selection source, workflow, and diagnostics. | Component diagnostics → Panel |

### Workflow description

Connect Working Directory.Analysis, inspect Ready and Status, then connect Radiance to simulation or IES conversion.

### Notes

Readiness includes executable/version and workflow probes, not scientific validation of a project. Standalone Radiance is preferred; right-click settings can override discovery.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.0.1**. Last component update: **2026-09-11T12:48:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Canvas revision labels now include an exact update time.

<a id="simulationpathssetupcomponent"></a>

## Simulation Paths

<!-- component: 71ce89f2-1439-4730-915f-07436692926c -->

- Short name / nickname: `Paths`.
- Category: 00 Setup; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [SimulationPathsSetupComponent](../../src/FlahaGrow.Grasshopper/Components/Setup/SimulationPathsSetupComponent.cs); component ID: `71ce89f2-1439-4730-915f-07436692926c`.

### Short description

Resolves and remembers project and material-library locations without creating files. Right-click to reset the automatic location.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Location | Project location | Param_String | item | Optional registration; see workflow | None registered | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Optional project root or project manifest path. | User value or upstream output described below |
| 1 | Mode | Location mode | Param_Integer | item | Required registration; default supplied | 0 | 0 Auto; 1 Project-relative; 2 System; 3 Custom | — (not a physical quantity) | 0 Auto, 1 Project-relative, 2 System, 3 Custom. | User value or upstream output described below |
| 2 | Library | Library location | Param_String | item | Optional registration; see workflow | None registered | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Optional asset root or parent folder. | User value or upstream output described below |
| 3 | Refresh | Refresh | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Connect a Button to recheck files. Does not create folders. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Paths | Resolved Paths | PathsParameter | item | Typed FlahaGrow object; do not substitute text | — (not a physical quantity) | Connect to Working Directory. | Simulation Paths → Working Directory.Paths |
| 1 | Folder | Project folder | Param_String | item | Text; see description | — (not a physical quantity) | Resolved project location; write access has not been tested. | Produced by this component; see workflow |
| 2 | Library | Library folder | Param_String | item | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Resolved asset root. | Produced by this component; see workflow |
| 3 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Resolution source and diagnostics. | Component diagnostics → Panel |

### Workflow description

Choose path mode and optional locations; connect Paths to Working Directory.

### Notes

Read-only resolution; no project folders are created. Modes: 0 automatic, 1 project-relative, 2 system, 3 custom. Automatic root is remembered; right-click reset is available.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.0.1**. Last component update: **2026-09-11T12:48:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Canvas revision labels now include an exact update time.

<a id="projectworkspacecomponent"></a>

## Working Directory

<!-- component: d236c57b-eab1-4329-8e4e-beb2285ba04d -->

- Short name / nickname: `Workspace`.
- Category: 00 Setup; visibility: placeable (primary).
- Icon available: Yes — embedded name-matched bitmap; see [icon inventory](icons.md).
- Implementation: [ProjectWorkspaceComponent](../../src/FlahaGrow.Grasshopper/Components/Setup/ProjectWorkspaceComponent.cs); component ID: `d236c57b-eab1-4329-8e4e-beb2285ba04d`.

### Short description

Opens or explicitly initializes a project and named analysis. Connect a Button to Initialize.

### Inputs

| # | Port letter / nickname | Port name | Data type | Access | Required status | Default value | Valid range / format | Units | Description | Source / connection |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Paths | Resolved Paths | PathsParameter | item | Required registration | None registered | Typed FlahaGrow object; do not substitute text | — (not a physical quantity) | From Simulation Paths. | Simulation Paths.Paths |
| 1 | Name | Project name | Param_String | item | Required registration; default supplied | Greenhouse Study | Text; see description | — (not a physical quantity) | Display name when creating a project. | User value or upstream output described below |
| 2 | Analysis | Analysis name | Param_String | item | Required registration; default supplied | baseline | Text; see description | — (not a physical quantity) | Named analysis folder. | User value or upstream output described below |
| 3 | Workflow | Workflow | Param_Integer | item | Required registration; default supplied | 0 | 0 daylight; 1 electric preparation | — (not a physical quantity) | 0 Annual daylight, 1 Electric-light preparation. | User value or upstream output described below |
| 4 | Initialize | Initialize | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Button: create/open workspace. Held True fires once. | Button/toggle; triggering behavior in workflow |
| 5 | Adopt | Adopt existing | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Explicitly allow initialization of nonempty folders without manifests; files are preserved. | Button/toggle; triggering behavior in workflow |
| 6 | Refresh | Refresh | Param_Boolean | item | Required registration; default supplied | False | True / False | — (not a physical quantity) | Button: reread an existing workspace without creating files. | Button/toggle; triggering behavior in workflow |

### Outputs

| # | Port letter / nickname | Port name | Data type | Access | Range / format | Units | Description | Source / destination |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 0 | Project | Project Context | ProjectParameter | item | Typed project context | — (not a physical quantity) | Opened project identity and configuration. | Working Directory → project-aware setup |
| 1 | Analysis | Analysis Context | AnalysisParameter | item | Typed analysis context | — (not a physical quantity) | Opened named analysis. | Working Directory → Radiance Status.Analysis / Annual Simulation.Analysis |
| 2 | Folder | Project folder | Param_String | item | Text; see description | — (not a physical quantity) | Project root. | Produced by this component; see workflow |
| 3 | Inputs | Input folder | Param_String | item | Text; see description | — (not a physical quantity) | Study input folders; not a Honeybee ModelToRad root. | Produced by this component; see workflow |
| 4 | Runs | Runs folder | Param_String | item | Text; see description | — (not a physical quantity) | Container for future isolated simulation runs. | Produced by this component; see workflow |
| 5 | Library | Library folder | Param_String | item | Filesystem folder/path; resolution rules in description and Notes | — (not a physical quantity) | Resolved shared assets. | Produced by this component; see workflow |
| 6 | Status | Status | Param_String | item | Diagnostic/provenance text; not calculation data | — (not a physical quantity) | Workspace operation status. | Component diagnostics → Panel |

### Workflow description

Connect Simulation Paths.Paths; choose project/analysis names and workflow; initialize once, then connect Analysis to Radiance Status and simulations.

### Notes

Existing projects open automatically. Initialize creates a workspace; Adopt explicitly accepts a nonempty folder without deleting its files. Refresh is read-only. Inputs is not a Honeybee ModelToRad export root.

Registered descriptions are reproduced above; operational qualifications in these notes clarify their meaning. Input defaults describe registration, not a guarantee of output before successful execution. Geometry/path/status outputs have no physical light unit.

### Version + last update

Component version: **1.0.2**. Last component update: **2026-09-11T12:50:00+03:00**. Documentation review: **2026-09-13**.

Revision note: Missing workspace manifests provide recovery guidance and no longer block explicit initialization of a legacy folder.
