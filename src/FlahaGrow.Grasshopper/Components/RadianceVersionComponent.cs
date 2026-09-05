using System.Diagnostics;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Runs the same rcontrib version check as the legacy component.</summary>
public sealed class RadianceVersionComponent : GH_Component
{
    public RadianceVersionComponent()
        : base("Radiance Version", "Radiance Ver", "Returns the installed Radiance rcontrib version.", "FlahaGrow", "Setup")
    {
    }

    public override Guid ComponentGuid => new("272aa83d-9898-460d-8cbd-7f49374153ba");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddBooleanParameter("Run", "Run", "Run rcontrib -version.", GH_ParamAccess.item, false);
        parameters.AddTextParameter("Radiance bin folder", "Bin", "Optional folder containing rcontrib.exe.", GH_ParamAccess.item);
        parameters[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddTextParameter("Version", "Version", "Radiance version or diagnostic message.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var run = false;
        var binFolder = string.Empty;
        dataAccess.GetData(0, ref run);
        dataAccess.GetData(1, ref binFolder);
        if (!run)
        {
            dataAccess.SetData(0, "Set Run to True to check Radiance.");
            return;
        }

        var executable = string.IsNullOrWhiteSpace(binFolder) ? "rcontrib" : Path.Combine(binFolder, "rcontrib.exe");
        try
        {
            using var process = Process.Start(new ProcessStartInfo(executable, "-version")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
            if (process is null)
            {
                throw new InvalidOperationException("Could not start rcontrib.");
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            var error = process.StandardError.ReadToEnd().Trim();
            process.WaitForExit();
            dataAccess.SetData(0, process.ExitCode == 0 ? output : $"Radiance found but version command failed: {error}");
        }
        catch (Exception exception)
        {
            dataAccess.SetData(0, $"Error: {exception.Message}");
        }
    }
}
