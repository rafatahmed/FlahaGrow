# Icons, spectral selector and timing deployment — 2026-09-12

Implementation commit: `42fbb36`. Deployed the tested Release assemblies and
PlantLight/NOTICE.md to `C:\Users\rafat\AppData\Roaming\Grasshopper\Libraries`
with Rhino confirmed closed. No other plugin files were changed.

Backup of all three previous files, verified before replacement:
`C:\Users\rafat\AppData\Local\FlahaGrow\DeploymentBackups\20260912-171930`.
To restore, close Rhino and copy those three backup files to their original paths together.

| Installed file | Verified SHA256 (matches Release output) |
|---|---|
| FlahaGrow.gha | `9005452840632618E8BA35838A6322A41375411EFBACA8E988C5AF9CCA17CF10` |
| FlahaGrow.Core.dll | `23E38BB3B1F0BEE2A9B66A50444A9ADC2D4D9C29F7B23C1210A672860B4F4756` |
| PlantLight/NOTICE.md | `ADB9E40018056C857005D641AA89A4B194B9458EF19AEBD340DFBD60E2ED2373` |

Validation: 185 core tests passed; component smoke checks passed, including
33 embedded icons, 51 component registrations, and 21 plant-light/timing
interface archives. Research audit: 23 profiles and 14 source files passed.
Release build: zero warnings and errors.

The build includes 32 named component icons plus the logo, the 18-reference
spectral picker, and corrected Hour Index outputs. Review the
[timing and sensor migration](../workflows/timing-and-sensor-contract.md)
before relying on old selected-hour indices. Live Rhino icon legibility,
actual saved-definition migration and physical PPFD accuracy are not certified
by these automated checks. Reopen Rhino to perform host acceptance.

Weather data and Illustrator working files remain outside these commits.
