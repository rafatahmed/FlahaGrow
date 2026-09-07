using FlahaGrow.Core.Projects;
using Xunit;

namespace FlahaGrow.Core.Tests.Projects;

public sealed class ManifestAndLayoutTests
{
    [Fact]
    public void V1FixtureRoundTripsWithoutChangingIdentity()
    {
        var manifest = ProjectManifestCodec.Read(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "project-v1.json")));
        Assert.Equal(manifest, ProjectManifestCodec.Read(ProjectManifestCodec.Write(manifest)));
        Assert.Equal(1, manifest.SchemaVersion);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":2}")]
    [InlineData("{\"schemaVersion\":1}")]
    public void UnsupportedOrIncompleteManifestsFail(string json) => Assert.Throws<InvalidDataException>(() => ProjectManifestCodec.Read(json));

    [Theory]
    [InlineData("../outside")]
    [InlineData("..\\outside")]
    [InlineData("CON.txt")]
    [InlineData("LPT1")]
    [InlineData("COM¹")]
    [InlineData("trailing.")]
    [InlineData("trailing ")]
    [InlineData("bad:name")]
    [InlineData("bad\nname")]
    [InlineData("")]
    public void InvalidAnalysisNamesFail(string name) => Assert.Throws<ArgumentException>(() => ProjectLayout.AnalysisFolder(@"D:\study", name));

    [Fact]
    public void AnalysisRunsAreNestedUnderProject() => Assert.Equal(@"D:\study\analyses\Baseline 01\runs", ProjectLayout.RunsFolder(@"D:\study", "Baseline 01"));
}
