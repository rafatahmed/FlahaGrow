using FlahaGrow.Core.Annual;
using FlahaGrow.Core.PlantLight;
using FlahaGrow.Grasshopper.Parameters;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace FlahaGrow.Grasshopper.Components;

public sealed class CustomSpectralProfileComponent : FlahaGrowComponent
{
    public CustomSpectralProfileComponent() : base("Custom Spectral Profile", "Custom Profile", "Advanced custom factor/CSV entry. For bundled references use Spectral Profile and a Button.", "FlahaGrow", "02 Spectral") { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa410");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Source label", "Label", "Required source/state label. A label does not certify a fixture.", GH_ParamAccess.item);
        p.AddNumberParameter("Explicit factor", "Factor", "µmol/m²/s per lux. Connect this OR CSV, never both. Existing spectral selector's Factor can connect here.", GH_ParamAccess.item); p[1].Optional = true;
        p.AddTextParameter("Spectral CSV", "CSV", "Headered wavelength_nm,value CSV. Explicit basis below; no raw multi-profile CIE files.", GH_ParamAccess.item); p[2].Optional = true;
        p.AddIntegerParameter("Spectral basis", "Basis", "0 = energy spectrum; 1 = photon spectrum. Applies to CSV only; relative normalization yields a ratio only.", GH_ParamAccess.item, 0);
        p.AddBooleanParameter("Accept zero tails", "Tails", "Explicitly assume unmeasured wavelengths outside CSV coverage are zero. PAR coverage remains mandatory.", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddParameter(new SpectralProfileParameter(), "Spectral Profile", "Profile", "Typed assumption for Plant Light Context.", GH_ParamAccess.item);
        p.AddNumberParameter("Factor", "Factor", "µmol/m²/s per lux.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Method, provenance and limitations.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string label = "", csv = ""; double factor = 0; int basis = 0; bool tails = false;
        if (!da.GetData(0, ref label)) return;
        var hasFactor = da.GetData(1, ref factor); da.GetData(2, ref csv); da.GetData(3, ref basis); da.GetData(4, ref tails);
        try
        {
            var hasCsv = !string.IsNullOrWhiteSpace(csv);
            if (hasFactor == hasCsv) throw new ArgumentException("Supply exactly one source: Factor or CSV.");
            SpectralProfile profile;
            if (hasCsv)
            {
                csv = Path.GetFullPath(csv); var hash = AnnualRun.HashFile(csv);
                var result = SpectralCalculator.Calculate(SpectralCalculator.ReadCsv(csv, (SpectralBasis)basis), (SpectralBasis)basis, tails);
                if (hash != AnnualRun.HashFile(csv)) throw new IOException("CSV changed while calculating; retry after saving it.");
                profile = new(label, result.Factor, SpectralCalculator.Method,
                    $"CSV {csv}; SHA256 {hash}; basis {(SpectralBasis)basis}; {result.MinimumNm:G}–{result.MaximumNm:G} nm; step 1 nm", result.Warning);
            }
            else profile = new(label, factor);
            da.SetData(0, new SpectralProfileGoo(profile)); da.SetData(1, profile.Factor);
            da.SetData(2, $"{profile.Method}; {profile.Provenance}; {profile.Warning}");
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, profile.Warning);
        }
        catch (Exception ex) { da.SetData(2, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}

public sealed class PlantLightContextComponent : FlahaGrowComponent
{
    public PlantLightContextComponent() : base("Plant Light Context", "Plant Context", "Binds a manifest-owned lux cache to one spectral profile. Readers remain independent.", "FlahaGrow", "05 PPFD") { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa411");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Result cache", "F32", "F32 output of Load Annual Result, before mixed-lux composition.", GH_ParamAccess.item);
        p[0].Optional = true;
        p.AddParameter(new SpectralProfileParameter(), "Spectral Profile", "Profile", "Connect Spectral Profile → Profile (or Custom Spectral Profile → Profile). Use the spectrum of this source.", GH_ParamAccess.item);
        p.AddTextParameter("Annual alignment declaration", "Alignment", "Legacy optional declaration. With Result connected, leave empty: weather alignment is inherited automatically. Never connect Date, Hour or Day.", GH_ParamAccess.item); p[2].Optional = true;
        p.AddParameter(new AnnualResultParameter(), "Annual Result", "Result", "Recommended: Load Annual Result → Result. Supplies cache and weather alignment automatically; leave F32 and Alignment empty.", GH_ParamAccess.item); p[3].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddParameter(new PlantLightContextParameter(), "Plant Light Context", "Context", "Connect independently to PPFD and DLI readers.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Run, assumptions and time convention.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string cache = "", time = ""; var profile = new SpectralProfileGoo();
        da.GetData(0, ref cache);
        if (!da.GetData(1, ref profile) || !profile.IsValid) return;
        da.GetData(2, ref time);
        try
        {
            var loaded = new AnnualResultGoo(); var hasResult = da.GetData(3, ref loaded) && loaded.IsValid;
            if (hasResult)
            {
                if (cache.Length > 0 && !string.Equals(Path.GetFullPath(cache), loaded.Value.Result.CachePath, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("F32 and Result refer to different caches. Use Result only.");
                var inherited = loaded.Value.Weather?.Alignment ?? "";
                if (time.Length > 0 && (inherited.Length == 0 || time != inherited)) throw new ArgumentException("Manual Alignment cannot override Result weather. Use Result only.");
                time = inherited;
            }
            if (!hasResult && cache.Length == 0) throw new ArgumentException("Connect Load Annual Result → Result (or legacy F32).");
            var context = new PlantLightContext(hasResult ? loaded.Value.Result : AnnualIlluminanceResult.Open(cache), profile.Value, time, hasResult && loaded.Value.Weather is not null);
            da.SetData(0, new PlantLightContextGoo(context)); da.SetData(1, context.Description + "; Assumptions: " + profile.Value.Warning);
            SetRevisionMessage("Annual context · select in readers");
        }
        catch (Exception ex) { da.SetData(1, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}

public sealed class CombinePlantLightComponent : FlahaGrowComponent
{
    public CombinePlantLightComponent() : base("Combine Plant Light", "Mix Plant Light", "Combines source contexts: convert each source separately, then add photons. Does not create a combined lux cache.", "FlahaGrow", "05 PPFD") { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa412");
    protected override void RegisterInputParams(GH_InputParamManager p) => p.AddParameter(new PlantLightContextParameter(), "Source contexts", "Sources", "Connect two or more Plant Light Context → Context outputs. Sensors and declared time axes must match.", GH_ParamAccess.list);
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddParameter(new PlantLightContextParameter(), "Combined context", "Context", "Connect to the same PPFD/DLI readers.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Source-specific conversion and alignment declaration.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var contexts = new List<PlantLightContextGoo>(); if (!da.GetDataList(0, contexts)) return;
        try
        {
            if (contexts.Any(c => !c.IsValid)) throw new ArgumentException("Invalid source context.");
            var result = PlantLightContext.Combine(contexts.Select(c => c.Value).ToArray());
            da.SetData(0, new PlantLightContextGoo(result)); da.SetData(1, result.Description);
            SetRevisionMessage("User-declared alignment");
        }
        catch (Exception ex) { da.SetData(1, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}

public abstract class PlantLightReaderComponent : FlahaGrowComponent
{
    private readonly bool dli, sensor;
    protected PlantLightReaderComponent(string name, string nick, bool dli, bool sensor)
        : base(name, nick, "Reads estimated incident plant light using one shared context; PAR 400–700 nm.", "FlahaGrow", dli ? "06 DLI" : "05 PPFD")
    { this.dli = dli; this.sensor = sensor; }
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddParameter(new PlantLightContextParameter(), "Plant Light Context", "Context", "Connect Plant Light Context → Context or Combine Plant Light → Context. No separate factor needed.", GH_ParamAccess.item);
        var isSensor = this is AnnualPpfdAtSensorComponent or AnnualDliAtSensorComponent;
        var isDay = this is DliForDayComponent;
        p.AddIntegerParameter(isSensor ? "Sensor index" : isDay ? "Day index" : "Hour index",
            isSensor ? "Sensor" : isDay ? "Day" : "Hour",
            isSensor ? "Integer slider: 0 to sensor count minus 1. Index into the exact saved simulation sensor order (0 = first point), not a Point or grid label. GenPts order matches only if exported unchanged."
            : isDay ? "Connect Select Date and Hour → Day. Zero-based 0–364; Jan 1 = 0. Do not connect Hour or Date."
            : "Connect Select Date and Hour → Hour. Zero-based 0–8759; Jan 1 00:00–01:00 = 0.", GH_ParamAccess.item);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        // GH calls registration during base construction; use runtime type rather
        // than derived-constructor fields, which have not been initialized yet.
        var isDli = this is DliForDayComponent or AnnualDliAtSensorComponent;
        p.AddNumberParameter(isDli ? "Estimated DLI" : "Estimated PPFD", isDli ? "DLI" : "PPFD",
            isDli ? "mol/m²/day. Day grid or 365-day sensor series, as named." : "µmol/m²/s. Hour grid or 8760-hour sensor series, as named.", GH_ParamAccess.list);
        p.AddTextParameter("Status", "Status", "Quantity, selection and method provenance. Indices do not imply a known calendar date.", GH_ParamAccess.item);
        if (this is DliForDayComponent)
            p.AddNumberParameter("Hourly photon integral", "Hourly", "24 values per sensor branch {sensor}: mol/m² per hourly interval, not PPFD.", GH_ParamAccess.tree);
        p.AddParameter(new PlotAttributesParameter(), "Plot Attributes", "Plot", "Connect to Annual Plot.Plot with this component's matching PPFD/DLI values. Grid outputs describe a grid, not an annual series.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var goo = new PlantLightContextGoo(); int index = 0;
        if (!da.GetData(0, ref goo) || !goo.IsValid || !da.GetData(1, ref index)) return;
        try
        {
            var c = goo.Value;
            double[] values;
            if (dli && !sensor)
            {
                var day = c.DliForDay(index); values = day.Daily;
                var tree = new GH_Structure<GH_Number>();
                for (var s = 0; s < c.Sensors; s++)
                    for (var h = 0; h < 24; h++) tree.Append(new GH_Number(day.HourlyIntegrals[h * c.Sensors + s]), new GH_Path(s));
                da.SetDataTree(2, tree);
            }
            else values = dli ? c.DliAtSensor(index) : sensor ? c.PpfdAtSensor(index) : c.PpfdAtHour(index);
            da.SetDataList(0, values);
            var selection = sensor ? $"sensor index {index} = saved sensor row {index + 1} of {c.Sensors}" : dli ? $"day index {index} (day-of-year {index + 1})"
                : $"hour interval {index}; day index {index / 24}; representative local-standard hour {index % 24 + .5:0.0}";
            da.SetData(1, $"{selection}; {values.Length} {(dli ? "mol/m²/day" : "µmol/m²/s")} values; {c.Description}");
            da.SetData(dli && !sensor ? 3 : 2, new PlotAttributesGoo(PlotAttributes.Create(values,
                dli ? "DLI" : "PPFD", dli ? "mol/m²/day" : "µmol/m²/s",
                sensor ? dli ? "annual-daily" : "annual-hourly" : "sensor-grid", selection, c.Description)));
        }
        catch (Exception ex) { da.SetData(1, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}
public sealed class PpfdAtHourComponent : PlantLightReaderComponent
{
    public PpfdAtHourComponent() : base("PPFD at Hour", "PPFD Hour", false, false) { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa413");
}
public sealed class AnnualPpfdAtSensorComponent : PlantLightReaderComponent
{
    public AnnualPpfdAtSensorComponent() : base("Annual PPFD at Sensor", "PPFD Sensor", false, true) { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa414");
}
public sealed class DliForDayComponent : PlantLightReaderComponent
{
    public DliForDayComponent() : base("DLI for Day", "DLI Day", true, false) { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa415");
}
public sealed class AnnualDliAtSensorComponent : PlantLightReaderComponent
{
    public AnnualDliAtSensorComponent() : base("Annual DLI at Sensor", "DLI Sensor", true, true) { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa416");
}
