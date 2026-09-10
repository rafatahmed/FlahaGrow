using System.Globalization;

namespace FlahaGrow.Core.Annual;

/// <summary>Strict scalar ASCII Radiance matrix reader; physical line wrapping does not change logical rows.</summary>
public static class AnnualMatrix
{
    public static IEnumerable<float[]> Rows(string path, int expectedHours, int expectedSensors)
    {
        if (expectedHours <= 0 || expectedSensors <= 0) throw new ArgumentOutOfRangeException(nameof(expectedHours));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new StreamReader(stream);
        if (reader.ReadLine()?.Trim() != "#?RADIANCE") throw new InvalidDataException("Missing Radiance matrix header: " + path);
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        var headerSize = 0;
        while (true)
        {
            var line = reader.ReadLine() ?? throw new InvalidDataException("Unterminated matrix header.");
            if ((headerSize += line.Length + 1) > 65536) throw new InvalidDataException("Matrix header exceeds 64 KiB.");
            // Some Radiance builds embed blank lines in copied command provenance.
            // The data separator is the blank line AFTER FORMAT, not the first blank.
            if (string.IsNullOrWhiteSpace(line))
            {
                if (fields.ContainsKey("FORMAT")) break;
                continue;
            }
            if (!fields.ContainsKey("FORMAT") && line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .All(token => double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
                throw new InvalidDataException("Numeric matrix data encountered before FORMAT.");
            var equals = line.IndexOf('=');
            if (equals < 0) continue; // Radiance command/provenance lines are legal inside the header.
            var key = line[..equals].Trim();
            if (key is "NROWS" or "NCOLS" or "NCOMP" or "FORMAT")
                if (!fields.TryAdd(key, line[(equals + 1)..].Trim())) throw new InvalidDataException("Duplicate matrix field: " + key);
        }
        int Dimension(string name) => fields.TryGetValue(name, out var text)
            && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value : throw new InvalidDataException("Missing or invalid matrix field: " + name);
        if (Dimension("NROWS") != expectedHours || Dimension("NCOLS") != expectedSensors || Dimension("NCOMP") != 1)
            throw new InvalidDataException("Matrix dimensions/components do not match the run's hours and sensor count.");
        if (!fields.TryGetValue("FORMAT", out var format) || format != "ascii") throw new InvalidDataException("Annual matrix FORMAT must be ascii.");
        var row = new float[expectedSensors]; var column = 0; var rows = 0;
        while (reader.ReadLine() is { } line)
        {
            foreach (var token in line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (rows == expectedHours) throw new InvalidDataException("Matrix has extra data after its declared dimensions.");
                if (!float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !float.IsFinite(value))
                    throw new InvalidDataException($"Invalid/nonfinite matrix value at row {rows + 1}, column {column + 1}: {token}");
                row[column++] = value;
                if (column != expectedSensors) continue;
                yield return row;
                rows++; column = 0; row = new float[expectedSensors];
            }
        }
        if (rows != expectedHours || column != 0) throw new InvalidDataException("Truncated matrix: fewer values than the declared dimensions.");
    }

    public static void Validate(string path, int hours, int sensors)
    {
        foreach (var _ in Rows(path, hours, sensors)) { }
    }

    public static void ValidateIlluminance(string path, int hours, int sensors)
    {
        var hour = 0;
        foreach (var row in Rows(path, hours, sensors))
        {
            for (var sensor = 0; sensor < row.Length; sensor++)
                if (row[sensor] < 0)
                    throw new InvalidDataException($"Negative illuminance {row[sensor].ToString("G9", CultureInfo.InvariantCulture)} lux at hour index {hour}, part-local sensor index {sensor} in {Path.GetFileName(path)}. Investigate total - direct + sun and rerun; values were not clamped.");
            hour++;
        }
    }

    public static int WeatherSteps(string epw)
    {
        using var reader = File.OpenText(epw);
        for (var i = 0; i < 8; i++)
            if (reader.ReadLine() is null) throw new InvalidDataException("EPW must contain eight header lines and weather records.");
        var count = 0;
        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line.Split(',').Length < 35) throw new InvalidDataException("Malformed EPW weather record.");
            count = checked(count + 1);
        }
        return count > 0 ? count : throw new InvalidDataException("EPW contains no weather records.");
    }
}
