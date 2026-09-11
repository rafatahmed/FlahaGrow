using System.Globalization;

namespace FlahaGrow.Core.Annual;

/// <summary>Strict EPW-to-WEA conversion matching Ladybug's climate-based-sky contract.</summary>
public static class LadybugWea
{
    public const int AnnualHours = 365 * 24;

    public static void WriteFromEpw(string epwPath, string weaPath)
    {
        using var input = new StreamReader(epwPath);
        var location = input.ReadLine() ?? throw new InvalidDataException("EPW is missing its LOCATION header.");
        var locationFields = Fields(location);
        if (locationFields.Length < 10 || !string.Equals(locationFields[0], "LOCATION", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("EPW LOCATION header is malformed.");
        var place = locationFields[1]; var latitude = Number(locationFields[6], "latitude"); var longitude = Number(locationFields[7], "longitude"); var timeZoneHours = Number(locationFields[8], "time zone"); var elevation = Number(locationFields[9], "site elevation");
        for (var index = 0; index < 7; index++) if (input.ReadLine() is null) throw new InvalidDataException("EPW must contain eight header lines.");
        var records = new List<WeaRecord>(AnnualHours); string? line;
        while ((line = input.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = Fields(line); if (fields.Length < 16) throw new InvalidDataException("EPW weather record has fewer than 16 fields.");
            var month = Integer(fields[1], "month"); var day = Integer(fields[2], "day"); var hour = Integer(fields[3], "hour");
            if (hour is < 1 or > 24) throw new InvalidDataException("EPW hour must be in [1, 24].");
            if (month == 2 && day == 29) throw new InvalidDataException("Leap-day EPW data is unsupported by the 8,760-hour annual workflow.");
            try { _ = new DateTime(2025, month, day); } catch (ArgumentOutOfRangeException) { throw new InvalidDataException("EPW contains an invalid calendar date."); }
            var dni = Number(fields[14], "direct normal irradiance"); var dhi = Number(fields[15], "diffuse horizontal irradiance");
            if (dni < 0 || dhi < 0) throw new InvalidDataException("EPW direct and diffuse irradiance must be nonnegative.");
            records.Add(new(month, day, hour - .5, dni, dhi));
        }
        if (records.Count != AnnualHours) throw new InvalidDataException($"Annual daylight requires exactly {AnnualHours} non-leap-year EPW records; found {records.Count}.");
        using var output = new StreamWriter(weaPath, false, System.Text.Encoding.ASCII);
        output.WriteLine("place " + place); output.WriteLine("latitude " + latitude.ToString("0.######", CultureInfo.InvariantCulture)); output.WriteLine("longitude " + (-longitude).ToString("0.######", CultureInfo.InvariantCulture)); output.WriteLine("time_zone " + (-timeZoneHours * 15).ToString("0.######", CultureInfo.InvariantCulture)); output.WriteLine("site_elevation " + elevation.ToString("0.######", CultureInfo.InvariantCulture)); output.WriteLine("weather_data_file_units 1");
        foreach (var record in records) output.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0} {1} {2:0.0} {3:0.###} {4:0.###}", record.Month, record.Day, record.Hour, record.Dni, record.Dhi));
    }

    private static string[] Fields(string line) => line.Split(',').Select(value => value.Trim()).ToArray();
    private static int Integer(string value, string name) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : throw new InvalidDataException("EPW " + name + " is invalid.");
    private static double Number(string value, string name) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && double.IsFinite(result) ? result : throw new InvalidDataException("EPW " + name + " is invalid.");
    private sealed record WeaRecord(int Month, int Day, double Hour, double Dni, double Dhi);
}
