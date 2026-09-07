namespace FlahaGrow.Core.Projects;

/// <summary>Lexical path rules only. A writer must additionally check reparse points before mutation.</summary>
public static class ProjectLayout
{
    public static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.EndsWith('.') || name.EndsWith(' ')
            || name.Any(c => c < 32 || "<>:\"/\\|?*".Contains(c)))
            throw new ArgumentException("Use a nonempty Windows folder name without separators or trailing dots/spaces.");
        var stem = name.Split('.')[0].ToUpperInvariant();
        if (stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$"
            || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal))
                && "123456789¹²³".Contains(stem[3])))
            throw new ArgumentException("The folder name is reserved by Windows.");
    }

    public static string Absolute(string input, string? baseFolder = null)
    {
        if (string.IsNullOrWhiteSpace(input)) throw new ArgumentException("A path is required.");
        if (!Path.IsPathFullyQualified(input))
        {
            if (Path.IsPathRooted(input)) throw new ArgumentException("Drive-relative and root-relative paths are ambiguous.");
            if (baseFolder is null || !Path.IsPathFullyQualified(baseFolder))
                throw new ArgumentException("A relative path requires a saved definition or project base folder.");
            input = Path.Combine(baseFolder, input);
        }
        var full = Path.GetFullPath(input);
        if (full.StartsWith(@"\\?\", StringComparison.Ordinal) || full.StartsWith(@"\\.\", StringComparison.Ordinal))
            throw new ArgumentException("Device paths are not supported.");
        var root = Path.GetPathRoot(full)!;
        foreach (var segment in full[root.Length..].Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries))
            ValidateName(segment);
        return Path.TrimEndingDirectorySeparator(full);
    }

    public static string AnalysisFolder(string projectRoot, string analysisName)
    {
        ValidateName(analysisName);
        return Path.Combine(Absolute(projectRoot), "analyses", analysisName);
    }

    public static string RunsFolder(string projectRoot, string analysisName) =>
        Path.Combine(AnalysisFolder(projectRoot, analysisName), "runs");
}
