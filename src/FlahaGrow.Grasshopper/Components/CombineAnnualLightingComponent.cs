using FlahaGrow.Core.Annual;
using FlahaGrow.Core.Operations;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Creates a provenance-owned annual illuminance result by adding completed daylight and electric runs.</summary>
public sealed class CombineAnnualLightingComponent : FlahaGrowComponent
{
    private readonly ActionLatch latch = new();
    private string? lastKey;
    private string? lastFolder;
    public CombineAnnualLightingComponent() : base("Combine Annual Lighting", "Daylight + Electric", "Adds matching completed daylight and electric annual runs into a new manifest-owned result.", "FlahaGrow", "Annual") { }
    public override Guid ComponentGuid => new("9f5f9a71-0ec2-4fb7-a481-49625f0871f2");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Daylight run folder", "Daylight", "Completed manifest-owned annual daylight result.", GH_ParamAccess.item);
        p.AddTextParameter("Electric run folder", "Electric", "Completed manifest-owned electric annual result with identical sensors/hours.", GH_ParamAccess.item);
        p.AddBooleanParameter("Combine", "Run", "Create the combined result once on a false→true edge.", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddTextParameter("Combined run folder", "Folder", "New provenance-owned annual result; connect it to Load Annual Result.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Validation or composition status.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string daylight = string.Empty, electric = string.Empty; var run = false;
        if (!da.GetData(0, ref daylight) || !da.GetData(1, ref electric)) return; da.GetData(2, ref run);
        try
        {
            daylight = Path.GetFullPath(daylight); electric = Path.GetFullPath(electric); var key = daylight + "|" + electric;
            if (!string.Equals(key, lastKey, StringComparison.Ordinal)) { lastKey = key; lastFolder = null; }
            if (latch.Observe(run)) lastFolder = AnnualResultComposition.ComposeRuns(daylight, electric);
            if (lastFolder is null) da.SetData(1, "Validated inputs pending: set Combine False then True.");
            else { da.SetData(0, lastFolder); da.SetData(1, "Combined result validated. Connect Folder to Load Annual Result."); }
        }
        catch (Exception ex) { da.SetData(1, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    public override bool Write(GH_IO.Serialization.GH_IWriter writer) { if (lastFolder is not null) writer.SetString("LastFolder", lastFolder); if (lastKey is not null) writer.SetString("LastKey", lastKey); return base.Write(writer); }
    public override bool Read(GH_IO.Serialization.GH_IReader reader) { lastFolder = reader.ItemExists("LastFolder") ? reader.GetString("LastFolder") : null; lastKey = reader.ItemExists("LastKey") ? reader.GetString("LastKey") : null; latch.Disarm(); return base.Read(reader); }
}
