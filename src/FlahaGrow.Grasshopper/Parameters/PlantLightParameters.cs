using FlahaGrow.Core.PlantLight;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace FlahaGrow.Grasshopper.Parameters;

public sealed class AnnualResultGoo : SetupGoo<LoadedAnnualResult>
{
    public AnnualResultGoo() { }
    public AnnualResultGoo(LoadedAnnualResult value) : base(value) { }
    public override string TypeName => "Annual Result";
    public override string TypeDescription => "Validated cache identity with verified run weather when available.";
    public override string ToString() => Value is null ? "Unresolved annual result" : $"{Value.Result.RunId}; {Value.Weather?.Location ?? "weather unavailable"}";
    public override IGH_Goo Duplicate() => Value is null ? new AnnualResultGoo() : new AnnualResultGoo(Value);
}
public sealed class AnnualResultParameter : GH_Param<AnnualResultGoo>
{
    public AnnualResultParameter() : base("Annual Result", "Result", "Load Annual Result → Result.", "FlahaGrow", "03 Annual", GH_ParamAccess.item) { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa403");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
}

public sealed class PlotAttributesGoo : SetupGoo<PlotAttributes>
{
    public PlotAttributesGoo() { }
    public PlotAttributesGoo(PlotAttributes value) : base(value) { }
    public override string TypeName => "Plot Attributes";
    public override string TypeDescription => "Quantity, units, temporal shape, selection, provenance and data identity.";
    public override string ToString() => Value?.Title ?? "Unresolved plot attributes";
    public override IGH_Goo Duplicate() => Value is null ? new PlotAttributesGoo() : new PlotAttributesGoo(Value);
}
public sealed class PlotAttributesParameter : GH_Param<PlotAttributesGoo>
{
    public PlotAttributesParameter() : base("Plot Attributes", "Plot", "Reader → Plot; connect its matching numeric data too.", "FlahaGrow", "03 Annual", GH_ParamAccess.item) { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa404");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
}

// Deliberately transient. Upstream components reconstruct from saved input
// configuration after reopen; trusted cache validation is never serialized.
public sealed class SpectralProfileGoo : SetupGoo<SpectralProfile>
{
    public SpectralProfileGoo() { }
    public SpectralProfileGoo(SpectralProfile value) : base(value) { }
    public override string TypeName => "Spectral Profile";
    public override string TypeDescription => "Explicit PAR photon-to-lux assumption and provenance.";
    public override string ToString() => Value is null ? "Unresolved profile" : $"{Value.Label}: {Value.Factor:G9} µmol/m²/s per lux";
    public override IGH_Goo Duplicate() => Value is null ? new SpectralProfileGoo() : new SpectralProfileGoo(Value);
}
public sealed class PlantLightContextGoo : SetupGoo<PlantLightContext>
{
    public PlantLightContextGoo() { }
    public PlantLightContextGoo(PlantLightContext value) : base(value) { }
    public override string TypeName => "Plant Light Context";
    public override string TypeDescription => "Manifest-owned illuminance with per-source spectral assumptions.";
    public override string ToString() => Value?.Description ?? "Unresolved plant-light context";
    public override IGH_Goo Duplicate() => Value is null ? new PlantLightContextGoo() : new PlantLightContextGoo(Value);
}
public sealed class SpectralProfileParameter : GH_Param<SpectralProfileGoo>
{
    public SpectralProfileParameter() : base("Spectral Profile", "Profile", "Explicit spectrum-specific conversion.", "FlahaGrow", "02 Spectral", GH_ParamAccess.item) { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa401");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
}
public sealed class PlantLightContextParameter : GH_Param<PlantLightContextGoo>
{
    public PlantLightContextParameter() : base("Plant Light Context", "Context", "Source-specific plant-light analysis.", "FlahaGrow", "05 PPFD", GH_ParamAccess.item) { }
    public override Guid ComponentGuid => new("a9c4973b-acb7-45be-96ee-a6d8a35fa402");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
}
