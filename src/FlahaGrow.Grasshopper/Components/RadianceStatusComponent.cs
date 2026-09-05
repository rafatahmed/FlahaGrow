using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>
/// Finds the Radiance rcontrib executable without relying on a machine-specific path.
/// </summary>
public sealed class RadianceStatusComponent : GH_Component
{
    public RadianceStatusComponent()
        : base("Radiance Status", "Radiance", "Checks whether the Radiance rcontrib executable can be found.", "FlahaGrow", "Setup")
    {
    }

    public override Guid ComponentGuid => new("f6f1d5d4-9a1a-4de7-a090-6299c94e0060");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddTextParameter("Radiance bin folder", "Bin", "Optional Radiance executable folder. Leave empty to search PATH.", GH_ParamAccess.item);
        parameters[0].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddBooleanParameter("Available", "OK", "True when rcontrib.exe is found.", GH_ParamAccess.item);
        parameters.AddTextParameter("rcontrib path", "rcontrib", "Resolved rcontrib executable path.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        string binFolder = string.Empty;
        dataAccess.GetData(0, ref binFolder);

        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(binFolder))
        {
            candidates.Add(Path.Combine(binFolder, "rcontrib.exe"));
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        candidates.AddRange(pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(folder => Path.Combine(folder.Trim(), "rcontrib.exe")));

        var executable = candidates.FirstOrDefault(File.Exists) ?? string.Empty;
        var available = !string.IsNullOrEmpty(executable);
        if (!available)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Radiance was not found. Provide its bin folder or add it to PATH.");
        }

        dataAccess.SetData(0, available);
        dataAccess.SetData(1, executable);
    }
}
