using System.Text;

namespace FlahaGrow.Core.Projects;

public sealed record WorkspaceRequest(ResolvedPaths Paths, string ProjectName, string AnalysisName = "baseline",
    AnalysisWorkflow Workflow = AnalysisWorkflow.AnnualDaylight, bool AdoptExisting = false);

public sealed record WorkspaceContext(ProjectContext Project, AnalysisContext Analysis, AnalysisManifest AnalysisManifest)
{
    public string InputsFolder => Path.Combine(Project.Root.Path, "inputs");
    public string RunsFolder => Path.Combine(Analysis.Folder, "runs");
}

/// <summary>Explicit filesystem operations. Call outside the host UI thread; Open never writes.</summary>
public sealed class WorkspaceService
{
    public const string LockFileName = ".flahagrow.workspace.lock";
    private readonly ProjectPathReader reader = new();

    public WorkspaceContext Open(ResolvedPaths paths, string analysisName = "baseline", CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var root = ProjectLayout.Absolute(paths.Project.Path);
        var analysisFolder = ProjectLayout.AnalysisFolder(root, analysisName);
        var project = ReadProject(root);
        EnsureIdentity(paths, project);
        var analysis = ReadAnalysis(analysisFolder, project, analysisName);
        return Context(paths, root, project, analysisFolder, analysis);
    }

    public WorkspaceContext Initialize(WorkspaceRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(request.ProjectName)) throw new ArgumentException("Project display name is required.");
        if (!Enum.IsDefined(typeof(AnalysisWorkflow), request.Workflow)) throw new ArgumentException("Unknown workflow.");
        var root = ProjectLayout.Absolute(request.Paths.Project.Path);
        var analysisFolder = ProjectLayout.AnalysisFolder(root, request.AnalysisName);
        CheckPath(root);
        Directory.CreateDirectory(root);
        var lockPath = Path.Combine(root, LockFileName);
        CheckPath(lockPath);
        // The lock file is retained. Deleting it on release would let another process lock a different file.
        using var operationLock = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        cancellationToken.ThrowIfCancellationRequested();
        var projectFile = Path.Combine(root, ProjectManifest.FileName);
        CheckPath(projectFile);
        ProjectManifest project;
        if (File.Exists(projectFile))
        {
            project = ReadProject(root);
            EnsureIdentity(request.Paths, project);
        }
        else
        {
            if (request.Paths.Manifest is not null || request.Paths.Project.Source == PathSource.SavedReference)
                throw new InvalidDataException("The selected existing project manifest is missing; initialization will not replace its identity.");
            RequireAdoption(root, request.AdoptExisting, LockFileName);
            project = new()
            {
                SchemaVersion = 1, ProjectId = Guid.NewGuid(), Name = request.ProjectName, CreatedUtc = DateTimeOffset.UtcNow,
                LibraryLocation = PersistLocation(root, request.Paths.Library),
                RadianceLocation = PersistLocation(root, request.Paths.RadianceLocation)
            };
        }

        // Validate analysis ownership and existing content before publishing a new project manifest.
        CheckPath(analysisFolder);
        var analysisFile = Path.Combine(analysisFolder, AnalysisManifest.FileName);
        CheckPath(analysisFile);
        AnalysisManifest analysis;
        if (File.Exists(analysisFile))
        {
            analysis = ReadAnalysis(analysisFolder, project, request.AnalysisName);
            if (analysis.Workflow != request.Workflow) throw new InvalidDataException("Existing analysis uses a different workflow. Choose another analysis name.");
        }
        else
        {
            RequireAdoption(analysisFolder, request.AdoptExisting);
            analysis = new()
            {
                SchemaVersion = 1, AnalysisId = Guid.NewGuid(), ProjectId = project.ProjectId,
                Name = request.AnalysisName, Workflow = request.Workflow, CreatedUtc = DateTimeOffset.UtcNow
            };
        }

        ProbeWriteAccess(root);
        cancellationToken.ThrowIfCancellationRequested();
        // Commit identities before layout creation: a failed layout operation can resume without replacing IDs.
        if (!File.Exists(projectFile)) PublishNew(projectFile, ProjectManifestCodec.Write(project), cancellationToken);
        foreach (var name in new[] { "geometry", "weather", "sensors", "lighting" })
            CreateDirectory(Path.Combine(root, "inputs", name), cancellationToken);
        CreateDirectory(analysisFolder, cancellationToken);
        if (!File.Exists(analysisFile)) PublishNew(analysisFile, AnalysisManifestCodec.Write(analysis), cancellationToken);
        CreateDirectory(Path.Combine(analysisFolder, "runs"), cancellationToken);
        return Context(request.Paths, root, project, analysisFolder, analysis);
    }

    private ProjectManifest ReadProject(string root)
    {
        var path = Path.Combine(root, ProjectManifest.FileName);
        CheckPath(path);
        return ProjectManifestCodec.Read(reader.ReadManifest(path));
    }

    private AnalysisManifest ReadAnalysis(string folder, ProjectManifest project, string name)
    {
        var path = Path.Combine(folder, AnalysisManifest.FileName);
        CheckPath(path);
        var analysis = AnalysisManifestCodec.Read(reader.ReadManifest(path));
        if (analysis.ProjectId != project.ProjectId || !string.Equals(analysis.Name, name, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Analysis identity does not match this project and folder.");
        return analysis;
    }

    private static void EnsureIdentity(ResolvedPaths paths, ProjectManifest project)
    {
        if (paths.Manifest is not null && paths.Manifest.ProjectId != project.ProjectId)
            throw new InvalidDataException("Project identity changed since path resolution. Resolve the project again.");
    }

    private static WorkspaceContext Context(ResolvedPaths paths, string root, ProjectManifest project, string folder, AnalysisManifest analysis)
    {
        var context = new ProjectContext(project, paths.Project with { Path = root, Exists = true });
        return new(context, new(context, analysis.AnalysisId, analysis.Name, folder), analysis);
    }

    private static string? PersistLocation(string root, ResolvedLocation? location)
    {
        if (location is null || location.Source == PathSource.Bundled) return null;
        var path = ProjectLayout.Absolute(location.Path);
        var relative = Path.GetRelativePath(root, path);
        return !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            ? relative : path;
    }

    private static void RequireAdoption(string folder, bool adopt, string? ignoredName = null)
    {
        if (!adopt && Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder)
                .Any(path => !string.Equals(Path.GetFileName(path), ignoredName, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The folder contains existing content without a manifest. Use explicit Adopt to retain and initialize it.");
    }

    private static void CreateDirectory(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        CheckPath(path);
        Directory.CreateDirectory(path);
    }

    private static void ProbeWriteAccess(string root)
    {
        var path = Path.Combine(root, ".flahagrow-probe-" + Guid.NewGuid().ToString("N"));
        CheckPath(path);
        using var probe = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        probe.WriteByte(0);
        probe.Flush(true);
    }

    private static void PublishNew(string path, string json, CancellationToken token)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        if (bytes.Length > 64 * 1024) throw new InvalidDataException("Manifest exceeds 64 KiB.");
        CheckPath(path);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes);
                stream.Flush(true);
            }
            token.ThrowIfCancellationRequested();
            CheckPath(path);
            File.Move(temporary, path, overwrite: false);
        }
        finally
        {
            // Only this operation's unique temporary file can be removed.
            CheckPath(temporary);
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <summary>Reject links in all existing ancestors, including dangling links. No traversal through junctions.</summary>
    private static void CheckPath(string path)
    {
        for (string? current = ProjectLayout.Absolute(path); current is not null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                    throw new IOException($"Workspace path traverses a symbolic link or junction: {current}");
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }
}
