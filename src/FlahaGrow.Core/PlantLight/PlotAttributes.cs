using System.Security.Cryptography;

namespace FlahaGrow.Core.PlantLight;

/// <summary>Plot identity bound to exact values; never guesses quantity from list length.</summary>
public sealed record PlotAttributes(string Quantity, string Units, string Shape, string Selection,
    string Provenance, int Count, string DataHash, double Minimum, double Maximum)
{
    public string Title => $"Estimated {Quantity} — {Selection} — {Units}";
    public double[] AutomaticRanges => Enumerable.Range(0, 4).Select(i => Minimum * (1 - i / 4d) + Maximum * (i / 4d)).ToArray();
    public static PlotAttributes Create(IReadOnlyList<double> values, string quantity, string units, string shape, string selection, string provenance)
    {
        if (values.Count == 0) throw new ArgumentException("Empty plot data.");
        if ((shape == "annual-hourly" && values.Count != 8760) || (shape == "annual-daily" && values.Count != 365) || (shape == "annual-monthly" && values.Count != 12))
            throw new ArgumentException("Temporal shape and value count disagree.");
        foreach (var value in values) PlantLightMath.NonNegative(value, "Plot value");
        return new(quantity, units, shape, selection, provenance, values.Count, Hash(values), values.Min(), values.Max());
    }
    public void RequireMatching(IReadOnlyList<double> values)
    {
        if (values.Count != Count || Hash(values) != DataHash) throw new ArgumentException("Data and Plot Attributes do not match. Connect both outputs from the same reader; do not reorder or filter one independently.");
    }
    private static string Hash(IReadOnlyList<double> values)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[8];
        foreach (var value in values) { System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(buffer, BitConverter.DoubleToInt64Bits(value)); hash.AppendData(buffer); }
        return Convert.ToHexString(hash.GetHashAndReset());
    }
}
