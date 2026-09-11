using System.Diagnostics;
using System.Globalization;
using FlahaGrow.Core.Annual;
using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Radiance;
using FlahaGrow.Grasshopper.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Prepares and launches the legacy annual daylight Radiance workflow.</summary>
public sealed class AnnualSimulationComponent : FlahaGrowComponent
{
    private readonly FlahaGrow.Core.Operations.ActionLatch runLatch = new();
    private readonly FlahaGrow.Core.Operations.ActionLatch cancelLatch = new();
    private readonly List<Process> activeProcesses = new();
    private string? lastRunFolder;
    private string? lastRunKey;

    public AnnualSimulationComponent() : base("Annual Simulation", "Annual Sim", "Prepares and launches the FlahaGrow annual Radiance daylight simulation.", "FlahaGrow", "Annual") { }
    public override Guid ComponentGuid => new("ca2ce6ef-a0c8-4d98-87a3-2adf2a91ca45");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Project folder", "Project", "Simulation project root containing model/grid and model/scene.", GH_ParamAccess.item);
        p.AddTextParameter("EPW weather file", "EPW", "Weather file for the annual simulation.", GH_ParamAccess.item);
        p.AddIntegerParameter("Sky subdivision", "Sky", "1 for Tregenza or 4 for Reinhart subdivision.", GH_ParamAccess.item, 1);
        p.AddTextParameter("Detail", "Detail", "low, mid, high, very high, or a custom Radiance parameter string.", GH_ParamAccess.item, "mid");
        p.AddBooleanParameter("Run", "Run", "Launch isolated annual jobs in the background. Use Annual Simulation Progress for states and diagnostics.", GH_ParamAccess.item, false);
        p.AddPointParameter("Sensor points", "Pts", "Sensor points written into the isolated run's 0.pts using upward-facing normals. The source grid is preserved.", GH_ParamAccess.list);
        p[5].Optional = true;
        p.AddTextParameter("Radiance bin folder", "Bin", "Optional folder containing Radiance executables. Leave blank for automatic detection.", GH_ParamAccess.item);
        p[6].Optional = true;
        p.AddParameter(new RadianceParameter(), "Radiance Environment", "Radiance", "Optional checked Radiance Status environment. When connected, it must be ready for annual daylight and determines the exact bin and library.", GH_ParamAccess.item);
        p[7].Optional = true;
        p.AddParameter(new AnalysisParameter(), "Analysis", "Analysis", "Optional annual Setup analysis owning the isolated run. Project remains the Honeybee export source.", GH_ParamAccess.item);
        p[8].Optional = true;
        p.AddBooleanParameter("Cancel", "Cancel", "Button: cancel this run's recorded batch processes, including after reopening Rhino. PID and start time are verified to prevent cancelling a reused PID. Run=False does not cancel a launched simulation.", GH_ParamAccess.item, false);
        p.AddTextParameter("Existing run folder", "Existing", "Optional manifest-owned run folder to reopen without preparing or launching a new simulation.", GH_ParamAccess.item);
        p[10].Optional = true;
        p.AddBooleanParameter("Keep intermediates", "Keep", "Keep large reproducible Radiance coefficient, sky, octree, and weather intermediates after successful commands. False keeps final/source-term matrices and frees disk space.", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddTextParameter("Result folder", "Folder", "Folder containing annualRfinal_part*.ill results.", GH_ParamAccess.item); p.AddTextParameter("Batch files", "BAT", "Generated batch-file paths.", GH_ParamAccess.list); p.AddTextParameter("Status", "Status", "Preparation or launch status.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string root = string.Empty, epw = string.Empty, detail = "mid", radianceBin = string.Empty, existing = string.Empty; var sky = 1; var run = false; var cancel = false; var keepIntermediates = false; var radiance = new RadianceGoo();
        var sensorPoints = new List<Point3d>();
        var hasProject = da.GetData(0, ref root);
        var hasEpw = da.GetData(1, ref epw);
        da.GetData(2, ref sky); da.GetData(3, ref detail); da.GetData(4, ref run); da.GetDataList(5, sensorPoints); da.GetData(6, ref radianceBin); da.GetData(7, ref radiance); da.GetData(9, ref cancel); da.GetData(10, ref existing); da.GetData(11, ref keepIntermediates);
        var launch = runLatch.Observe(run);
        var cancelRequested = cancelLatch.Observe(cancel);
        try
        {
            if (cancelRequested)
            {
                var cancelled = CancelActiveProcesses();
                if (EmitExistingRun(da, lastRunFolder, $"Cancellation requested for {cancelled} active annual batch process(es).")) return;
                da.SetData(2, cancelled == 0 ? "No recorded active annual batch process was found for this run." : $"Cancellation requested for {cancelled} verified annual batch process(es).");
                return;
            }
            if (!string.IsNullOrWhiteSpace(existing))
            {
                lastRunFolder = Path.GetFullPath(existing); lastRunKey = null;
                if (EmitExistingRun(da, lastRunFolder, "Loaded existing annual run. Run is ignored while Existing is connected.")) return;
                throw new InvalidDataException("Existing run folder is not a valid manifest-owned annual run.");
            }
            if (!hasProject) { SetMissingInputStatus("Project folder is required unless Existing run folder is connected."); return; }
            if (!hasEpw) { SetMissingInputStatus("EPW weather file is required unless Existing run folder is connected."); return; }
            var subdivision = AnnualSkySubdivision.Validate(sky);
            if (detail.IndexOfAny(new[] { '&', '|', '<', '>', '^', '%', '!', '"', '\r', '\n', '(', ')' }) >= 0)
                throw new ArgumentException("Detail must contain Radiance arguments only; shell syntax is not supported.");
            var verifiedEnvironment = radiance.IsValid ? RadianceExecutionEnvironment.Require(radiance.Value, AnalysisWorkflow.AnnualDaylight, radianceBin) : null;
            if (!radiance.IsValid && Params.Input[7].SourceCount > 0) throw new InvalidOperationException("Connected Radiance environment is unresolved.");
            var analysis = new AnalysisGoo(); da.GetData(8, ref analysis);
            if (!analysis.IsValid && Params.Input[8].SourceCount > 0) throw new InvalidOperationException("Connected analysis is unresolved.");
            if (analysis.IsValid && analysis.Value.AnalysisManifest.Workflow != AnalysisWorkflow.AnnualDaylight)
                throw new InvalidOperationException("The connected analysis is not an annual daylight analysis.");
            if (analysis.IsValid)
            {
                var current = new WorkspaceService().Open(new ResolvedPaths(analysis.Value.Project.Root, null, null, analysis.Value.Project.Manifest), analysis.Value.Analysis.Name);
                if (current.AnalysisManifest.AnalysisId != analysis.Value.AnalysisManifest.AnalysisId)
                    throw new InvalidOperationException("Analysis identity changed. Refresh Setup.");
            }
            var resolvedBin = verifiedEnvironment?.BinFolder ?? FindRadianceBin(radianceBin);
            if (run && resolvedBin is null) throw new DirectoryNotFoundException("Radiance executables were not found. Provide the Radiance bin folder.");
            var radianceLib = verifiedEnvironment?.LibraryFolder ?? (resolvedBin is null ? null : Path.Combine(Directory.GetParent(resolvedBin)!.FullName, "lib"));
            if (run && (radianceLib is null || !File.Exists(Path.Combine(radianceLib, "reinsrc.cal")) || !File.Exists(Path.Combine(radianceLib, "reinhart.cal")))) throw new DirectoryNotFoundException("Selected Radiance calculation library is missing required files.");
            ValidateBatchPath(resolvedBin); ValidateBatchPath(radianceLib);
            if (verifiedEnvironment is not null)
                foreach (var tool in new[] { "rcontrib", "gendaymtx", "oconv", "rfluxmtx", "dctimestep", "rmtxop", "cnt", "rcalc" })
                {
                    if (!verifiedEnvironment.Executables.TryGetValue(tool, out var executable) || !Path.IsPathFullyQualified(executable) || !File.Exists(executable))
                        throw new FileNotFoundException("Checked Radiance executable is unavailable: " + tool + ". Refresh Radiance Status.");
                    ValidateBatchPath(executable);
                }
            root = Path.GetFullPath(root); if (!File.Exists(epw)) throw new FileNotFoundException("EPW weather file was not found.");
            var expectedHours = LadybugWea.AnnualHours;
            var gridFolder = Path.Combine(root, "model", "grid"); var grid = Path.Combine(gridFolder, "0.pts"); var scene = Path.Combine(root, "model", "scene");
            if (sensorPoints.Count == 0 && !File.Exists(grid) && Directory.Exists(gridFolder))
            {
                var sourceGrid = Directory.EnumerateFiles(gridFolder, "*.pts").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
                if (sourceGrid is not null) grid = sourceGrid;
            }
            var sceneFiles = new[] { Path.Combine(scene, "envelope.rad"), Path.Combine(scene, "envelope.mat"), Path.Combine(scene, "envelope.blk") };
            foreach (var file in sceneFiles) if (!File.Exists(file)) throw new FileNotFoundException($"Required annual-simulation file was not found: {file}");
            var points = sensorPoints.Count > 0 ? sensorPoints.Select(ToRadiancePoint).ToList()
                : File.ReadAllLines(grid).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            if (points.Count == 0) throw new InvalidDataException("The sensor grid is empty.");
            var sourceRoot = root;
            var hashes = sceneFiles.Append(epw).ToDictionary(path => Path.GetFullPath(path), AnnualRun.HashFile, StringComparer.OrdinalIgnoreCase);
            var runKey = RunKey(sourceRoot, epw, subdivision, detail, points, resolvedBin, radianceLib);
            if (string.Equals(lastRunKey, runKey, StringComparison.Ordinal) && HasExistingRun(lastRunFolder))
            {
                if (!launch) { EmitExistingRun(da, lastRunFolder, "Reusing the previous prepared or completed run. Set Run False then True to launch a prepared run or create a new run."); return; }
                if (TryLaunchPreparedRun(lastRunFolder!, out var launchedExisting))
                {
                    EmitExistingRun(da, lastRunFolder, $"Launched {launchedExisting} prepared annual job(s). Run=False does not cancel jobs; use Cancel.");
                    return;
                }
            }
            var runContainer = analysis.IsValid ? analysis.Value.RunsFolder : Path.Combine(sourceRoot, "runs");
            if (launch) AnnualDiskSpace.Require(runContainer, expectedHours, points.Count);
            root = AnnualRun.Create(runContainer, sourceRoot,
                subdivision, points, hashes, analysis.IsValid ? analysis.Value.Project.Manifest.ProjectId : null,
                analysis.IsValid ? analysis.Value.AnalysisManifest.AnalysisId : null, expectedHours);
            foreach (var file in sceneFiles) File.Copy(file, Path.Combine(root, Path.GetFileName(file)));
            File.Copy(epw, Path.Combine(root, "weather.epw"));
            LadybugWea.WriteFromEpw(Path.Combine(root, "weather.epw"), Path.Combine(root, "weather.wea"));
            foreach (var file in sceneFiles)
                if (AnnualRun.HashFile(Path.Combine(root, Path.GetFileName(file))) != hashes[Path.GetFullPath(file)])
                    throw new IOException("Scene input changed during preparation. Prepare a new run.");
            if (AnnualRun.HashFile(Path.Combine(root, "weather.epw")) != hashes[Path.GetFullPath(epw)])
                throw new IOException("Weather input changed during preparation. Prepare a new run.");
            File.WriteAllLines(Path.Combine(root, "0.pts"), points);
            // The receiver is a 1-bin ground plus a Reinhart sky. The two source
            // modifier names deliberately do not contain an underscore: Radiance
            // 5.4's rfluxmtx generated the sky basis for both `*_glow` names,
            // yielding 290 rather than 146 columns and breaking dctimestep.
            File.WriteAllText(Path.Combine(root, "skyglow.rad"), $"#@rfluxmtx u=+Y h=u\nvoid glow groundglow\n0\n0\n4 1 1 1 0\ngroundglow source ground\n0\n0\n4 0 0 -1 180\n#@rfluxmtx u=+Y {AnnualSkySubdivision.ReceiverDirective(subdivision)}\nvoid glow skyglow\n0\n0\n4 1 1 1 0\nskyglow source sky\n0\n0\n4 0 0 1 180\n");
            var split = points.Count > 10; var partCount = split ? 4 : 1; var batches = new List<string>();
            var runManifest = AnnualRun.Read(root);
            lastRunFolder = root; lastRunKey = runKey;
            var cpu = Math.Max(1, Environment.ProcessorCount); var parameters = Detail(detail, cpu); var directSun = DirectSun(detail);
            var perPartCpu = split ? Math.Max(1, cpu / 4) : cpu;
            parameters = SetThreadCount(parameters, perPartCpu);
            for (var part = 0; part < partCount; part++)
            {
                // Preserve the legacy Python ordering: each part owns a contiguous sensor block.
                var partPoints = split ? ContiguousPart(points, part, partCount) : points;
                var pointFile = split ? $"0_part{part}.pts" : "0.pts";
                if (split) File.WriteAllLines(Path.Combine(root, pointFile), partPoints);
                var batch = Path.Combine(root, split ? $"run_part{part}.bat" : "run_annual_single.bat"); var weather = "weather.wea";
                File.WriteAllLines(batch, AnnualBatch.Build(Commands(weather, pointFile, partPoints.Count, subdivision, parameters, directSun, perPartCpu, part, resolvedBin, radianceLib, verifiedEnvironment), runManifest.RunId, part, keepIntermediates));
                batches.Add(batch);
            }
            var launched = launch ? Launch(root, runManifest, batches) : 0;
            da.SetData(0, root); da.SetDataList(1, batches); da.SetData(2, launch ? $"Launched {launched}/{batches.Count} annual jobs. Run=False does not cancel jobs; use Cancel. Check Annual Simulation Progress for validated completion." : $"Prepared {batches.Count} batch file(s). Set Run False then True to launch exactly once.");
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private void SetMissingInputStatus(string message)
    {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
        Params.Output[2].ClearData();
        Params.Output[2].AddVolatileData(new global::Grasshopper.Kernel.Data.GH_Path(0), 0, new global::Grasshopper.Kernel.Types.GH_String(message));
    }
    private bool HasExistingRun(string? folder)
    {
        try { return !string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder) && AnnualRun.Read(folder) is not null; }
        catch (Exception) { return false; }
    }
    private bool EmitExistingRun(IGH_DataAccess da, string? folder, string status)
    {
        if (!HasExistingRun(folder)) return false;
        var manifest = AnnualRun.Read(folder!);
        var batches = BatchPaths(folder!, manifest);
        da.SetData(0, folder); da.SetDataList(1, batches); da.SetData(2, status + " Folder: " + folder);
        return true;
    }
    private static List<string> BatchPaths(string folder, AnnualRunManifest manifest) => manifest.Parts.Select(part =>
        Path.Combine(folder, manifest.Parts.Length == 1 ? "run_annual_single.bat" : $"run_part{part.Index}.bat")).ToList();
    private bool TryLaunchPreparedRun(string folder, out int launched)
    {
        launched = 0;
        try
        {
            var manifest = AnnualRun.Read(folder);
            if (manifest.Parts.Any(part => AnnualPartStatus.Read(folder, manifest, part).State != "Prepared")) return false;
            launched = Launch(folder, manifest, BatchPaths(folder, manifest));
            return launched > 0;
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); return false; }
    }
    private int Launch(string folder, AnnualRunManifest manifest, IReadOnlyList<string> batches)
    {
        RemoveExitedProcesses(); var launched = 0;
        for (var part = 0; part < batches.Count; part++)
        {
            var batch = batches[part];
            try
            {
                var process = Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), $"/d /c {Path.GetFileName(batch)}")
                    { WorkingDirectory = folder, UseShellExecute = false, CreateNoWindow = true }) ?? throw new IOException("Could not launch annual job.");
                activeProcesses.Add(process); launched++;
                AnnualRun.WriteProcessIdentity(folder, manifest, manifest.Parts[part],
                    new AnnualProcessIdentity(process.Id, process.StartTime.ToUniversalTime().Ticks));
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(folder, manifest.Parts[part].StateFile), manifest.RunId.ToString("N") + " Failed launch");
                File.WriteAllText(Path.Combine(folder, manifest.Parts[part].LogFile), ex.Message);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Part {part} failed to launch: {ex.Message}");
            }
        }
        return launched;
    }
    private int CancelActiveProcesses()
    {
        var cancelled = 0;
        var cancelledPids = new HashSet<int>();
        foreach (var process in activeProcesses.ToArray())
            try { if (!process.HasExited) { process.Kill(entireProcessTree: true); cancelled++; cancelledPids.Add(process.Id); } }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not cancel annual process: " + ex.Message); }
        RemoveExitedProcesses();
        if (HasExistingRun(lastRunFolder))
        {
            var manifest = AnnualRun.Read(lastRunFolder!);
            foreach (var part in manifest.Parts)
            {
                var identity = AnnualRun.ReadProcessIdentity(lastRunFolder!, manifest, part);
                if (identity is null || cancelledPids.Contains(identity.ProcessId)) continue;
                try { if (AnnualProcessControl.TryTerminate(identity)) { cancelled++; cancelledPids.Add(identity.ProcessId); } }
                catch (System.ComponentModel.Win32Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not cancel persisted annual process: " + ex.Message); }
            }
            foreach (var part in manifest.Parts)
            {
                var identity = AnnualRun.ReadProcessIdentity(lastRunFolder!, manifest, part);
                if (identity is null || !cancelledPids.Contains(identity.ProcessId)) continue;
                var state = Path.Combine(lastRunFolder!, part.StateFile);
                if (File.Exists(state) && AnnualPartStatus.Read(lastRunFolder!, manifest, part).Complete) continue;
                File.WriteAllText(state, manifest.RunId.ToString("N") + " Cancelled");
            }
        }
        return cancelled;
    }
    private void RemoveExitedProcesses()
    {
        for (var index = activeProcesses.Count - 1; index >= 0; index--)
        {
            var process = activeProcesses[index];
            try { if (!process.HasExited) continue; }
            catch (InvalidOperationException) { }
            process.Dispose(); activeProcesses.RemoveAt(index);
        }
    }
    private static string RunKey(string sourceRoot, string epw, int sky, string detail, IReadOnlyList<string> points, string? bin, string? lib) =>
        string.Join("|", Path.GetFullPath(sourceRoot), AnnualRun.HashFile(epw), sky.ToString(CultureInfo.InvariantCulture), detail.Trim(),
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", points)))), bin ?? string.Empty, lib ?? string.Empty);
    public override bool Write(GH_IWriter writer)
    {
        if (!string.IsNullOrWhiteSpace(lastRunFolder)) writer.SetString("LastRunFolder", lastRunFolder);
        if (!string.IsNullOrWhiteSpace(lastRunKey)) writer.SetString("LastRunKey", lastRunKey);
        return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
        lastRunFolder = reader.ItemExists("LastRunFolder") ? reader.GetString("LastRunFolder") : null;
        lastRunKey = reader.ItemExists("LastRunKey") ? reader.GetString("LastRunKey") : null;
        runLatch.Disarm(); cancelLatch.Disarm();
        return base.Read(reader);
    }
    private static string ToRadiancePoint(Point3d point) => string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17} 0 0 1", point.X, point.Y, point.Z);
    private static List<string> ContiguousPart(List<string> points, int part, int partCount)
    {
        var baseCount = points.Count / partCount;
        var remainder = points.Count % partCount;
        var count = baseCount + (part < remainder ? 1 : 0);
        var start = part * baseCount + Math.Min(part, remainder);
        return points.GetRange(start, count);
    }
    private static string? FindRadianceBin(string requestedFolder)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(requestedFolder)) candidates.Add(requestedFolder);
        candidates.AddRange((Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries));
        candidates.Add(@"C:\Program Files\ladybug_tools\radiance\bin");
        candidates.Add(@"C:\Radiance\bin");
        return candidates.Select(path => path.Trim().Trim('"')).FirstOrDefault(path => File.Exists(Path.Combine(path, "rfluxmtx.exe")) && File.Exists(Path.Combine(path, "rmtxop.exe")));
    }
    private static string Detail(string detail, int cpu) => detail.Contains('-') ? detail : detail.Trim().ToLowerInvariant() switch { "1" or "very low" => $"-lw .01 -ab 1 -ad 256 -n {cpu}", "2" or "low" => $"-lw .005 -ab 2 -ad 512 -n {cpu}", "4" or "high" => $"-lw .0015 -ab 3 -ad 1536 -n {cpu}", "5" or "very high" => $"-lw .001 -ab 3 -ad 2048 -n {cpu}", _ => $"-lw .002 -ab 2 -ad 1024 -n {cpu}" };
    private static string DirectParameters(string parameters)
    {
        // Five-phase direct subtraction is a direct-only black-scene matrix. Never
        // inherit the total calculation's interreflection depth (or add one bounce)
        // because that can make the term being subtracted exceed the total matrix.
        var tokens = parameters.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
        for (var i = tokens.Count - 1; i >= 0; i--)
            if (tokens[i] == "-ab")
            {
                if (i + 1 >= tokens.Count) throw new ArgumentException("Missing value for -ab.");
                tokens.RemoveRange(i, 2);
            }
        return string.Join(" ", tokens) + " -ab 0";
    }
    private static string SetThreadCount(string parameters, int threads)
    {
        var tokens = parameters.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
        var position = tokens.FindIndex(token => string.Equals(token, "-n", StringComparison.OrdinalIgnoreCase));
        if (position >= 0 && position + 1 < tokens.Count) tokens[position + 1] = threads.ToString(CultureInfo.InvariantCulture);
        else { tokens.Add("-n"); tokens.Add(threads.ToString(CultureInfo.InvariantCulture)); }
        return string.Join(" ", tokens);
    }
    private static DirectSunSettings DirectSun(string detail) => detail.Trim().ToLowerInvariant() switch
    {
        "4" or "high" => new DirectSunSettings(5, 3625, 1, "1e-3"),
        "5" or "very high" => new DirectSunSettings(6, 5221, 1, "1e-3"),
        "3" or "mid" => new DirectSunSettings(4, 2321, 1, "1e-3"),
        "2" or "low" => new DirectSunSettings(3, 1297, 1, "5e-3"),
        _ => new DirectSunSettings(2, 577, 1, "1e-2")
    };
    private static void ValidateBatchPath(string? path)
    {
        if (path is not null && path.IndexOfAny(new[] { '%', '!', '"', '\r', '\n' }) >= 0)
            throw new ArgumentException("Radiance paths containing batch expansion characters are unsupported.");
    }
    private static IEnumerable<string> Commands(string epw, string pts, int sensors, int sky, string detail, DirectSunSettings directSun, int threads, int part, string? radianceBin, string? radianceLib, RadianceInstallation? installation)
    {
        var lines = new List<string> { "@echo off", "setlocal DisableDelayedExpansion", string.IsNullOrWhiteSpace(radianceBin) ? "" : $"set \"PATH={radianceBin};%PATH%\"", string.IsNullOrWhiteSpace(radianceLib) ? "" : $"set \"RAYPATH=.;{radianceLib}\"", $"echo Annual simulation part {part} > annual_progress_part{part}.log" };
        AddStage(lines, part, "1/8 Sky matrix"); lines.Add($"gendaymtx -m {sky} {epw} > Weather_{part}.smx");
        AddStage(lines, part, "2/8 Total daylight coefficients"); lines.Add($"oconv envelope.mat envelope.rad > amodel_{part}.oct"); lines.Add($"rfluxmtx -I+ -y {sensors} {detail} - skyglow.rad -i amodel_{part}.oct < {pts} > illum_part{part}.mtx");
        AddStage(lines, part, "3/8 Total daylight annual matrix"); lines.Add($"dctimestep illum_part{part}.mtx Weather_{part}.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualR_part{part}.ill");
        AddStage(lines, part, "4/8 Direct daylight coefficients"); lines.Add($"oconv envelope.blk envelope.rad > bmodel_{part}.oct"); lines.Add($"rfluxmtx -I+ -y {sensors} {DirectParameters(detail)} - skyglow.rad -i bmodel_{part}.oct < {pts} > billum_part{part}.mtx");
        AddStage(lines, part, "5/8 Direct daylight annual matrix"); lines.Add($"gendaymtx -m {sky} -d {epw} > Weatherd_{part}.smx"); lines.Add($"dctimestep billum_part{part}.mtx Weatherd_{part}.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualRd_part{part}.ill");
        AddStage(lines, part, "6/8 Direct-sun coefficients"); lines.Add($"echo void light solar 0 0 3 1e6 1e6 1e6 > suns_{part}.rad"); lines.Add($"cnt {directSun.Count} | rcalc -e MF:{directSun.Mf} -f reinsrc.cal -e Rbin=recno -o \"solar source sun 0 0 4 ${{Dx}} ${{Dy}} ${{Dz}} 0.533\" >> suns_{part}.rad"); lines.Add($"oconv -f envelope.blk envelope.rad suns_{part}.rad > sunCoefficientsDDS_{part}.oct"); lines.Add($"rcontrib -I+ -ab {directSun.Ab} -y {sensors} -n {threads} -ad 64 -lw {directSun.Lw} -dc 1 -dt 0 -dj 0 -fa -e MF:{directSun.Mf} -f reinhart.cal -b rbin -bn Nrbins -m solar sunCoefficientsDDS_{part}.oct < {pts} > cdsDDS_part{part}.mtx");
        AddStage(lines, part, "7/8 Direct-sun annual matrix"); lines.Add($"gendaymtx -5 0.533 -d -m {directSun.Mf} {epw} > WeathersunM{directSun.Mf}_{part}.smx"); lines.Add($"dctimestep cdsDDS_part{part}.mtx WeathersunM{directSun.Mf}_{part}.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualRs_part{part}.ill");
        AddStage(lines, part, "8/8 Combining final annual illuminance"); lines.Add($"rmtxop -fa annualR_part{part}.ill + -s -1 annualRd_part{part}.ill + annualRs_part{part}.ill > annualRfinal_part{part}.ill");
        if (installation is not null)
            return lines.Select(line => System.Text.RegularExpressions.Regex.Replace(line,
                @"(?<=^|\| )(gendaymtx|oconv|rfluxmtx|dctimestep|rmtxop|cnt|rcalc|rcontrib)(?= )",
                match => "\"" + installation.Executables[match.Value] + "\""));
        return lines;
    }
    private static void AddStage(List<string> lines, int part, string message)
    {
        var text = $"[Part {part}] {message}";
        lines.Add($"echo {text}");
        lines.Add($"echo {text}>> annual_progress_part{part}.log");
    }
    private sealed record DirectSunSettings(int Mf, int Count, int Ab, string Lw);
}
