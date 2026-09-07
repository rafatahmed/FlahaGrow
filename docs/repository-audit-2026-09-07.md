# Repository structure and code audit

Audit date: 2026-09-07. Source revision: `254d720`.

## Scope and assumptions

This is a repository map and implementation audit, not a certification of simulation accuracy. The compiled plugin is treated as the primary implementation; the Python components are migration references. Source fixes, installation, publishing, and external-service changes were outside this audit. The pre-existing untracked `None/` directory was excluded and left untouched.

The Release build succeeds, but the annual pipeline has correctness and result-integrity defects that should be resolved before relying on it for study results.

## Repository map

| Location | Responsibility |
| --- | --- |
| `FlahaGrow.sln` | Single-project .NET solution |
| `Directory.Build.props` | Nullable reference types, implicit usings, latest language version, warnings as errors |
| `src/FlahaGrow.Grasshopper/` | Rhino 8 Windows plugin targeting `net7.0-windows`; builds `FlahaGrow.gha` |
| `src/FlahaGrow.Grasshopper/Components/` | 25 C# source files containing Grasshopper components, UI dialogs, simulation execution, parsers, cache access, and metrics |
| `src/Code/` | 25 legacy Python files arranged into setup, preparation, RGB simulation, and result/metric stages |
| `src/Library/` | Reusable Radiance materials, glazing, IES photometry, textures, and tabular material data |
| `tools/New-YakPackage.ps1` | Release build and local Yak package staging |
| `package/manifest.yml` | Yak package metadata |
| `docs/` | Scope, annual workflow, plugin development, and migration contracts |
| `artifacts/` | Ignored generated packages and audit harness |

The project references Grasshopper NuGet version `8.33.26188.13001`; Rhino supplies the host assemblies. There is no separate computational library, test project, or tracked CI workflow. No tracked `.gh`, `.ghx`, or `.3dm` reference study was found.

## How the code fits together

1. **Setup:** `SimulationPathsComponent`, `WorkingDirectoryComponent`, and the Radiance detection/version components establish filesystem locations and executable availability.
2. **Inputs:** material/glazing/IES selectors use WinForms dialogs. Spectral components select or calculate lux-to-PPFD factors.
3. **Electric-light preparation:** IES conversion writes `.rad`/`.dat` files; lighting geometry generates `!xform` statements; compilation writes `luminaries.rad`.
4. **Annual daylight simulation:** `AnnualSimulationComponent` consumes a Honeybee ModelToRad project and EPW file, copies inputs, partitions sensors into one or four jobs, and generates/launches Windows batch files.
5. **Results:** `AnnualResultCacheComponent` merges text matrices into float32 data ordered by hour, then sensor. Metadata records dimensions and component count. Illuminance and PPFD readers retrieve rows or columns.
6. **Metrics and visualization:** scalar/list lux-to-PPFD conversion, daily DLI, target comparison, power-schedule energy integration, sensor markers, and annual heatmaps.

The annual runner explicitly runs daylight commands; it does not consume the compiled luminaire file. Lighting Energy integrates an externally supplied power schedule. Automatic supplemental-light control and combined daylight/electric annual simulation are therefore not an end-to-end implemented pipeline in this runner. Hyperspectral simulation remains planned.

## Prioritized findings

### F01 — High: Sky=4 generates incompatible matrix bases

Evidence: `src/FlahaGrow.Grasshopper/Components/AnnualSimulationComponent.cs:47` and `:116`.

The receiver file always uses `h=r1`, while selecting Sky=4 generates the weather matrix with `gendaymtx -m 4`. The coefficients therefore use a different sky basis from the weather matrix consumed by `dctimestep`. The same hardcoding appears in the legacy annual script.

This is a source-level conclusion supported by the official [rfluxmtx manual](https://www.radiance-online.org/learning/documentation/manual-pages/pdfs/rfluxmtx.pdf) and [gendaymtx manual](https://www.radiance-online.org/learning/documentation/manual-pages/pdfs/gendaymtx.pdf): the receiver subdivision and weather subdivision must agree; `-m 4` has 2,306 patches including ground, whereas the default basis has 146.

Correction: derive both settings from one validated subdivision and test both supported resolutions.

### F02 — High: Result-folder reuse can silently mix studies

Evidence: `AnnualSimulationComponent.cs:48` and `AnnualResultCacheComponent.cs:23`, under the Components directory above.

The runner writes one part for ten or fewer sensors and four for larger grids. It neither isolates runs nor invalidates previous results. The cache builder merges every matching `annualRfinal_part*.ill`. After a four-part run, a single-part rerun leaves parts 1–3 available for merging with the new part 0. Missing parts are also accepted if at least one matching file exists. Equal hour counts do not identify this corruption.

Correction: give each run a manifest and isolated output location; load only its declared complete part set. Record input identity and sensor ordering with the cache.

### F03 — High: Corrupt or truncated matrices are accepted

Evidence: `AnnualResultCacheComponent.cs:24` and `:39`.

The parser discards any line outside its numeric regex, including corrupt data rows, and ignores `NROWS`, `NCOLS`, `NCOMP`, and `FORMAT`. Equal observed row lengths/counts across available parts can still represent incomplete output. Removing a corrupt row also shifts every later timestamp.

Reproduced with the actual parser method extracted into a local harness: a file declaring 8,760 rows and containing `1 2`, `NaN 3`, and `4 5` was accepted as two rows. There was no error for the corrupt row or declared-length mismatch.

Correction: parse metadata explicitly, distinguish headers from data, reject malformed/nonfinite values, enforce dimensions, and publish cache files only after successful validation.

### F04 — High: Failed simulation commands still reach “Completed”

Evidence: `AnnualSimulationComponent.cs:116`–`:123`; `AnnualSimulationProgressComponent.cs:31`–`:33`.

Generated batch files never check stage exit codes and always append “Completed.” The progress component treats any nonempty final file as completed, including a partially written file or stale output. It also hardcodes `/4` for the single-job case.

Correction: stop at the first failed command, capture diagnostics, validate final output, and expose explicit run/part states. Check both sides of piped commands. Use the run manifest for expected part count.

### F05 — High: Luminaire conversion and compilation disagree on folder location

Evidence: `IesToRadianceComponent.cs:45`; `CompileLuminariesComponent.cs:34`.

For the same project input, conversion creates `<project>/Luminaire_files`, but compilation requires `<parent-of-project>/Luminaire_files`. Connecting both to the same Project output therefore fails on a fresh workspace, or writes into another study's sibling folder if it happens to exist.

Correction: use one shared project-path resolver and test the complete conversion → placement → compilation sequence.

### F06 — Medium: Glazing property display reads argument counts as RGB

Evidence: `GlazingMaterialComponent.cs:72`–`:76`.

The parser collects every numeric token, including Radiance's `0`, `0`, and `3` argument counts, then uses the first three numbers as RGB.

Reproduced against bundled `glazing_92.rad`: actual channel values `0.85 0.85 0.85` were displayed as `0.000, 0.000, 3.000`, with VLT `0.2`. The modifier name remains correct; the selector's property display is incorrect.

Correction: parse the primitive's counted argument blocks and calculate displayed properties from the actual material parameters.

### F07 — Medium: Radiance output depends on the Windows numeric culture

Evidence: `IesToRadianceComponent.cs:96`; `LightingGeometryComponent.cs:53`–`:56`.

RGB and xform numeric strings use current-culture interpolation. Under `de-DE`, the extracted RGB writer produced `3 0,265 0,67 0,065`. Decimal commas do not preserve the intended Radiance numeric syntax. Several material/IES parsers also use current-culture `double.TryParse` on dot-decimal files.

Correction: use invariant culture for all machine-readable numeric input/output; localize only UI display.

### F08 — High, conditional security risk: custom Detail text enters executable batch syntax

Evidence: `AnnualSimulationComponent.cs:96` and `:117`.

Any Detail string containing a hyphen is accepted as custom parameters and interpolated into a `.bat` file. Shell operators such as `&` are preserved. If a definition or upstream text source supplies untrusted Detail content, setting Run=True can execute additional Windows commands with the Rhino user's permissions.

This is a local input trust boundary, not evidence of a remotely exposed service. No injection payload was executed during the audit.

Correction: parse allowed Radiance options into arguments and use direct process execution; otherwise reject shell metacharacters and validate each option/value before generating batch content.

### F09 — Medium: selected spectral factors are not serialized

Evidence: `SpectralConversionFactorComponents.cs:10` and `:31`.

Selected factors, CSV-derived quantities, and source labels live only in private fields. The classes implement no Grasshopper Read/Write persistence and have no input carrying the selected value. A newly loaded component therefore starts from the default factor or zero instead of restoring the study's spectral choice. This is a source-level finding; save/reopen behavior was not exercised in Rhino.

Correction: persist the selected factor, source, and calculation settings and verify a save/reopen round trip. Material and IES selectors also need lifecycle review because Run=False returns without re-emitting a stored selection.

### F10 — Medium: packaging can continue after a failed build

Evidence: `tools/New-YakPackage.ps1:29`.

The script checks the Yak exit code but not the preceding dotnet build exit code. In Windows PowerShell, `$ErrorActionPreference = 'Stop'` does not itself turn a native nonzero exit into a terminating error. If an older Release `.gha` exists, a failed build can be followed by packaging that stale binary with the newly requested manifest version.

Correction: check `$LASTEXITCODE` immediately after dotnet build and abort before staging; verify assembly/package version consistency.

### F11 — Medium: hour reads overflow for large caches

Evidence: `IlluminanceReaderComponents.cs:31`.

`index * meta.Sensors * sizeof(float)` is calculated as a 32-bit integer before assignment to the stream's long Position. At hour 8,000 and 100,000 sensors the intended byte offset is 3,200,000,000, which exceeds Int32. Other cache access code already casts to long before multiplication.

Correction: use checked long offset arithmetic consistently and verify reads above the 2 GiB boundary using a sparse fixture.

### F12 — Medium: EPW files already in the project root prevent preparation

Evidence: `AnnualSimulationComponent.cs:46`.

The code copies the selected EPW into the project root unconditionally. If the user selects a weather file already there, source and destination are the same and File.Copy throws. This prevents a valid local project arrangement from running.

Correction: compare normalized source/destination paths and skip the copy when they identify the same file.

## Architecture and validation gaps

- UI, calculation, parsing, filesystem writes, and process orchestration are tightly coupled in GH_Component classes. Extract small computational and infrastructure services behind thin components as fixes require them; avoid a wholesale rewrite.
- No tracked automated tests, CI workflow, or executable reference study was found. Manual validation instructions are useful but cannot detect regressions automatically.
- The spectral CSV algorithm needs an independent numerical reference review. It rounds wavelengths into dictionary keys, samples exact keys with previous-value carry-forward, and describes unweighted sums as integrated quantities. Agreement with legacy code alone does not establish physical accuracy.
- The DLI component accepts arbitrary sample duration while always grouping 24 samples into each of 365 days. Clarify whether only hourly input is supported and enforce that contract.
- Numeric guards generally reject negatives but not NaN/infinity. PPFD variants do not apply identical checks.
- The cache builder retains all text matrices in memory and writes each float separately. Large studies will benefit from a streaming parser and buffered writes after correctness fixes.
- Local installation documentation copies only the `.gha`, while default library discovery expects `shared/Library/FlahaGrow_Library_Small` beside it. Explain library copying or an explicit library input for manual installations.
- `Simulation Paths` describes the optional Library input as a folder containing `FlahaGrow_Library_Small`, but the implementation expects that child folder itself.
- Migration documentation has drift: the first rows of `component-migration.md` associate legacy folder toggles and version execution with components whose actual interfaces differ. Reconcile it with the detailed comparison and current code.

## Validation performed

| Check | Result and boundary |
| --- | --- |
| `dotnet build FlahaGrow.sln --configuration Release --no-restore` | Passed, zero warnings/errors using existing restored dependencies |
| Python AST parsing | All 25 legacy Python files parsed successfully under local Python 3.14; this does not verify IronPython compatibility or Rhino imports |
| Source-extracted C# harness | Reproduced corrupt/truncated matrix acceptance, glazing argument-count bug, and culture-dependent RGB output |
| Repository inventory | No tracked test project, CI workflow, or Rhino/Grasshopper sample model found |
| Official Radiance documentation | Verified sky subdivision interpretation supporting F01 |
| End-to-end host simulation | Not performed; Rhino and Radiance executables exist locally, but no tracked reference study was available and no live Rhino session was used |

The generated harness is in ignored `artifacts/audit-harness/`. It runs extracted parser/writer methods under .NET 9, not the plugin inside its .NET 7 Rhino host; only method visibility/wrapping was adapted. No application source was modified. The audit does not establish dependency vulnerability status, numerical accuracy, or production readiness.

## Recommended implementation order

1. Fix annual sky basis, run isolation, failure handling, and cache validation; add single/four-part and rerun fixtures.
2. Fix luminaire paths, glazing parsing, invariant numeric I/O, and custom-option validation.
3. Persist study selections, correct large-file arithmetic and same-path copying, and make packaging fail on build errors.
4. Add a small committed reference study and numerical tests for spectral conversion, PPFD, DLI, and energy; then reconcile the workflow and migration documentation.
