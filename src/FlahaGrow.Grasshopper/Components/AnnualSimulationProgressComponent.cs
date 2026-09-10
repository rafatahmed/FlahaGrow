using Grasshopper.Kernel;
using FlahaGrow.Core.Annual;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Reports the latest stage written by each annual Radiance batch job.</summary>
public sealed class AnnualSimulationProgressComponent : GH_Component
{
    public AnnualSimulationProgressComponent() : base("Annual Simulation Progress", "Annual Progress", "Reads the progress logs written by Annual Simulation. Attach a Grasshopper Timer to Refresh for live updates.", "FlahaGrow", "Annual") { }
    public override Guid ComponentGuid => new("1a2d08d7-6d3e-459e-a2c2-62636cbbaf24");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Result folder", "Folder", "Annual Simulation result folder.", GH_ParamAccess.item);
        p.AddBooleanParameter("Refresh", "Refresh", "Use with a Grasshopper Timer to update while batch jobs run.", GH_ParamAccess.item, true);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddTextParameter("Part progress", "Progress", "Latest stage reported by each part.", GH_ParamAccess.list);
        p.AddIntegerParameter("Completed parts", "Done", "Declared parts whose commands succeeded and final matrix passed validation.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Overall annual-simulation progress.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string folder = string.Empty; var refresh = true;
        if (!da.GetData(0, ref folder)) return; da.GetData(1, ref refresh);
        try
        {
            folder = Path.GetFullPath(folder);
            if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Result folder was not found.");
            var manifest = AnnualRun.Read(folder);
            var reports = manifest.Parts.Select(part => AnnualPartStatus.Read(folder, manifest, part)).ToArray();
            var progress = reports.Select((report, index) => $"Part {index}: {report.State}. {report.Detail}").ToList();
            var completed = reports.Count(report => report.Complete);
            da.SetDataList(0, progress); da.SetData(1, completed);
            da.SetData(2, $"Run {manifest.RunId}: {completed}/{manifest.Parts.Length} parts validated. {string.Join(" | ", progress)}");
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}
