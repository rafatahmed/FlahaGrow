using Grasshopper.Kernel;
using FlahaGrow.Core.PlantLight;
using FlahaGrow.Grasshopper.Parameters;

namespace FlahaGrow.Grasshopper.Components;

public sealed class ReadIlluminanceComponent : FlahaGrowComponent
{
    public ReadIlluminanceComponent() : base("Read Illuminance", "Read Lux", "Reads annual illuminance from a FlahaGrow .f32 cache by sensor or hour.", "FlahaGrow", "03 Annual") { }
    public override Guid ComponentGuid => new("3d38a66d-b381-45f2-ad70-57e6be84a6cc");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddTextParameter("Result cache", "F32", "Annual .f32 cache.", GH_ParamAccess.item); p.AddTextParameter("Mode", "Mode", "sensor or hour.", GH_ParamAccess.item, "sensor"); p.AddIntegerParameter("Index", "i", "Sensor or hour index.", GH_ParamAccess.item); p.AddBooleanParameter("Run", "Run", "Read the cache.", GH_ParamAccess.item, false); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("Illuminance", "Lux", "Selected illuminance values.", GH_ParamAccess.list); p.AddTextParameter("Status", "Status", "Read status.", GH_ParamAccess.item); p.AddParameter(new PlotAttributesParameter(), "Plot Attributes", "Plot", "Connect with Lux to Annual Plot for sensor series; hour selections describe a sensor grid.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string path = string.Empty, mode = "sensor"; var index = 0; var run = false; if (!da.GetData(0, ref path)) return; da.GetData(1, ref mode); da.GetData(2, ref index); da.GetData(3, ref run); if (!run) return;
        try
        {
            mode = mode.Trim().ToLowerInvariant();
            if (mode is not ("hour" or "sensor")) throw new ArgumentException("Mode must be 'hour' or 'sensor'.");
            var result = AnnualIlluminanceResult.Open(path);
            var values = mode == "hour" ? result.ReadHour(index) : result.ReadSensor(index);
            var plot = PlotAttributes.Create(values, "Illuminance", "lux",
                mode == "hour" ? "sensor-grid" : "annual-hourly", $"{mode} index {index}",
                $"run {result.RunId}; validated identity {result.Identity}");
            da.SetDataList(0, values);
            da.SetData(2, new PlotAttributesGoo(plot));
            da.SetData(1, $"OK: {mode} {index} → {values.Length} {(mode == "hour" ? "sensor" : "hour")} values.");
        }
        catch (Exception ex) { da.SetData(1, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}
