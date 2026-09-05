using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Creates the legacy FlahaGrow working-directory layout.</summary>
public sealed class WorkingDirectoryComponent : GH_Component
{
    private static readonly (string Name, string Nickname, string Folder)[] FolderDefinitions =
    {
        ("Point-in-time illuminance", "PIT Ill", "point_in_time_illuminance"),
        ("Point-in-time render", "PIT Render", "point_in_time_render"),
        ("Annual illuminance", "Annual Ill", "annual_illuminance"),
        ("Electric illuminance", "Electric Ill", "electric_illuminance"),
        ("Spectral point-in-time", "Spectral PIT", "spectral_point_in_time"),
        ("Spectral annual illuminance", "Spectral Annual", "spectral_annual_illuminance")
    };

    public WorkingDirectoryComponent()
        : base("Working Directory", "Work Dir", "Creates the FlahaGrow simulation working-directory layout.", "FlahaGrow", "Setup")
    {
    }

    public override Guid ComponentGuid => new("3bc3011e-2b2f-4c14-9344-dcb3554f3722");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddTextParameter("Root folder", "Root", "Writable root folder for the simulation study.", GH_ParamAccess.item);
        foreach (var definition in FolderDefinitions)
        {
            parameters.AddBooleanParameter(definition.Name, definition.Nickname, $"Create the {definition.Folder} subfolder.", GH_ParamAccess.item, false);
        }
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddTextParameter("Root folder", "Root", "Resolved root folder.", GH_ParamAccess.item);
        foreach (var definition in FolderDefinitions)
        {
            parameters.AddTextParameter(definition.Name, definition.Nickname, "Created folder path; null when disabled.", GH_ParamAccess.item);
        }
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        string rootFolder = string.Empty;
        if (!dataAccess.GetData(0, ref rootFolder) || string.IsNullOrWhiteSpace(rootFolder))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Root folder is required.");
            return;
        }

        try
        {
            rootFolder = Path.GetFullPath(rootFolder);
            Directory.CreateDirectory(rootFolder);
            dataAccess.SetData(0, rootFolder);

            for (var index = 0; index < FolderDefinitions.Length; index++)
            {
                var enabled = false;
                dataAccess.GetData(index + 1, ref enabled);
                if (enabled)
                {
                    var path = Path.Combine(rootFolder, FolderDefinitions[index].Folder);
                    Directory.CreateDirectory(path);
                    dataAccess.SetData(index + 1, path);
                }
            }
        }
        catch (Exception exception)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exception.Message);
        }
    }
}
