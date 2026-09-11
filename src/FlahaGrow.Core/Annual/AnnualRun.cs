using System.Security.Cryptography;
using System.Text.Json;
using FlahaGrow.Core.Projects;

namespace FlahaGrow.Core.Annual;

public sealed record AnnualRunPart(int Index, int SensorStart, int Sensors)
{
    public string ResultFile => $"annualRfinal_part{Index}.ill";
    public string LogFile => $"annual_progress_part{Index}.log";
    public string StateFile => $"annual_state_part{Index}.txt";
    public string ProcessFile => $"annual_process_part{Index}.txt";
}

/// <summary>Identity of the cmd.exe process that owns one launched batch part.</summary>
public sealed record AnnualProcessIdentity(int ProcessId, long StartUtcTicks);

public sealed record AnnualRunManifest(int SchemaVersion, Guid RunId, Guid? ProjectId, Guid? AnalysisId,
    string SourceRoot, int Sky, int Sensors, string SensorHash, Dictionary<string, string> Inputs,
    AnnualRunPart[] Parts, int Hours);

/// <summary>Owns run identity and declared result membership. Never discovers results by wildcard.</summary>
public static class AnnualRun
{
    public const string ManifestName = "flahagrow.run.json";
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static string Create(string container, string sourceRoot, int sky, IReadOnlyList<string> sensors,
        Dictionary<string, string> inputHashes, Guid? projectId = null, Guid? analysisId = null, int hours = 8760)
    {
        AnnualSkySubdivision.Validate(sky);
        if (sensors.Count == 0) throw new InvalidDataException("The sensor grid is empty.");
        if (hours <= 0) throw new InvalidDataException("Expected weather step count must be positive.");
        container = ProjectLayout.Absolute(container);
        CheckPath(container);
        var id = Guid.NewGuid();
        var folder = Path.Combine(container, id.ToString("N"));
        Directory.CreateDirectory(folder);
        var count = sensors.Count > 10 ? 4 : 1;
        var start = 0;
        var parts = Enumerable.Range(0, count).Select(index =>
        {
            var size = sensors.Count / count + (index < sensors.Count % count ? 1 : 0);
            var part = new AnnualRunPart(index, start, size); start += size; return part;
        }).ToArray();
        var sensorHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", sensors))));
        var manifest = new AnnualRunManifest(2, id, projectId, analysisId, ProjectLayout.Absolute(sourceRoot),
            sky, sensors.Count, sensorHash, inputHashes, parts, hours);
        File.WriteAllText(Path.Combine(folder, ManifestName), JsonSerializer.Serialize(manifest, Json));
        return folder;
    }

    public static AnnualRunManifest Read(string folder)
    {
        var path = Path.Combine(ProjectLayout.Absolute(folder), ManifestName);
        if (!File.Exists(path)) throw new InvalidDataException("No annual run manifest. Connect Annual Simulation's run Folder output. Legacy flat results must be regenerated; their study identity cannot be verified.");
        if (new FileInfo(path).Length > 1024 * 1024) throw new InvalidDataException("Run manifest is too large.");
        var manifest = JsonSerializer.Deserialize<AnnualRunManifest>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Empty run manifest.");
        if (manifest.SchemaVersion != 2) throw new InvalidDataException("Run manifest schema is unsupported. Regenerate the run to record expected weather steps and checked command states.");
        if (manifest.Hours <= 0 || manifest.RunId == Guid.Empty || manifest.Sensors <= 0
            || manifest.Parts is null || manifest.Parts.Length != (manifest.Sensors > 10 ? 4 : 1)
            || manifest.Inputs is null || string.IsNullOrWhiteSpace(manifest.SensorHash))
            throw new InvalidDataException("Invalid annual run manifest.");
        AnnualSkySubdivision.Validate(manifest.Sky);
        long start = 0;
        for (var i = 0; i < manifest.Parts.Length; i++)
        {
            var part = manifest.Parts[i];
            if (part is null || part.Index != i || part.SensorStart != start || part.Sensors <= 0)
                throw new InvalidDataException("Invalid sensor ordering in run manifest.");
            start += part.Sensors;
        }
        if (start != manifest.Sensors) throw new InvalidDataException("Run sensor counts disagree.");
        return manifest;
    }

    public static string[] RequireResults(string folder, AnnualRunManifest manifest)
    {
        var paths = manifest.Parts.Select(part => Path.Combine(folder, part.ResultFile)).ToArray();
        foreach (var path in paths)
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
                throw new InvalidDataException("Run result is missing or empty: " + Path.GetFileName(path));
        return paths;
    }

    public static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    /// <summary>
    /// Records a process in a run-owned file. The start time prevents a recycled PID
    /// from being mistaken for a Radiance job after Rhino has been restarted.
    /// </summary>
    public static void WriteProcessIdentity(string folder, AnnualRunManifest manifest, AnnualRunPart part, AnnualProcessIdentity identity)
    {
        if (identity.ProcessId <= 0 || identity.StartUtcTicks <= 0) throw new ArgumentOutOfRangeException(nameof(identity));
        File.WriteAllText(Path.Combine(folder, part.ProcessFile), $"{manifest.RunId:N} {identity.ProcessId} {identity.StartUtcTicks}");
    }

    public static AnnualProcessIdentity? ReadProcessIdentity(string folder, AnnualRunManifest manifest, AnnualRunPart part)
    {
        var path = Path.Combine(folder, part.ProcessFile);
        if (!File.Exists(path)) return null;
        var pieces = File.ReadAllText(path).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length != 3 || !string.Equals(pieces[0], manifest.RunId.ToString("N"), StringComparison.Ordinal)
            || !int.TryParse(pieces[1], out var processId) || !long.TryParse(pieces[2], out var startTicks)
            || processId <= 0 || startTicks <= 0)
            throw new InvalidDataException("Annual process identity is invalid or belongs to another run.");
        return new AnnualProcessIdentity(processId, startTicks);
    }

    private static void CheckPath(string path)
    {
        for (DirectoryInfo? current = new(path); current is not null; current = current.Parent)
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Run folders cannot traverse a junction or symbolic link.");
    }
}
