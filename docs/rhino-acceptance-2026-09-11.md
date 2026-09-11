# Rhino annual daylight acceptance record

Observed 2026-09-11 using the installed FlahaGrow plugin in Grasshopper. This record is evidence for the stated run only; it does not certify electric workflow, cancellation after a Rhino restart, or spectral parity.

## Evidence

The supplied Grasshopper canvas completed the annual daylight workflow and displayed the cache/result-reader portion of the definition. The observed run folder is:

`C:\Users\rafat\Downloads\FlahGrow02\runs\ea43dd519be64bc5aa256f36b1a642e4`

Its manifest records a non-leap 8,760-hour study, 980 sensors, Sky=1, and four declared 245-sensor parts. Each `annual_state_part*.txt` contains the manifest run ID followed by `CommandsSucceeded`. All declared `annualRfinal_part*.ill` matrices exist and the cache metadata records the same run ID, 980 sensors, 8,760 hours, scalar row-major layout, source signature, and cache hash. The cache size is 34,339,200 bytes, exactly `980 × 8760 × 4`.

The part logs record stages 1/8 through 8/8. `rcontrib` reported `warning - no light sources found` during direct-sun generation. In this daylight-only scene that warning is retained as diagnostics; it did not produce an invalid matrix or non-zero batch exit.

## Closed by this observation

- Installed-plugin annual daylight launch, four-part completion, and final matrix validation.
- Progress-stage production in a real Grasshopper run.
- Load Annual Result cache creation and cache-native result-reader wiring for this run.

## Not observed here

- Grasshopper Timer refresh behaviour.
- Save/reopen and `Existing` restoration.
- Cancel after restarting Rhino.
- Electric Annual / Combine Annual Lighting with a real luminaire export.
- Custom spectral CSV parity.
