using System.Diagnostics;
using System.Globalization;
using FlahaGrow.Core.Annual;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Radiance;
using FlahaGrow.Core.PlantLight;
using FlahaGrow.Grasshopper.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Calculates a full-output Radiance electric field then expands it by one 8,760-hour dimming schedule.</summary>
public sealed class ElectricAnnualSimulationComponent : FlahaGrowComponent
{
    private readonly ActionLatch runLatch = new();
    private readonly SetupOperation<ElectricResult> operation = new();
    private string? lastFolder;
    private string? lastKey;
    private long publishedRevision = -1;

    private sealed record ElectricResult(IReadOnlyList<double> FullOutput, string Status);

    public ElectricAnnualSimulationComponent() : base("Electric Annual Simulation", "Electric Annual", "Runs a full-output Radiance electric-light calculation and expands it by one validated 8,760-hour dimming schedule.", "FlahaGrow", "04 Electric Light") { }
    public override Guid ComponentGuid => new("b87a6c40-49df-4aef-9ee4-99d5d806bb2d");

    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Project folder", "Project", "Honeybee ModelToRad export root containing model/scene files.", GH_ParamAccess.item);
        p.AddTextParameter("Luminaire Radiance file", "Lum", "Compiled luminaries.rad or one Radiance luminaire file.", GH_ParamAccess.item);
        p.AddNumberParameter("Dimming schedule", "Dim", "Exactly 8,760 hourly fractions in [0,1]. One common schedule controls the supplied luminaire set.", GH_ParamAccess.list);
        p.AddPointParameter("Sensor points", "Pts", "Optional sensor points; otherwise the exported .pts grid is used.", GH_ParamAccess.list); p[3].Optional = true;
        p.AddTextParameter("Detail", "Detail", "low, mid, high, or very high; controls Radiance indirect sampling.", GH_ParamAccess.item, "mid");
        p.AddBooleanParameter("Run", "Run", "Launch the full-output Radiance calculation once on a false→true edge.", GH_ParamAccess.item, false);
        p.AddTextParameter("Radiance bin folder", "Bin", "Optional Radiance bin folder. Leave blank for automatic detection.", GH_ParamAccess.item); p[6].Optional = true;
        p.AddParameter(new RadianceParameter(), "Radiance Environment", "Radiance", "Optional checked environment; when connected it must be ready for electric lighting and supplies exact executables.", GH_ParamAccess.item); p[7].Optional = true;
        p.AddTextParameter("Existing run folder", "Existing", "Optional manifest-owned completed electric annual run to reopen.", GH_ParamAccess.item); p[8].Optional = true;
        p.AddParameter(new AnnualResultParameter(), "Schedule time reference", "Time Result", "Optional Load Annual Result → Result from the daylight study. Connecting declares that the 8760 dimming values use that verified weather calendar/local standard time. Snapshot is retained for automatic timing; no manual UTC.", GH_ParamAccess.item); p[9].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddTextParameter("Result folder", "Folder", "Manifest-owned folder containing annualRfinal_part0.ill.", GH_ParamAccess.item);
        p.AddNumberParameter("Full-output illuminance", "Full Lux", "Radiance full-output illuminance at every sensor before schedule dimming.", GH_ParamAccess.list);
        p.AddTextParameter("Status", "Status", "Preparation, execution, or validation status.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string project = string.Empty, luminaire = string.Empty, detail = "mid", bin = string.Empty, existing = string.Empty; var schedule = new List<double>(); var points = new List<Point3d>(); var run = false; var radiance = new RadianceGoo();
        if (!da.GetData(0, ref project) || !da.GetData(1, ref luminaire) || !da.GetDataList(2, schedule)) return;
        da.GetDataList(3, points); da.GetData(4, ref detail); da.GetData(5, ref run); da.GetData(6, ref bin); da.GetData(7, ref radiance); da.GetData(8, ref existing);
        try
        {
            var timeResult = new AnnualResultGoo(); string? weatherPath = null, weatherHash = null;
            if (da.GetData(9, ref timeResult) && timeResult.IsValid)
            {
                if (timeResult.Value.Weather is null) throw new ArgumentException("Time Result has no verified weather calendar.");
                weatherPath = Path.Combine(Path.GetDirectoryName(timeResult.Value.Result.CachePath)!, "weather.epw");
                weatherHash = AnnualRun.HashFile(weatherPath);
                if (weatherHash != timeResult.Value.Weather.SourceHash) throw new IOException("Time reference EPW changed. Reload its annual result.");
            }
            if (!string.IsNullOrWhiteSpace(existing)) { Emit(da, Path.GetFullPath(existing), "Loaded existing electric annual run."); return; }
            ElectricAnnualMatrix.ValidateSchedule(schedule);
            project = ProjectLayout.Absolute(project); luminaire = Path.GetFullPath(luminaire);
            if (!File.Exists(luminaire)) throw new FileNotFoundException("Luminaire Radiance file was not found.", luminaire);
            var scene = Path.Combine(project, "model", "scene"); var sceneFiles = new[] { Path.Combine(scene, "envelope.rad"), Path.Combine(scene, "envelope.mat") };
            foreach (var file in sceneFiles) if (!File.Exists(file)) throw new FileNotFoundException("Required electric annual source file was not found: " + file);
            var grid = Path.Combine(project, "model", "grid", "0.pts");
            if (points.Count == 0 && !File.Exists(grid)) grid = Directory.Exists(Path.GetDirectoryName(grid)!) ? Directory.EnumerateFiles(Path.GetDirectoryName(grid)!, "*.pts").OrderBy(path => path, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? grid : grid;
            var radiancePoints = points.Count > 0 ? points.Select(point => string.Format(CultureInfo.InvariantCulture, "{0:G17} {1:G17} {2:G17} 0 0 1", point.X, point.Y, point.Z)).ToList() : File.ReadAllLines(grid).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            if (radiancePoints.Count == 0) throw new InvalidDataException("Electric annual sensor grid is empty.");
            var environment = radiance.IsValid ? RadianceExecutionEnvironment.Require(radiance.Value, AnalysisWorkflow.ElectricLighting, bin) : null;
            if (!radiance.IsValid && Params.Input[7].SourceCount > 0) throw new InvalidOperationException("Connected Radiance environment is unresolved.");
            var resolvedBin = environment?.BinFolder ?? FindBin(bin);
            if (resolvedBin is null) throw new DirectoryNotFoundException("Radiance rtrace.exe and oconv.exe were not found. Provide a checked Radiance environment or bin folder.");
            var key = string.Join("|", project, AnnualRun.HashFile(luminaire), string.Join(";", schedule.Select(value => value.ToString("R", CultureInfo.InvariantCulture))), string.Join("\n", radiancePoints), detail.Trim(), resolvedBin, weatherHash ?? "no-weather-reference");
            var launch = runLatch.Observe(run);
            if (string.Equals(key, lastKey, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(lastFolder) && Directory.Exists(lastFolder))
            {
                Publish(da, lastFolder, launch, detail, resolvedBin, environment); return;
            }
            operation.Cancel();
            var runs = Path.Combine(project, "runs"); if (launch) AnnualDiskSpace.Require(runs, ElectricAnnualMatrix.HoursPerNonLeapYear, radiancePoints.Count);
            var hashes = sceneFiles.Append(luminaire).ToDictionary(Path.GetFullPath, AnnualRun.HashFile, StringComparer.OrdinalIgnoreCase);
            if (weatherPath is not null) hashes[weatherPath] = weatherHash!;
            var folder = AnnualRun.Create(runs, project, 1, radiancePoints, hashes, hours: ElectricAnnualMatrix.HoursPerNonLeapYear);
            foreach (var file in sceneFiles) File.Copy(file, Path.Combine(folder, Path.GetFileName(file)));
            File.Copy(luminaire, Path.Combine(folder, "luminaries.rad")); File.WriteAllLines(Path.Combine(folder, "0.pts"), radiancePoints);
            if (weatherPath is not null)
            {
                var snapshot = Path.Combine(folder, "weather.epw"); File.Copy(weatherPath, snapshot);
                if (AnnualRun.HashFile(snapshot) != weatherHash) throw new IOException("Time reference changed during snapshot.");
            }
            File.WriteAllLines(Path.Combine(folder, "electric_dimming_schedule.txt"), schedule.Select(value => value.ToString("G17", CultureInfo.InvariantCulture)));
            lastFolder = folder; lastKey = key;
            if (!launch) { da.SetData(0, folder); da.SetData(2, "Prepared electric annual run. Set Run False then True to execute."); return; }
            Publish(da, folder, true, detail, resolvedBin, environment);
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); da.SetData(2, ex.Message); }
    }

    private void Publish(IGH_DataAccess da, string folder, bool launch, string detail, string bin, RadianceInstallation? environment)
    {
        var manifest = AnnualRun.Read(folder);
        var completed = manifest.Parts.All(part => AnnualPartStatus.Read(folder, manifest, part).Complete);
        if (launch && !completed)
        {
            if (operation.Current is null || operation.Current.IsCompleted)
            {
                var document = OnPingDocument();
                var task = operation.Start(token => Task.Run(() => Execute(folder, manifest, manifest.Sensors, detail, bin, environment, token), token));
                var revision = operation.Revision;
                if (document is not null) _ = task.ContinueWith(_ => Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    if (operation.Revision == revision && OnPingDocument() == document)
                        document.ScheduleSolution(1, _ => ExpireSolution(false));
                })), TaskScheduler.Default);
            }
        }
        da.SetData(0, folder);
        if (operation.Current is { IsCompletedSuccessfully: true } finished && publishedRevision != operation.Revision)
        {
            publishedRevision = operation.Revision;
            var outcome = finished.Result;
            if (outcome.Error is not null) throw new InvalidDataException(outcome.Error);
            if (outcome.Cancelled) { da.SetData(2, "Electric annual calculation cancelled."); return; }
            da.SetDataList(1, outcome.Value!.FullOutput); da.SetData(2, outcome.Value.Status); return;
        }
        da.SetData(2, completed ? "Completed electric annual result. Connect Load Annual Result to build the provenance-bound cache." : operation.Current is { IsCompleted: false } ? "Electric annual calculation is running in the background." : "Prepared electric annual run. Set Run False then True to execute.");
    }

    private static ElectricResult Execute(string folder, AnnualRunManifest manifest, int sensors, string detail, string bin, RadianceInstallation? environment, CancellationToken cancellationToken)
    {
        var part = manifest.Parts[0]; var log = Path.Combine(folder, part.LogFile); var errors = Path.Combine(folder, "annual_errors_part0.log");
        void State(string value)
        {
            foreach (var declared in manifest.Parts)
                File.WriteAllText(Path.Combine(folder, declared.StateFile), manifest.RunId.ToString("N") + " " + value);
        }
        try
        {
            State("Running"); File.WriteAllText(log, "[Part 0] 1/2 Compiling electric scene\n");
            var childEnvironment = environment is null ? null : RadianceStatusService.ChildEnvironment(environment);
            RunOctree(bin, new[] { "envelope.mat", "envelope.rad", "luminaries.rad" }, folder, childEnvironment, errors, cancellationToken);
            File.AppendAllText(log, "[Part 0] 2/2 Calculating full-output electric illuminance\n");
            var detailArgs = Detail(detail); var rgb = Run(bin, "rtrace", new[] { "-I+", "-h", "-n", Math.Max(1, Environment.ProcessorCount).ToString(CultureInfo.InvariantCulture), "-ab", detailArgs.Ab.ToString(CultureInfo.InvariantCulture), "-ad", detailArgs.Ad.ToString(CultureInfo.InvariantCulture), "-lw", detailArgs.Lw, "electric.oct" }, folder, childEnvironment, errors, "0.pts", cancellationToken);
            var values = rgb.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Select(value => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) ? result : throw new InvalidDataException("rtrace returned a nonnumeric RGB value.")).ToArray();
            if (values.Length != checked(sensors * 3)) throw new InvalidDataException($"rtrace returned {values.Length} RGB values; expected {sensors * 3}.");
            var full = Enumerable.Range(0, sensors).Select(index => 47.4 * values[index * 3] + 119.9 * values[index * 3 + 1] + 11.6 * values[index * 3 + 2]).ToList();
            ElectricAnnualMatrix.WriteRun(folder, manifest, full, ReadSchedule(folder, manifest));
            State("CommandsSucceeded");
            AnnualPartStatus.RequireComplete(folder, manifest); File.AppendAllText(log, "[Part 0] Commands succeeded; final annual matrix validated.\n");
            return new ElectricResult(full, "Completed and validated electric annual result. Connect Load Annual Result to build the provenance-bound cache.");
        }
        catch (OperationCanceledException)
        {
            State("Cancelled"); throw;
        }
        catch
        {
            State("Failed ElectricCalculation"); throw;
        }
    }

    // The schedule is stored beside its result so a completed run is self-contained.
    private static IReadOnlyList<double> ReadSchedule(string folder, AnnualRunManifest manifest)
    {
        var path = Path.Combine(folder, "electric_dimming_schedule.txt");
        if (!File.Exists(path)) throw new InvalidDataException("Electric annual schedule snapshot is missing.");
        var values = File.ReadLines(path).Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray(); ElectricAnnualMatrix.ValidateSchedule(values, manifest.Hours); return values;
    }
    private static string Run(string bin, string tool, IEnumerable<string> args, string folder, IReadOnlyDictionary<string, string>? environment, string errors, string? inputFile, CancellationToken cancellationToken)
    {
        var executable = Path.Combine(bin, tool + ".exe"); if (!File.Exists(executable)) throw new FileNotFoundException("Required Radiance executable was not found.", executable);
        var command = new ProcessCommand(executable, args.ToArray(), folder, environment ?? new Dictionary<string, string>(), TimeSpan.FromMinutes(5), 1048576)
        { StandardInput = inputFile is null ? null : File.ReadAllText(Path.Combine(folder, inputFile)) };
        var report = new RadianceProcessRunner().RunAsync(command, cancellationToken).GetAwaiter().GetResult();
        File.AppendAllText(errors, tool + "\n" + report.StandardError + "\n");
        if (report.State == ProcessState.Cancelled) throw new OperationCanceledException(cancellationToken);
        if (report.State != ProcessState.Exited || report.ExitCode != 0 || report.OutputTruncated) throw new InvalidDataException($"Radiance {tool} failed: {report.State}, exit {report.ExitCode}. {report.Diagnostic} {report.StandardError}".Trim());
        return report.StandardOutput;
    }

    /// <summary>oconv writes an octree as binary stdout; never route that stream through text APIs.</summary>
    private static void RunOctree(string bin, IEnumerable<string> args, string folder, IReadOnlyDictionary<string, string>? environment, string errors, CancellationToken cancellationToken)
    {
        var executable = Path.Combine(bin, "oconv.exe"); if (!File.Exists(executable)) throw new FileNotFoundException("Required Radiance executable was not found.", executable);
        var start = new ProcessStartInfo(executable) { WorkingDirectory = folder, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        foreach (var arg in args) start.ArgumentList.Add(arg); if (environment is not null) foreach (var pair in environment) start.Environment[pair.Key] = pair.Value;
        using var process = Process.Start(start) ?? throw new IOException("Could not start Radiance oconv.");
        using var stop = cancellationToken.Register(() => { try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } });
        using var octree = File.Create(Path.Combine(folder, "electric.oct"));
        var output = process.StandardOutput.BaseStream.CopyToAsync(octree, cancellationToken);
        var error = process.StandardError.ReadToEndAsync();
        try { process.WaitForExitAsync(cancellationToken).GetAwaiter().GetResult(); Task.WhenAll(output, error).GetAwaiter().GetResult(); }
        catch (OperationCanceledException) { throw; }
        var stderr = error.GetAwaiter().GetResult(); File.AppendAllText(errors, "oconv\n" + stderr + "\n");
        if (process.ExitCode != 0 || new FileInfo(octree.Name).Length == 0) throw new InvalidDataException($"Radiance oconv failed with exit code {process.ExitCode}: {stderr}");
    }
    private void Emit(IGH_DataAccess da, string folder, string status)
    {
        var manifest = AnnualRun.Read(folder); da.SetData(0, folder); da.SetData(2, status); if (AnnualPartStatus.Read(folder, manifest, manifest.Parts[0]).Complete) da.SetData(2, status + " Completed.");
    }
    private static (int Ab, int Ad, string Lw) Detail(string detail) => detail.Trim().ToLowerInvariant() switch { "low" => (1, 128, "0.01"), "high" => (4, 1024, "0.001"), "very high" => (6, 4096, "0.00025"), _ => (2, 512, "0.002") };
    private static string? FindBin(string requested) =>
        new RadianceDiscovery().FindBin(RadianceRequest.FromSystem() with
        {
            ExplicitLocation = string.IsNullOrWhiteSpace(requested) ? null : requested,
            Workflow = AnalysisWorkflow.ElectricLighting
        }, "oconv", "rtrace");
    public override bool Write(GH_IO.Serialization.GH_IWriter writer) { if (!string.IsNullOrWhiteSpace(lastFolder)) writer.SetString("LastFolder", lastFolder); if (!string.IsNullOrWhiteSpace(lastKey)) writer.SetString("LastKey", lastKey); return base.Write(writer); }
    public override bool Read(GH_IO.Serialization.GH_IReader reader) { lastFolder = reader.ItemExists("LastFolder") ? reader.GetString("LastFolder") : null; lastKey = reader.ItemExists("LastKey") ? reader.GetString("LastKey") : null; runLatch.Disarm(); return base.Read(reader); }
}
