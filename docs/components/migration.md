# Compatibility and migration

Grasshopper definitions identify components by GUID, therefore published GUIDs and parameter order must not change. `FlahaGrowComponent` records a revision in the GH archive and flags a saved/current revision difference for review.

Prior setup components and the legacy spectral selector remain registered but hidden to load old definitions. For new definitions use the visible Setup trio and **Select Spectral Factor**. Legacy flat annual results have no schema-2 provenance and cannot be adopted as validated caches; regenerate them.

For an incompatible I/O change, publish a new GUID, retain a hidden compatibility component where practical, update the revision ledger, and add focused tests.
