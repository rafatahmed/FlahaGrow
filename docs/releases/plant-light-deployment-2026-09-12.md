# Plant-light local deployment — 2026-09-12

## Scope

Deployed the Release build of implementation commit `d633aa1`, with documentation and reference data from `f3c34a1`, to `C:\Users\rafat\AppData\Roaming\Grasshopper\Libraries`. Rhino was confirmed closed immediately before deployment. Only `FlahaGrow.gha`, `FlahaGrow.Core.dll`, and `PlantLight\NOTICE.md` were installed; other plugins and shared assets were unchanged.

Existing replaced files were backed up and checksum-verified at `C:\Users\rafat\AppData\Local\FlahaGrow\DeploymentBackups\20260912-130807`, outside Grasshopper's plugin search path. To restore the previous assemblies, close Rhino and copy that backup's two assemblies to Libraries together.

## Verification

- Release build: zero warnings and zero errors.
- Core tests: 183 passed, zero failures or skips.
- Setup/component smoke runner: passed, including 50 revision registrations, 18 plant-light interface/archive cases, and a 13-sensor end-to-end plant-light pipeline with four independent readers.
- Research audit: 23 profiles and 14 source files passed snapshot and self-tests.
- Installed files match Release output SHA256 hashes below.

| Installed file | SHA256 |
|---|---|
| FlahaGrow.gha | `6237C607899B426FB462F7118182654D6A611D616DA74939EBA5933BD744A83A` |
| FlahaGrow.Core.dll | `B994863341E7A0F81EE9E3F9E85CC5C27D2C5345A84498AA23133A4DECBDDEFC` |
| PlantLight/NOTICE.md | `F06E99DAC7232D59648AF147948F7F0A016B241F1ECDCFEAF52A926D21040DF0` |

## Acceptance boundary

This records installation and automated verification, not live Rhino acceptance or physical validation of PPFD predictions. Reopen Rhino/Grasshopper and check the new components using the [plant-light workflow](../workflows/plant-light-workflow.md). Saved-canvas behavior and large-grid interaction remain host acceptance checks. Lux-derived PPFD remains a spectrum-dependent estimate.

Pre-existing `FlahaGrow.backup-*` folders inside Libraries were left untouched; investigate those if Grasshopper reports duplicate plugin loading. New backups should remain outside Libraries. Unrelated local weather data and Illustrator artwork were excluded from the commits.
