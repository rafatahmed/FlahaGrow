using System.Diagnostics;
using FlahaGrow.Core.Annual;
using Xunit;

namespace FlahaGrow.Core.Tests.Annual;

public sealed class AnnualValidationTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "FlahaGrow-validation", Guid.NewGuid().ToString("N"));
    public AnnualValidationTests() => Directory.CreateDirectory(root);
    private string Matrix(string data, string header = "NROWS=2\nNCOLS=2\nNCOMP=1\nFORMAT=ascii")
    {
        var path = Path.Combine(root, "matrix.ill");
        File.WriteAllText(path, "#?RADIANCE\nrmtxop -fa fixture\n" + header + "\n\n" + data);
        return path;
    }

    [Fact]
    public void LogicalRowsAcceptWhitespaceWrappingAndScientificNotation()
    {
        var rows = AnnualMatrix.Rows(Matrix("1\t-2e-1\n\n3\n4.5"), 2, 2).ToArray();
        Assert.Equal(new float[] { 1, -.2f }, rows[0]); Assert.Equal(new float[] { 3, 4.5f }, rows[1]);
    }

    [Theory]
    [InlineData("1 2\nNaN 4")]
    [InlineData("1 2\nInfinity 4")]
    [InlineData("1 2\n1e99 4")]
    [InlineData("1 2\ngarbage 4")]
    [InlineData("1 2\n#ignored 4")]
    [InlineData("1 2\n3")]
    [InlineData("1 2\n3 4 5")]
    [InlineData("1 2\n3,5 4")]
    [InlineData("")]
    public void CorruptOrTruncatedOrExtraDataIsRejected(string data) =>
        Assert.Throws<InvalidDataException>(() => AnnualMatrix.Validate(Matrix(data), 2, 2));

    [Fact]
    public void Radiance54CopiedProvenanceCanContainBlankLinesBeforeDimensions()
    {
        var path = Matrix("1 2\n3 4", "\ndctimestep fixture\n\nCAPDATE= 2026:09:10\n\nApplied 1x3 component transform\n\nNROWS=2\nNCOLS=2\nNCOMP=1\nFORMAT=ascii");
        AnnualMatrix.ValidateIlluminance(path, 2, 2);
    }

    [Fact]
    public void NumericPayloadBeforeFormatCannotBeHiddenAsProvenance()
    {
        var path = Matrix("1 2 3 4", "\n99 88\nNROWS=2\nNCOLS=2\nNCOMP=1\nFORMAT=ascii");
        Assert.Throws<InvalidDataException>(() => AnnualMatrix.Validate(path, 2, 2));
    }

    [Fact]
    public void NegativeFinalIlluminanceIsRejectedWithoutChangingTheFile()
    {
        var path = Matrix("1 2\n-28075.27629 4"); var original = File.ReadAllBytes(path);
        var error = Assert.Throws<InvalidDataException>(() => AnnualMatrix.ValidateIlluminance(path, 2, 2));
        Assert.Contains("hour index 1", error.Message);
        Assert.Contains("sensor index 0", error.Message);
        Assert.Equal(original, File.ReadAllBytes(path));
    }

    [Theory]
    [InlineData("NROWS=8760\nNCOLS=2\nNCOMP=1\nFORMAT=ascii")]
    [InlineData("NROWS=2\nNCOLS=3\nNCOMP=1\nFORMAT=ascii")]
    [InlineData("NROWS=2\nNCOLS=2\nNCOMP=3\nFORMAT=ascii")]
    [InlineData("NROWS=2\nNCOLS=2\nNCOMP=1\nFORMAT=float")]
    [InlineData("NROWS=2\nNCOLS=2\nNCOMP=1")]
    [InlineData("NROWS=2\nNROWS=2\nNCOLS=2\nNCOMP=1\nFORMAT=ascii")]
    [InlineData("NROWS=0\nNCOLS=2\nNCOMP=1\nFORMAT=ascii")]
    public void UnsupportedOrMismatchedHeadersFail(string header) =>
        Assert.Throws<InvalidDataException>(() => AnnualMatrix.Validate(Matrix("1 2 3 4", header), 2, 2));

    [Fact]
    public void HeaderlessResultsAreNotAccepted()
    {
        var path = Matrix("1 2 3 4"); File.WriteAllText(path, "1 2\n3 4");
        Assert.Throws<InvalidDataException>(() => AnnualMatrix.Validate(path, 2, 2));
    }

    [Theory]
    [InlineData(7, 0)]
    [InlineData(0, 8)]
    [InlineData(-1, 0)]
    public async Task EitherPipelineFailureStopsLaterCommands(int producerExit, int consumerExit)
    {
        var cmd = "\"" + Path.Combine(Environment.SystemDirectory, "cmd.exe") + "\" /d /c exit ";
        var id = Guid.NewGuid();
        var commands = new[] { cmd + producerExit + " | " + cmd + consumerExit, "echo should-not-run> later.txt" };
        var code = await RunBatch(AnnualBatch.Build(commands, id, 0));
        Assert.Equal(producerExit != 0 ? producerExit : consumerExit, code);
        Assert.False(File.Exists(Path.Combine(root, "later.txt")));
        Assert.Contains("Failed step=", File.ReadAllText(Path.Combine(root, "annual_state_part0.txt")));
        Assert.DoesNotContain("CommandsSucceeded", File.ReadAllText(Path.Combine(root, "annual_state_part0.txt")));
    }

    [Fact]
    public async Task SuccessfulPipelineRemovesItsTransientFile()
    {
        var id = Guid.NewGuid();
        var lines = AnnualBatch.Build(new[] { "echo 42 | findstr 42 > result.txt" }, id, 0);
        Assert.Equal(0, await RunBatch(lines));
        Assert.Equal("42", File.ReadAllText(Path.Combine(root, "result.txt")).Trim());
        Assert.Empty(Directory.EnumerateFiles(root, "pipeline_part0_step*.tmp"));
    }

    [Fact]
    public void SuccessfulRunDeletesLargeReproducibleIntermediatesUnlessKept()
    {
        var cleaned = AnnualBatch.Build(new[] { "echo done" }, Guid.NewGuid(), 0);
        Assert.Contains(cleaned, line => line.Contains("WeathersunM*_0.smx"));
        Assert.Contains(cleaned, line => line.Contains("cdsDDS_part0.mtx"));
        var retained = AnnualBatch.Build(new[] { "echo done" }, Guid.NewGuid(), 0, keepIntermediates: true);
        Assert.DoesNotContain(retained, line => line.Contains("WeathersunM*_0.smx"));
    }

    [Fact]
    public void DiskSpaceEstimateIsConservativeAndChecked()
    {
        Assert.True(AnnualDiskSpace.RequiredFreeBytes(8760, 980) > 2L << 30);
        Assert.Throws<ArgumentOutOfRangeException>(() => AnnualDiskSpace.RequiredFreeBytes(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => AnnualDiskSpace.RequiredFreeBytes(1, 0));
    }

    [Fact]
    public void ProcessIdentityIsRunBoundAndRejectsForeignOrMalformedRecords()
    {
        var folder = AnnualRun.Create(root, root, 1, new[] { "0 0 0 0 0 1" }, new(), hours: 2);
        var manifest = AnnualRun.Read(folder); var part = manifest.Parts[0];
        AnnualRun.WriteProcessIdentity(folder, manifest, part, new AnnualProcessIdentity(1234, 638930000000000000));
        Assert.Equal(new AnnualProcessIdentity(1234, 638930000000000000), AnnualRun.ReadProcessIdentity(folder, manifest, part));
        File.WriteAllText(Path.Combine(folder, part.ProcessFile), Guid.NewGuid().ToString("N") + " 1234 1");
        Assert.Throws<InvalidDataException>(() => AnnualRun.ReadProcessIdentity(folder, manifest, part));
    }

    [Fact]
    public async Task PersistedProcessIdentityTerminatesOnlyTheMatchingRealProcess()
    {
        using var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/d /c timeout /t 30 /nobreak >nul") { UseShellExecute = false, CreateNoWindow = true })!;
        var identity = new AnnualProcessIdentity(process.Id, process.StartTime.ToUniversalTime().Ticks);
        Assert.False(AnnualProcessControl.TryTerminate(identity with { StartUtcTicks = identity.StartUtcTicks + 1 }));
        Assert.False(process.HasExited);
        Assert.True(AnnualProcessControl.TryTerminate(identity));
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ElectricScheduleExpansionWritesAValidatedAnnualRadianceMatrix()
    {
        var path = Path.Combine(root, "electric.ill");
        var schedule = Enumerable.Repeat(0d, ElectricAnnualMatrix.HoursPerNonLeapYear).ToArray();
        schedule[1] = .5; schedule[^1] = 1;
        ElectricAnnualMatrix.Write(path, new[] { 10d, 20d }, schedule);
        var rows = AnnualMatrix.Rows(path, ElectricAnnualMatrix.HoursPerNonLeapYear, 2).ToArray();
        Assert.Equal(new[] { 0f, 0f }, rows[0]);
        Assert.Equal(new[] { 5f, 10f }, rows[1]);
        Assert.Equal(new[] { 10f, 20f }, rows[^1]);
    }

    [Fact]
    public void AnnualCompositionAddsValidatedDaylightAndElectricMatrices()
    {
        var daylight = Matrix("1 2\n3 4");
        var electric = Path.Combine(root, "electric-source.ill");
        File.WriteAllText(electric, "#?RADIANCE\nNROWS=2\nNCOLS=2\nNCOMP=1\nFORMAT=ascii\n\n10 20\n30 40");
        var combined = Path.Combine(root, "combined.ill");
        AnnualResultComposition.Add(daylight, electric, combined, 2, 2);
        Assert.Equal(new[] { 11f, 22f }, AnnualMatrix.Rows(combined, 2, 2).First());
        Assert.Equal(new[] { 33f, 44f }, AnnualMatrix.Rows(combined, 2, 2).Last());
    }

    [Fact]
    public void RunCompositionRequiresMatchingProvenanceAndProducesCacheableParts()
    {
        var source = Path.Combine(root, "source"); Directory.CreateDirectory(Path.Combine(source, "runs"));
        var points = new[] { "0 0 1 0 0 1" };
        string CreateCompletedRun(float first, float second)
        {
            var folder = AnnualRun.Create(Path.Combine(source, "runs"), source, 1, points, new(), hours: 2);
            var manifest = AnnualRun.Read(folder); File.WriteAllLines(Path.Combine(folder, "0.pts"), points);
            File.WriteAllText(Path.Combine(folder, manifest.Parts[0].ResultFile), $"#?RADIANCE\nNROWS=2\nNCOLS=1\nNCOMP=1\nFORMAT=ascii\n\n{first.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n{second.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            File.WriteAllText(Path.Combine(folder, manifest.Parts[0].StateFile), manifest.RunId.ToString("N") + " CommandsSucceeded");
            return folder;
        }
        var daylight = CreateCompletedRun(1, 2); var electric = CreateCompletedRun(10, 20);
        var combined = AnnualResultComposition.ComposeRuns(daylight, electric);
        var manifest = AnnualRun.Read(combined);
        AnnualPartStatus.RequireComplete(combined, manifest);
        Assert.Equal(new[] { 11f }, AnnualMatrix.Rows(Path.Combine(combined, manifest.Parts[0].ResultFile), 2, 1).First());
        Assert.Equal(new[] { 22f }, AnnualMatrix.Rows(Path.Combine(combined, manifest.Parts[0].ResultFile), 2, 1).Last());
    }

    [Theory]
    [InlineData(-.01)]
    [InlineData(1.01)]
    [InlineData(double.NaN)]
    public void ElectricScheduleRejectsInvalidDimmingValues(double value)
    {
        var schedule = Enumerable.Repeat(0d, ElectricAnnualMatrix.HoursPerNonLeapYear).ToArray(); schedule[12] = value;
        Assert.Throws<InvalidDataException>(() => ElectricAnnualMatrix.ValidateSchedule(schedule));
    }

    [Fact]
    public void LadybugWeaWritesTheAnnualMidpointWeatherContract()
    {
        var epw = Path.Combine(root, "weather.epw"); var wea = Path.Combine(root, "weather.wea");
        var record = string.Join(",", Enumerable.Range(0, 35).Select(index => index switch { 1 => "1", 2 => "1", 3 => "1", 14 => "123", 15 => "45", _ => "0" }));
        File.WriteAllLines(epw, new[] { "LOCATION,Fixture,-,FIX,0,0,25.25,51.57,3,10" }.Concat(Enumerable.Repeat("header", 7)).Concat(Enumerable.Repeat(record, LadybugWea.AnnualHours)));
        LadybugWea.WriteFromEpw(epw, wea);
        var lines = File.ReadLines(wea).Take(7).ToArray();
        Assert.Equal("latitude 25.25", lines[1]); Assert.Equal("longitude -51.57", lines[2]); Assert.Equal("time_zone -45", lines[3]);
        Assert.Equal("1 1 0.5 123 45", lines[6]);
    }

    [Fact]
    public async Task SuccessRequiresValidationAndCannotBeRerunInPlace()
    {
        var folder = AnnualRun.Create(root, root, 1, new[] { "0 0 0 0 0 1" }, new(), hours: 2);
        var manifest = AnnualRun.Read(folder); var part = manifest.Parts[0];
        // Run in the owned folder; output initially has the wrong dimensions despite zero command exit.
        File.Copy(Matrix("1 2 3 4"), Path.Combine(folder, "fixture.ill"));
        var lines = AnnualBatch.Build(new[] { "type fixture.ill > annualRfinal_part0.ill" }, manifest.RunId, 0);
        Assert.Equal(0, await RunBatch(lines, folder));
        Assert.Equal("Invalid", AnnualPartStatus.Read(folder, manifest, part).State);
        Assert.NotEqual(0, await RunBatch(lines, folder));
        File.WriteAllText(Path.Combine(folder, part.ResultFile), "#?RADIANCE\nNROWS=2\nNCOLS=1\nNCOMP=1\nFORMAT=ascii\n\n1 2");
        Assert.True(AnnualPartStatus.Read(folder, manifest, part).Complete);
        File.WriteAllText(Path.Combine(folder, part.StateFile), Guid.NewGuid().ToString("N") + " CommandsSucceeded");
        Assert.False(AnnualPartStatus.Read(folder, manifest, part).Complete);
    }

    [Theory]
    [InlineData("Running")]
    [InlineData("Failed step=2 exit=1")]
    public void ExistingResultDoesNotOverrideCommandState(string state)
    {
        var folder = AnnualRun.Create(root, root, 1, new[] { "0 0 0 0 0 1" }, new(), hours: 2);
        var manifest = AnnualRun.Read(folder); var part = manifest.Parts[0];
        File.WriteAllText(Path.Combine(folder, part.ResultFile), "#?RADIANCE\nNROWS=2\nNCOLS=1\nNCOMP=1\nFORMAT=ascii\n\n1 2");
        File.WriteAllText(Path.Combine(folder, part.StateFile), manifest.RunId.ToString("N") + " " + state);
        Assert.False(AnnualPartStatus.Read(folder, manifest, part).Complete);
        Assert.Throws<InvalidDataException>(() => AnnualPartStatus.RequireComplete(folder, manifest));
    }

    private async Task<int> RunBatch(IReadOnlyList<string> lines, string? folder = null)
    {
        folder ??= root;
        File.WriteAllLines(Path.Combine(folder, "run.bat"), lines);
        using var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/d /c run.bat")
            { WorkingDirectory = folder, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try { await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15)); }
        catch { process.Kill(true); throw; }
        await Task.WhenAll(stdout, stderr);
        return process.ExitCode;
    }

    public void Dispose()
    {
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FlahaGrow-validation")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(root).StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new Exception("Invalid cleanup path.");
        Directory.Delete(root, true);
    }
}
