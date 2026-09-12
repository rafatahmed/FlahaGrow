using FlahaGrow.Core.PlantLight;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>
/// Aggregates one hourly annual PPFD series into daily light integrals.
/// </summary>
public sealed class AnnualDliComponent : FlahaGrowComponent
{

    public AnnualDliComponent()
        : base("Annual DLI", "DLI", "Converts an hourly annual PPFD series into 365 daily light integral values.", "FlahaGrow", "06 DLI")
    {
    }

    public override Guid ComponentGuid => new("f32f1cbd-04b5-42ed-9fdf-c194851011b2");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddNumberParameter("Annual PPFD", "PPFD", "A complete non-leap-year PPFD series in μmol/m²/s; sample count must match Timestep.", GH_ParamAccess.list);
        parameters.AddNumberParameter("Timestep", "dt", "Duration of each sample in seconds.", GH_ParamAccess.item, 3600.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddNumberParameter("Daily DLI", "DLI", "365 daily light integral values in mol/m²/day.", GH_ParamAccess.list);
        parameters.AddNumberParameter("Annual mean DLI", "Mean", "Mean daily light integral in mol/m²/day.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var ppfd = new List<double>();
        var timestepSeconds = 3600.0;
        if (!dataAccess.GetDataList(0, ppfd))
        {
            return;
        }

        dataAccess.GetData(1, ref timestepSeconds);
        try
        {
            var values = PlantLightMath.AnnualDli(ppfd, timestepSeconds);
            dataAccess.SetDataList(0, values);
            dataAccess.SetData(1, values.Average());
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}
