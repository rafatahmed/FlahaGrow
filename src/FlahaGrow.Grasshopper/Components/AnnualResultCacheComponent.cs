using FlahaGrow.Core.Annual;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.PlantLight;
using FlahaGrow.Grasshopper.Components.Setup;
using FlahaGrow.Grasshopper.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

public sealed class AnnualResultCacheComponent : AsyncSetupComponent<LoadedAnnualResult>
{
    private readonly ActionLatch buildLatch = new();
    private volatile string progress = "";
    public bool IsLoading => Status == "Working…";
    public AnnualResultCacheComponent() : base("Load Annual Result", "Load Result", "Validates or builds annual caches in the background. Reuses valid caches; exposes verified run weather.", "03 Annual") { }
    public override Guid ComponentGuid => new("0e5f7114-fbb9-4a77-a3f4-40ccd0c0c258");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Result folder", "Folder", "Annual Simulation → Folder.", GH_ParamAccess.item);
        p.AddBooleanParameter("Build", "Build", "Button: validate/reuse a cache, or build if missing/invalid. Held True does not rebuild each solve.", GH_ParamAccess.item, false);
        p.AddBooleanParameter("Cancel", "Cancel", "Cancel pending work. Click Build again to retry.", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddTextParameter("Result cache", "F32", "Compatibility cache path.", GH_ParamAccess.item);
        p.AddIntegerParameter("Sensors", "S", "Sensor count.", GH_ParamAccess.item);
        p.AddIntegerParameter("Hours", "H", "Hour count.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Background progress, completion and weather status.", GH_ParamAccess.item);
        p.AddParameter(new AnnualResultParameter(), "Annual Result", "Result", "Connect to Hour Index.Result and Plant Light Context.Result. Includes weather; no manual UTC needed.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string folder = ""; bool build = false, cancel = false;
        if (!da.GetData(0, ref folder)) { Invalidate(); return; }
        da.GetData(1, ref build); da.GetData(2, ref cancel);
        var requested = buildLatch.Observe(build);
        if (cancel) { Invalidate(); da.SetData(3, "Cancelled. Release Cancel and click Build to retry."); return; }
        try
        {
            folder = Path.GetFullPath(folder);
            var changed = Changed(folder);
            Update(folder, changed || requested, token => Task.FromResult(AnnualCacheLoader.Load(folder, build, token, value => progress = value)), "Waiting for cache.");
            da.SetData(3, Status == "Working…" ? progress.Length == 0 ? Status : progress : Status);
            if (Status == "Working…")
            {
                OnPingDocument()?.ScheduleSolution(250, doc => { if (OnPingDocument() == doc) ExpireSolution(false); });
            }
            if (Result is null) return;
            da.SetData(0, Result.Result.CachePath); da.SetData(1, Result.Result.Sensors); da.SetData(2, Result.Result.Hours);
            da.SetData(3, "Ready; verified cache reused or built. " + Result.WeatherStatus);
            da.SetData(4, new AnnualResultGoo(Result));
        }
        catch (Exception ex) { Invalidate(); da.SetData(3, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    public override bool Read(GH_IReader reader) { buildLatch.Disarm(); Invalidate(); return base.Read(reader); }
}
