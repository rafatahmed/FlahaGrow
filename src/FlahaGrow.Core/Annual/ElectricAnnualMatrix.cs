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
