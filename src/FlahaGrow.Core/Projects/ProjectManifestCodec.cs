using System.Text.Json;

namespace FlahaGrow.Core.Projects;

public static class ProjectManifestCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static ProjectManifest Read(string json)
    {
        var manifest = JsonSerializer.Deserialize<ProjectManifest>(json, Options)
            ?? throw new InvalidDataException("Project manifest is empty.");
        Validate(manifest);
        return manifest;
    }

    public static string Write(ProjectManifest manifest)
    {
        Validate(manifest);
        return JsonSerializer.Serialize(manifest, Options);
    }

    private static void Validate(ProjectManifest manifest)
    {
        if (manifest.SchemaVersion != 1)
            throw new InvalidDataException($"Unsupported project schema {manifest.SchemaVersion}; expected 1.");
        if (manifest.ProjectId == Guid.Empty || string.IsNullOrWhiteSpace(manifest.Name) || manifest.CreatedUtc == default)
            throw new InvalidDataException("Project manifest requires an ID, name, and creation timestamp.");
    }
}
