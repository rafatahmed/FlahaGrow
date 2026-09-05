using System.Text.Json;
using System.Text.Json.Serialization;
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
        try
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Provide a valid .f32 result-cache path.");
            var metaPath = Path.ChangeExtension(path, ".meta.json");
            if (!File.Exists(metaPath)) throw new FileNotFoundException($"Meta not found beside cache: {metaPath}");
            var meta = JsonSerializer.Deserialize<Meta>(File.ReadAllText(metaPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("The annual-result metadata could not be read.");
            if (meta.Sensors <= 0 || meta.Hours <= 0) throw new InvalidDataException($"Invalid dims in meta: sensors={meta.Sensors}, hours={meta.Hours}");
            if (meta.Ncomp != 1) throw new InvalidDataException($"ncomp={meta.Ncomp} is not supported; illuminance requires one component.");
            var expectedBytes = checked((long)meta.Sensors * meta.Hours * sizeof(float));
            if (new FileInfo(path).Length != expectedBytes) throw new InvalidDataException($"Cache size mismatch: got {new FileInfo(path).Length / sizeof(float)} floats, expected {(long)meta.Sensors * meta.Hours}.");
            using var f = File.OpenRead(path); var values = new List<float>();
            if (mode.Equals("hour", StringComparison.OrdinalIgnoreCase))
            {
                if (index < 0 || index >= meta.Hours) throw new ArgumentOutOfRangeException(nameof(index), $"Hour index out of range [0..{meta.Hours - 1}]");
                f.Position = index * meta.Sensors * sizeof(float); var b = new byte[meta.Sensors * sizeof(float)]; f.ReadExactly(b);
                for (var i = 0; i < meta.Sensors; i++) values.Add(BitConverter.ToSingle(b, i * sizeof(float)));
                da.SetData(1, $"OK: hour {index} → {values.Count} sensor values.");
            }
            else
            {
                if (index < 0 || index >= meta.Sensors) throw new ArgumentOutOfRangeException(nameof(index), $"Sensor index out of range [0..{meta.Sensors - 1}]");
                var b = new byte[sizeof(float)];
                for (var h = 0; h < meta.Hours; h++) { f.Position = ((long)h * meta.Sensors + index) * sizeof(float); f.ReadExactly(b); values.Add(BitConverter.ToSingle(b)); }
                da.SetData(1, $"OK: sensor {index} → {values.Count} hour values.");
            }
            da.SetDataList(0, values);
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private sealed record Meta(
        [property: JsonPropertyName("sensors")] int Sensors,
        [property: JsonPropertyName("hours")] int Hours,
        [property: JsonPropertyName("ncomp")] int Ncomp);
}
public sealed class IlluminancePointInTimeComponent : IlluminanceReaderComponent { public IlluminancePointInTimeComponent() : base("Illuminance Point in Time", "Illuminance", new Guid("9e076d21-00df-4ea2-870e-caf9748ac3d3")) { } }
public sealed class IlluminanceSensorComponent : IlluminanceReaderComponent { public IlluminanceSensorComponent() : base("Illuminance Sensor", "Illuminance Sensor", new Guid("3d38a66d-b381-45f2-ad70-57e6be84a6cc")) { } }
