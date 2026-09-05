using System.Buffers.Binary;
using System.Text.Json;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Builds the legacy row-major annual float cache from one or four Radiance .ill files.</summary>
public sealed class AnnualResultCacheComponent : GH_Component
{
    public AnnualResultCacheComponent() : base("Build Annual Cache", "Annual Cache", "Merges annualRfinal part files and writes the FlahaGrow .f32 plus metadata cache.", "FlahaGrow", "Annual") { }
    public override Guid ComponentGuid => new("0e5f7114-fbb9-4a77-a3f4-40ccd0c0c258");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddTextParameter("Result folder", "Folder", "Folder containing annualRfinal_part*.ill files.", GH_ParamAccess.item); p.AddBooleanParameter("Build", "Build", "Build or read the cache.", GH_ParamAccess.item, false); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddTextParameter("Result cache", "F32", "Little-endian float32 cache path.", GH_ParamAccess.item); p.AddIntegerParameter("Sensors", "S", "Sensor count.", GH_ParamAccess.item); p.AddIntegerParameter("Hours", "H", "Hour count.", GH_ParamAccess.item); p.AddTextParameter("Status", "Status", "Cache status.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string folder = string.Empty; var build = false; if (!da.GetData(0, ref folder)) return; da.GetData(1, ref build);
        try
        {
            folder = Path.GetFullPath(folder); var raw = Path.Combine(folder, "annualRfinal.f32"); var meta = Path.Combine(folder, "annualRfinal.meta.json");
            if (!build) { if (!File.Exists(raw) || !File.Exists(meta)) { da.SetData(3, "No cache yet — set Build True."); return; } var cached = JsonSerializer.Deserialize<CacheMeta>(File.ReadAllText(meta))!; da.SetData(0, raw); da.SetData(1, cached.Sensors); da.SetData(2, cached.Hours); da.SetData(3, "Cache exists"); return; }
            var parts = Directory.EnumerateFiles(folder, "annualRfinal_part*.ill").OrderBy(path => path).ToList(); if (parts.Count == 0) throw new FileNotFoundException("No annualRfinal_part*.ill files were found.");
            var matrices = parts.Select(ReadMatrix).ToList(); var hours = matrices[0].Count; if (matrices.Any(matrix => matrix.Count != hours)) throw new InvalidDataException("Part files have different hour counts.");
            var sensors = matrices.Sum(matrix => matrix[0].Length); using var stream = File.Create(raw);
            for (var hour = 0; hour < hours; hour++) foreach (var matrix in matrices) foreach (var value in matrix[hour]) stream.Write(BitConverter.GetBytes(value));
            File.WriteAllText(meta, JsonSerializer.Serialize(new CacheMeta(sensors, hours, 1, "row-major hours x sensors"), new JsonSerializerOptions { WriteIndented = true }));
            da.SetData(0, raw); da.SetData(1, sensors); da.SetData(2, hours); da.SetData(3, $"Merged + cached: {sensors} sensors × {hours} hours");
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private static List<float[]> ReadMatrix(string path) => File.ReadLines(path).Where(line => line.Length > 0 && !line.StartsWith("#") && !line.Contains('=' )).Select(line => line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(float.Parse).ToArray()).Where(row => row.Length > 0).ToList();
    private sealed record CacheMeta(int Sensors, int Hours, int Ncomp, string Order);
}
