using System.Globalization;
using System.Text.Json;
using FlahaGrow.Core.Annual;
using FlahaGrow.Core.PlantLight;
using Xunit;

namespace FlahaGrow.Core.Tests.Annual;

public sealed class PlantLightTests : IDisposable
{
    [Fact]
    public void CacheReuseAndCancellationPreserveFiles()
    {
        var path = CreateCache(2); var stamp = File.GetLastWriteTimeUtc(path);
        var result = AnnualCacheLoader.Load(Path.GetDirectoryName(path)!, true, CancellationToken.None);
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(path)); Assert.Null(result.Weather);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Assert.Throws<OperationCanceledException>(() => AnnualCacheLoader.Load(Path.GetDirectoryName(path)!, true, cancel.Token));
        Assert.Equal(stamp, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public void WeatherIsInheritedOnlyFromAuthenticatedSnapshot()
    {
        var path = CreateCache(2, withWeather: true);
        var loaded = LoadedAnnualResult.From(AnnualIlluminanceResult.Open(path));
        Assert.NotNull(loaded.Weather);
        Assert.Equal(330, loaded.Weather!.UtcMinutes);
        Assert.Equal("annual-365;UTC+05:30;local-standard;hourly", loaded.Weather.Alignment);
        var snapshot = Path.Combine(Path.GetDirectoryName(path)!, "weather.epw");
        File.AppendAllText(snapshot, "tampered");
        Assert.Throws<InvalidDataException>(() => LoadedAnnualResult.From(AnnualIlluminanceResult.Open(path)));
    }

    [Fact]
    public void PlotMetadataBindsValuesAndComputesAutomaticRanges()
    {
        var values = Enumerable.Range(0, 365).Select(i => (double)i).ToArray();
        var plot = PlotAttributes.Create(values, "DLI", "mol/m²/day", "annual-daily", "sensor index 5", "test run");
        plot.RequireMatching(values);
        Assert.Equal(new[] { 0d, 91, 182, 273 }, plot.AutomaticRanges);
        Assert.Contains("mol/m²/day", plot.Title);
        Assert.Throws<ArgumentException>(() => plot.RequireMatching(values.Reverse().ToArray()));
        Assert.Throws<ArgumentException>(() => PlotAttributes.Create(values, "PPFD", "units", "annual-hourly", "s", "p"));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnnualPlotData.Validate(Enumerable.Repeat(double.NaN, 365).ToArray(), plot.AutomaticRanges));
        Assert.Equal(0, AnnualPlotData.Classify(0, plot.AutomaticRanges));
        Assert.Equal("x ≤ 0", AnnualPlotData.IntervalLabel(0, plot.AutomaticRanges));
        var flat = PlotAttributes.Create(Enumerable.Repeat(10d, 365).ToArray(), "DLI", "mol/m²/day", "annual-daily", "s", "p");
        Assert.All(flat.AutomaticRanges, v => Assert.Equal(10, v));
    }
    [Fact]
    public void AnnualTimingUsesIntervalStartsAndStrictAlignment()
    {
        Assert.Equal(0, AnnualTime.HourIndex(1, 1, 0));
        Assert.Equal(8759, AnnualTime.HourIndex(12, 31, 23));
        Assert.Equal(59 * 24, AnnualTime.HourIndex(3, 1, 0));
        Assert.Equal(6109, AnnualTime.HourIndex(9, 12, 13));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnnualTime.HourIndex(2, 29, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnnualTime.HourIndex(1, 1, 24));
        Assert.Contains("00:00–01:00", AnnualTime.Label(0));
        Assert.Equal("annual-365;UTC+03:00;local-standard;hourly", AnnualTime.Alignment(180));
        Assert.Equal(AnnualTime.Alignment(-210), AnnualTime.ValidateAxis(AnnualTime.Alignment(-210)));
        Assert.Equal("", AnnualTime.ValidateAxis(""));
        Assert.Throws<ArgumentException>(() => AnnualTime.ValidateAxis("September 12, 1 PM"));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnnualTime.Alignment(841));
    }
    [Fact]
    public void BundledReferencesHaveTraceableFactorsAndExplicitLimitations()
    {
        Assert.Equal(18, SpectralProfileLibrary.Profiles.Count);
        Assert.Equal(6, SpectralProfileLibrary.Profiles.Count(p => p.Category == "Horticultural research"));
        foreach (var item in SpectralProfileLibrary.Profiles)
        {
            Assert.True(double.IsFinite(item.Profile.Factor) && item.Profile.Factor > 0);
            Assert.Contains("https://doi.org/", item.Profile.Provenance);
            Assert.Contains("SHA256", item.Profile.Provenance);
            Assert.Contains("not a matched commercial fixture", item.Profile.Warning);
            Assert.Same(item, SpectralProfileLibrary.Get(item.Id));
        }
        Assert.Equal(.01801871704609, SpectralProfileLibrary.Get("CIE_std_illum_D65:1").Profile.Factor, 12);
        Assert.Throws<ArgumentException>(() => SpectralProfileLibrary.Get("unknown"));
    }
    private readonly string root = Path.Combine(Path.GetTempPath(), "FlahaGrow-plant-light-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void KnownValuesAndCompleteYear()
    {
        Assert.Equal(18.5, PlantLightMath.Ppfd(1000, .0185));
        Assert.Equal(1.5984, PlantLightMath.PhotonIntegral(18.5, 86400), 10);
        Assert.All(PlantLightMath.AnnualDli(Enumerable.Repeat(100d, 8760).ToArray()), v => Assert.Equal(8.64, v, 10));
        Assert.All(PlantLightMath.AnnualDli(Enumerable.Repeat(100d, 17520).ToArray(), 1800), v => Assert.Equal(8.64, v, 10));
        Assert.Throws<ArgumentException>(() => PlantLightMath.AnnualDli(new double[365]));
        Assert.Throws<ArgumentException>(() => PlantLightMath.AnnualDli(new double[8760], 3500));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(-1)]
    public void RejectsInvalidNumbers(double value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PlantLightMath.Ppfd(value, .0185));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlantLightMath.Ppfd(1000, value));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlantLightMath.AnnualDli(new double[8760], value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SpectralProfile("Custom", value));
    }

    [Fact]
    public void RejectsOverflowAndEmptyLabel()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PlantLightMath.Ppfd(double.MaxValue, 2));
        Assert.Throws<ArgumentException>(() => new SpectralProfile(" ", .0185));
        Assert.Throws<ArgumentOutOfRangeException>(() => PlantLightMath.PhotonIntegral(10, 0));
    }

    [Fact]
    public void SpectralBasisScalingAndCoverage()
    {
        var energy = Enumerable.Range(360, 471).Select(n => new SpectralSample(n, 1)).ToArray();
        var photons = energy.Select(s => new SpectralSample(s.Wavelength, s.Value * SpectralCalculator.PhotonMultiplier * s.Wavelength)).ToArray();
        var result = SpectralCalculator.Calculate(energy, SpectralBasis.Energy);
        Assert.Equal(result.Factor, SpectralCalculator.Calculate(photons, SpectralBasis.Photon).Factor, 12);
        Assert.Equal(result.Factor, SpectralCalculator.Calculate(energy.Select(s => s with { Value = 7 }).ToArray(), SpectralBasis.Energy).Factor, 12);
        Assert.Equal(683, SpectralCalculator.Photopic(555) * 683, 10);
        var narrow = new[] { new SpectralSample(400, 1), new SpectralSample(700, 1) };
        Assert.Throws<InvalidDataException>(() => SpectralCalculator.Calculate(narrow, SpectralBasis.Energy));
        Assert.Contains("tails", SpectralCalculator.Calculate(narrow, SpectralBasis.Energy, true).Warning);
        Assert.Throws<InvalidDataException>(() => SpectralCalculator.Calculate(new[] { new SpectralSample(401, 1), new SpectralSample(700, 1) }, SpectralBasis.Energy, true));
        Assert.Throws<InvalidDataException>(() => SpectralCalculator.Calculate(new[] { new SpectralSample(360, 0), new SpectralSample(830, 0) }, SpectralBasis.Energy));
        Assert.Throws<InvalidDataException>(() => SpectralCalculator.Calculate(new[] { new SpectralSample(400, 1), new SpectralSample(400, 2) }, SpectralBasis.Energy, true));
        Assert.Equal(4, SpectralCalculator.Interpolate(new[] { new SpectralSample(400.5, 0), new SpectralSample(405.5, 10) }, 402.5));
    }

    [Fact]
    public void OfficialD65MatchesIndependentResearchCalculation()
    {
        var samples = File.ReadLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CIE_std_illum_D65.csv"))
            .Select(line => line.Split(',')).Select(c => new SpectralSample(double.Parse(c[0], CultureInfo.InvariantCulture), double.Parse(c[1], CultureInfo.InvariantCulture))).ToArray();
        Assert.Equal(.018018717046093483, SpectralCalculator.Calculate(samples, SpectralBasis.Energy).Factor, 12);
    }

    [Theory]
    [InlineData("wavelength_nm,value\n400,1\n700,2\n")]
    [InlineData("Wavelength (nm),Spectral Power (W/m2/nm1)\n400.5,1\n700,2\n", false)]
    public void CsvIsStrict(string text, bool valid = true)
    {
        Directory.CreateDirectory(root); var path = Path.Combine(root, "source.csv"); File.WriteAllText(path, text);
        if (valid) Assert.Equal(2, SpectralCalculator.ReadCsv(path).Count);
        else Assert.Throws<InvalidDataException>(() => SpectralCalculator.ReadCsv(path));
        File.WriteAllText(path, "wavelength_nm,value\n400,1\n500,bad\n700,2\n");
        Assert.Throws<InvalidDataException>(() => SpectralCalculator.ReadCsv(path));
        File.WriteAllText(path, "400,1\n700,2\n");
        Assert.Throws<InvalidDataException>(() => SpectralCalculator.ReadCsv(path));
    }

    [Fact]
    public void IndependentReadersAndMixedFactorsAgree()
    {
        var first = new PlantLightContext(AnnualIlluminanceResult.Open(CreateCache(2)), new("Daylight", .0185), AnnualTime.Alignment(180));
        var second = new PlantLightContext(AnnualIlluminanceResult.Open(CreateCache(2)), new("LED", .03), first.TimeAxis);
        var mixed = PlantLightContext.Combine(new[] { first, second });
        Assert.Equal(new[] { 48.5, 97d }, mixed.PpfdAtHour(0));
        var day = mixed.DliForDay(0);
        Assert.Equal(48, day.HourlyIntegrals.Length);
        Assert.Equal(4.1904, day.Daily[0], 10);
        Assert.Equal(8.3808, day.Daily[1], 10);
        Assert.All(mixed.DliAtSensor(1), v => Assert.Equal(day.Daily[1], v, 10));
        Assert.Equal(day.Daily[1], PlantLightMath.AnnualDli(mixed.PpfdAtSensor(1))[0], 10);
        Assert.Equal(1000, first.Sources[0].Result.ReadHour(0)[0]); // lux is unchanged
        Assert.Throws<ArgumentOutOfRangeException>(() => first.PpfdAtHour(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.PpfdAtHour(8760));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.DliForDay(365));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.DliForDay(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => first.PpfdAtSensor(2));
        Assert.Throws<ArgumentException>(() => PlantLightContext.Combine(new[] { first, first }));
        var unknown = new PlantLightContext(second.Sources[0].Result, new("LED", .03));
        Assert.Throws<ArgumentException>(() => PlantLightContext.Combine(new[] { first, unknown }));
        var mismatch = new PlantLightContext(AnnualIlluminanceResult.Open(CreateCache(3)), new("LED", .03), first.TimeAxis);
        Assert.Throws<ArgumentException>(() => PlantLightContext.Combine(new[] { first, mismatch }));
    }

    [Fact]
    public void CacheTamperingWithUnchangedSizeAndTimestampIsRejected()
    {
        var path = CreateCache(1); var result = AnnualIlluminanceResult.Open(path);
        var stamp = File.GetLastWriteTimeUtc(path);
        using (var writer = new BinaryWriter(File.OpenWrite(path))) writer.Write(2000f);
        File.SetLastWriteTimeUtc(path, stamp);
        Assert.Throws<InvalidDataException>(() => result.ReadHour(0));
    }

    [Fact]
    public void ChangedSourceAndMetadataAreRejected()
    {
        var path = CreateCache(1); var result = AnnualIlluminanceResult.Open(path);
        var matrix = Path.Combine(Path.GetDirectoryName(path)!, "annualRfinal_part0.ill");
        File.AppendAllText(matrix, "\n1\n");
        Assert.Throws<InvalidDataException>(() => result.ReadSensor(0));
        path = CreateCache(1); var meta = Path.ChangeExtension(path, ".meta.json");
        File.WriteAllText(meta, File.ReadAllText(meta).Replace("row-major hours x sensors", "column-major"));
        Assert.Throws<InvalidDataException>(() => AnnualIlluminanceResult.Open(path));
    }

    [Fact]
    public void FourPartsKeepGlobalSensorOrderingAndRejectCombinedLux()
    {
        var result = AnnualIlluminanceResult.Open(CreateCache(13));
        Assert.Equal(Enumerable.Range(1, 13).Select(i => 1000d * i), result.ReadHour(0));
        Assert.All(result.ReadSensor(12), v => Assert.Equal(13000, v));
        var path = CreateCache(1, true);
        Assert.Throws<ArgumentException>(() => new PlantLightContext(AnnualIlluminanceResult.Open(path), new("Mixed", .0185)));
    }

    private string CreateCache(int sensors, bool combined = false, bool withWeather = false)
    {
        var inputs = combined ? new Dictionary<string, string> { ["daylightManifest"] = "source" } : new();
        var weatherPath = Path.Combine(root, "source.epw");
        if (withWeather)
        {
            Directory.CreateDirectory(root);
            using (var writer = new StreamWriter(weatherPath))
            {
                writer.WriteLine("LOCATION,Test,-,-,-,-,25,51,5.5,10");
                for (var i = 0; i < 7; i++) writer.WriteLine("header");
                for (var i = 0; i < 8760; i++)
                {
                    var date = new DateTime(2001, 1, 1).AddHours(i);
                    var fields = Enumerable.Repeat("0", 35).ToArray(); fields[0] = "2001"; fields[1] = date.Month.ToString(); fields[2] = date.Day.ToString(); fields[3] = (date.Hour + 1).ToString();
                    writer.WriteLine(string.Join(",", fields));
                }
            }
            inputs[weatherPath] = AnnualRun.HashFile(weatherPath);
        }
        var folder = AnnualRun.Create(root, root, 1, Enumerable.Range(0, sensors).Select(i => $"{i} 0 0 0 0 1").ToArray(), inputs);
        if (withWeather) File.Copy(weatherPath, Path.Combine(folder, "weather.epw"));
        var run = AnnualRun.Read(folder);
        var schedule = Enumerable.Repeat(1d, 8760).ToArray();
        File.WriteAllLines(Path.Combine(folder, "0.pts"), Enumerable.Range(0, sensors).Select(i => $"{i} 0 0 0 0 1"));
        ElectricAnnualMatrix.WriteRun(folder, run, Enumerable.Range(1, sensors).Select(i => 1000d * i).ToArray(), schedule);
        foreach (var part in run.Parts)
        {
            File.WriteAllText(Path.Combine(folder, part.StateFile), run.RunId.ToString("N") + " CommandsSucceeded");
        }
        var path = Path.Combine(folder, "annualRfinal.f32");
        using (var writer = new BinaryWriter(File.Create(path)))
            for (var hour = 0; hour < 8760; hour++)
                for (var sensor = 0; sensor < sensors; sensor++) writer.Write(1000f * (sensor + 1));
        var signature = AnnualRun.HashFile(Path.Combine(folder, AnnualRun.ManifestName)) + ":" + string.Join(":", AnnualRun.RequireResults(folder, run).Select(AnnualRun.HashFile));
        File.WriteAllText(Path.ChangeExtension(path, ".meta.json"), JsonSerializer.Serialize(new
        { sensors, hours = 8760, ncomp = 1, order = "row-major hours x sensors", runId = run.RunId, sourceSignature = signature, validationVersion = 1, cacheHash = AnnualRun.HashFile(path) }));
        return path;
    }

    [Fact]
    public void ElectricPartitionedRunsAlsoComposeLuxWithoutDroppingSensors()
    {
        var a = Path.GetDirectoryName(CreateCache(13))!;
        var b = Path.GetDirectoryName(CreateCache(13))!;
        var combined = AnnualResultComposition.ComposeRuns(a, b);
        var manifest = AnnualRun.Read(combined);
        AnnualPartStatus.RequireComplete(combined, manifest);
        foreach (var part in manifest.Parts)
        {
            var row = AnnualMatrix.Rows(Path.Combine(combined, part.ResultFile), 8760, part.Sensors).First();
            Assert.Equal(Enumerable.Range(part.SensorStart + 1, part.Sensors).Select(i => 2000f * i), row);
        }
    }

    public void Dispose()
    {
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FlahaGrow-plant-light-tests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(root).StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid cleanup path.");
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
