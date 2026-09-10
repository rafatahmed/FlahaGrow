# Current implementation, installation and validation status

Updated 2026-09-10. This is the current status index; dated audits retain their original evidence and explicitly dated follow-ups.

## Documentation map

| Document | Use |
| --- | --- |
| [Migration and wiring](component-migration.md) | Visible/hidden identities, supported connections and old-result policy. |
| [25-script I/O audit](component-io-audit-2026-09-09.md) | Per-script ports, defaults, behavioral differences and IO findings. |
| [Annual run contract](annual-run-isolation.md) | Ownership, states, matrix/cache validation and lifecycle limits. |
| [Live-result audit](annual-result-audit-2026-09-10.md) | Cache parity, negative-value diagnosis, numerical probes and installed-version evidence. |
| [Repository tracker](repository-audit-2026-09-07.md) | Original F findings and implementation history. |
| [Setup consistency](setup-consistency-review-2026-09-07.md) | C findings and producer/consumer connections. |
| [Setup guide](setup-components.md) | Visible Setup operation and installation requirements. |

## Implemented and tested

- C01–C04: optional checked Radiance environment, analysis/run ownership, consistent luminaire folders and library-root support.
- F01–F04: sky basis consistency, isolated declared runs, strict matrix/cache validation and checked command failures.
- F05/C03 and F07: luminaire folder agreement and invariant numeric I/O. F12: snapshot weather copying avoids the original same-file copy error.
- F10: packaging build-exit guard (source fix; forced build-failure injection was not repeated).
- September 10: actual Radiance blank-provenance parsing, nonnegative final-illuminance validation, negative/nonfinite selected legacy-cache rejection, numeric/named quality agreement, direct-stage bounce settings, trimmed reader modes and checked 64-bit hour seeks (F11 source fix; multi-gigabyte cache stress validation remains pending).
- C06 documentation mappings and wiring are reconciled. Exact migration in saved Python definitions remains unverified.

## Open work

| Area | Remaining work |
| --- | --- |
| Numerical validity | The old FlahGrow01 study has 7,584 negative values. Cache matches every final value; caching is not the cause. Fresh full-year and convergence/reference checks remain required. |
| IO03/IO04 | Custom spectral CSV calculation differs from Python; PAR wavelength mask/weighting, CSV handling, export and selection behavior need correction. |
| F08 / IO02 | Complete option/value allowlist and independent custom-quality input design. Shell metacharacter rejection is already implemented. |
| IO07 | IES rerun file ownership and Radiance/DAT rewrite correctness. Shared folder consistency does not close these issues. |
| F06 / F09 / C05 | Glazing counted-block parser; selector persistence; annual trigger/process and UI lifecycle. |
| IO06 / IO08 / IO10 | Legacy output exposure, whole-tree ordering, missing-file diagnostics and leap-year/hour conventions. |
| IO11 / IO12 | Explicit Honeybee export/reference study, electric simulation/combination, DLI timestep and power-schedule semantics. |
| C06 verification | Original saved-definition port/access migration and Rhino save/reopen checks. |

Do not treat structurally valid, nonnegative results as scientific certification. Negative results are rejected, never silently clamped. The old completed study is preserved for diagnosis.

## Installed release

With the user's explicit approval and Rhino closed, replaced both files in `C:\Users\rafat\AppData\Roaming\Grasshopper\Libraries`:

| File | SHA-256 |
| --- | --- |
| FlahaGrow.gha | `652B8C4A3A9AAAE5BE578FE238BD85CCABA993E6BEB39BBF50D7BC0E8D9C848E` |
| FlahaGrow.Core.dll | `7A57DE9F3FC270838F05AB9A1CBC5B2A35FAA5180AB168CC936278A808A527AE` |

Both installed hashes match the staged, tested package. Product version is `0.1.1-audit.20260910+4f28a3391778d8880763f2725c70deac983c895a`. The suffix records the Git HEAD at build time; the binaries also include the subsequent working-tree fixes committed with this documentation. Use hashes to identify this exact build; it is not a clean build of 4f28a33 alone.

Previous assemblies are backed up under ignored `artifacts/plugin-backup-20260910/`. Existing installed libraries and the Downloads study were unchanged. The local package is `artifacts/yak-staging/flahagrow-0.1.1-audit.20260910-rh8_33-win.yak`, SHA-256 `14D40EA1F4B784A09004E0A4FA3C15ADD329ACC58D9A00D1A073C6FEDDFB4160`. Binaries/backups/studies are not committed to Git.

Installation is complete; post-restart Rhino loading and the next full study have not yet been observed. Confirm the appended Annual Simulation Radiance/Analysis inputs after reopening. Use a new manifest-owned run; do not fabricate manifests for old results.

## Validation evidence

156 Core tests passed. Expanded component/archive/integration checks and real Ladybug rmtxop nested-header/failure checks passed; builds had zero warnings/errors. The package contents and installed hashes were verified. The full source/cache comparison found zero mismatches across 8,584,800 values. Original study files were read only.

These checks do not exercise every Rhino canvas lifecycle, prove spectral/annual physical accuracy, or certify large-cache performance. See each dated audit for exact probe boundaries.
