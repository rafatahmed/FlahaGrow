using System.Text.Json;
using System.Text.Json.Serialization;
using FlahaGrow.Core.PlantLight;

namespace FlahaGrow.Core.Annual;

public static class AnnualCacheLoader
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> Gates = new(StringComparer.OrdinalIgnoreCase);
    public static LoadedAnnualResult Load(string folder, bool build, CancellationToken token, Action<string>? progress = null)
    {
        folder = Path.GetFullPath(folder);
        var gate = Gates.GetOrAdd(folder, _ => new SemaphoreSlim(1, 1));
        gate.Wait(token);
        try
        {
            folder = Path.GetFullPath(folder); var raw = Path.Combine(folder, "annualRfinal.f32"); var meta = Path.Combine(folder, "annualRfinal.meta.json");
            var manifest = AnnualRun.Read(folder);
            AnnualPartStatus.RequireComplete(folder, manifest);
            var parts = AnnualRun.RequireResults(folder, manifest);
            token.ThrowIfCancellationRequested(); progress?.Invoke("Validating source files…");
            AnnualIlluminanceResult? reusable = null;
            if (File.Exists(raw) && File.Exists(meta))
            {
                try { reusable = AnnualIlluminanceResult.Open(raw); }
                catch (Exception ex) when (build && ex is IOException or InvalidDataException or ArgumentException or JsonException) { }
            }
            if (reusable is not null) { token.ThrowIfCancellationRequested(); return LoadedAnnualResult.From(reusable); }
            if (!build) throw new InvalidDataException("No valid cache. Click Build to create it.");
            var signature = AnnualRun.HashFile(Path.Combine(folder, AnnualRun.ManifestName)) + ":" + string.Join(":", parts.Select(AnnualRun.HashFile));
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
                    {
                        token.ThrowIfCancellationRequested();
                        if (hour % 128 == 0) progress?.Invoke($"Building cache: {hour * 100 / hours}%");
                        foreach (var reader in readers)
                        {
                            if (!reader.MoveNext()) throw new InvalidDataException("Truncated annual matrix.");
                            foreach (var value in reader.Current) writer.Write(value);
                        }
                    }
                    foreach (var reader in readers)
                        if (reader.MoveNext()) throw new InvalidDataException("Extra annual matrix rows.");
                }
                finally { foreach (var reader in readers) reader.Dispose(); }
                token.ThrowIfCancellationRequested(); progress?.Invoke("Verifying completed cache…");
                var after = AnnualRun.HashFile(Path.Combine(folder, AnnualRun.ManifestName)) + ":" + string.Join(":", parts.Select(AnnualRun.HashFile));
                if (after != signature) throw new InvalidDataException("Run results changed while building the cache. Wait for the run to finish.");
                AnnualPartStatus.RequireComplete(folder, manifest);
                token.ThrowIfCancellationRequested();
                if (File.Exists(meta)) File.Delete(meta);
                File.Move(temporary, raw, true);
                var metadata = JsonSerializer.Serialize(new CacheMeta(sensors, hours, 1, "row-major hours x sensors", manifest.RunId, signature, 1, AnnualRun.HashFile(raw)), new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(temporary, metadata);
                File.Move(temporary, meta, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return LoadedAnnualResult.From(AnnualIlluminanceResult.Open(raw));
        }
        finally { gate.Release(); }
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
