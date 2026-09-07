using System.Globalization;
using FlahaGrow.Core.Projects;

namespace FlahaGrow.Core.Radiance;

public sealed record WorkflowCheck(bool Passed, string Detail);
public interface IRadianceWorkflowCheck
{
    Task<WorkflowCheck> CheckAsync(RadianceInstallation installation, AnalysisWorkflow workflow);
}

/// <summary>Small execution fixtures, isolated from projects and installations.</summary>
public sealed class RadianceWorkflowCheck : IRadianceWorkflowCheck
{
    private readonly IRadianceProcessRunner runner;
    public RadianceWorkflowCheck(IRadianceProcessRunner? runner = null) => this.runner = runner ?? new RadianceProcessRunner();

    public async Task<WorkflowCheck> CheckAsync(RadianceInstallation installation, AnalysisWorkflow workflow)
    {
        var root = Path.Combine(Path.GetTempPath(), "FlahaGrow.Readiness", Guid.NewGuid().ToString("N"));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try
        {
            Directory.CreateDirectory(root);
            async Task<ProcessReport> Run(string tool, params string[] args) => await runner.RunAsync(
                new(installation.Executables[tool], args, root, RadianceStatusService.ChildEnvironment(installation), TimeSpan.FromSeconds(5)), deadline.Token).ConfigureAwait(false);
            static bool Success(ProcessReport report) => report.State == ProcessState.Exited && report.ExitCode == 0 && !report.OutputTruncated;
            static WorkflowCheck Failure(string stage, ProcessReport report) => new(false,
                $"{stage} execution check failed: {report.State}, exit {report.ExitCode}; {report.Diagnostic} {report.StandardError}".Trim());
            if (workflow == AnalysisWorkflow.AnnualDaylight)
            {
                var input = Path.Combine(root, "record.txt");
                await File.WriteAllTextAsync(input, "1\n", deadline.Token).ConfigureAwait(false);
                var result = await Run("rcalc", "-e", "MF:1", "-f", Path.Combine(installation.LibraryFolder, "reinsrc.cal"),
                    "-e", "Rbin=recno", "-o", "${Dx} ${Dy} ${Dz}", input).ConfigureAwait(false);
                if (!Success(result)) return Failure("Sun-direction calculation", result);
                var values = result.StandardOutput.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
                var numbers = values.Select(value => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : double.NaN).ToArray();
                if (numbers.Length != 3 || numbers.Any(n => !double.IsFinite(n)) || Math.Abs(numbers.Sum(n => n * n) - 1) > .001)
                    return new(false, "Sun-direction execution check returned an invalid unit vector.");
                return new(true, "Sun-direction execution check passed. Full annual simulation has not been tested.");
            }
            var ies = Path.Combine(root, "fixture.ies");
            await File.WriteAllTextAsync(ies, "IESNA:LM-63-1995\n[TEST] FlahaGrow readiness\nTILT=NONE\n1 1000 1 3 1 1 2 0 0 0\n1 1 10\n0 90 180\n0\n100 100 100\n", deadline.Token).ConfigureAwait(false);
            var converted = await Run("ies2rad", "-t", "default", "-o", "fixture", ies).ConfigureAwait(false);
            if (!Success(converted)) return Failure("IES conversion", converted);
            var scene = Path.Combine(root, "fixture.rad");
            if (!File.Exists(scene) || new FileInfo(scene).Length == 0) return new(false, "IES conversion produced no scene.");
            var transformed = await Run("xform", "-t", "0", "0", "0", scene).ConfigureAwait(false);
            if (!Success(transformed) || string.IsNullOrWhiteSpace(transformed.StandardOutput)) return Failure("Scene transformation", transformed);
            var transformedScene = Path.Combine(root, "transformed.rad");
            await File.WriteAllTextAsync(transformedScene, transformed.StandardOutput, deadline.Token).ConfigureAwait(false);
            var compiled = await Run("oconv", transformedScene).ConfigureAwait(false);
            if (!Success(compiled) || compiled.StandardOutput.Length == 0) return Failure("Scene compilation", compiled);
            return new(true, "IES conversion, transformation, and scene compilation checks passed. Full lighting simulation has not been tested.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or OperationCanceledException)
        { return new(false, "Workflow execution check could not complete: " + ex.Message); }
        finally
        {
            // Only the unique directory allocated above is removed.
            try
            {
                var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FlahaGrow.Readiness")) + Path.DirectorySeparatorChar;
                if (Path.GetFullPath(root).StartsWith(parent, StringComparison.OrdinalIgnoreCase) && Directory.Exists(root)) Directory.Delete(root, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }
}
