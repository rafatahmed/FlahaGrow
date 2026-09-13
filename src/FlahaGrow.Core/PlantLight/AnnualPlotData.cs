using System.Globalization;

namespace FlahaGrow.Core.PlantLight;

/// <summary>Shared contracts for annual nonnegative light-series visualization.</summary>
public static class AnnualPlotData
{
    public static void Validate(IReadOnlyList<double> values, IReadOnlyList<double> ranges)
    {
        if (values.Count is not (8760 or 365 or 12)) throw new ArgumentException("Supply 8,760 hourly, 365 daily, or 12 monthly values. Length does not identify quantity or units.");
        foreach (var value in values) PlantLightMath.NonNegative(value, "Plot value");
        if (ranges.Count != 4 || ranges.Any(v => !double.IsFinite(v)) || ranges.Zip(ranges.Skip(1), (a, b) => a <= b).Any(ok => !ok))
            throw new ArgumentException("Four finite ascending thresholds are required (R1 ≤ R2 ≤ R3 ≤ R4).");
    }
    public static int Classify(double value, IReadOnlyList<double> ranges) =>
        value <= ranges[0] ? 0 : value <= ranges[1] ? 1 : value <= ranges[2] ? 2 : value <= ranges[3] ? 3 : 4;
    public static string IntervalLabel(int bin, IReadOnlyList<double> ranges)
    {
        string N(double v) => v.ToString("G6", CultureInfo.InvariantCulture);
        return bin switch
        {
            0 => $"x ≤ {N(ranges[0])}",
            4 => $"x > {N(ranges[3])}",
            >= 1 and <= 3 => $"{N(ranges[bin - 1])} < x ≤ {N(ranges[bin])}",
            _ => throw new ArgumentOutOfRangeException(nameof(bin))
        };
    }
}
