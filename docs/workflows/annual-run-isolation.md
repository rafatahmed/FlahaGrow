# Annual run and cache contract

Each annual run has a schema-2 `flahagrow.run.json`. It records run ID, optional Setup IDs, source root, sky, expected hours, sensor count/order hash, input hashes, and one or four declared parts. Results are never discovered by wildcard.

| Files | Purpose |
| --- | --- |
| `envelope.*`, `weather.epw`, `weather.wea`, `0.pts` | Run snapshot inputs. |
| `run_*.bat`, `annual_progress_*`, `annual_errors_*`, `annual_state_*` | Execution and diagnosis. |
| `annualRfinal_part*.ill` | Declared final ASCII Radiance matrices. |
| `annualRfinal.f32` + `.meta.json` | Validated cache built by Load Annual Result. |

The cache is little-endian, row-major `float32`: hour rows and sensor columns. Metadata records `sensors`, `hours`, `ncomp`, run ID, source signature, validation version, and cache hash. It remains valid only while all metadata and result hashes match.

Part states are `Prepared`, `Running`, `CommandsSucceeded`, `Failed`, `Cancelled`, and `Completed`. `Completed` requires final-matrix validation, not just process exit. Failed/cancelled runs keep diagnostics. Do not edit a run to reuse it; prepare a new run when inputs or conditions change.
