using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlahaGrow.Core.Projects;

public enum AnalysisWorkflow { AnnualDaylight, ElectricLighting }

public sealed record AnalysisManifest
{
    public const string FileName = "analysis.json";
    public int SchemaVersion { get; init; }
    public Guid AnalysisId { get; init; }
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public AnalysisWorkflow Workflow { get; init; } = (AnalysisWorkflow)(-1);
    public DateTimeOffset CreatedUtc { get; init; }
}

public static class AnalysisManifestCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };

    public static AnalysisManifest Read(string json)
    {
        var value = JsonSerializer.Deserialize<AnalysisManifest>(json, Options)
            ?? throw new InvalidDataException("Analysis manifest is empty.");
        Validate(value);
        return value;
    }

    public static string Write(AnalysisManifest value)
    {
        Validate(value);
        return JsonSerializer.Serialize(value, Options);
    }

    private static void Validate(AnalysisManifest value)
    {
        if (value.SchemaVersion != 1) throw new InvalidDataException($"Unsupported analysis schema {value.SchemaVersion}; expected 1.");
        if (value.AnalysisId == Guid.Empty || value.ProjectId == Guid.Empty || value.CreatedUtc == default
            || !Enum.IsDefined(typeof(AnalysisWorkflow), value.Workflow))
            throw new InvalidDataException("Analysis requires valid IDs, creation time, and workflow.");
        ProjectLayout.ValidateName(value.Name);
    }
}
