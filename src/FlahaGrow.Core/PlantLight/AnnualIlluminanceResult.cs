using System.Text.Json;
using FlahaGrow.Core.Annual;

namespace FlahaGrow.Core.PlantLight;

/// <summary>
/// Immutable descriptor, no annual matrix or live stream retained. Reads lock
/// all manifest-owned inputs and validate content, not just size/mtime.
/// Full validation is intentionally retained until a measured safe reuse design exists.
/// </summary>
public sealed class AnnualIlluminanceResult
{
    public string CachePath { get; }
    public string Identity { get; }
    public string SensorHash { get; }
    public Guid RunId { get; }
    public int Sensors { get; }
    public int Hours { get; }
    public bool IsCombinedLux { get; }

    private AnnualIlluminanceResult(string path, string identity, AnnualRunManifest run)
    {
        CachePath = path; Identity = identity; SensorHash = run.SensorHash;
        RunId = run.RunId; Sensors = run.Sensors; Hours = run.Hours;
        IsCombinedLux = run.Inputs.ContainsKey("daylightManifest") || run.Inputs.ContainsKey("electricManifest");
    }

    public static AnnualIlluminanceResult Open(string path) => WithValidated(path, (result, _) => result);

    public double[] ReadHour(int hour) => ReadBlock(hour, 1);
    public double[] ReadDay(int day)
    {
        if (Hours != 8760) throw new InvalidDataException("Daily plant-light results require 8,760 hourly intervals.");
        if (day < 0 || day >= 365) throw new ArgumentOutOfRangeException(nameof(day), "Day index must be 0–364.");
        return ReadBlock(day * 24, 24);
    }

    private double[] ReadBlock(int hour, int count)
    {
        if (hour < 0 || hour > Hours - count) throw new ArgumentOutOfRangeException(nameof(hour));
        return Read(reader =>
        {
            reader.BaseStream.Position = checked((long)hour * Sensors * sizeof(float));
            var values = new double[checked(count * Sensors)];
            for (var i = 0; i < values.Length; i++) values[i] = PlantLightMath.NonNegative(reader.ReadSingle(), "Cache lux");
            return values;
        });
    }

    public double[] ReadSensor(int sensor)
    {
        if (sensor < 0 || sensor >= Sensors) throw new ArgumentOutOfRangeException(nameof(sensor));
        return Read(reader =>
        {
            var values = new double[Hours];
            for (var hour = 0; hour < Hours; hour++)
            {
                reader.BaseStream.Position = checked(((long)hour * Sensors + sensor) * sizeof(float));
                values[hour] = PlantLightMath.NonNegative(reader.ReadSingle(), "Cache lux");
            }
            return values;
        });
    }

    private T Read<T>(Func<BinaryReader, T> action) => WithValidated(CachePath, (current, reader) =>
    {
        if (current.Identity != Identity) throw new InvalidDataException("Annual result changed. Recompute Plant Light Context from Load Annual Result.");
        return action(reader);
    });

    private static T WithValidated<T>(string path, Func<AnnualIlluminanceResult, BinaryReader, T> action)
    {
        path = Path.GetFullPath(path);
        var folder = Path.GetDirectoryName(path)!;
        var manifestPath = Path.Combine(folder, AnnualRun.ManifestName);
        var metaPath = Path.ChangeExtension(path, ".meta.json");
        var locks = new List<FileStream>();
        FileStream Lock(string file)
        {
            var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            locks.Add(stream); return stream;
        }
        try
        {
            Lock(manifestPath); Lock(metaPath);
            var cache = Lock(path);
            var run = AnnualRun.Read(folder);
            var parts = AnnualRun.RequireResults(folder, run);
            foreach (var file in parts) Lock(file);
            foreach (var part in run.Parts) Lock(Path.Combine(folder, part.StateFile));
            if (new FileInfo(metaPath).Length > 1024 * 1024) throw new InvalidDataException("Cache metadata exceeds 1 MiB.");
            using var json = JsonDocument.Parse(File.ReadAllText(metaPath));
            var meta = json.RootElement;
            if (meta.GetProperty("sensors").GetInt32() != run.Sensors || meta.GetProperty("hours").GetInt32() != run.Hours
                || meta.GetProperty("ncomp").GetInt32() != 1 || meta.GetProperty("validationVersion").GetInt32() != 1
                || meta.GetProperty("runId").GetGuid() != run.RunId
                || meta.GetProperty("order").GetString() != "row-major hours x sensors"
                || cache.Length != checked((long)run.Sensors * run.Hours * sizeof(float)))
                throw new InvalidDataException("Cache dimensions, order or run identity do not match the manifest.");
            AnnualPartStatus.RequireComplete(folder, run);
            var signature = AnnualRun.HashFile(manifestPath) + ":" + string.Join(":", parts.Select(AnnualRun.HashFile));
            var hash = AnnualRun.HashFile(path);
            if (meta.GetProperty("sourceSignature").GetString() != signature
                || !string.Equals(meta.GetProperty("cacheHash").GetString(), hash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Cache provenance mismatch. Rebuild Load Annual Result.");
            var result = new AnnualIlluminanceResult(path, signature + ":" + hash + ":" + AnnualRun.HashFile(metaPath), run);
            using var reader = new BinaryReader(cache, System.Text.Encoding.UTF8, leaveOpen: true);
            return action(result, reader);
        }
        finally { foreach (var stream in locks) stream.Dispose(); }
    }
}
