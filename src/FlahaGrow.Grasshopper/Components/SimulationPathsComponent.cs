using System.Reflection;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>
/// Establishes a portable project workspace and finds the bundled library.
/// </summary>
public sealed class SimulationPathsComponent : GH_Component
{
    public SimulationPathsComponent()
        : base(
            "Simulation Paths",
            "Paths",
            "Creates a project workspace and resolves the FlahaGrow material, glazing, and luminaire library paths.",
            "FlahaGrow",
            "Setup")
    {
    }

    public override Guid ComponentGuid => new("71c6a045-9308-4a0c-9f72-cab76ceefa5c");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddTextParameter("Project folder", "Project", "Writable folder for this simulation study.", GH_ParamAccess.item);
        parameters.AddTextParameter("Library folder", "Library", "Optional folder containing FlahaGrow_Library_Small. Leave empty to use the bundled library.", GH_ParamAccess.item);
        parameters[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddTextParameter("Project folder", "Project", "Resolved simulation project folder.", GH_ParamAccess.item);
        parameters.AddTextParameter("Materials", "Materials", "Radiance opaque-material library folder.", GH_ParamAccess.item);
        parameters.AddTextParameter("Glazing", "Glazing", "Radiance glazing library folder.", GH_ParamAccess.item);
        parameters.AddTextParameter("IES", "IES", "Luminaire IES library folder.", GH_ParamAccess.item);
        parameters.AddTextParameter("Annual results", "Annual", "Folder for annual simulation results.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        string projectFolder = string.Empty;
        string libraryFolder = string.Empty;

        if (!dataAccess.GetData(0, ref projectFolder) || string.IsNullOrWhiteSpace(projectFolder))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Project folder is required.");
            return;
        }

        dataAccess.GetData(1, ref libraryFolder);

        try
        {
            projectFolder = Path.GetFullPath(projectFolder);
            Directory.CreateDirectory(projectFolder);
            var annualResults = Path.Combine(projectFolder, "annual_results");
            Directory.CreateDirectory(annualResults);

            var libraryRoot = string.IsNullOrWhiteSpace(libraryFolder)
                ? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "shared", "Library", "FlahaGrow_Library_Small")
                : Path.GetFullPath(libraryFolder);

            var materials = Path.Combine(libraryRoot, "RadMaterials");
            var glazing = Path.Combine(libraryRoot, "RadGlazing");
            var ies = Path.Combine(libraryRoot, "RadIES");

            if (!Directory.Exists(materials) || !Directory.Exists(glazing) || !Directory.Exists(ies))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Library folder must contain RadMaterials, RadGlazing, and RadIES.");
                return;
            }

            dataAccess.SetData(0, projectFolder);
            dataAccess.SetData(1, materials);
            dataAccess.SetData(2, glazing);
            dataAccess.SetData(3, ies);
            dataAccess.SetData(4, annualResults);
        }
        catch (Exception exception)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exception.Message);
        }
    }
}
