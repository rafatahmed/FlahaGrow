namespace FlahaGrow.Core.PlantLight;

/// <summary>Incident PAR photons (400–700 nm), never crop-weighted or absorbed light.</summary>
public static class PlantLightMath
{
    public static double NonNegative(double value, string name)
    {
        if (!double.IsFinite(value) || value < 0)
            throw new ArgumentOutOfRangeException(name, "Value must be finite and non-negative.");
        return value;
    }

    public static double Ppfd(double lux, double factor) =>
        NonNegative(NonNegative(lux, nameof(lux)) * NonNegative(factor, nameof(factor)), "PPFD");

    public static double[] ConvertLux(IEnumerable<double> lux, double factor)
    {
        NonNegative(factor, nameof(factor));
        return lux.Select(value => Ppfd(value, factor)).ToArray();
    }

    public static double PhotonIntegral(double ppfd, double seconds)
    {
        if (!double.IsFinite(seconds) || seconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Duration must be finite and positive.");
        return NonNegative(NonNegative(ppfd, nameof(ppfd)) * (seconds / 1e6), "Photon integral");
    }

    public static double[] AnnualDli(IReadOnlyList<double> ppfd, double seconds = 3600)
    {
        PhotonIntegral(0, seconds);
        var count = 86400 / seconds;
        if (count < 1 || count > int.MaxValue / 365 || Math.Abs(count - Math.Round(count)) > 1e-9)
            throw new ArgumentException("Timestep must divide a day exactly and fit a 365-day series.", nameof(seconds));
        var samples = (int)Math.Round(count);
        if (ppfd.Count != checked(365 * samples))
            throw new ArgumentException($"Expected {365 * samples} temporal PPFD samples, received {ppfd.Count}.", nameof(ppfd));
        var result = new double[365];
        for (var day = 0; day < result.Length; day++)
            for (var t = 0; t < samples; t++)
                result[day] = NonNegative(result[day] + PhotonIntegral(ppfd[day * samples + t], seconds), "DLI");
        return result;
    }
}

/// <summary>Immutable explicit assumption. A label is not a certification of its spectrum.</summary>
public sealed class SpectralProfile
{
    public string Label { get; }
    public double Factor { get; }
    public string Method { get; }
    public string Provenance { get; }
    public string Warning { get; }

    public SpectralProfile(string label, double factor, string method = "explicit-factor-v1",
        string provenance = "User-supplied assumption", string warning = "Not project-validated; spectrum must represent light at the sensor.")
    {
        if (string.IsNullOrWhiteSpace(label) || string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(provenance))
            throw new ArgumentException("Profile label, method and provenance are required.");
        Label = label.Trim(); Factor = PlantLightMath.NonNegative(factor, nameof(factor));
        Method = method; Provenance = provenance; Warning = warning;
    }
}
