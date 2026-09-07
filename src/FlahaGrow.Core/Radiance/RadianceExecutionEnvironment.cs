using FlahaGrow.Core.Projects;

namespace FlahaGrow.Core.Radiance;

/// <summary>Prevents an execution component from silently substituting a different checked installation.</summary>
public static class RadianceExecutionEnvironment
{
    public static RadianceInstallation Require(RadianceStatus status, AnalysisWorkflow workflow)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (!status.Ready) throw new InvalidOperationException("Connected Radiance environment is not ready.");
        if (status.Workflow != workflow) throw new InvalidOperationException($"Connected Radiance environment was checked for {status.Workflow}, not {workflow}.");
        return status.Installation ?? throw new InvalidOperationException("Ready Radiance environment has no selected installation.");
    }

    public static RadianceInstallation Require(RadianceStatus status, AnalysisWorkflow workflow, string? requestedBin)
    {
        var installation = Require(status, workflow);
        if (!string.IsNullOrWhiteSpace(requestedBin)
            && !string.Equals(ProjectLayout.Absolute(requestedBin), installation.BinFolder, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Radiance bin folder does not match the connected checked environment.");
        return installation;
    }
}
