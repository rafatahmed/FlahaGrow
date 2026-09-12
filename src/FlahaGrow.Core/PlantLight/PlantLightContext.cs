namespace FlahaGrow.Core.PlantLight;

public sealed record PlantLightSource(AnnualIlluminanceResult Result, SpectralProfile Profile);

/// <summary>Independent readers share source/profile bindings, never a derived annual matrix.</summary>
public sealed class PlantLightContext
{
    private readonly PlantLightSource[] sources;
    public IReadOnlyList<PlantLightSource> Sources => Array.AsReadOnly(sources);
    public int Sensors => sources[0].Result.Sensors;
    public string TimeAxis { get; }
    public string Description => $"Lux-derived estimated PPFD/DLI; PAR 400–700 nm; 8760 × 3600 s; axis {TimeAxis}; "
        + string.Join("; ", sources.Select(s => $"run {s.Result.RunId}, {s.Profile.Label}, factor {s.Profile.Factor:G9}, {s.Profile.Method}, {s.Profile.Provenance}"));

    public PlantLightContext(AnnualIlluminanceResult result, SpectralProfile profile, string timeAxis = "")
        : this(new[] { new PlantLightSource(result, profile) }, timeAxis) { }

    private PlantLightContext(PlantLightSource[] items, string timeAxis)
    {
        if (items.Length == 0) throw new ArgumentException("At least one source required.");
        sources = items.ToArray(); TimeAxis = timeAxis.Trim();
        foreach (var s in sources)
        {
            if (s.Result.IsCombinedLux)
                throw new ArgumentException("Combined lux cannot use one source profile. Load the original source runs and use Combine Plant Light.");
            if (s.Result.Hours != 8760 || s.Result.Sensors != Sensors || s.Result.SensorHash != sources[0].Result.SensorHash)
                throw new ArgumentException("Sources must have identical sensor order/normals and 8,760 hourly intervals.");
        }
        if (sources.Select(s => s.Result.RunId).Distinct().Count() != sources.Length)
            throw new ArgumentException("Duplicate source run would double-count light.");
    }

    public static PlantLightContext Combine(IReadOnlyList<PlantLightContext> contexts)
    {
        if (contexts.Count < 2) throw new ArgumentException("Connect at least two source contexts.");
        var axis = contexts[0].TimeAxis;
        if (string.IsNullOrWhiteSpace(axis) || contexts.Any(c => c.TimeAxis != axis))
            throw new ArgumentException("Mixed sources require the same explicit time-axis declaration (calendar, local standard time, timezone and interval convention). Counts alone are insufficient.");
        return new(contexts.SelectMany(c => c.sources).ToArray(), axis);
    }

    private double[] Sum(Func<AnnualIlluminanceResult, double[]> read)
    {
        double[]? sum = null;
        foreach (var s in sources)
        {
            var values = PlantLightMath.ConvertLux(read(s.Result), s.Profile.Factor);
            if (sum is null) sum = values;
            else
                for (var i = 0; i < sum.Length; i++) sum[i] = PlantLightMath.NonNegative(sum[i] + values[i], "Combined PPFD");
        }
        return sum!;
    }
    public double[] PpfdAtHour(int hour) => Sum(r => r.ReadHour(hour));
    public double[] PpfdAtSensor(int sensor) => Sum(r => r.ReadSensor(sensor));
    public double[] DliAtSensor(int sensor) => PlantLightMath.AnnualDli(PpfdAtSensor(sensor));
    public (double[] Daily, double[] HourlyIntegrals) DliForDay(int day)
    {
        var ppfd = Sum(r => r.ReadDay(day));
        var hourly = ppfd.Select(v => PlantLightMath.PhotonIntegral(v, 3600)).ToArray();
        var daily = new double[Sensors];
        for (var h = 0; h < 24; h++)
            for (var s = 0; s < Sensors; s++) daily[s] = PlantLightMath.NonNegative(daily[s] + hourly[h * Sensors + s], "DLI");
        return (daily, hourly);
    }
}
