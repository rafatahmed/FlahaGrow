using System.Text.Json;
using System.Text.Json.Serialization;
using Grasshopper.Kernel;
using FlahaGrow.Core.Annual;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Builds the legacy row-major annual float cache from one or four Radiance .ill files.</summary>
public sealed class AnnualResultCacheComponent : FlahaGrowComponent
{
    public AnnualResultCacheComponent() : base("Load Annual Result", "Load Result", "Merges annualRfinal part files and writes the FlahaGrow .f32 plus metadata cache.", "FlahaGrow", "03 Annual") { }
    public override Guid ComponentGuid => new("0e5f7114-fbb9-4a77-a3f4-40ccd0c0c258");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddTextParameter("Result folder", "Folder", "Folder containing annualRfinal_part*.ill files.", GH_ParamAccess.item); p.AddBooleanParameter("Build", "Build", "Build or read the cache.", GH_ParamAccess.item, false); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddTextParameter("Result cache", "F32", "Little-endian float32 cache path.", GH_ParamAccess.item); p.AddIntegerParameter("Sensors", "S", "Sensor count.", GH_ParamAccess.item); p.AddIntegerParameter("Hours", "H", "Hour count.", GH_ParamAccess.item); p.AddTextParameter("Status", "Status", "Cache status.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string folder = string.Empty; var build = false; if (!da.GetData(0, ref folder)) return; da.GetData(1, ref build);
        try
        {
            folder = Path.GetFullPath(folder); var raw = Path.Combine(folder, "annualRfinal.f32"); var meta = Path.Combine(folder, "annualRfinal.meta.json");
            var manifest = AnnualRun.Read(folder);
            AnnualPartStatus.RequireComplete(folder, manifest);
            var parts = AnnualRun.RequireResults(folder, manifest);
            var signature = AnnualRun.HashFile(Path.Combine(folder, AnnualRun.ManifestName)) + ":" + string.Join(":", parts.Select(AnnualRun.HashFile));
            if (!build) { if (!File.Exists(raw) || !File.Exists(meta)) { da.SetData(3, "No cache yet - set Build True."); return; } var cached = JsonSerializer.Deserialize<CacheMeta>(File.ReadAllText(meta)) ?? throw new InvalidDataException("Empty cache metadata.");
                if (cached.RunId != manifest.RunId || cached.Signature != signature || cached.Sensors != manifest.Sensors
                    || cached.Hours != manifest.Hours || cached.Ncomp != 1 || cached.ValidationVersion != 1
                    || new FileInfo(raw).Length != checked((long)cached.Sensors * cached.Hours * sizeof(float))
                    || cached.CacheHash != AnnualRun.HashFile(raw))
                    throw new InvalidDataException("Cache does not match this run and its current results. Rebuild the cache.");
                da.SetData(0, raw); da.SetData(1, cached.Sensors); da.SetData(2, cached.Hours); da.SetData(3, "Cache matches run " + manifest.RunId); return; }
            var hours = manifest.Hours; var sensors = manifest.Sensors;
            var temporary = raw + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                var readers = parts.Select((path, index) => AnnualMatrix.Rows(path, hours, manifest.Parts[index].Sensors).GetEnumerator()).ToArray();
                try
                {
                    using var stream = File.Create(temporary);
                    using var writer = new BinaryWriter(stream);
                    for (var hour = 0; hour < hours; hour++)
                        foreach (var reader in readers)
                        {
                            if (!reader.MoveNext()) throw new InvalidDataException("Truncated annual matrix.");
                            foreach (var value in reader.Current) writer.Write(value);
                        }
                    foreach (var reader in readers)
                        if (reader.MoveNext()) throw new InvalidDataException("Extra annual matrix rows.");
                }
                finally { foreach (var reader in readers) reader.Dispose(); }
                var after = AnnualRun.HashFile(Path.Combine(folder, AnnualRun.ManifestName)) + ":" + string.Join(":", parts.Select(AnnualRun.HashFile));
                if (after != signature) throw new InvalidDataException("Run results changed while building the cache. Wait for the run to finish.");
                AnnualPartStatus.RequireComplete(folder, manifest);
                if (File.Exists(meta)) File.Delete(meta);
                File.Move(temporary, raw, true);
                var metadata = JsonSerializer.Serialize(new CacheMeta(sensors, hours, 1, "row-major hours x sensors", manifest.RunId, signature, 1, AnnualRun.HashFile(raw)), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(temporary, metadata);
                File.Move(temporary, meta, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            da.SetData(0, raw); da.SetData(1, sensors); da.SetData(2, hours); da.SetData(3, $"Merged + cached: {sensors} sensors × {hours} hours");
        }
        catch (Exception ex) { da.SetData(3, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private sealed record CacheMeta(
        [property: JsonPropertyName("sensors")] int Sensors,
        [property: JsonPropertyName("hours")] int Hours,
        [property: JsonPropertyName("ncomp")] int Ncomp,
        [property: JsonPropertyName("order")] string Order,
        [property: JsonPropertyName("runId")] Guid RunId,
        [property: JsonPropertyName("sourceSignature")] string Signature,
        [property: JsonPropertyName("validationVersion")] int ValidationVersion,
        [property: JsonPropertyName("cacheHash")] string CacheHash);
}
