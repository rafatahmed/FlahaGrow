using System.Globalization;

namespace FlahaGrow.Core.Annual;

/// <summary>
/// Expands a full-output electric illuminance field by one annual dimming schedule.
/// The field is obtained from a real Radiance calculation; this class owns only the
/// deterministic annual schedule expansion and matrix serialization.
/// </summary>
public static class ElectricAnnualMatrix
{
    public const int HoursPerNonLeapYear = 365 * 24;

    public static void WriteRun(string folder, AnnualRunManifest manifest, IReadOnlyList<double> fullOutputLux, IReadOnlyList<double> schedule)
    {
        if (fullOutputLux.Count != manifest.Sensors) throw new InvalidDataException("Electric sensor count does not match the run manifest.");
        ValidateSchedule(schedule, manifest.Hours);
        foreach (var value in fullOutputLux)
            if (!double.IsFinite(value) || value < 0) throw new InvalidDataException("Invalid full-output illuminance.");
        foreach (var part in manifest.Parts)
            Write(Path.Combine(folder, part.ResultFile), fullOutputLux.Skip(part.SensorStart).Take(part.Sensors).ToArray(), schedule, manifest.Hours);
    }

    public static void ValidateSchedule(IReadOnlyList<double> schedule, int hours = HoursPerNonLeapYear)
    {
        if (hours <= 0) throw new ArgumentOutOfRangeException(nameof(hours));
        if (schedule.Count != hours)
            throw new InvalidDataException($"Electric dimming schedule contains {schedule.Count} values; expected {hours}.");
        for (var hour = 0; hour < schedule.Count; hour++)
            if (!double.IsFinite(schedule[hour]) || schedule[hour] < 0 || schedule[hour] > 1)
                throw new InvalidDataException($"Electric dimming schedule value at hour index {hour} must be finite and within [0, 1].");
    }

    public static void Write(string path, IReadOnlyList<double> fullOutputLux, IReadOnlyList<double> schedule, int hours = HoursPerNonLeapYear)
    {
        if (fullOutputLux.Count == 0) throw new InvalidDataException("Electric full-output illuminance field is empty.");
        ValidateSchedule(schedule, hours);
        for (var sensor = 0; sensor < fullOutputLux.Count; sensor++)
            if (!double.IsFinite(fullOutputLux[sensor]) || fullOutputLux[sensor] < 0)
                throw new InvalidDataException($"Electric full-output illuminance at sensor index {sensor} must be finite and nonnegative.");
        using var writer = new StreamWriter(path, false, System.Text.Encoding.ASCII);
        writer.Write("#?RADIANCE\n");
        writer.Write("SOFTWARE= FlahaGrow ElectricAnnualMatrix\n");
        writer.Write($"NROWS={hours}\nNCOLS={fullOutputLux.Count}\nNCOMP=1\nFORMAT=ascii\n\n");
        for (var hour = 0; hour < hours; hour++)
        {
            var dim = schedule[hour];
            for (var sensor = 0; sensor < fullOutputLux.Count; sensor++)
            {
                if (sensor > 0) writer.Write(' ');
                writer.Write((fullOutputLux[sensor] * dim).ToString("G17", CultureInfo.InvariantCulture));
            }
            writer.Write('\n');
        }
    }
}
