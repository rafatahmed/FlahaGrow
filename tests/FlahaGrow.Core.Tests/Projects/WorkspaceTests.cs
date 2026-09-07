using FlahaGrow.Core.Projects;
using System.Diagnostics;
using Xunit;

namespace FlahaGrow.Core.Tests.Projects;

public sealed class WorkspaceTests : IDisposable
{
    private readonly string sandbox = Path.Combine(Path.GetTempPath(), "FlahaGrow.WorkspaceTests", Guid.NewGuid().ToString("N"));
    private readonly WorkspaceService service = new();
    private string Root => Path.Combine(sandbox, "Study");
    private ResolvedPaths Paths => new(new(Root, PathSource.Explicit, Directory.Exists(Root)), null, null, null);
    private WorkspaceRequest Request => new(Paths, "Greenhouse");

    [Fact]
    public void InitializeCreatesLayoutAndOpenPreservesIdentity()
    {
        var created = service.Initialize(Request);
        var reopened = service.Open(Paths);
        Assert.Equal(created, reopened);
        Assert.True(Directory.Exists(Path.Combine(Root, "inputs", "weather")));
        Assert.True(Directory.Exists(created.RunsFolder));
        Assert.Empty(Directory.EnumerateFileSystemEntries(created.RunsFolder));
        Assert.False(Directory.Exists(Path.Combine(Root, "library")));
    }

    [Fact]
    public void RepeatedInitializationPreservesFilesAndManifestBytes()
    {
        var first = service.Initialize(Request);
        var manifestPath = Path.Combine(Root, ProjectManifest.FileName);
        var original = File.ReadAllBytes(manifestPath);
        var input = Path.Combine(first.InputsFolder, "weather", "study.epw");
        File.WriteAllText(input, "weather data");
        var result = Path.Combine(first.RunsFolder, "existing-result.txt");
        File.WriteAllText(result, "prior result");
        var second = service.Initialize(Request with { ProjectName = "Do not rename" });
        Assert.Equal(first, second);
        Assert.Equal(original, File.ReadAllBytes(manifestPath));
        Assert.Equal("weather data", File.ReadAllText(input));
        Assert.Equal("prior result", File.ReadAllText(result));
        Assert.Empty(Directory.EnumerateFiles(Root, "*.tmp"));
        Assert.Empty(Directory.EnumerateFiles(Root, ".flahagrow-probe-*"));
    }

    [Fact]
    public void OpenMissingWorkspaceDoesNotCreateAnything()
    {
        Assert.ThrowsAny<IOException>(() => service.Open(Paths));
        Assert.False(Directory.Exists(Root));
    }

    [Fact]
    public void OpenDoesNotRepairMissingFolders()
    {
        var created = service.Initialize(Request);
        Directory.Delete(created.RunsFolder);
        service.Open(Paths);
        Assert.False(Directory.Exists(created.RunsFolder));
        service.Initialize(Request);
        Assert.True(Directory.Exists(created.RunsFolder));
    }

    [Fact]
    public void NonemptyRootRequiresExplicitAdoption()
    {
        Directory.CreateDirectory(Root);
        var file = Path.Combine(Root, "user.txt"); File.WriteAllText(file, "keep");
        Assert.Throws<InvalidDataException>(() => service.Initialize(Request));
        Assert.False(File.Exists(Path.Combine(Root, ProjectManifest.FileName)));
        service.Initialize(Request with { AdoptExisting = true });
        Assert.Equal("keep", File.ReadAllText(file));
    }

    [Fact]
    public void SeparateAnalysesHaveSeparateIdentitiesAndRunFolders()
    {
        var baseline = service.Initialize(Request);
        var alternative = service.Initialize(Request with { AnalysisName = "Alternative", Workflow = AnalysisWorkflow.ElectricLighting });
        Assert.Equal(baseline.Project, alternative.Project);
        Assert.NotEqual(baseline.Analysis.AnalysisId, alternative.Analysis.AnalysisId);
        Assert.NotEqual(baseline.RunsFolder, alternative.RunsFolder);
        Assert.Equal(alternative, service.Open(Paths, "Alternative"));
        Assert.Throws<InvalidDataException>(() => service.Initialize(Request with { Workflow = AnalysisWorkflow.ElectricLighting }));
    }

    [Fact]
    public void ExistingAnalysisFolderNeedsAdoption()
    {
        service.Initialize(Request);
        var folder = ProjectLayout.AnalysisFolder(Root, "Imported"); Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "user.txt"); File.WriteAllText(file, "keep");
        Assert.Throws<InvalidDataException>(() => service.Initialize(Request with { AnalysisName = "Imported" }));
        service.Initialize(Request with { AnalysisName = "Imported", AdoptExisting = true });
        Assert.Equal("keep", File.ReadAllText(file));
    }

    [Fact]
    public void LayoutFailureCanResumeWithoutChangingProjectIdentity()
    {
        Directory.CreateDirectory(Path.Combine(Root, "inputs"));
        var conflict = Path.Combine(Root, "inputs", "weather"); File.WriteAllText(conflict, "blocking file");
        Assert.ThrowsAny<IOException>(() => service.Initialize(Request with { AdoptExisting = true }));
        var projectFile = Path.Combine(Root, ProjectManifest.FileName);
        var identity = ProjectManifestCodec.Read(File.ReadAllText(projectFile)).ProjectId;
        Assert.Equal("blocking file", File.ReadAllText(conflict));
        File.Delete(conflict);
        var completed = service.Initialize(Request);
        Assert.Equal(identity, completed.Project.Manifest.ProjectId);
        Assert.True(Directory.Exists(completed.RunsFolder));
    }

    [Fact]
    public void AnalysisLayoutFailurePreservesItsIdentityOnRetry()
    {
        service.Initialize(Request);
        var folder = ProjectLayout.AnalysisFolder(Root, "Interrupted"); Directory.CreateDirectory(folder);
        var conflict = Path.Combine(folder, "runs"); File.WriteAllText(conflict, "keep until resolved");
        Assert.ThrowsAny<IOException>(() => service.Initialize(Request with { AnalysisName = "Interrupted", AdoptExisting = true }));
        var id = AnalysisManifestCodec.Read(File.ReadAllText(Path.Combine(folder, AnalysisManifest.FileName))).AnalysisId;
        File.Delete(conflict);
        var resumed = service.Initialize(Request with { AnalysisName = "Interrupted" });
        Assert.Equal(id, resumed.Analysis.AnalysisId);
    }

    [Fact]
    public void LinkedAnalysisAncestorIsRejectedWithoutTouchingTarget()
    {
        Directory.CreateDirectory(Root);
        var external = Path.Combine(sandbox, "outside-project"); Directory.CreateDirectory(external);
        var sentinel = Path.Combine(external, "keep.txt"); File.WriteAllText(sentinel, "unchanged");
        var link = Path.Combine(Root, "analyses");
        var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add("-NoProfile"); start.ArgumentList.Add("-NonInteractive"); start.ArgumentList.Add("-Command");
        start.ArgumentList.Add("$ErrorActionPreference = 'Stop'; New-Item -ItemType Junction -Path '" + link.Replace("'", "''")
            + "' -Target '" + external.Replace("'", "''") + "' | Out-Null");
        using var process = Process.Start(start)!;
        if (!process.WaitForExit(10000)) { process.Kill(entireProcessTree: true); throw new TimeoutException("Junction fixture creation timed out."); }
        Assert.Equal(0, process.ExitCode);
        try
        {
            Assert.ThrowsAny<IOException>(() => service.Initialize(Request with { AdoptExisting = true }));
            Assert.Equal("unchanged", File.ReadAllText(sentinel));
            Assert.Single(Directory.EnumerateFileSystemEntries(external));
            Assert.False(File.Exists(Path.Combine(Root, ProjectManifest.FileName)));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public void ReadOnlyManifestCanBeOpenedAndIsNotRewritten()
    {
        var original = service.Initialize(Request);
        var path = Path.Combine(Root, ProjectManifest.FileName);
        File.SetAttributes(path, FileAttributes.ReadOnly);
        try
        {
            Assert.Equal(original, service.Open(Paths));
            Assert.Equal(original, service.Initialize(Request));
        }
        finally { File.SetAttributes(path, FileAttributes.Normal); }
    }

    [Fact]
    public void CompetingInitializerFailsWhileLockIsHeld()
    {
        var existing = service.Initialize(Request);
        using var held = new FileStream(Path.Combine(Root, WorkspaceService.LockFileName), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.ThrowsAny<IOException>(() => service.Initialize(Request));
        Assert.Equal(existing, service.Open(Paths));
    }

    [Fact]
    public async Task ConcurrentInitializersCannotCreateDifferentIdentities()
    {
        var tasks = Enumerable.Range(0, 6).Select(_ => Task.Run(() =>
        {
            try { return service.Initialize(Request).Project.Manifest.ProjectId; }
            catch (IOException) { return (Guid?)null; }
        })).ToArray();
        var results = await Task.WhenAll(tasks);
        var ids = results.Where(id => id.HasValue).Distinct().ToList();
        Assert.Single(ids);
        Assert.Equal(ids[0], service.Open(Paths).Project.Manifest.ProjectId);
    }

    [Fact]
    public void CancelledInitializationCreatesNothing()
    {
        Assert.Throws<OperationCanceledException>(() => service.Initialize(Request, new CancellationToken(true)));
        Assert.False(Directory.Exists(Root));
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("CON")]
    [InlineData("trailing.")]
    public void InvalidAnalysisFailsBeforeCreatingRoot(string name)
    {
        Assert.Throws<ArgumentException>(() => service.Initialize(Request with { AnalysisName = name }));
        Assert.False(Directory.Exists(Root));
    }

    [Fact]
    public void NewerProjectManifestIsNeverRewritten()
    {
        Directory.CreateDirectory(Root);
        var path = Path.Combine(Root, ProjectManifest.FileName); File.WriteAllText(path, "{\"schemaVersion\":99}");
        Assert.Throws<InvalidDataException>(() => service.Initialize(Request));
        Assert.Equal("{\"schemaVersion\":99}", File.ReadAllText(path));
        Assert.False(Directory.Exists(Path.Combine(Root, "inputs")));
    }

    [Fact]
    public void ForeignAnalysisIdentityIsRejected()
    {
        var workspace = service.Initialize(Request);
        var path = Path.Combine(workspace.Analysis.Folder, AnalysisManifest.FileName);
        File.WriteAllText(path, AnalysisManifestCodec.Write(workspace.AnalysisManifest with { ProjectId = Guid.NewGuid() }));
        Assert.Throws<InvalidDataException>(() => service.Open(Paths));
        Assert.Throws<InvalidDataException>(() => service.Initialize(Request));
    }

    [Fact]
    public void ResolvedIdentityCannotBeSilentlyReplaced()
    {
        var workspace = service.Initialize(Request);
        var stale = Paths with { Manifest = workspace.Project.Manifest with { ProjectId = Guid.NewGuid() } };
        Assert.Throws<InvalidDataException>(() => service.Open(stale));
        Assert.Throws<InvalidDataException>(() => service.Initialize(Request with { Paths = stale }));
        File.Delete(Path.Combine(Root, ProjectManifest.FileName));
        Assert.Throws<InvalidDataException>(() => service.Initialize(Request with { Paths = stale, AdoptExisting = true }));
    }

    [Fact]
    public void ProjectLocalAssetLocationIsPortableAndBundleIsNotPinned()
    {
        var paths = Paths with { Library = new(Path.Combine(Root, "library"), PathSource.ProjectLibrary, true) };
        Assert.Equal("library", service.Initialize(Request with { Paths = paths }).Project.Manifest.LibraryLocation);
        var otherRoot = Path.Combine(sandbox, "Other");
        paths = Paths with { Project = new(otherRoot, PathSource.Explicit, false), Library = new(@"C:\Bundle", PathSource.Bundled, true) };
        Assert.Null(service.Initialize(Request with { Paths = paths }).Project.Manifest.LibraryLocation);
    }

    [Fact]
    public void AnalysisManifestRoundTripsAndRejectsNewerSchema()
    {
        var manifest = new AnalysisManifest { SchemaVersion = 1, AnalysisId = Guid.NewGuid(), ProjectId = Guid.NewGuid(), Name = "baseline", Workflow = AnalysisWorkflow.AnnualDaylight, CreatedUtc = DateTimeOffset.UtcNow };
        Assert.Equal(manifest, AnalysisManifestCodec.Read(AnalysisManifestCodec.Write(manifest)));
        Assert.Throws<InvalidDataException>(() => AnalysisManifestCodec.Read("{\"schemaVersion\":2}"));
        Assert.Throws<InvalidDataException>(() => AnalysisManifestCodec.Write(manifest with { Workflow = (AnalysisWorkflow)(-1) }));
    }

    public void Dispose()
    {
        var target = Path.GetFullPath(sandbox);
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FlahaGrow.WorkspaceTests")) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Test cleanup escaped its sandbox.");
        if (Directory.Exists(target)) Directory.Delete(target, recursive: true);
    }
}
