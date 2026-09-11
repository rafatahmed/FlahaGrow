# Component revision ledger

Introduced 2026-09-11. Every FlahaGrow Grasshopper component has a component-level revision, last-updated date, and short change note in [the central catalog](../src/FlahaGrow.Grasshopper/Components/FlahaGrowComponent.cs). The catalog is the authoritative ledger because the compiled plugin reads it directly.

## On the Grasshopper canvas

Each component displays `v<revision> · <MM-DD HH:mm>` in its canvas message area and shows the full timestamp, including its UTC offset, in its context menu. Setup components retain their operational status and append the revision label. The version is the primary identity; the timestamp distinguishes separate same-day builds.

When a definition is saved, the component revision is stored with it. On reopening with a plugin where that component's revision changed, its message and context menu show `review <saved>→<current>`. This identifies affected nodes without adding inputs/outputs or changing their GUIDs.

## Update policy

1. Change only the catalog entries for components whose behavior, ports, persistence, diagnostics, or output meaning changed.
2. Bump the affected component's version using semantic intent: patch for a compatible correction, minor for a compatible behavior/capability addition, and major for a migration that needs operator review. Never reuse a component version, even for two changes on the same day.
3. Set the actual timestamp (including UTC offset) and replace the short change note. Do not update unaffected entries.
4. Add or update focused tests, then run `tools/Test-SetupComponents.ps1 -NoRestore`. Its revision-ledger check fails if any concrete component lacks tracking or if revision persistence breaks.
5. Build and install the plugin normally. A compatible component update loads in place; replace a canvas node only when the migration guide says its GUID or port contract is intentionally incompatible.

## Release and canvas checklist

1. Close Rhino, build the Release plugin, and replace **both** `FlahaGrow.gha` and `FlahaGrow.Core.dll` from the same build.
2. Keep timestamped copies of the two previous installed binaries so rollback is possible as a matched pair.
3. Restart Rhino and open a definition. Confirm the canvas message and component context menu show the expected revision/date.
4. If a node shows `review <saved>→<current>`, consult the catalog change note and its focused tests. Replace the canvas node only when its migration guidance says the saved port/GUID contract is incompatible; a compatible revision updates in place and preserves its wires.
5. Save the definition after review so it records the active component revision.

Electric Annual Simulation is `1.0.0` (2026-09-11 19:45 +03:00): it runs a background full-output electric Radiance calculation, snapshots its dimming schedule, and writes a manifest-owned result. The catalog source remains the complete ledger.

The initial tracking baseline is `1.0.1` (2026-09-11 12:48 +03:00). Working Directory is `1.0.2` (2026-09-11 12:50 +03:00). Annual Simulation is `1.6.0` (2026-09-11 19:20 +03:00): strict Ladybug-compatible EPW-to-WEA conversion, the verified 146-column ground-plus-sky receiver basis, a compatible `Keep` input, disk preflight/cleanup, and verified persisted-process cancellation. Annual Simulation Progress is `1.1.0` (2026-09-11 16:10 +03:00): per-part 1–8 stages and stage coverage. Annual cache readers are `1.1.0` (2026-09-11 18:05 +03:00): manifest/signature/cache-hash provenance is mandatory. Spectral components are `1.0.3` (2026-09-11 18:05 +03:00): custom CSV path and content hash persist with the result. The catalog source is the complete ledger.
