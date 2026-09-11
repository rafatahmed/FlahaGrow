using System.Text.Json;
using System.Text.Json.Serialization;
using FlahaGrow.Core.Annual;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Shared reader for the legacy-compatible annual float cache.</summary>
internal static class AnnualCacheData
{
    internal static (int Sensors, int Hours) Dimensions(string cachePath)
    {
        if (!File.Exists(cachePath)) throw new FileNotFoundException("Annual result cache was not found.", cachePath);
        var metaPath = Path.ChangeExtension(cachePath, ".meta.json");
        if (!File.Exists(metaPath)) throw new FileNotFoundException("Metadata JSON was not found beside the annual cache.", metaPath);
        var meta = JsonSerializer.Deserialize<Meta>(File.ReadAllText(metaPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidDataException("Annual cache metadata could not be read.");
        if (meta.Sensors <= 0 || meta.Hours <= 0 || meta.Ncomp != 1) throw new InvalidDataException("Annual cache metadata has invalid dimensions or component count.");
        var expectedBytes = checked((long)meta.Sensors * meta.Hours * sizeof(float));
        if (new FileInfo(cachePath).Length != expectedBytes) throw new InvalidDataException("Annual cache size does not match its metadata.");
        RequireProvenance(cachePath, meta);
        return (meta.Sensors, meta.Hours);
    }
    internal static List<double> Hour(string cachePath, int hour)
    {
        var (sensors, hours) = Dimensions(cachePath);
        if (hour < 0 || hour >= hours) throw new ArgumentOutOfRangeException(nameof(hour), $"Hour index out of range [0..{hours - 1}].");
        using var stream = File.OpenRead(cachePath); stream.Position = (long)hour * sensors * sizeof(float);
        var bytes = new byte[sensors * sizeof(float)]; stream.ReadExactly(bytes);
        var values = Enumerable.Range(0, sensors).Select(index => (double)BitConverter.ToSingle(bytes, index * sizeof(float))).ToList();
        RequireIlluminance(values);
        return values;
    }
    internal static List<double> Sensor(string cachePath, int sensor)
    {
        var (sensors, hours) = Dimensions(cachePath);
        if (sensor < 0 || sensor >= sensors) throw new ArgumentOutOfRangeException(nameof(sensor), $"Sensor index out of range [0..{sensors - 1}].");
        using var stream = File.OpenRead(cachePath); var bytes = new byte[sizeof(float)]; var values = new List<double>(hours);
        for (var hour = 0; hour < hours; hour++) { stream.Position = ((long)hour * sensors + sensor) * sizeof(float); stream.ReadExactly(bytes); values.Add(BitConverter.ToSingle(bytes)); }
        RequireIlluminance(values);
        return values;
    }
    /// <summary>
    /// Reads the selected legacy 24-hour day for every sensor in one contiguous
    /// operation. Provenance and cache integrity are checked once, rather than
    /// once per sensor, which is essential for interactive Grasshopper solves.
    /// </summary>
    internal static (int Sensors, int Hours, int StartHour, List<double> Values) Day24(string cachePath, int hourIndex)
    {
        var (sensors, hours) = Dimensions(cachePath);
        var requestedHour = Math.Max(0, hourIndex);
        var daysAvailable = hours >= 24 ? Math.Max(1, hours / 24) : 1;
        var dayIndex = Math.Min(requestedHour / 24, daysAvailable - 1);
        var startHour = dayIndex * 24;
        var sampleHours = Math.Min(24, hours - startHour);
        using var stream = File.OpenRead(cachePath);
        stream.Position = checked((long)startHour * sensors * sizeof(float));
        var bytes = new byte[checked(sampleHours * sensors * sizeof(float))];
        stream.ReadExactly(bytes);
        var values = Enumerable.Range(0, checked(sampleHours * sensors))
            .Select(index => (double)BitConverter.ToSingle(bytes, index * sizeof(float))).ToList();
        RequireIlluminance(values);
        return (sensors, hours, startHour, values);
    }
    internal static void RequireIlluminance(IEnumerable<double> values)
    {
        if (values.Any(value => !double.IsFinite(value) || value < 0))
            throw new InvalidDataException("Cache contains negative or nonfinite illuminance. Investigate the annual calculation and rebuild from corrected results; values were not clamped.");
    }
    internal static double Factor(object? value)
    {
        if (value is null) return .0185;
        if (value is IConvertible convertible && value is not string) { try { return convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture); } catch { } }
        var text = value.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
        return text switch { "electric" or "elec" or "electriconly" or "electric_light" or "electriclighting" => .015, "sunonly" or "sun" or "sunlight" => .0205, "skyonly" or "sky" => .0135, _ when double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var factor) => factor, _ => .0185 };
    }
    private static void RequireProvenance(string cachePath, Meta meta)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(cachePath)) ?? throw new InvalidDataException("Annual cache folder is unavailable.");
        var manifest = AnnualRun.Read(folder);
        AnnualPartStatus.RequireComplete(folder, manifest);
        var parts = AnnualRun.RequireResults(folder, manifest);
        var signature = AnnualRun.HashFile(Path.Combine(folder, AnnualRun.ManifestName)) + ":" + string.Join(":", parts.Select(AnnualRun.HashFile));
        if (meta.RunId != manifest.RunId || meta.SourceSignature != signature || meta.ValidationVersion != 1
            || !string.Equals(meta.CacheHash, AnnualRun.HashFile(cachePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Cache provenance does not match the manifest-owned, validated annual result. Rebuild Load Annual Result.");
    }
    private sealed record Meta(
        [property: JsonPropertyName("sensors")] int Sensors,
        [property: JsonPropertyName("hours")] int Hours,
        [property: JsonPropertyName("ncomp")] int Ncomp,
        [property: JsonPropertyName("runId")] Guid RunId,
        [property: JsonPropertyName("sourceSignature")] string SourceSignature,
        [property: JsonPropertyName("validationVersion")] int ValidationVersion,
        [property: JsonPropertyName("cacheHash")] string CacheHash);
}
