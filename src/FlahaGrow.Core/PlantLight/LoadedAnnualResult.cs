using System.Globalization;
using FlahaGrow.Core.Annual;

namespace FlahaGrow.Core.PlantLight;

public sealed record AnnualWeather(string Location, double Latitude, double Longitude, int UtcMinutes, string SourceHash)
{
    public string Alignment => AnnualTime.Alignment(UtcMinutes);
}

/// <summary>Transient, verified result identity and run-owned weather provenance.</summary>
public sealed record LoadedAnnualResult(AnnualIlluminanceResult Result, AnnualWeather? Weather, string WeatherStatus)
{
    public static LoadedAnnualResult From(AnnualIlluminanceResult result)
    {
        var folder = Path.GetDirectoryName(result.CachePath)!;
        var path = Path.Combine(folder, "weather.epw");
        if (!File.Exists(path)) return new(result, null, "No run-owned EPW snapshot. Calendar/UTC unavailable; no timezone was assumed.");
        if (result.Hours != 8760) throw new InvalidDataException("Run weather inheritance requires 8760 hourly results.");
        var manifest = AnnualRun.Read(folder);
        var expected = manifest.Inputs.Where(p => p.Key.EndsWith(".epw", StringComparison.OrdinalIgnoreCase)).Select(p => p.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var hash = AnnualRun.HashFile(path);
        if (expected.Length != 1 || !string.Equals(hash, expected[0], StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Run weather snapshot does not match its recorded EPW hash. Timing was not inferred.");
        using var reader = new StreamReader(path);
        var fields = (reader.ReadLine() ?? "").Split(',').Select(x => x.Trim()).ToArray();
        if (fields.Length < 10 || fields[0] != "LOCATION") throw new InvalidDataException("Malformed EPW LOCATION header.");
        double Number(int i) => double.TryParse(fields[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var n) && double.IsFinite(n) ? n : throw new InvalidDataException("Invalid EPW location number.");
        var lat = Number(6); var lon = Number(7); var offset = Number(8) * 60;
        if (Math.Abs(lat) > 90 || Math.Abs(lon) > 180 || Math.Abs(offset) > 840 || Math.Abs(offset - Math.Round(offset)) > 1e-8)
            throw new InvalidDataException("EPW location or UTC offset is out of range.");
        for (var i = 0; i < 7; i++) if (reader.ReadLine() is null) throw new InvalidDataException("Incomplete EPW header.");
        for (var i = 0; i < 8760; i++)
        {
            var record = (reader.ReadLine() ?? "").Split(',');
            var expectedDate = new DateTime(2001, 1, 1).AddHours(i);
            if (record.Length < 35 || !int.TryParse(record[1], out var m) || !int.TryParse(record[2], out var d) || !int.TryParse(record[3], out var h)
                || m != expectedDate.Month || d != expectedDate.Day || h != expectedDate.Hour + 1)
                throw new InvalidDataException($"EPW calendar is not consecutive Jan–Dec hourly data at record {i}. Timing cannot be inherited.");
        }
        if (!string.IsNullOrWhiteSpace(reader.ReadToEnd())) throw new InvalidDataException("EPW has extra records.");
        if (AnnualRun.HashFile(path) != hash) throw new IOException("Weather changed during verification.");
        return new(result, new(fields[1], lat, lon, (int)Math.Round(offset), hash), "Verified EPW; 365-day local standard time; EPW hour-end records represented by hourly intervals.");
    }
}
