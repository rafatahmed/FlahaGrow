using System.Text.Json;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

public abstract class IlluminanceReaderComponent : GH_Component
{
    protected IlluminanceReaderComponent(string name, string nick, Guid id) : base(name, nick, "Reads annual illuminance from a FlahaGrow .f32 cache by sensor or hour.", "FlahaGrow", "Annual") => Id = id;
    private Guid Id { get; }
    public override Guid ComponentGuid => Id;
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddTextParameter("Result cache", "F32", "Annual .f32 cache.", GH_ParamAccess.item); p.AddTextParameter("Mode", "Mode", "sensor or hour.", GH_ParamAccess.item, "sensor"); p.AddIntegerParameter("Index", "i", "Sensor or hour index.", GH_ParamAccess.item); p.AddBooleanParameter("Run", "Run", "Read the cache.", GH_ParamAccess.item, false); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("Illuminance", "Lux", "Selected illuminance values.", GH_ParamAccess.list); p.AddTextParameter("Status", "Status", "Read status.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string path = string.Empty, mode = "sensor"; var index = 0; var run = false; if (!da.GetData(0, ref path)) return; da.GetData(1, ref mode); da.GetData(2, ref index); da.GetData(3, ref run); if (!run) return;
        try { var meta = JsonSerializer.Deserialize<Meta>(File.ReadAllText(Path.ChangeExtension(path, ".meta.json")))!; using var f = File.OpenRead(path); var values = new List<float>(); if (mode.Equals("hour", StringComparison.OrdinalIgnoreCase)) { if (index < 0 || index >= meta.Hours) throw new IndexOutOfRangeException(); f.Position = index * meta.Sensors * 4L; var b = new byte[meta.Sensors * 4]; f.ReadExactly(b); for (var i = 0; i < meta.Sensors; i++) values.Add(BitConverter.ToSingle(b, i * 4)); } else { if (index < 0 || index >= meta.Sensors) throw new IndexOutOfRangeException(); for (var h = 0; h < meta.Hours; h++) { f.Position = (h * meta.Sensors + index) * 4L; var b = new byte[4]; f.ReadExactly(b); values.Add(BitConverter.ToSingle(b)); } } da.SetDataList(0, values); da.SetData(1, $"OK: {values.Count} values."); }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private sealed record Meta(int Sensors, int Hours, int Ncomp);
}
public sealed class IlluminancePointInTimeComponent : IlluminanceReaderComponent { public IlluminancePointInTimeComponent() : base("Illuminance Point in Time", "Illuminance", new Guid("9e076d21-00df-4ea2-870e-caf9748ac3d3")) { } }
public sealed class IlluminanceSensorComponent : IlluminanceReaderComponent { public IlluminanceSensorComponent() : base("Illuminance Sensor", "Illuminance Sensor", new Guid("3d38a66d-b381-45f2-ad70-57e6be84a6cc")) { } }
