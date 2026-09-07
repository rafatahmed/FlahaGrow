using FlahaGrow.Core.Projects;
using Xunit;

namespace FlahaGrow.Core.Tests.Projects;

public sealed class ProjectPathTests
{
    private readonly MemoryReader reader = new();
    private static readonly Guid Draft = Guid.Parse("43c157fc-dd07-45b4-b5b9-1d230f4646d5");
    private static PathRequest Defaults => new() { DocumentsFolder = @"C:\Users\Grow\Documents", DraftId = Draft };
    private PathResolution Resolve(PathRequest request) => new ProjectPathResolver(reader).Resolve(request);

    [Fact]
    public void SimplifiedPathsIgnoreLegacyRadianceLocation()
    {
        var request = Defaults with { ProjectLocation = @"D:\study", RadianceLocation = @"C:\missing-radiance" };
        Assert.False(Resolve(request).Success);
        Assert.True(Resolve(request with { ResolveRadianceLocation = false }).Success);
    }

    [Fact]
    public void ExplicitProjectWinsAndDoesNotRequireExistingDirectory()
    {
        var result = Resolve(Defaults with { ProjectLocation = @"D:\Studies\بيت زجاجي", SavedProjectReference = @"C:\missing" });
        Assert.True(result.Success);
        Assert.Equal(@"D:\Studies\بيت زجاجي", result.Paths!.Project.Path);
        Assert.Equal(PathSource.Explicit, result.Paths.Project.Source);
        Assert.False(result.Paths.Project.Exists);
        Assert.Empty(reader.Reads);
    }

    [Fact]
    public void AbsoluteExplicitLocationIgnoresInvalidDefinitionReference() =>
        Assert.True(Resolve(Defaults with { ProjectLocation = @"D:\study", DefinitionPath = "invalid-relative.gh" }).Success);

    [Fact]
    public void SavedProjectStaysBoundWhenDefinitionMoves()
    {
        AddProject(@"D:\existing");
        var result = Resolve(Defaults with { SavedProjectReference = @"D:\existing", DefinitionPath = @"C:\new\study.gh" });
        Assert.Equal(@"D:\existing", result.Paths!.Project.Path);
        Assert.Equal(PathSource.SavedReference, result.Paths.Project.Source);
        Assert.Single(reader.Reads);
    }

    [Fact]
    public void MissingSavedProjectDoesNotBecomeANewProject()
    {
        var result = Resolve(Defaults with { SavedProjectReference = @"D:\deleted" });
        Assert.False(result.Success);
        Assert.Contains("missing its manifest", Assert.Single(result.Errors));
    }

    [Fact]
    public void DefinitionRelativeLocationUsesDefinitionName()
    {
        var result = Resolve(Defaults with { DefinitionPath = @"D:\Studies\Green house.gh" });
        Assert.Equal(@"D:\Studies\Green house.FlahaGrow", result.Paths!.Project.Path);
        Assert.Equal(PathSource.Definition, result.Paths.Project.Source);
    }

    [Fact]
    public void UnsavedAutoLocationIsStableAcrossSolutions()
    {
        Assert.Equal(Resolve(Defaults), Resolve(Defaults));
        Assert.Equal(PathSource.System, Resolve(Defaults).Paths!.Project.Source);
    }

    [Fact]
    public void ForcedSystemIgnoresDefinitionAndSavedReference()
    {
        var result = Resolve(Defaults with { Mode = LocationMode.System, DefinitionPath = @"D:\a.gh", SavedProjectReference = @"D:\missing" });
        Assert.Equal(PathSource.System, result.Paths!.Project.Source);
    }

    [Theory]
    [InlineData(LocationMode.ProjectRelative)]
    [InlineData(LocationMode.Custom)]
    public void ForcedModesDoNotSilentlyFallBack(LocationMode mode) => Assert.False(Resolve(Defaults with { Mode = mode }).Success);

    [Fact]
    public void MissingDocumentsRequiresExplicitLocation() => Assert.False(Resolve(Defaults with { DocumentsFolder = null }).Success);

    [Fact]
    public void RelativeProjectRequiresDefinitionBase()
    {
        Assert.False(Resolve(Defaults with { ProjectLocation = "study" }).Success);
        Assert.Equal(@"D:\models\study", Resolve(Defaults with { ProjectLocation = "study", DefinitionPath = @"D:\models\a.gh" }).Paths!.Project.Path);
    }

    [Fact]
    public void ManifestInputOpensContainingProject()
    {
        AddProject(@"D:\existing");
        var result = Resolve(Defaults with { ProjectLocation = @"D:\existing\flahagrow.project.json" });
        Assert.Equal(@"D:\existing", result.Paths!.Project.Path);
        Assert.NotNull(result.Paths.Manifest);
    }

    [Fact]
    public void MissingManifestInputFails() => Assert.False(Resolve(Defaults with { ProjectLocation = @"D:\missing\flahagrow.project.json" }).Success);

    [Theory]
    [InlineData(@"C:relative")]
    [InlineData(@"\relative")]
    [InlineData(@"D:\bad|name")]
    [InlineData(@"D:\CON")]
    [InlineData("")]
    public void InvalidExplicitProjectNeverFallsBack(string path) => Assert.False(Resolve(Defaults with { ProjectLocation = path }).Success);

    [Fact]
    public void LibraryAcceptsParentAndAssetRoot()
    {
        AddLibrary(@"D:\bundle\FlahaGrow_Library_Small");
        foreach (var input in new[] { @"D:\bundle", @"D:\bundle\FlahaGrow_Library_Small" })
            Assert.Equal(@"D:\bundle\FlahaGrow_Library_Small", Resolve(Defaults with { LibraryLocation = input }).Paths!.Library!.Path);
    }

    [Fact]
    public void InvalidExplicitLibraryNeverFallsBackToBundle()
    {
        AddLibrary(@"D:\bundle");
        Assert.False(Resolve(Defaults with { LibraryLocation = @"D:\missing", BundledLibraryLocation = @"D:\bundle" }).Success);
    }

    [Fact]
    public void ProjectLibraryOverridesBundledLibrary()
    {
        AddLibrary(@"D:\study\library"); AddLibrary(@"D:\bundle");
        var result = Resolve(Defaults with { ProjectLocation = @"D:\study", BundledLibraryLocation = @"D:\bundle" });
        Assert.Equal(PathSource.ProjectLibrary, result.Paths!.Library!.Source);
    }

    [Fact]
    public void ManifestLibraryIsResolvedRelativeToProject()
    {
        reader.Files[@"D:\study\flahagrow.project.json"] = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "project-v1.json"));
        AddLibrary(@"D:\study\library");
        Assert.Equal(@"D:\study\library", Resolve(Defaults with { ProjectLocation = @"D:\study" }).Paths!.Library!.Path);
    }

    [Fact]
    public void InvalidRadianceOverrideFailsWithoutSearchingElsewhere() =>
        Assert.False(Resolve(Defaults with { RadianceLocation = @"D:\missing" }).Success);

    [Fact]
    public void RadianceLocationIsOnlyALocationNotReadiness()
    {
        reader.Directories.Add(@"D:\study\tools");
        var result = Resolve(Defaults with { ProjectLocation = @"D:\study", RadianceLocation = "tools" });
        Assert.Equal(@"D:\study\tools", result.Paths!.RadianceLocation!.Path);
    }

    [Fact]
    public void MalformedManifestDoesNotFallBack()
    {
        reader.Files[@"D:\study\flahagrow.project.json"] = "{broken";
        Assert.False(Resolve(Defaults with { ProjectLocation = @"D:\study" }).Success);
    }

    [Fact]
    public void UnsupportedManifestIsReportedAsAResolutionError()
    {
        reader.Files[@"D:\study\flahagrow.project.json"] = "{\"schemaVersion\":2}";
        Assert.False(Resolve(Defaults with { ProjectLocation = @"D:\study" }).Success);
    }

    [Fact]
    public void ExistingFileCannotBeUsedAsProjectRoot()
    {
        reader.Files[@"D:\study"] = "not a project";
        Assert.False(Resolve(Defaults with { ProjectLocation = @"D:\study" }).Success);
    }

    [Fact]
    public void PhysicalResolutionCreatesNothing()
    {
        var path = Path.Combine(Path.GetTempPath(), "FlahaGrow-resolve-" + Guid.NewGuid().ToString("N"));
        var result = new ProjectPathResolver().Resolve(new() { ProjectLocation = path });
        Assert.True(result.Success);
        Assert.False(Directory.Exists(path));
    }

    private void AddProject(string root)
    {
        reader.Directories.Add(root);
        reader.Files[Path.Combine(root, ProjectManifest.FileName)] = ProjectManifestCodec.Write(new()
        { SchemaVersion = 1, ProjectId = Draft, Name = "Study", CreatedUtc = DateTimeOffset.UtcNow });
    }

    private void AddLibrary(string root)
    {
        reader.Directories.Add(root);
        foreach (var child in new[] { "RadMaterials", "RadGlazing", "RadIES" }) reader.Directories.Add(Path.Combine(root, child));
    }

    private sealed class MemoryReader : IProjectPathReader
    {
        public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Reads { get; } = new();
        public bool DirectoryExists(string path) => Directories.Contains(path);
        public bool FileExists(string path) => Files.ContainsKey(path);
        public string ReadManifest(string path) { Reads.Add(path); return Files[path]; }
    }
}
