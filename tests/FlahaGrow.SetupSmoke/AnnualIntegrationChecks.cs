using System.Reflection;
using System.Globalization;
using FlahaGrow.Core.Annual;
using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Radiance;
using FlahaGrow.Grasshopper.Components;
using FlahaGrow.Grasshopper.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;

internal static class AnnualIntegrationChecks
{
    public static void Run(string testRoot, string? radianceBin = null)
    {
        // Separate from the workspace used by the Setup smoke checks.
        var root = Path.Combine(testRoot, "integration");
        var source = Path.Combine(root, "source");
        Directory.CreateDirectory(Path.Combine(source, "model", "scene"));
        Directory.CreateDirectory(Path.Combine(source, "model", "grid"));
        foreach (var name in new[] { "envelope.rad", "envelope.mat", "envelope.blk" })
            File.WriteAllText(Path.Combine(source, "model", "scene", name), "# fixture");
        var grid = Path.Combine(source, "model", "grid", "original.pts");
        File.WriteAllText(grid, "0 0 0 0 0 1");
        var weather = Path.Combine(source, "weather.epw");
        File.WriteAllLines(weather, Enumerable.Repeat("fixture header", 8).Concat(Enumerable.Repeat(string.Join(",", Enumerable.Repeat("1", 35)), 2)));
        var bin = Path.Combine(root, "checked bin"); Directory.CreateDirectory(bin);
        var tools = new[] { "rcontrib", "epw2wea", "gendaymtx", "oconv", "rfluxmtx", "dctimestep", "rmtxop", "cnt", "rcalc" }
            .ToDictionary(name => name, name => Path.Combine(bin, name + ".exe"));
        foreach (var path in tools.Values) File.WriteAllText(path, "never executed");
        var lib = Path.Combine(root, "separate library"); Directory.CreateDirectory(lib);
        var installation = new RadianceInstallation(bin, lib, tools, Array.Empty<string>(), "fixture");
        var status = new RadianceStatus(RadianceState.Ready, installation, "fixture", null, Array.Empty<string>()) { Workflow = AnalysisWorkflow.AnnualDaylight };
        Dictionary<int, object> Inputs(int sky, int sensors) => new()
        {
            [0] = source, [1] = weather, [2] = sky, [4] = false,
            [5] = Enumerable.Range(0, sensors).Select(i => new Point3d(i, 0, 0)).ToList(), [7] = new RadianceGoo(status)
        };
        var invalid = Inputs(4, 12); invalid[7] = new RadianceGoo(status with { State = RadianceState.Failed });
        var rejected = Solve(new AnnualSimulationComponent(), invalid);
        if (rejected.Outputs.ContainsKey(0) || Directory.Exists(Path.Combine(source, "runs"))) throw new Exception("Not-ready environment wrote annual preparation.");
        if (File.ReadAllText(grid) != "0 0 0 0 0 1") throw new Exception("Rejected preparation changed source grid.");
        foreach (var workflow in new[] { AnalysisWorkflow.ElectricLighting })
        {
            var wrongWorkflow = Inputs(1, 1); wrongWorkflow[7] = new RadianceGoo(status with { Workflow = workflow });
            if (Solve(new AnnualSimulationComponent(), wrongWorkflow).Outputs.ContainsKey(0) || Directory.Exists(Path.Combine(source, "runs")))
                throw new Exception("Wrong workflow wrote preparation.");
        }
        var wrongBin = Inputs(1, 1); wrongBin[6] = Path.Combine(root, "another installation");
        if (Solve(new AnnualSimulationComponent(), wrongBin).Outputs.ContainsKey(0) || Directory.Exists(Path.Combine(source, "runs"))) throw new Exception("Bin mismatch wrote preparation.");
        var invalidSky = Inputs(2, 1);
        if (Solve(new AnnualSimulationComponent(), invalidSky).Outputs.ContainsKey(0) || Directory.Exists(Path.Combine(source, "runs"))) throw new Exception("Invalid sky wrote preparation.");
        var first = Solve(new AnnualSimulationComponent(), Inputs(4, 12));
        foreach (var (numeric, named) in new[] { ("1", "very low"), ("2", "low"), ("3", "mid"), ("4", "high"), ("5", "very high") })
        {
            var method = typeof(AnnualSimulationComponent).GetMethod("Detail", BindingFlags.NonPublic | BindingFlags.Static)!;
            if (!Equals(method.Invoke(null, new object[] { numeric, 8 }), method.Invoke(null, new object[] { named, 8 })))
                throw new Exception("Numeric/named quality diverged: " + numeric);
        }
        var firstFolder = (string)first.Outputs[0]!;
        var second = Solve(new AnnualSimulationComponent(), Inputs(1, 1));
        var secondFolder = (string)second.Outputs[0]!;
        if (firstFolder == secondFolder || AnnualRun.Read(firstFolder).Parts.Length != 4 || AnnualRun.Read(secondFolder).Parts.Length != 1)
            throw new Exception("Annual runs are not isolated.");
        foreach (var (folder, sky) in new[] { (firstFolder, 4), (secondFolder, 1) })
        {
            if (!File.ReadAllText(Path.Combine(folder, "skyglow.rad")).Contains("h=r" + sky)) throw new Exception("Receiver mismatch.");
            var batch = File.ReadAllText(Directory.GetFiles(folder, "*.bat")[0]);
            if (!batch.Contains("\"" + tools["gendaymtx"] + "\" -m " + sky) || !batch.Contains("\"" + tools["rmtxop"] + "\" -fa -t") || batch.Contains(" | ")) throw new Exception("Batch uses unselected executable, unchecked pipeline, or sky basis.");
            if (!batch.Contains(lib)) throw new Exception("Custom library was lost.");
            var directLine = batch.Split('\n').Single(line => line.Contains("rfluxmtx.exe") && line.Contains("bmodel_"));
            if (!directLine.Contains("-ab 1") || directLine.Contains("-ab 2") || directLine.Contains("-ab 3"))
                throw new Exception("Direct subtraction inherited total interreflection depth.");
            if (!batch.Contains("rcontrib.exe\" -I+ -ab 1")) throw new Exception("Sun coefficient depth drifted from DDS contract.");
            File.WriteAllText(Path.Combine(folder, "annualRfinal_part99.ill"), "999 999");
        }
        if (File.ReadAllText(grid) != "0 0 0 0 0 1" || File.Exists(Path.Combine(source, "model", "grid", "0.pts"))) throw new Exception("Preparation modified source grid.");
        var paths = new ProjectPathResolver().Resolve(new() { ProjectLocation = Path.Combine(root, "workspace") }).Paths!;
        var workspace = new WorkspaceService().Initialize(new(paths, "Study"));
        var withAnalysis = Inputs(1, 1); withAnalysis[8] = new AnalysisGoo(workspace);
        var owned = (string)Solve(new AnnualSimulationComponent(), withAnalysis).Outputs[0]!;
        var ownedManifest = AnnualRun.Read(owned);
        if (Path.GetDirectoryName(owned) != workspace.RunsFolder || ownedManifest.ProjectId != workspace.Project.Manifest.ProjectId
            || ownedManifest.AnalysisId != workspace.AnalysisManifest.AnalysisId) throw new Exception("Run lost Setup ownership.");
        foreach (var part in AnnualRun.Read(firstFolder).Parts.Take(3)) Result(firstFolder, part.Index, "1 2 3\n4 5 6");
        var incomplete = Solve(new AnnualResultCacheComponent(), new() { [0] = firstFolder, [1] = true });
        if (incomplete.Outputs.ContainsKey(0)) throw new Exception("Incomplete four-part result was accepted.");
        Result(firstFolder, 3, "1 2 3\n4 5 6");
        var full = Solve(new AnnualResultCacheComponent(), new() { [0] = firstFolder, [1] = true });
        if ((int)full.Outputs[1]! != 12) throw new Exception("Declared part order/count lost.");
        var firstRaw = (string)full.Outputs[0]!; var firstBytes = File.ReadAllBytes(firstRaw);
        for (var i = 0; i < 4; i++) Result(firstFolder, i, "1 2 3");
        if (Solve(new AnnualResultCacheComponent(), new() { [0] = firstFolder, [1] = true }).Outputs.ContainsKey(0)
            || !File.ReadAllBytes(firstRaw).SequenceEqual(firstBytes)) throw new Exception("Equal-length truncated parts replaced valid cache.");
        for (var i = 0; i < 4; i++) Result(firstFolder, i, "1 2 3\n4 5 6");
        var firstManifest = AnnualRun.Read(firstFolder);
        File.WriteAllText(Path.Combine(firstFolder, firstManifest.Parts[3].StateFile), firstManifest.RunId.ToString("N") + " Failed step=7 exit=2");
        if (Solve(new AnnualResultCacheComponent(), new() { [0] = firstFolder, [1] = false }).Outputs.ContainsKey(0)) throw new Exception("Failed run reused prior cache.");
        var partial = Solve(new AnnualSimulationProgressComponent(), new() { [0] = firstFolder });
        if ((int)partial.Outputs[1]! != 3 || !((string)partial.Outputs[2]!).Contains("3/4")) throw new Exception("Failed fourth part counted complete.");
        Result(secondFolder, 0, "7\n8");
        var cached = Solve(new AnnualResultCacheComponent(), new() { [0] = secondFolder, [1] = true });
        if ((int)cached.Outputs[1]! != 1) throw new Exception("Single-part run mixed stale parts.");
        var reuse = Solve(new AnnualResultCacheComponent(), new() { [0] = secondFolder, [1] = false });
        if (!reuse.Outputs.ContainsKey(0)) throw new Exception("Valid cache could not reopen.");
        var raw = (string)cached.Outputs[0]!;
        var original = File.ReadAllBytes(raw); File.WriteAllBytes(raw, new byte[original.Length]);
        if (Solve(new AnnualResultCacheComponent(), new() { [0] = secondFolder, [1] = false }).Outputs.ContainsKey(0)) throw new Exception("Corrupt cache reused.");
        File.WriteAllBytes(raw, original);
        Result(secondFolder, 0, "-28075.27629\n8");
        var negative = Solve(new AnnualResultCacheComponent(), new() { [0] = secondFolder, [1] = true });
        if (negative.Outputs.ContainsKey(0) || !((string)negative.Outputs[3]!).Contains("Negative illuminance")
            || !File.ReadAllBytes(raw).SequenceEqual(original)) throw new Exception("Negative final result was published or diagnostic lost.");
        if ((int)Solve(new AnnualSimulationProgressComponent(), new() { [0] = secondFolder }).Outputs[1]! != 0)
            throw new Exception("Negative illuminance counted as completed.");
        Result(secondFolder, 0, "NaN\n8");
        if (Solve(new AnnualResultCacheComponent(), new() { [0] = secondFolder, [1] = true }).Outputs.ContainsKey(0)) throw new Exception("Malformed matrix accepted.");
        if (!File.ReadAllBytes(raw).SequenceEqual(original)) throw new Exception("Rejected matrix replaced cache.");
        Result(secondFolder, 0, "9\n8");
        if (Solve(new AnnualResultCacheComponent(), new() { [0] = secondFolder, [1] = false }).Outputs.ContainsKey(0)) throw new Exception("Changed source reused stale cache.");
        File.WriteAllText(Path.Combine(secondFolder, "annual_progress_part0.log"), "[Part 0] Completed");
        File.WriteAllText(Path.Combine(secondFolder, "annual_progress_part99.log"), "unrelated");
        var progress = Solve(new AnnualSimulationProgressComponent(), new() { [0] = secondFolder });
        if ((int)progress.Outputs[1]! != 1 || !((string)progress.Outputs[2]!).Contains("1/1")) throw new Exception("Progress mixed runs or part counts.");
        var manifest = AnnualRun.Read(secondFolder);
        File.WriteAllText(Path.Combine(secondFolder, manifest.Parts[0].StateFile), manifest.RunId.ToString("N") + " Failed step=3 exit=1");
        var failed = Solve(new AnnualSimulationProgressComponent(), new() { [0] = secondFolder });
        if ((int)failed.Outputs[1]! != 0 || !((string)failed.Outputs[2]!).Contains("Failed")) throw new Exception("Progress concealed command failure.");
        CheckCulture(root);
        CheckSelectorErrors(root);
        CheckLuminairePaths(root);
        CheckCacheReaders(root);
        if (!string.IsNullOrWhiteSpace(radianceBin)) CheckLiveRadiance(root, radianceBin);
        Console.WriteLine("PASS annual integration: fail-before-write, source preservation, Sky 1/4, exact executable paths, separate library, unique one/four-part runs, declared-only cache/progress, stale-cache rejection, invariant RGB/xform.");
        // Remove this subfixture so the original read-only Setup assertion remains valid.
        if (Path.GetDirectoryName(Path.GetFullPath(root)) != Path.GetFullPath(testRoot)) throw new Exception("Invalid integration fixture cleanup.");
        Directory.Delete(root, true);
        Directory.Delete(testRoot);
    }

    private static void CheckLiveRadiance(string root, string bin)
    {
        var exe = Path.Combine(Path.GetFullPath(bin), "rmtxop.exe");
        if (!File.Exists(exe)) throw new FileNotFoundException("Live validation executable missing.", exe);
        foreach (var fail in new[] { false, true })
        {
            var folder = AnnualRun.Create(Path.Combine(root, "live"), root, 1, new[] { "0 0 0 0 0 1" }, new(), hours: 2);
            var manifest = AnnualRun.Read(folder);
            File.WriteAllText(Path.Combine(folder, "fixture.ill"), "#?RADIANCE\nNROWS=2\nNCOLS=1\nNCOMP=1\nFORMAT=ascii\n\n1\n2\n");
            var command = $"\"{exe}\" -fa -s 2 {(fail ? "missing.ill" : "intermediate.ill")} > annualRfinal_part0.ill";
            // A nested rmtxop header reproduces copied provenance from an actual annual pipeline.
            File.WriteAllLines(Path.Combine(folder, "live.bat"), AnnualBatch.Build(new[] { $"\"{exe}\" -fa fixture.ill > intermediate.ill", command, "echo reached > later.txt" }, manifest.RunId, 0));
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"), "/d /c live.bat")
                { WorkingDirectory = folder, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(15000)) { process.Kill(true); throw new Exception("Live rmtxop check timed out."); }
            Task.WhenAll(stdout, stderr).GetAwaiter().GetResult();
            var report = AnnualPartStatus.Read(folder, manifest, manifest.Parts[0]);
            if (fail)
            {
                if (process.ExitCode == 0 || report.State != "Failed" || File.Exists(Path.Combine(folder, "later.txt"))) throw new Exception("Real Radiance failure did not stop the batch.");
            }
            else
            {
                if (process.ExitCode != 0 || !report.Complete) throw new Exception("Real Radiance matrix failed validation: " + report.Detail);
                var rows = AnnualMatrix.Rows(Path.Combine(folder, manifest.Parts[0].ResultFile), 2, 1).ToArray();
                if (rows[0][0] != 2 || rows[1][0] != 4) throw new Exception("Real Radiance fixture produced wrong values.");
            }
        }
        Console.WriteLine("PASS live rmtxop: real ASCII output validated; missing-input failure stopped downstream execution. This is not a full annual simulation.");
    }

    private static void CheckCacheReaders(string root)
    {
        var path = Path.Combine(root, "legacy-reader.f32");
        File.WriteAllText(Path.ChangeExtension(path, ".meta.json"), "{\"sensors\":2,\"hours\":2,\"ncomp\":1}");
        using (var writer = new BinaryWriter(File.Create(path)))
            foreach (var value in new[] { 1f, 2f, 3f, 4f }) writer.Write(value);
        var valid = Solve(new IlluminancePointInTimeComponent(), new() { [0] = path, [1] = " hour ", [2] = 1, [3] = true });
        if (!((string)valid.Outputs[1]!).Contains("hour 1")) throw new Exception("Whitespace mode did not select hour.");
        using (var writer = new BinaryWriter(File.Create(path)))
            foreach (var value in new[] { 1f, 2f, -28075f, 4f }) writer.Write(value);
        foreach (var component in new GH_Component[] { new IlluminancePointInTimeComponent(), new IlluminanceSensorComponent() })
        {
            var bad = Solve(component, new() { [0] = path, [1] = "hour", [2] = 1, [3] = true });
            if (bad.Outputs.ContainsKey(0) || !((string)bad.Outputs[1]!).Contains("negative")) throw new Exception("Legacy negative cache was emitted as lux.");
        }
        if (Solve(new HourlyParComponent(), new() { [0] = path, [1] = 1 }).Outputs.ContainsKey(0)
            || Solve(new ParEachSensorComponent(), new() { [0] = path, [1] = 0 }).Outputs.ContainsKey(0))
            throw new Exception("Legacy negative cache was converted to PPFD.");
        Console.WriteLine("PASS cache reader quality: whitespace mode, legacy negative lux/PPFD rejection, preserved invalid source.");
    }

    private static void Result(string folder, int index, string values)
    {
        var manifest = AnnualRun.Read(folder); var part = manifest.Parts[index];
        File.WriteAllText(Path.Combine(folder, part.ResultFile), $"#?RADIANCE\nNROWS={manifest.Hours}\nNCOLS={part.Sensors}\nNCOMP=1\nFORMAT=ascii\n\n{values}");
        File.WriteAllText(Path.Combine(folder, part.StateFile), manifest.RunId.ToString("N") + " CommandsSucceeded");
    }

    private static void CheckLuminairePaths(string root)
    {
        var project = Path.Combine(root, "electric");
        var ies = Path.Combine(root, "fixture.ies"); File.WriteAllText(ies, "Fixture (Run=False)");
        Solve(new IesToRadianceComponent(), new() { [0] = ies, [6] = project, [8] = false });
        var folder = LuminairePathResolver.ResolveFolder(project);
        if (!Directory.Exists(folder)) throw new Exception("IES preparation did not use project-local output.");
        var rad = Path.Combine(folder, "fixture.rad"); File.WriteAllText(rad, "# stand-in for generated luminaire");
        var geometry = Solve(new LightingGeometryComponent(), new() { [0] = new List<Point3d> { new(1, 2, 3) }, [4] = new List<string> { rad } });
        var sibling = Path.Combine(root, "Luminaire_files"); Directory.CreateDirectory(sibling);
        var decoy = Path.Combine(sibling, "luminaries.rad"); File.WriteAllText(decoy, "sibling study");
        var compiled = Solve(new CompileLuminariesComponent(), new() { [0] = geometry.Outputs[0]!, [1] = project, [2] = true });
        if ((string)compiled.Outputs[0]! != Path.Combine(folder, "luminaries.rad") || File.ReadAllText(decoy) != "sibling study")
            throw new Exception("Compilation selected a sibling study.");
    }

    private static void CheckSelectorErrors(string root)
    {
        foreach (var component in new GH_Component[] { new FacadeMaterialComponent(), new GlazingMaterialComponent(), new IesLuminaireSelectorComponent() })
        {
            Solve(component, new() { [0] = true, [1] = Path.Combine(root, "missing library") });
            if (component.RuntimeMessages(GH_RuntimeMessageLevel.Error).Count == 0) throw new Exception("Missing selector library did not produce a diagnostic.");
            var bundled = Path.Combine(Path.GetDirectoryName(component.GetType().Assembly.Location)!, "shared", "Library", "FlahaGrow_Library_Small");
            if (!Directory.Exists(bundled)) Solve(component, new() { [0] = true });
        }
    }

    private static void CheckCulture(string root)
    {
        var culture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var path = Path.Combine(root, "light.rad"); File.WriteAllText(path, "3 1 1 1");
            typeof(IesToRadianceComponent).GetMethod("RewriteRad", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object?[] { path, .265, .67, .065, null });
            if (File.ReadAllText(path).Trim() != "3 0.265 0.67 0.065") throw new Exception("RGB writer depends on culture.");
            var line = (string)typeof(LightingGeometryComponent).GetMethod("Build", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, new object[] { new Point3d(1.25, 2.5, 3.75), 4.5, 0.0, 0.0, path })!;
            if (!line.Contains("-rx 4.5 -t 1.25 2.5 3.75")) throw new Exception("xform writer depends on culture.");
        }
        finally { CultureInfo.CurrentCulture = culture; }
    }

    private static TestData Solve(GH_Component component, Dictionary<int, object> inputs)
    {
        var data = TestData.Create(inputs);
        component.GetType().GetMethod("SolveInstance", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(component, new object[] { data.Access });
        return data;
    }
}
