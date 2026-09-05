using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>
/// Compares daily light integrals with a plant-specific target.
/// </summary>
public sealed class DliTargetComponent : GH_Component
{
    public DliTargetComponent()
        : base("DLI Target", "DLI Target", "Reports daily DLI sufficiency and deficiency against a plant-specific target.", "FlahaGrow", "Metrics")
    {
    }

    public override Guid ComponentGuid => new("0f9f53a1-6fe6-4a4f-ab14-ee19f53c4fdd");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddNumberParameter("Daily DLI", "DLI", "Daily light integral values in mol/m²/day.", GH_ParamAccess.list);
        parameters.AddNumberParameter("Target DLI", "Target", "Plant-specific daily light integral target in mol/m²/day.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddBooleanParameter("Sufficient", "OK", "True where the target DLI is met.", GH_ParamAccess.list);
        parameters.AddNumberParameter("Deficiency", "Deficit", "Additional DLI required in mol/m²/day.", GH_ParamAccess.list);
        parameters.AddNumberParameter("Sufficient days", "Days", "Number of days meeting the target.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var dailyDli = new List<double>();
        var target = 0.0;
        if (!dataAccess.GetDataList(0, dailyDli) || !dataAccess.GetData(1, ref target))
        {
            return;
        }

        if (target < 0 || dailyDli.Any(value => value < 0))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "DLI values and the target must be zero or greater.");
            return;
        }

        var sufficient = dailyDli.Select(value => value >= target).ToList();
        var deficiency = dailyDli.Select(value => Math.Max(0, target - value)).ToList();
        dataAccess.SetDataList(0, sufficient);
        dataAccess.SetDataList(1, deficiency);
        dataAccess.SetData(2, sufficient.Count(value => value));
    }
}
