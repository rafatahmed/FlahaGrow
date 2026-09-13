using System.Globalization;

namespace FlahaGrow.Core.Radiance;

public sealed record IesConversionRequest(string IesPath, string OutputFolder, string OutputStem, string Executable,
    IReadOnlyDictionary<string, string> Environment, double R, double G, double B, double? Multiplier, string? DatOverride);
public sealed record IesConversionResult(string RadianceFile, IReadOnlyList<string> DataFiles, ProcessReport Process);

/// <summary>Convert in an isolated directory; only publish validated, freshly generated outputs.</summary>
public sealed class IesConversionService
{
    private readonly IRadianceProcessRunner runner;
    public IesConversionService(IRadianceProcessRunner? runner = null) => this.runner = runner ?? new RadianceProcessRunner();

    public async Task<IesConversionResult> ConvertAsync(IesConversionRequest request, CancellationToken token = default)
    {
        var ies = Path.GetFullPath(request.IesPath);
        if (!File.Exists(ies)) throw new FileNotFoundException("IES file was not found.", ies);
        var folder = Path.GetFullPath(request.OutputFolder);
        if (string.IsNullOrWhiteSpace(request.OutputStem) || request.OutputStem is "." or ".."
            || request.OutputStem.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-' or '.')))
            throw new ArgumentException("Invalid luminaire output name.");
        var rgb = LuminaireOutput.Normalize(request.R, request.G, request.B);
        if (request.Multiplier is { } multiplier && (!double.IsFinite(multiplier) || multiplier <= 0))
            throw new ArgumentException("Multiplier must be finite and positive when supplied.");
        var dat = string.IsNullOrWhiteSpace(request.DatOverride) ? null : Path.GetFullPath(request.DatOverride);
        if (dat is not null && !File.Exists(dat)) throw new FileNotFoundException("DAT override was not found.", dat);
        token.ThrowIfCancellationRequested();
        using var sourceLock = new FileStream(ies, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var datLock = dat is null ? null : new FileStream(dat, FileMode.Open, FileAccess.Read, FileShare.Read);
        Directory.CreateDirectory(folder);
        using var outputLock = new FileStream(Path.Combine(folder, "." + request.OutputStem + ".conversion.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose);
        var staging = Path.Combine(folder, ".conversion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(staging);
        try
        {
            var args = new List<string> { "-o", request.OutputStem, "-t", "default" };
            if (request.Multiplier is { } value) { args.Add("-m"); args.Add(value.ToString("G17", CultureInfo.InvariantCulture)); }
            args.Add(ies);
            var report = await runner.RunAsync(new(request.Executable, args, staging, request.Environment, TimeSpan.FromMinutes(2)), token).ConfigureAwait(false);
            if (report.State != ProcessState.Exited || report.ExitCode != 0)
                throw new InvalidOperationException($"ies2rad {report.State} (exit {report.ExitCode}): {report.StandardError} {report.Diagnostic}");
            var generated = Path.Combine(staging, request.OutputStem + ".rad");
            if (!File.Exists(generated)) throw new FileNotFoundException("ies2rad did not produce the expected new Radiance file.", generated);
            var rewritten = LuminaireOutput.Rewrite(File.ReadAllText(generated), rgb.R, rgb.G, rgb.B, dat, folder);
            if (dat is null)
                foreach (System.Text.RegularExpressions.Match reference in System.Text.RegularExpressions.Regex.Matches(rewritten,
                    "\"([^\"\r\n]+\\.dat)\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    if (!File.Exists(Path.Combine(staging, Path.GetFileName(reference.Groups[1].Value))))
                        throw new FileNotFoundException("Generated output references a missing fresh DAT file.", reference.Groups[1].Value);
            File.WriteAllText(generated, rewritten);
            var dataFiles = dat is null ? Directory.GetFiles(staging, "*.dat") : Array.Empty<string>();
            token.ThrowIfCancellationRequested();
            // Each move is atomic. Publication of the whole RAD/DAT set is not a multi-file transaction.
            foreach (var source in dataFiles) File.Move(source, Path.Combine(folder, Path.GetFileName(source)), true);
            var output = Path.Combine(folder, Path.GetFileName(generated));
            File.Move(generated, output, true);
            return new(output, dataFiles.Select(p => Path.Combine(folder, Path.GetFileName(p))).ToArray(), report);
        }
        finally
        {
            if (Path.GetDirectoryName(Path.GetFullPath(staging)) != folder) throw new InvalidOperationException("Conversion cleanup escaped its output directory.");
            if (Directory.Exists(staging)) Directory.Delete(staging, recursive: true);
        }
    }
}
