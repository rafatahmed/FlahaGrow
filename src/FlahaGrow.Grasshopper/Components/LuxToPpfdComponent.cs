using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>
/// Converts illuminance to PPFD using a user-supplied, spectrum-specific factor.
/// </summary>
public sealed class LuxToPpfdComponent : GH_Component
{
    private const double DefaultConversionFactor = 0.0185;

    public LuxToPpfdComponent()
        : base(
            "Lux to PPFD",
            "Lux→PPFD",
            "Converts illuminance (lux) to PPFD using a spectrum-specific conversion factor.",
            "FlahaGrow",
            "Metrics")
    {
    }

    public override Guid ComponentGuid => new("ac8f8d0f-c1d7-480c-8f37-4fe4c76247aa");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddNumberParameter("Illuminance", "Lux", "Illuminance in lux.", GH_ParamAccess.item);
        parameters.AddNumberParameter(
            "Conversion factor",
            "Factor",
            "Micromoles per square metre per second per lux. Use a value derived from the active light spectrum.",
            GH_ParamAccess.item,
            DefaultConversionFactor);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddNumberParameter("PPFD", "PPFD", "Photosynthetic photon flux density in μmol/m²/s.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        double illuminance = 0;
        double conversionFactor = DefaultConversionFactor;

        if (!dataAccess.GetData(0, ref illuminance))
        {
            return;
        }

        dataAccess.GetData(1, ref conversionFactor);

        if (conversionFactor < 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Conversion factor must be zero or greater.");
            return;
        }

        dataAccess.SetData(0, illuminance * conversionFactor);
    }
}
