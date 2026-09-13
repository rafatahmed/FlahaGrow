# Audited plugin deployment — 2026-09-13

Implementation commit: `176834c7484ba65ab129572098afac68870d9a85`. Pushed to `origin/main` before deployment.

Deployed at `2026-09-13T15:46:20.8900241+03:00` to `C:\Users\rafat\AppData\Roaming\Grasshopper\Libraries` with Rhino confirmed closed. The installed plugin/Core pair matches the smoke-validated Release assemblies. Library assets already matched the repository; other plugin files were unchanged.

Verified rollback backup: `C:\Users\rafat\AppData\Local\FlahaGrow\DeploymentBackups\20260913-154620-495c367f`. Close Rhino and copy all three backup files to their original relative paths together to restore.

| Installed file | SHA256 |
| --- | --- |
| FlahaGrow.gha | `C5EDAD7C0C5886686D48CBB3D2282A6F012B8FADDC5C169F2E272A9FEF7A50D6` |
| FlahaGrow.Core.dll | `950BE7AB4B86DE78D96D7B6E9A967BA51935EDE3B93679C085E751DBD04A7651` |
| PlantLight/NOTICE.md | `ADB9E40018056C857005D641AA89A4B194B9458EF19AEBD340DFBD60E2ED2373` |

Validation: 198 Core tests passed; Release build had zero warnings/errors. All 30 component archives, 206 documented ports, eight typed wire parameters, 23 component icons plus logo, inventory and documentation gates passed. Real ies2rad/rmtxop fixtures and local Yak packaging passed. The deployment audit reran successfully immediately before replacement, and all installed hashes were verified.

The 27 src/Code files remain unchanged. The 10 approved docs/archive files were removed. Unrelated local weather data and Illustrator working assets were excluded from commits.

This records local installation, not live Rhino canvas acceptance or physical study validation. Reopen Rhino/Grasshopper and use the [migration guide](../components/migration.md) to rebuild affected saved definitions. See the [audit limits](../quality/plugin-audit.md).
