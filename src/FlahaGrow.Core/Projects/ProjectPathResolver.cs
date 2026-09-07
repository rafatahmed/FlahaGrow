using System.Text.Json;

namespace FlahaGrow.Core.Projects;

/// <summary>The resolver has no write, enumeration, or process API.</summary>
public interface IProjectPathReader
{
    bool DirectoryExists(string path);
    bool FileExists(string path);
    string ReadManifest(string path);
}

public sealed class ProjectPathReader : IProjectPathReader
{
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public bool FileExists(string path) => File.Exists(path);
    public string ReadManifest(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > 64 * 1024) throw new InvalidDataException("Project manifest exceeds 64 KiB.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

public sealed class ProjectPathResolver
{
    private readonly IProjectPathReader reader;
    public ProjectPathResolver(IProjectPathReader? reader = null) => this.reader = reader ?? new ProjectPathReader();

    public PathResolution Resolve(PathRequest request)
    {
        try
        {
            if (!Enum.IsDefined(typeof(LocationMode), request.Mode)) throw new ArgumentException("Unknown location mode.");
            var (root, source) = ResolveRoot(request);
            if (reader.FileExists(root)) throw new InvalidDataException("Project location is a file, not a directory.");
            var manifestPath = Path.Combine(root, ProjectManifest.FileName);
            var manifest = reader.FileExists(manifestPath) ? ProjectManifestCodec.Read(reader.ReadManifest(manifestPath)) : null;
            if (source == PathSource.SavedReference && manifest is null)
                throw new InvalidDataException("Saved project is missing its manifest. Locate the existing project explicitly.");

            var libraryInput = request.LibraryLocation ?? manifest?.LibraryLocation;
            var librarySource = request.LibraryLocation is not null ? PathSource.Explicit : PathSource.ProjectConfiguration;
            ResolvedLocation? library = null;
            if (libraryInput is not null)
                library = Library(ProjectLayout.Absolute(libraryInput, root), librarySource);
            else if (reader.DirectoryExists(Path.Combine(root, "library")))
                library = Library(Path.Combine(root, "library"), PathSource.ProjectLibrary);
            else if (request.BundledLibraryLocation is not null)
                library = Library(ProjectLayout.Absolute(request.BundledLibraryLocation), PathSource.Bundled);

            var radianceInput = request.ResolveRadianceLocation ? request.RadianceLocation ?? manifest?.RadianceLocation : null;
            ResolvedLocation? radiance = null;
            if (radianceInput is not null)
            {
                var path = ProjectLayout.Absolute(radianceInput, root);
                if (!reader.DirectoryExists(path)) throw new DirectoryNotFoundException("Configured Radiance location does not exist.");
                radiance = new(path, request.RadianceLocation is not null ? PathSource.Explicit : PathSource.ProjectConfiguration, true);
            }
            return new(new(new(root, source, reader.DirectoryExists(root)), library, radiance, manifest), Array.Empty<string>());
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return new(null, Array.AsReadOnly(new[] { ex.Message }));
        }
    }

    private (string Root, PathSource Source) ResolveRoot(PathRequest request)
    {
        string? DefinitionFolder() => request.DefinitionPath is null ? null
            : Path.GetDirectoryName(ProjectLayout.Absolute(request.DefinitionPath));
        if (request.ProjectLocation is not null)
            return (ProjectRoot(request.ProjectLocation, Path.IsPathFullyQualified(request.ProjectLocation) ? null : DefinitionFolder()), PathSource.Explicit);
        if (request.Mode == LocationMode.Custom) throw new ArgumentException("Custom mode requires a project location.");
        if (request.Mode == LocationMode.Auto && request.SavedProjectReference is not null)
            return (ProjectRoot(request.SavedProjectReference, Path.IsPathFullyQualified(request.SavedProjectReference) ? null : DefinitionFolder()), PathSource.SavedReference);
        var definitionFolder = request.Mode == LocationMode.System ? null : DefinitionFolder();
        if (request.Mode is LocationMode.Auto or LocationMode.ProjectRelative && definitionFolder is not null)
            return (Path.Combine(definitionFolder, Path.GetFileNameWithoutExtension(request.DefinitionPath) + ".FlahaGrow"), PathSource.Definition);
        if (request.Mode == LocationMode.ProjectRelative) throw new ArgumentException("Save the Grasshopper definition before using Project-relative mode.");
        if (request.DocumentsFolder is null || request.DraftId == Guid.Empty)
            throw new ArgumentException("System location requires a Documents folder and a stable draft ID.");
        return (Path.Combine(ProjectLayout.Absolute(request.DocumentsFolder), "FlahaGrow", "Projects", request.DraftId.ToString("N")), PathSource.System);
    }

    private string ProjectRoot(string location, string? definitionFolder)
    {
        var path = ProjectLayout.Absolute(location, definitionFolder);
        if (!string.Equals(Path.GetFileName(path), ProjectManifest.FileName, StringComparison.OrdinalIgnoreCase)) return path;
        if (!reader.FileExists(path)) throw new FileNotFoundException("The selected project manifest does not exist.");
        return Path.GetDirectoryName(path)!;
    }

    private ResolvedLocation Library(string path, PathSource source)
    {
        bool IsLibrary(string folder) => new[] { "RadMaterials", "RadGlazing", "RadIES" }
            .All(name => reader.DirectoryExists(Path.Combine(folder, name)));
        if (IsLibrary(path)) return new(path, source, true);
        var child = Path.Combine(path, "FlahaGrow_Library_Small");
        if (IsLibrary(child)) return new(child, source, true);
        throw new DirectoryNotFoundException("Library must contain RadMaterials, RadGlazing, and RadIES, directly or under FlahaGrow_Library_Small.");
    }
}
