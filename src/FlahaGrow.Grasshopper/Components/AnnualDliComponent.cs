using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>
/// Aggregates one hourly annual PPFD series into daily light integrals.
/// </summary>
public sealed class AnnualDliComponent : GH_Component
{
    private const int HoursPerYear = 8760;
    private const int DaysPerYear = 365;
    private const int HoursPerDay = 24;

    public AnnualDliComponent()
        : base("Annual DLI", "DLI", "Converts an hourly annual PPFD series into 365 daily light integral values.", "FlahaGrow", "Metrics")
    {
    }

    public override Guid ComponentGuid => new("f32f1cbd-04b5-42ed-9fdf-c194851011b2");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddNumberParameter("Hourly PPFD", "PPFD", "Exactly 8,760 hourly PPFD values in μmol/m²/s.", GH_ParamAccess.list);
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
        if (ppfd.Count != HoursPerYear)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Hourly PPFD must contain exactly 8,760 values.");
            return;
        }

        if (timestepSeconds <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Timestep must be greater than zero.");
            return;
        }

        if (ppfd.Any(value => value < 0))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Hourly PPFD values must be zero or greater.");
            return;
        }

        var dailyDli = new List<double>(DaysPerYear);
        for (var day = 0; day < DaysPerYear; day++)
        {
            var dailyPpfdSeconds = ppfd.Skip(day * HoursPerDay).Take(HoursPerDay).Sum() * timestepSeconds;
            dailyDli.Add(dailyPpfdSeconds / 1_000_000.0);
        }

        dataAccess.SetDataList(0, dailyDli);
        dataAccess.SetData(1, dailyDli.Average());
    }
}
