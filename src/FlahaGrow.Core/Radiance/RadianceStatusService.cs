using System.Collections.ObjectModel;
using FlahaGrow.Core.Projects;

namespace FlahaGrow.Core.Radiance;

public sealed record RadianceStatus(RadianceState State, RadianceInstallation? Installation, string? Version,
    ProcessReport? Probe, IReadOnlyList<string> Diagnostics)
{
    public bool Ready => State == RadianceState.Ready;
    public AnalysisWorkflow Workflow { get; init; }
    public IReadOnlyList<RadianceInstallation> Candidates { get; init; } = Array.Empty<RadianceInstallation>();
}

/// <summary>Instance-scoped bounded cache. Discovery is repeated; unchanged probes are shared.</summary>
public sealed class RadianceStatusService
{
    private readonly RadianceDiscovery discovery;
    private readonly IRadianceProcessRunner runner;
    private readonly IRadianceWorkflowCheck workflowCheck;
    private readonly Dictionary<string, Task<RadianceStatus>> checks = new(StringComparer.Ordinal);
    private readonly object gate = new();
    private readonly SemaphoreSlim discoverySlots = new(4);
    private const int CacheLimit = 32;
    public const int MaximumVersionProbes = 4;

    public RadianceStatusService(RadianceDiscovery? discovery = null, IRadianceProcessRunner? runner = null, IRadianceWorkflowCheck? workflowCheck = null)
    { this.discovery = discovery ?? new(); this.runner = runner ?? new RadianceProcessRunner(); this.workflowCheck = workflowCheck ?? new RadianceWorkflowCheck(this.runner); }

    public async Task<RadianceStatus> CheckAsync(RadianceRequest request, bool refresh = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // File checks may block on network storage; keep them away from the host UI thread.
        RadianceDiscoveryResult result;
        await discoverySlots.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { result = await Task.Run(() => discovery.Discover(request), cancellationToken).ConfigureAwait(false); }
        finally { discoverySlots.Release(); }
        var installation = result.Selected;
        if (installation is null) return new(RadianceState.NotFound, null, null, null, result.Diagnostics) { Workflow = request.Workflow, Candidates = result.Candidates };
        if (installation.Missing.Count > 0) return new(RadianceState.Incomplete, installation, null, null,
            Array.AsReadOnly(result.Diagnostics.Concat(installation.Missing.Select(file => "Missing: " + file)).ToArray())) { Workflow = request.Workflow, Candidates = result.Candidates };
        var configured = request.ExplicitLocation ?? request.ProjectRadianceLocation;
        var candidates = configured is null ? result.Candidates.Where(candidate => candidate.Missing.Count == 0) : new[] { installation };
        var diagnostics = result.Diagnostics.ToList();
        RadianceStatus? last = null;
        var attempts = 0;
        foreach (var candidate in candidates)
        {
            if (attempts++ >= MaximumVersionProbes)
            {
                diagnostics.Add("Automatic version check limit reached; select an installation from Found to check it explicitly.");
                break;
            }
            cancellationToken.ThrowIfCancellationRequested();
            last = await CheckInstallationAsync(candidate, request.Workflow, refresh, cancellationToken).ConfigureAwait(false);
            last = last with { Installation = candidate };
            if (last.Ready)
                return last with { Installation = candidate, Workflow = request.Workflow, Candidates = result.Candidates,
                    Diagnostics = Array.AsReadOnly(diagnostics.Concat(last.Diagnostics).ToArray()) };
            diagnostics.Add($"Readiness check failed at {candidate.BinFolder}: {string.Join("; ", last.Diagnostics)}");
        }
        return last! with { Workflow = request.Workflow, Candidates = result.Candidates, Diagnostics = diagnostics.AsReadOnly() };
    }

    private async Task<RadianceStatus> CheckInstallationAsync(RadianceInstallation installation, AnalysisWorkflow workflow, bool refresh, CancellationToken cancellationToken)
    {
        Task<RadianceStatus> check;
        lock (gate)
        {
            var key = installation.Fingerprint;
            if (checks.TryGetValue(key, out var existing) && (!refresh || !existing.IsCompleted)) check = existing;
            else
            {
                if (!checks.ContainsKey(key) && checks.Count >= CacheLimit)
                {
                    var completed = checks.FirstOrDefault(pair => pair.Value.IsCompleted);
                    if (completed.Key is null) throw new InvalidOperationException("Radiance check limit reached; wait for an active check to finish.");
                    checks.Remove(completed.Key);
                }
                check = ProbeAsync(installation, workflow);
                checks[key] = check;
            }
        }
        try
        {
            // Caller cancellation abandons this wait, not a probe shared by other callers. Probes have their own deadline.
            var status = await check.WaitAsync(cancellationToken).ConfigureAwait(false);
            if (!status.Ready) RemoveCheck(installation.Fingerprint, check);
            return status;
        }
        catch
        {
            if (check.IsFaulted || check.IsCanceled) RemoveCheck(installation.Fingerprint, check);
            throw;
        }
    }

    private void RemoveCheck(string key, Task<RadianceStatus> check)
    {
        lock (gate) if (checks.TryGetValue(key, out var current) && ReferenceEquals(current, check)) checks.Remove(key);
    }

    public static IReadOnlyDictionary<string, string> ChildEnvironment(RadianceInstallation installation)
    {
        return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
        {
            ["PATH"] = installation.BinFolder + Path.PathSeparator + (Environment.GetEnvironmentVariable("PATH") ?? ""),
            ["RAYPATH"] = "." + Path.PathSeparator + installation.LibraryFolder
        });
    }

    private async Task<RadianceStatus> ProbeAsync(RadianceInstallation installation, AnalysisWorkflow workflow)
    {
        var report = await runner.RunAsync(new(installation.Executables["rcontrib"], new[] { "-version" }, installation.BinFolder,
            ChildEnvironment(installation), TimeSpan.FromSeconds(5))).ConfigureAwait(false);
        var version = string.Join("\n", new[] { report.StandardOutput.Trim(), report.StandardError.Trim() }.Where(value => value.Length > 0));
        var state = report.State == ProcessState.TimedOut ? RadianceState.TimedOut
            : report.State == ProcessState.Exited && report.ExitCode == 0 && version.Length > 0 ? RadianceState.Ready : RadianceState.Failed;
        var diagnostics = new List<string>();
        if (report.Diagnostic is not null) diagnostics.Add(report.Diagnostic);
        if (state != RadianceState.Ready) diagnostics.Add($"Version check did not succeed: {report.State}, exit {report.ExitCode?.ToString() ?? "unknown"}.");
        if (report.OutputTruncated) diagnostics.Add("Version output was truncated.");
        if (state == RadianceState.Ready)
        {
            var execution = await workflowCheck.CheckAsync(installation, workflow).ConfigureAwait(false);
            diagnostics.Add(execution.Detail);
            if (!execution.Passed) state = RadianceState.Failed;
        }
        return new(state, installation, version.Length > 0 ? version : null, report, diagnostics.AsReadOnly());
    }
}
