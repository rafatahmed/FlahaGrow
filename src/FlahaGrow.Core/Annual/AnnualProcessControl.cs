using System.Diagnostics;

namespace FlahaGrow.Core.Annual;

/// <summary>Safely terminates only a process whose PID and creation time match a run-owned identity.</summary>
public static class AnnualProcessControl
{
    public static bool TryTerminate(AnnualProcessIdentity identity)
    {
        try
        {
            using var process = Process.GetProcessById(identity.ProcessId);
            if (process.HasExited || process.StartTime.ToUniversalTime().Ticks != identity.StartUtcTicks) return false;
            process.Kill(entireProcessTree: true);
            return true;
        }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
    }
}
