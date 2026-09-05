using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Writes generated xform placement lines into the legacy luminaries.rad file.</summary>
public sealed class CompileLuminariesComponent : GH_Component
{
    public CompileLuminariesComponent() : base("Compile Luminaires", "Compile Lights", "Writes luminaries.rad in the project Luminaire_files folder.", "FlahaGrow", "Electric Light") { }
    public override Guid ComponentGuid => new("31b19b55-7384-4f37-bb1a-f436c3cbaa8b");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddTextParameter("Lighting geometry", "xform", "Radiance xform placement lines.", GH_ParamAccess.list);
        parameters.AddTextParameter("Project folder", "Project", "FlahaGrow project folder.", GH_ParamAccess.item);
        parameters.AddBooleanParameter("Write", "Write", "Write luminaries.rad.", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddTextParameter("Luminaire Radiance file", "Rad", "Path to luminaries.rad.", GH_ParamAccess.item);
        parameters.AddTextParameter("Status", "Status", "Write status.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var lines = new List<string>(); var project = string.Empty; var write = false;
        if (!dataAccess.GetDataList(0, lines) || !dataAccess.GetData(1, ref project)) return;
        dataAccess.GetData(2, ref write);
        if (!write) { dataAccess.SetData(1, "Set Write to True to generate luminaries.rad."); return; }
        try
        {
            if (lines.Count == 0) throw new ArgumentException("No xform lines were supplied.");
            project = Path.GetFullPath(project);
            var folder = Path.Combine(Directory.GetParent(project)?.FullName ?? project, "Luminaire_files");
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($"Luminaire_files folder was not found: {folder}");
            var output = Path.Combine(folder, "luminaries.rad");
            File.WriteAllLines(output, new[] { "# Auto-generated luminaire placement file", "# Created by FlahaGrow", string.Empty }.Concat(lines));
            dataAccess.SetData(0, output);
            dataAccess.SetData(1, $"Wrote {lines.Count} line(s) to {output}");
        }
        catch (Exception exception) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exception.Message); }
    }
}
