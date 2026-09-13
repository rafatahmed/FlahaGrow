# Plant-light architecture and remaining work

The current scalar implementation uses a validated annual result, a source-specific spectral profile, a typed context and four independent PPFD/DLI readers. The [workflow](../workflows/plant-light-workflow.md) and [component reference](../components/README.md) define implemented behavior.

Shared Core services validate source ownership, cache content, sensors and the 8,760-interval time axis. Each source is converted before context composition. Result weather supplies local-standard-time alignment when authenticated run EPW data is available. Plot Attributes bind values to quantity, shape, units, selection and provenance.

The toolbar has one implementation per supported purpose. Numeric lux conversion and numeric PPFD integration remain separate from context readers because they accept independently supplied data. Material surface labels do not create separate executable selectors. One viewer serves supported annual quantities.

Remaining acceptance work:

- Rebuild and save/reopen a representative Rhino definition using the current ports; check dialogs, cancellation, responsiveness and marker geometry.
- Measure cache and reader performance with representative sensor counts and storage; synthetic small-grid timings do not establish UI performance.
- Compare Radiance illuminance, estimated PPFD and integrated DLI separately against independent numerical cases and measurements with stated tolerances.
- Verify electric schedules and weather share their declared time axis; inspect mixed-source sensor ordering and source provenance.
- Review licensed measured fixture spectra and receiving-light assumptions before promoting reference profiles to study-specific recommendations.
- Evaluate native wavelength-resolved transport separately before claiming spectral simulation. Scalar lux conversion does not implement it.

The [scientific assessment](ppfd-dli-scientific-assessment.md) and [research package](../research/plant-light/README.md) preserve scientific evidence and limitations. These are not claims of completed physical validation.
