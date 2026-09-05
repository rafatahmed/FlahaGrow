using System.Text.Json;
using System.Text.Json.Serialization;

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
        return (meta.Sensors, meta.Hours);
    }
    internal static List<double> Hour(string cachePath, int hour)
    {
        var (sensors, hours) = Dimensions(cachePath);
        if (hour < 0 || hour >= hours) throw new ArgumentOutOfRangeException(nameof(hour), $"Hour index out of range [0..{hours - 1}].");
        using var stream = File.OpenRead(cachePath); stream.Position = (long)hour * sensors * sizeof(float);
        var bytes = new byte[sensors * sizeof(float)]; stream.ReadExactly(bytes);
        return Enumerable.Range(0, sensors).Select(index => (double)BitConverter.ToSingle(bytes, index * sizeof(float))).ToList();
    }
    internal static List<double> Sensor(string cachePath, int sensor)
    {
        var (sensors, hours) = Dimensions(cachePath);
        if (sensor < 0 || sensor >= sensors) throw new ArgumentOutOfRangeException(nameof(sensor), $"Sensor index out of range [0..{sensors - 1}].");
        using var stream = File.OpenRead(cachePath); var bytes = new byte[sizeof(float)]; var values = new List<double>(hours);
        for (var hour = 0; hour < hours; hour++) { stream.Position = ((long)hour * sensors + sensor) * sizeof(float); stream.ReadExactly(bytes); values.Add(BitConverter.ToSingle(bytes)); }
        return values;
    }
    internal static double Factor(object? value)
    {
        if (value is null) return .0185;
        if (value is IConvertible convertible && value is not string) { try { return convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture); } catch { } }
        var text = value.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;
        return text switch { "electric" or "elec" or "electriconly" or "electric_light" or "electriclighting" => .015, "sunonly" or "sun" or "sunlight" => .0205, "skyonly" or "sky" => .0135, _ when double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var factor) => factor, _ => .0185 };
    }
    private sealed record Meta([property: JsonPropertyName("sensors")] int Sensors, [property: JsonPropertyName("hours")] int Hours, [property: JsonPropertyName("ncomp")] int Ncomp);
}
