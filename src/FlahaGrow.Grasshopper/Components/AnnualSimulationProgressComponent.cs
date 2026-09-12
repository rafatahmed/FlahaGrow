using Grasshopper.Kernel;
using FlahaGrow.Core.Annual;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Reports the latest stage written by each annual Radiance batch job.</summary>
public sealed class AnnualSimulationProgressComponent : FlahaGrowComponent
{
    public AnnualSimulationProgressComponent() : base("Annual Simulation Progress", "Annual Progress", "Reads the progress logs written by Annual Simulation. Attach a Grasshopper Timer to Refresh for live updates.", "FlahaGrow", "03 Annual") { }
    public override Guid ComponentGuid => new("1a2d08d7-6d3e-459e-a2c2-62636cbbaf24");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Result folder", "Folder", "Annual Simulation result folder.", GH_ParamAccess.item);
        p.AddBooleanParameter("Refresh", "Refresh", "Use with a Grasshopper Timer to update while batch jobs run.", GH_ParamAccess.item, true);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddTextParameter("Part progress", "Progress", "Lifecycle state, latest batch stage, and log update time for each declared part.", GH_ParamAccess.list);
        p.AddIntegerParameter("Completed parts", "Done", "Declared parts whose commands succeeded and final matrix passed validation.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Overall annual-simulation progress.", GH_ParamAccess.item);
        p.AddNumberParameter("Stage coverage", "%", "Completed stage coverage across declared parts. This is stage-based, not an elapsed-time estimate.", GH_ParamAccess.item);
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
            var stages = manifest.Parts.Select((part, index) => ReadStage(folder, part, reports[index])).ToArray();
            var progress = reports.Select((report, index) => $"Part {index}: {report.State}. {stages[index].Text} {report.Detail}").ToList();
            var completed = reports.Count(report => report.Complete);
            var finishedStages = reports.Select((report, index) => FinishedStages(report, stages[index])).Sum();
            var totalStages = manifest.Parts.Length * 8;
            var coverage = totalStages == 0 ? 0 : finishedStages * 100d / totalStages;
            da.SetDataList(0, progress); da.SetData(1, completed); da.SetData(3, coverage);
            da.SetData(2, $"Run {manifest.RunId}: {completed}/{manifest.Parts.Length} parts validated; {finishedStages}/{totalStages} stages complete ({coverage.ToString("0.#", CultureInfo.InvariantCulture)}%, stage-based). {string.Join(" | ", progress)}");
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private static int FinishedStages(AnnualPartReport report, StageReport stage)
    {
        // A stage is written immediately before its command. A running/failed/cancelled
        // part has therefore only completed the preceding stage. CommandsSucceeded is
        // written only after all eight commands, even if matrix validation then fails.
        if (report.Complete || stage.CommandsSucceeded) return 8;
        return Math.Max(0, stage.Stage - 1);
    }
    private static StageReport ReadStage(string folder, AnnualRunPart part, AnnualPartReport report)
    {
        if (report.Complete) return new(8, true, "Stage 8/8 complete.");
        var path = Path.Combine(folder, part.LogFile);
        if (!File.Exists(path)) return new(0, false, "No batch stage has been logged.");
        try
        {
            var lines = File.ReadLines(path).ToArray();
            var commandSucceeded = lines.Any(value => Regex.IsMatch(value, "^\\[Part \\d+\\] Commands succeeded"));
            var line = lines.Reverse().FirstOrDefault(value => Regex.IsMatch(value, "^\\[Part \\d+\\] \\d+/8 "));
            if (line is null) return new(0, commandSucceeded, "No batch stage has been logged.");
            var match = Regex.Match(line, "^\\[Part \\d+\\] (?<stage>[1-8])/8 (?<detail>.+)$");
            if (!match.Success) return new(0, commandSucceeded, "Latest batch stage is unreadable.");
            var updated = File.GetLastWriteTime(path).ToString("HH:mm:ss", CultureInfo.InvariantCulture);
            return new(int.Parse(match.Groups["stage"].Value, CultureInfo.InvariantCulture), commandSucceeded, $"Stage {match.Groups["stage"].Value}/8: {match.Groups["detail"].Value} (log {updated}).");
        }
        catch (IOException) { return new(0, false, "Progress log is being updated."); }
        catch (UnauthorizedAccessException) { return new(0, false, "Progress log cannot be read."); }
    }
    private sealed record StageReport(int Stage, bool CommandsSucceeded, string Text);
}
