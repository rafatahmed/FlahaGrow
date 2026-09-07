# Contributing to FlahaGrow

## Scope and conventions

- Keep Grasshopper component scripts in their numbered workflow stage under `src/Code`.
- Preserve the existing component-facing variable conventions (for example, `_run`, `_folder`, and `ghenv`) unless the corresponding Grasshopper definition is updated too.
- Store reusable Radiance materials, glazing, IES profiles, and textures under `src/Library`.
- Do not commit local simulation outputs, render files, cache files, or machine-specific paths.

## Validation before a change is merged

For the shared Setup core, run `dotnet test tests/FlahaGrow.Core.Tests/FlahaGrow.Core.Tests.csproj --configuration Release` and `dotnet build FlahaGrow.sln --configuration Release`. The Windows path tests run outside Rhino. See [the Setup foundation notes](docs/setup-foundation.md) for implemented scope and host validation still required.

Run validation in the environment the component targets:

1. Confirm the script loads in its Grasshopper Python component without syntax or import errors.
2. For changes touching Radiance commands, verify Radiance detection and execute a small point-in-time case.
3. For annual-result changes, verify expected dimensions (8,760 hourly values and 365 daily DLI values where applicable).
4. For PPFD conversion changes, record the selected spectral source and conversion factor used for the check.

## Pull requests

Describe the simulation stage affected, the sample model/material/light used to validate it, and any expected impact on PPFD, DLI, or energy results.
