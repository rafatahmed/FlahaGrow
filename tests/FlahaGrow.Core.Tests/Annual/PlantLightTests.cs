using System.Globalization;
using System.Text.Json;
using FlahaGrow.Core.Annual;
using FlahaGrow.Core.PlantLight;
using Xunit;

namespace FlahaGrow.Core.Tests.Annual;

public sealed class PlantLightTests : IDisposable
{
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
        var first = new PlantLightContext(AnnualIlluminanceResult.Open(CreateCache(2)), new("Daylight", .0185), "nonleap Jan1; UTC+03 standard; hourly midpoint");
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

    private string CreateCache(int sensors, bool combined = false)
    {
        var inputs = combined ? new Dictionary<string, string> { ["daylightManifest"] = "source" } : new();
        var folder = AnnualRun.Create(root, root, 1, Enumerable.Range(0, sensors).Select(i => $"{i} 0 0 0 0 1").ToArray(), inputs);
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
