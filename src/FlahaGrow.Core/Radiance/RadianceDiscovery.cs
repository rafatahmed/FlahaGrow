using System.Collections.ObjectModel;
using FlahaGrow.Core.Projects;

namespace FlahaGrow.Core.Radiance;

public enum RadianceState { NotFound, Located, Incomplete, Ready, Failed, TimedOut }
public sealed record RadianceRequest
{
    public string? ExplicitLocation { get; init; }
    public string? ProjectRadianceLocation { get; init; }
    public string? LibraryOverride { get; init; }
    public AnalysisWorkflow Workflow { get; init; } = AnalysisWorkflow.AnnualDaylight;
    public IReadOnlyList<string> SearchPaths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> StandaloneLocations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> LadybugLocations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> KnownLocations { get; init; } = Array.Empty<string>();

    public static RadianceRequest FromSystem() => new()
    {
        SearchPaths = (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries),
        StandaloneLocations = new[]
        {
            @"C:\Radiance",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Radiance"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Radiance")
        },
        LadybugLocations = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ladybug_tools", "radiance"),
            @"C:\ladybug_tools\radiance",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "ladybug_tools", "radiance")
        }
    };
}

public sealed record RadianceInstallation(string BinFolder, string LibraryFolder,
    IReadOnlyDictionary<string, string> Executables, IReadOnlyList<string> Missing, string Fingerprint)
{
    public string Source { get; init; } = "Configured";
}
public sealed record RadianceDiscoveryResult(RadianceInstallation? Selected, IReadOnlyList<RadianceInstallation> Candidates,
    IReadOnlyList<string> Diagnostics);

public interface IRadianceFiles
{
    bool DirectoryExists(string path);
    string? Fingerprint(string path);
}

public sealed class RadianceFiles : IRadianceFiles
{
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public string? Fingerprint(string path)
    {
        var file = new FileInfo(path);
        return file.Exists ? $"{file.Length}:{file.LastWriteTimeUtc.Ticks}" : null;
    }
}

/// <summary>Bounded existence checks only; never enumerates a disk or launches a process.</summary>
public sealed class RadianceDiscovery
{
    public const int MaximumCandidates = 128;
    private readonly IRadianceFiles files;
    public RadianceDiscovery(IRadianceFiles? files = null) => this.files = files ?? new RadianceFiles();

    public RadianceDiscoveryResult Discover(RadianceRequest request)
    {
        var diagnostics = new List<string>();
        var candidates = new List<RadianceInstallation>();
        var locations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var supplied = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configured = request.ExplicitLocation ?? request.ProjectRadianceLocation;
        if (!Enum.IsDefined(typeof(AnalysisWorkflow), request.Workflow))
            return new(null, candidates.AsReadOnly(), new[] { "Unknown Radiance workflow." });
        var sources = configured is null
            ? request.StandaloneLocations.Select(path => (Path: path, Source: "Standalone"))
                .Concat(request.LadybugLocations.Select(path => (Path: path, Source: "Ladybug Tools")))
                .Concat(request.KnownLocations.Select(path => (Path: path, Source: "Known location")))
                .Concat(request.SearchPaths.Select(path => (Path: path, Source: "PATH")))
            : new[] { (Path: configured, Source: request.ExplicitLocation is null ? "Project setting" : "Custom") };
        var inspected = 0;
        foreach (var source in sources)
        {
            try
            {
                var path = ProjectLayout.Absolute(source.Path.Trim().Trim('"'));
                if (!supplied.Add(path)) continue;
                if (inspected++ >= MaximumCandidates) { diagnostics.Add("Discovery limit reached (128 unique locations); additional locations were not inspected. This does not invalidate the selected installation."); break; }
                var bin = files.DirectoryExists(Path.Combine(path, "bin")) ? Path.Combine(path, "bin") : path;
                if (!locations.Add(bin)) continue;
                if (!files.DirectoryExists(bin))
                {
                    if (configured is not null) diagnostics.Add("Configured Radiance folder does not exist; automatic fallback is disabled.");
                    continue;
                }
                var library = request.LibraryOverride is null
                    ? Path.Combine(Path.GetDirectoryName(bin) ?? bin, "lib") : ProjectLayout.Absolute(request.LibraryOverride);
                var installation = Inspect(bin, library, request.Workflow) with { Source = source.Source };
                if (configured is not null || installation.Executables.Count > 0) candidates.Add(installation);
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException or NotSupportedException)
            { diagnostics.Add(ex.Message); }
        }
        // Automatic selection can skip incomplete installations, but always retains one coherent bin/lib pair.
        var selected = configured is null ? candidates.FirstOrDefault(candidate => candidate.Missing.Count == 0) ?? candidates.FirstOrDefault() : candidates.FirstOrDefault();
        if (configured is null && selected is not null)
            foreach (var skipped in candidates.TakeWhile(candidate => !ReferenceEquals(candidate, selected)))
                diagnostics.Add($"Skipped incomplete {skipped.Source} installation at {skipped.BinFolder}: missing {string.Join(", ", skipped.Missing)}.");
        if (selected is null && diagnostics.Count == 0)
            diagnostics.Add("Radiance was not found in the standard standalone, Ladybug Tools, or PATH locations. Install Radiance or choose a custom location.");
        return new(selected, candidates.AsReadOnly(), diagnostics.AsReadOnly());
    }

    private RadianceInstallation Inspect(string bin, string library, AnalysisWorkflow workflow)
    {
        var tools = workflow switch
        {
            AnalysisWorkflow.AnnualDaylight => new[] { "rcontrib", "epw2wea", "gendaymtx", "oconv", "rfluxmtx", "dctimestep", "rmtxop", "cnt", "rcalc" },
            AnalysisWorkflow.ElectricLighting => new[] { "rcontrib", "ies2rad", "xform", "oconv" },
            _ => throw new ArgumentException("Unknown Radiance workflow.")
        };
        var calculations = workflow == AnalysisWorkflow.AnnualDaylight
            ? new[] { "reinsrc.cal", "reinhart.cal" } : new[] { "source.cal", "lamp.tab" };
        var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();
        var fingerprint = new List<string> { bin.ToUpperInvariant(), library.ToUpperInvariant(), workflow.ToString() };
        foreach (var tool in tools)
        {
            var path = Path.Combine(bin, tool + ".exe");
            var identity = files.Fingerprint(path);
            fingerprint.Add(tool + "=" + identity);
            if (identity is null) missing.Add(tool + ".exe"); else found.Add(tool, path);
        }
        foreach (var file in calculations)
        {
            var identity = files.Fingerprint(Path.Combine(library, file));
            fingerprint.Add(file + "=" + identity);
            if (identity is null) missing.Add(file);
        }
        return new(bin, library, new ReadOnlyDictionary<string, string>(found), missing.AsReadOnly(), string.Join("|", fingerprint));
    }
}
