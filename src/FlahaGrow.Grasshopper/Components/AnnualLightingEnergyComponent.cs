using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>
/// Converts a power schedule to energy consumption.
/// </summary>
public sealed class AnnualLightingEnergyComponent : GH_Component
{
    public AnnualLightingEnergyComponent()
        : base("Lighting Energy", "Energy", "Calculates lighting energy use from a power schedule.", "FlahaGrow", "Metrics")
    {
    }

    public override Guid ComponentGuid => new("5f1d58a4-064f-4fc9-b79b-640a380a3e43");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddNumberParameter("Power schedule", "W", "Lighting power for each timestep in watts.", GH_ParamAccess.list);
        parameters.AddNumberParameter("Timestep", "dt", "Duration of each power sample in seconds.", GH_ParamAccess.item, 3600.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddNumberParameter("Energy", "kWh", "Total lighting energy in kilowatt-hours.", GH_ParamAccess.item);
        parameters.AddNumberParameter("Operating hours", "Hours", "Number of schedule hours represented.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var watts = new List<double>();
        var timestepSeconds = 3600.0;
        if (!dataAccess.GetDataList(0, watts))
        {
            return;
        }

        dataAccess.GetData(1, ref timestepSeconds);
        if (timestepSeconds <= 0 || watts.Any(value => value < 0))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Timestep and power values must be zero or greater, with timestep greater than zero.");
            return;
        }

        dataAccess.SetData(0, watts.Sum() * timestepSeconds / 3_600_000.0);
        dataAccess.SetData(1, watts.Count * timestepSeconds / 3600.0);
    }
}
