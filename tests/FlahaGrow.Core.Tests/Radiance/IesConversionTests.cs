using FlahaGrow.Core.Radiance;
using Xunit;

namespace FlahaGrow.Core.Tests.Radiance;

public sealed class IesConversionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "FlahaGrow-IesTests", Guid.NewGuid().ToString("N"));
    private const string Scene = "# retain comments\nvoid brightdata dist\n1 \"fixture.dat\"\n0\n3 9 8 7\ndist light lamp\n0\n0\n3 1 1 1\nlamp sphere bulb\n0\n0\n4 1 2 3 .1\n";
    public IesConversionTests() { Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "fixture.ies"), "fixture"); }
    private IesConversionRequest Request => new(Path.Combine(root, "fixture.ies"), Path.Combine(root, "output"), "fixture",
        Path.Combine(root, "ies2rad.exe"), new Dictionary<string, string>(), 1, 1, 1, null, null);

    [Fact]
    public void RewriterPreservesGeometryAndNonEmitterTriples()
    {
        var text = LuminaireOutput.Rewrite(Scene, .25, .5, .25, Path.Combine(root, "data $1.dat"));
        Assert.Contains("3 9 8 7", text);
        Assert.Contains("4 1 2 3 .1", text);
        Assert.Contains("3 0.25 0.5 0.25", text);
        Assert.Contains("data $1.dat\"", text);
        Assert.Contains("# retain comments", text);
    }

    [Fact]
    public async Task MissingFreshOutputDoesNotAdoptOrOverwriteExistingResult()
    {
        Directory.CreateDirectory(Request.OutputFolder);
        var old = Path.Combine(Request.OutputFolder, "fixture.rad"); File.WriteAllText(old, "old result");
        await Assert.ThrowsAsync<FileNotFoundException>(() => new IesConversionService(new Runner()).ConvertAsync(Request));
        Assert.Equal("old result", File.ReadAllText(old));
        Assert.Empty(Directory.GetDirectories(Request.OutputFolder));
    }

    [Fact]
    public async Task ConversionPublishesFreshFilesAndCleansStaging()
    {
        var runner = new Runner(command =>
        {
            Assert.Equal(Path.GetFullPath(Request.IesPath), command.Arguments.Last());
            File.WriteAllText(Path.Combine(command.WorkingDirectory, "fixture.rad"), Scene);
            File.WriteAllText(Path.Combine(command.WorkingDirectory, "fixture.dat"), "fresh");
        });
        var result = await new IesConversionService(runner).ConvertAsync(Request);
        Assert.Equal("fresh", File.ReadAllText(Assert.Single(result.DataFiles)));
        Assert.Contains(Request.OutputFolder.Replace('\\', '/') + "/fixture.dat", File.ReadAllText(result.RadianceFile));
        Assert.Empty(Directory.GetDirectories(Request.OutputFolder));
    }

    [Fact]
    public async Task InvalidInputsAndFailedProcessDoNotPublishResults()
    {
        var runner = new Runner(); var service = new IesConversionService(runner);
        await Assert.ThrowsAsync<ArgumentException>(() => service.ConvertAsync(Request with { R = double.NaN }));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ConvertAsync(Request with { Multiplier = 0 }));
        await Assert.ThrowsAsync<FileNotFoundException>(() => service.ConvertAsync(Request with { DatOverride = Path.Combine(root, "missing.dat") }));
        Assert.Equal(0, runner.Calls);
        Assert.False(Directory.Exists(Request.OutputFolder));
        runner.Report = new(ProcessState.TimedOut, null, "", "timeout", false);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConvertAsync(Request));
        Assert.Empty(Directory.GetFileSystemEntries(Request.OutputFolder));
    }

    [Fact]
    public void NormalizationRejectsInvalidChannelsAndAvoidsOverflow()
    {
        Assert.Throws<ArgumentException>(() => LuminaireOutput.Normalize(0, 0, 0));
        Assert.Throws<ArgumentException>(() => LuminaireOutput.Normalize(-1, 1, 1));
        var rgb = LuminaireOutput.Normalize(double.MaxValue, double.MaxValue, double.MaxValue);
        Assert.Equal(1, rgb.R + rgb.G + rgb.B, 12);
        Assert.Throws<InvalidDataException>(() => LuminaireOutput.Rewrite("void light bad\n0\n0\n3 1", 1, 1, 1));
    }

    [Fact]
    public async Task MissingDatIsRejectedAndExplicitOverrideIsPreserved()
    {
        var runner = new Runner(command => File.WriteAllText(Path.Combine(command.WorkingDirectory, "fixture.rad"), Scene));
        await Assert.ThrowsAsync<FileNotFoundException>(() => new IesConversionService(runner).ConvertAsync(Request));
        var dat = Path.Combine(Request.OutputFolder, "fixture.dat"); File.WriteAllText(dat, "user-supplied");
        var result = await new IesConversionService(runner).ConvertAsync(Request with { DatOverride = dat });
        Assert.Equal("user-supplied", File.ReadAllText(dat));
        Assert.Empty(result.DataFiles);
        Assert.Contains(dat.Replace('\\', '/'), File.ReadAllText(result.RadianceFile));
    }

    private sealed class Runner : IRadianceProcessRunner
    {
        private readonly Action<ProcessCommand>? write;
        public int Calls;
        public ProcessReport Report = new(ProcessState.Exited, 0, "", "", false);
        public Runner(Action<ProcessCommand>? write = null) => this.write = write;
        public Task<ProcessReport> RunAsync(ProcessCommand command, CancellationToken cancellationToken = default)
        { Calls++; write?.Invoke(command); return Task.FromResult(Report); }
    }
    public void Dispose()
    {
        var parent = Path.Combine(Path.GetTempPath(), "FlahaGrow-IesTests") + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(root).StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid fixture cleanup path.");
        Directory.Delete(root, recursive: true);
    }
}
