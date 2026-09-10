using System.Text.Json;
using FlahaGrow.Core.Annual;
using Xunit;

namespace FlahaGrow.Core.Tests.Annual;

public sealed class AnnualRunTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "FlahaGrow-run-tests", Guid.NewGuid().ToString("N"));

    private string Create(int sensors, Guid? project = null, Guid? analysis = null) => AnnualRun.Create(root, root, 4,
        Enumerable.Range(0, sensors).Select(i => $"{i} 0 0 0 0 1").ToArray(), new() { ["weather.epw"] = "hash" }, project, analysis);

    [Fact]
    public void RerunsHaveSeparateIdentityAndContiguousSensorPartitions()
    {
        var project = Guid.NewGuid(); var analysis = Guid.NewGuid();
        var first = Create(13, project, analysis); var second = Create(1, project, analysis);
        var a = AnnualRun.Read(first); var b = AnnualRun.Read(second);
        Assert.NotEqual(a.RunId, b.RunId);
        Assert.Equal(project, a.ProjectId); Assert.Equal(analysis, a.AnalysisId);
        Assert.Equal(new[] { 0, 4, 7, 10 }, a.Parts.Select(p => p.SensorStart));
        Assert.Equal(new[] { 4, 3, 3, 3 }, a.Parts.Select(p => p.Sensors));
        Assert.Single(b.Parts); Assert.NotEqual(a.SensorHash, b.SensorHash);
    }

    [Fact]
    public void UndeclaredFilesAreIgnoredAndMissingDeclaredFilesFail()
    {
        var folder = Create(12); var manifest = AnnualRun.Read(folder);
        foreach (var part in manifest.Parts.Take(3)) File.WriteAllText(Path.Combine(folder, part.ResultFile), "1 2 3");
        File.WriteAllText(Path.Combine(folder, "annualRfinal_part99.ill"), "stale");
        Assert.Throws<InvalidDataException>(() => AnnualRun.RequireResults(folder, manifest));
        File.WriteAllText(Path.Combine(folder, manifest.Parts[3].ResultFile), "1 2 3");
        Assert.Equal(manifest.Parts.Select(p => p.ResultFile), AnnualRun.RequireResults(folder, manifest).Select(Path.GetFileName));
    }

    [Fact]
    public void InvalidManifestCannotChangeDeclaredSensorOrder()
    {
        var folder = Create(12); var manifest = AnnualRun.Read(folder);
        manifest.Parts[1] = new(1, 0, 3);
        File.WriteAllText(Path.Combine(folder, AnnualRun.ManifestName), JsonSerializer.Serialize(manifest));
        Assert.Throws<InvalidDataException>(() => AnnualRun.Read(folder));
    }

    [Fact]
    public void LegacyFolderIsRejectedRatherThanGuessingItsRun()
    {
        Directory.CreateDirectory(root); File.WriteAllText(Path.Combine(root, "annualRfinal_part0.ill"), "1");
        Assert.Throws<InvalidDataException>(() => AnnualRun.Read(root));
    }

    public void Dispose()
    {
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FlahaGrow-run-tests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(root).StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new Exception("Invalid cleanup path.");
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
