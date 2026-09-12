# Live annual result and installed-build audit

Source: `C:\Users\rafat\Downloads\FlahGrow01`, inspected read-only on 2026-09-10. No study files were edited, clamped, deleted or regenerated. Repository changes include the previously uncommitted annual fixes.

## Confirmed findings

- **The loader preserved the simulation output:** all 8,584,800 cache entries match the four final ASCII parts after float32 conversion. Metadata correctly declares 980 sensors, 8,760 hours, NCOMP=1. All final parts have 8,760 rows × 245 columns.
- **Negative values originate in the simulation:** 7,584 entries are negative. At zero-based hour 2360 / sensor 11 (April 9, WEA 08:30), total=21,069.62711 lux, direct=49,144.9034 lux, sun=0; final=-28,075.27629 lux. Rebuilding the cache cannot correct the calculation.
- **Rhino is using an old installed plugin:** process modules identify `C:\Users\rafat\AppData\Roaming\Grasshopper\Libraries\FlahaGrow.gha` and `FlahaGrow.Core.dll`, both reporting revision `254d7207547d70af5fe53a4df80a0fe6a14ea851`, dated September 7. Generated batches lack run manifests and fail-fast checks. Building the repository does not update loaded assemblies.
- **The newer parser needed real-engine compatibility:** Radiance 5.4 copied provenance contains blank lines before NROWS/NCOLS/NCOMP/FORMAT. Stopping at the first blank rejects those files.
- **Direct-light command settings drifted:** total and black-scene direct rfluxmtx inherited the same bounce count. Numeric Detail 2 selected mid diffuse settings but low direct-sun settings. The DDS reference uses one ambient bounce for direct subtraction and sun coefficients: [Radiance matrix-method tutorial, Appendix B.2](https://www.radiance-online.org/learning/tutorials/matrix-based-methods).

## Numerical investigation

Focused commands used the installed Ladybug Radiance, existing octrees/materials and worst sensor/hour. New outputs are under ignored `artifacts/annual-result-investigation/`. For non-frozen octrees the source study was the read-only working directory to resolve relative scene references.

Changing only sun ab=0 to ab=1 still gave zero. **The sun bounce change alone does not explain or repair the negative result.** Zero direct sun may be legitimate at a shaded sensor.

New one-sensor/one-thread coefficient calculations used total ab=2, direct ab=1 and lw=1/ad:

| Sampling | Total lux | Direct lux | Total minus direct; sun=0 |
| --- | ---: | ---: | ---: |
| ad=1024 | 36,547.23153 | 12,322.696404 | 24,224.535126 |
| ad=10000 | 26,166.59851 | 16,536.276223 | 9,630.322287 |

These are diagnostics, not reference answers. Differences from the multi-sensor run and between probes show sensitivity to sampling/settings; they do not prove convergence, correct geometry or full-year accuracy. Do not clamp the original data and present it as repaired. A corrected full run and convergence/reference checks remain required.

## Implemented fixes

- Parse a bounded header through FORMAT and its separator, accepting copied blank provenance while retaining duplicate/dimension/format checks and rejecting numeric payload before FORMAT.
- Preserve signed generic matrix parsing, but require **nonnegative finite final illuminance** before Progress reports completion or Load Annual Result publishes/reuses a cache. This conservative policy rejects even small negatives; no tolerance or clamping is introduced.
- Report offending hour, part-local sensor, value and filename. Cache failures populate Status and the GH runtime error; rejected inputs preserve existing caches.
- Reject negative/nonfinite selected values in illuminance and cache-native PPFD readers, including legacy caches. Selected-series validation does not certify the rest of a cache or its provenance.
- Trim illuminance Mode and use checked 64-bit hour seek arithmetic. Preserve component GUIDs and port order.
- Align numeric Detail 1–5 with named presets. Direct subtraction forces ab=1 independently of total/custom bounce depth; sun coefficients use ab=1 per the DDS reference. Independent custom-quality input design (IO02) and full option allowlisting (F08) remain open.

The manifest-based loader still intentionally rejects this older flat-folder run. It does not silently import it. Use the read-only audit for diagnosis and generate a new manifest-owned simulation for the supported pipeline.

## Validation

`python tools/Audit-AnnualCache.py "C:\Users\rafat\Downloads\FlahGrow01"` reproduces cache parity and the minimum-value trace without writes. This diagnostic expects scalar ASCII legacy parts with one logical row per physical line; it is not an importer or repair tool.

156 Core tests passed. Expanded component checks passed for quality mappings/direct commands, negative-result rejection preserving caches, invalid progress, legacy lux/PPFD rejection, whitespace mode and existing Setup/archive behavior. Live installed rmtxop checks passed with nested provenance and command failure. Builds reported zero warnings/errors.

These validate implementation behavior, not a fresh full annual study, original GH wire/access parity or numerical convergence. Remaining F/C/IO findings are not collectively closed.

## Installation and rerun

An updated package is prepared under `artifacts/yak-staging/` as version `0.1.1-audit.20260910`. Update the plugin and Core assembly together. On the user's explicit approval after Rhino closed, both installed assemblies were backed up and replaced. Installed hashes match the package; see [installation record](../quality/current-status.md#installed-release). The original study and existing libraries remain unchanged. Post-restart host verification is pending.

After installation/restart, verify loaded path/build and the Annual Simulation Radiance/Analysis inputs. Preserve the old study, create a new run with the typed environment and analysis connected, and validate before caching. An old Completed log does not establish scientific validity.
