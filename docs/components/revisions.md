# Component revisions

`ComponentRevisionCatalog` in `FlahaGrowComponent.cs` is the revision ledger. Every concrete component has a semantic version, timestamp, and change note. Update an entry only when its behavior, ports, persistence, diagnostics, or output meaning changes. The setup smoke test verifies ledger coverage and archive behavior outside Rhino; review changes in Rhino before release.
