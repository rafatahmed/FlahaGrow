using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

public sealed class HourlyPpfdComponent : GH_Component
{
    public HourlyPpfdComponent() : base("Hourly PPFD", "Hourly PPFD", "Converts selected-hour illuminance values to PPFD.", "FlahaGrow", "PPFD") { }
    public override Guid ComponentGuid => new("9eebe812-eeb5-476c-a4b6-ca0822940f1f");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddNumberParameter("Illuminance", "Lux", "Per-sensor illuminance values.", GH_ParamAccess.list); p.AddNumberParameter("Conversion factor", "Factor", "PPFD per lux.", GH_ParamAccess.item, .0185); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) => p.AddNumberParameter("PPFD", "PPFD", "Per-sensor PPFD in μmol/m²/s.", GH_ParamAccess.list);
    protected override void SolveInstance(IGH_DataAccess da) { var lux = new List<double>(); var factor = .0185; if (!da.GetDataList(0, lux)) return; da.GetData(1, ref factor); if (factor < 0 || lux.Any(x => x < 0)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Illuminance and factor must be non-negative."); return; } da.SetDataList(0, lux.Select(x => x * factor)); }
}
public sealed class AnnualSensorPpfdComponent : GH_Component
{
    public AnnualSensorPpfdComponent() : base("PPFD Each Sensor", "Sensor PPFD", "Converts one sensor's annual illuminance series to PPFD.", "FlahaGrow", "PPFD") { }
    public override Guid ComponentGuid => new("0bc7a4db-8702-4e8d-a5bc-dd648ba3ec6e");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddNumberParameter("Annual illuminance", "Lux", "Hourly sensor illuminance values.", GH_ParamAccess.list); p.AddNumberParameter("Conversion factor", "Factor", "PPFD per lux.", GH_ParamAccess.item, .0185); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) => p.AddNumberParameter("Annual PPFD", "PPFD", "Hourly PPFD values.", GH_ParamAccess.list);
    protected override void SolveInstance(IGH_DataAccess da) { var lux = new List<double>(); var factor = .0185; if (!da.GetDataList(0, lux)) return; da.GetData(1, ref factor); if (lux.Count != 8760) AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Expected 8,760 annual values."); da.SetDataList(0, lux.Select(x => x * factor)); }
}
