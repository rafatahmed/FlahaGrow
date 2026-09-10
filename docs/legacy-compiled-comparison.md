# Legacy and compiled component comparison

Updated 2026-09-10. The complete, source-verified comparison of **all 25 Python scripts** is maintained in [component-io-audit-2026-09-09.md](component-io-audit-2026-09-09.md), including ordered compiled inputs/outputs, defaults, optionality, inferred Python contracts, and IO01–IO12.

The earlier broad “Match” labels have been removed: matching a workflow role did not establish numerical, UI, port-order or file-format parity.

| Area | Comparison and current limits |
| --- | --- |
| Setup | Visible typed trio is a redesign; hidden legacy identities remain. Python has seven folder toggles, compiled legacy helper six. |
| Materials/glazing/IES selection | Main modifier/path outputs retained; portable library roots supported. Preview capabilities and persistence differ; glazing parsing remains F06. |
| Spectral load/selection | Standard constants retained. Custom CSV numerical calculations, column handling, table/export and selection behavior differ; IO03/IO04 remain open. |
| Electric preparation | Shared project-local output folder replaces Python's parent-relative folder. Tree flattening, IES rerun selection and file rewriting need work. |
| Annual simulation | Isolated manifest-owned runs and verified environment inputs added. Numeric/named quality mapping corrected September 10; independent custom override input remains unresolved. |
| Annual cache/progress | Strict run/state/matrix/negative-value checks replace permissive legacy parsing. No merged .ill export. Old flat folders are not silently imported. |
| Illuminance/PPFD readers | Cache layout retained. Selected negative/nonfinite data is rejected. Reader Mode whitespace and hour seek arithmetic corrected. Candidate legacy metadata outputs and tree access require definition-level review. |
| Calendar/plots/metrics | Basic annual display roles retained; leap-year/hour convention and supported time axis require validation. DLI/power helpers are additional compiled components. |

Use [component-migration.md](component-migration.md) for practical wiring and compatibility policy, [current-status.md](current-status.md) for implemented/open work, and [annual-result-audit-2026-09-10.md](annual-result-audit-2026-09-10.md) for the real study/cache findings.

Python script source alone does not serialize original Grasshopper port order or type hints. A saved reference definition is required before declaring exact canvas migration compatibility.
