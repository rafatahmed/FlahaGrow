using System.Diagnostics;
using System.Globalization;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Prepares and launches the legacy annual daylight Radiance workflow.</summary>
public sealed class AnnualSimulationComponent : GH_Component
{
    public AnnualSimulationComponent() : base("Annual Simulation", "Annual Sim", "Prepares and launches the FlahaGrow annual Radiance daylight simulation.", "FlahaGrow", "Annual") { }
    public override Guid ComponentGuid => new("ca2ce6ef-a0c8-4d98-87a3-2adf2a91ca45");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Project folder", "Project", "Simulation project root containing model/grid and model/scene.", GH_ParamAccess.item);
        p.AddTextParameter("EPW weather file", "EPW", "Weather file for the annual simulation.", GH_ParamAccess.item);
        p.AddIntegerParameter("Sky subdivision", "Sky", "1 for Tregenza or 4 for Reinhart subdivision.", GH_ParamAccess.item, 1);
        p.AddTextParameter("Detail", "Detail", "low, mid, high, very high, or a custom Radiance parameter string.", GH_ParamAccess.item, "mid");
        p.AddBooleanParameter("Run", "Run", "Launch generated batch files in command windows.", GH_ParamAccess.item, false);
        p.AddPointParameter("Sensor points", "Pts", "Sensor points from Ladybug Tools 'Generate Point Grid'. They are written to model/grid/0.pts using upward-facing normals.", GH_ParamAccess.list);
        p[5].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddTextParameter("Result folder", "Folder", "Folder containing annualRfinal_part*.ill results.", GH_ParamAccess.item); p.AddTextParameter("Batch files", "BAT", "Generated batch-file paths.", GH_ParamAccess.list); p.AddTextParameter("Status", "Status", "Preparation or launch status.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string root = string.Empty, epw = string.Empty, detail = "mid"; var sky = 1; var run = false;
        var sensorPoints = new List<Point3d>();
        if (!da.GetData(0, ref root)) { SetMissingInputStatus("Project folder is required."); return; }
        if (!da.GetData(1, ref epw)) { SetMissingInputStatus("EPW weather file is required."); return; }
        da.GetData(2, ref sky); da.GetData(3, ref detail); da.GetData(4, ref run); da.GetDataList(5, sensorPoints);
        try
        {
            root = Path.GetFullPath(root); if (!File.Exists(epw)) throw new FileNotFoundException("EPW weather file was not found.");
            var gridFolder = Path.Combine(root, "model", "grid"); var grid = Path.Combine(gridFolder, "0.pts"); var scene = Path.Combine(root, "model", "scene");
            Directory.CreateDirectory(gridFolder);
            if (sensorPoints.Count > 0) File.WriteAllLines(grid, sensorPoints.Select(ToRadiancePoint));
            else if (!File.Exists(grid))
            {
                // Match the legacy Python component: ModelToRad may name its only sensor grid differently.
                var sourceGrid = Directory.EnumerateFiles(gridFolder, "*.pts").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if (sourceGrid is not null) File.Move(sourceGrid, grid);
            }
            foreach (var file in new[] { grid, Path.Combine(scene, "envelope.rad"), Path.Combine(scene, "envelope.mat"), Path.Combine(scene, "envelope.blk") }) if (!File.Exists(file)) throw new FileNotFoundException($"Required annual-simulation file was not found: {file}");
            Directory.CreateDirectory(root); File.Copy(epw, Path.Combine(root, Path.GetFileName(epw)), true); foreach (var file in new[] { grid, Path.Combine(scene, "envelope.rad"), Path.Combine(scene, "envelope.mat"), Path.Combine(scene, "envelope.blk") }) File.Copy(file, Path.Combine(root, Path.GetFileName(file)), true);
            File.WriteAllText(Path.Combine(root, "skyglow.rad"), "#@rfluxmtx u=+Y h=u\nvoid glow ground_glow\n0\n0\n4 1 1 1 0\nground_glow source ground\n0\n0\n4 0 0 -1 180\n#@rfluxmtx u=+Y h=r1\nvoid glow sky_glow\n0\n0\n4 1 1 1 0\nsky_glow source sky\n0\n0\n4 0 0 1 180\n");
            var points = File.ReadAllLines(Path.Combine(root, "0.pts")).Where(line => !string.IsNullOrWhiteSpace(line)).ToList(); if (points.Count == 0) throw new InvalidDataException("0.pts is empty.");
            var split = points.Count > 10; var partCount = split ? 4 : 1; var batches = new List<string>(); var parameters = Detail(detail, Environment.ProcessorCount);
            for (var part = 0; part < partCount; part++) { var partPoints = split ? points.Where((_, index) => index % partCount == part).ToList() : points; var pointFile = split ? $"0_part{part}.pts" : "0.pts"; if (split) File.WriteAllLines(Path.Combine(root, pointFile), partPoints); var batch = Path.Combine(root, split ? $"run_part{part}.bat" : "run_annual_single.bat"); var weather = Path.GetFileName(epw); File.WriteAllLines(batch, Commands(weather, pointFile, partPoints.Count, sky == 4 ? 4 : 1, parameters, part)); batches.Add(batch); if (run) Process.Start(new ProcessStartInfo("cmd.exe", $"/c start cmd /c \"{Path.GetFileName(batch)}\"") { WorkingDirectory = root, UseShellExecute = true }); }
            da.SetData(0, root); da.SetDataList(1, batches); da.SetData(2, run ? $"Launched {batches.Count} annual simulation job(s)." : $"Prepared {batches.Count} batch file(s). Set Run True to launch.");
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private void SetMissingInputStatus(string message)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
        Params.Output[2].ClearData();
        Params.Output[2].AddVolatileData(new global::Grasshopper.Kernel.Data.GH_Path(0), 0, new global::Grasshopper.Kernel.Types.GH_String(message));
    }
    private static string ToRadiancePoint(Point3d point) => string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17} 0 0 1", point.X, point.Y, point.Z);
    private static string Detail(string detail, int cpu) => detail.Contains('-') ? detail : detail.Trim().ToLowerInvariant() switch { "low" => $"-lw .005 -ab 2 -ad 512 -n {cpu}", "high" => $"-lw .0015 -ab 3 -ad 1536 -n {cpu}", "very high" => $"-lw .001 -ab 3 -ad 2048 -n {cpu}", _ => $"-lw .002 -ab 2 -ad 1024 -n {cpu}" };
    private static IEnumerable<string> Commands(string epw, string pts, int sensors, int sky, string detail, int part) => new[] { "@echo off", $"epw2wea \"{epw}\" Weather_{part}.wea", $"gendaymtx -m {sky} Weather_{part}.wea > Weather_{part}.smx", "oconv envelope.mat envelope.rad > amodel.oct", $"rfluxmtx -I+ -y {sensors} {detail} - skyglow.rad -i amodel.oct < {pts} > illum_{part}.mtx", $"dctimestep illum_{part}.mtx Weather_{part}.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualRfinal_part{part}.ill" };
}
