namespace FlahaGrow.Core.Projects;

public enum LocationMode { Auto, ProjectRelative, System, Custom }
public enum PathSource { Explicit, SavedReference, Definition, System, ProjectConfiguration, ProjectLibrary, Bundled }

/// <summary>Existence is an observation, not a guarantee of write access.</summary>
public sealed record ResolvedLocation(string Path, PathSource Source, bool Exists);
public sealed record ProjectContext(ProjectManifest Manifest, ResolvedLocation Root);
public sealed record AnalysisContext(ProjectContext Project, Guid AnalysisId, string Name, string Folder);
public sealed record ResolvedPaths(ResolvedLocation Project, ResolvedLocation? Library,
    ResolvedLocation? RadianceLocation, ProjectManifest? Manifest);
public sealed record PathResolution(ResolvedPaths? Paths, IReadOnlyList<string> Errors)
{
    public bool Success => Paths is not null && Errors.Count == 0;
}

/// <summary>Host-owned values are explicit, so resolution never depends on the process working directory.</summary>
public sealed record PathRequest
{
    public LocationMode Mode { get; init; }
    public string? ProjectLocation { get; init; }
    public string? SavedProjectReference { get; init; }
    public string? DefinitionPath { get; init; }
    public string? DocumentsFolder { get; init; }
    public Guid DraftId { get; init; }
    public string? LibraryLocation { get; init; }
    public string? BundledLibraryLocation { get; init; }
    public string? RadianceLocation { get; init; }
    public bool ResolveRadianceLocation { get; init; } = true;
}

/// <summary>Schema v1 uses a fixed managed layout; external locations may be project-relative.</summary>
public sealed record ProjectManifest
{
    public const string FileName = "flahagrow.project.json";
    public int SchemaVersion { get; init; }
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; init; }
    public string? LibraryLocation { get; init; }
    public string? RadianceLocation { get; init; }
}
