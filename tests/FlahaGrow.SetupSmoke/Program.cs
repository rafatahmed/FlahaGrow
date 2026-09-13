using System.Reflection;
using FlahaGrow.Core.Operations;
using FlahaGrow.Grasshopper;
using FlahaGrow.Grasshopper.Parameters;
using FlahaGrow.Grasshopper.Components;
using FlahaGrow.Grasshopper.Components.Setup;
using GH_IO.Serialization;
using Grasshopper.Kernel;

if (args.Contains("--component-catalog")) { ComponentCatalogExport.Print(args.SkipWhile(a => a != "--component-catalog").Skip(1).FirstOrDefault()); return; }

var cases = new (GH_Component Component, Guid Id, int Inputs, int Outputs)[]
{
    (new ProjectWorkspaceComponent(), new("d236c57b-eab1-4329-8e4e-beb2285ba04d"), 7, 7),
    (new SimulationPathsSetupComponent(), new("71ce89f2-1439-4730-915f-07436692926c"), 4, 4),
    (new RadianceSetupComponent(), new("9cd39fc4-7c35-4aee-a247-1c8b980c4b17"), 2, 7)
};
var ids = new HashSet<Guid>();
foreach (var (component, id, inputs, outputs) in cases)
{
    if (component.ComponentGuid != id || !ids.Add(id) || component.Params.Input.Count != inputs || component.Params.Output.Count != outputs)
        throw new InvalidOperationException("Interface mismatch: " + component.Name);
    var archive = new GH_Archive();
    if (!archive.AppendObject(component, "Component")) throw new InvalidOperationException("Archive write failed.");
    var xml = archive.Serialize_Xml();
    var reloaded = new GH_Archive();
    if (!reloaded.Deserialize_Xml(xml)) throw new InvalidOperationException("Archive XML read failed.");
    var restored = (GH_Component)Activator.CreateInstance(component.GetType())!;
    if (!reloaded.ExtractObject(restored, "Component")) throw new InvalidOperationException("Archive extraction failed.");
    var before = component.Params.Input.Concat(component.Params.Output).Select(p => (p.Name, p.Access, p.Optional, p.GetType())).ToArray();
    var after = restored.Params.Input.Concat(restored.Params.Output).Select(p => (p.Name, p.Access, p.Optional, p.GetType())).ToArray();
    if (!before.SequenceEqual(after)) throw new InvalidOperationException("Parameter archive mismatch.");
    for (var type = restored.GetType(); type is not null; type = type.BaseType)
    foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(f => f.FieldType == typeof(ActionLatch)))
    {
        var latch = (ActionLatch)field.GetValue(restored)!;
        if (latch.Observe(true) || latch.Observe(false) || !latch.Observe(true)) throw new InvalidOperationException("Restored action was not disarmed.");
    }
    Console.WriteLine($"PASS {component.Name}: GUID, {inputs}/{outputs} parameters, archive round trip, action restore.");
}
var visible = cases.Where(c => c.Component.Exposure != GH_Exposure.hidden).Select(c => c.Component.Name).OrderBy(n => n).ToArray();
if (!visible.SequenceEqual(new[] { "Radiance Status", "Simulation Paths", "Working Directory" })) throw new Exception("Setup toolbar has duplicate or missing components.");
Console.WriteLine("Three component checks passed; exactly three registered Setup components. This does not exercise the Rhino canvas or UI scheduler.");

var tracked = typeof(FlahaGrowAssemblyInfo).Assembly.GetTypes()
    .Where(type => !type.IsAbstract && typeof(GH_Component).IsAssignableFrom(type))
    .Select(type => (GH_Component)Activator.CreateInstance(type, type.GetConstructors().Single().GetParameters()
        .Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : throw new InvalidOperationException($"Component constructor requires an explicit value: {type.FullName}"))
        .ToArray())!)
    .OrderBy(component => component.ComponentGuid)
    .ToArray();
if (tracked.Length == 0 || tracked.Length != ComponentRevisionCatalog.Count || tracked.Any(component => component is not FlahaGrowComponent || !ComponentRevisionCatalog.Contains(component.ComponentGuid)))
    throw new InvalidOperationException("Every concrete FlahaGrow component must inherit revision tracking and have a catalog entry.");
if (tracked.Length != 30 || tracked.Any(c => c.Exposure == GH_Exposure.hidden)
    || tracked.Select(c => c.ComponentGuid).Distinct().Count() != tracked.Length
    || tracked.GroupBy(c => (c.SubCategory, c.Name)).Any(group => group.Count() > 1))
    throw new InvalidOperationException("Expected 30 visible components with unique names and GUIDs; no hidden predecessors.");
var portTypes = tracked.SelectMany(c => c.Params.Input.Concat(c.Params.Output)).Select(p => p.GetType()).ToHashSet();
var wireTypes = typeof(FlahaGrowAssemblyInfo).Assembly.GetTypes()
    .Where(t => !t.IsAbstract && typeof(IGH_Param).IsAssignableFrom(t)).ToArray();
if (wireTypes.Length != 8 || wireTypes.Any(t => !portTypes.Contains(t)))
    throw new InvalidOperationException("Every typed wire parameter must be used by a registered component port.");
var wireParameters = wireTypes.Select(t => (IGH_Param)Activator.CreateInstance(t)!).ToArray();
if (tracked.Select(c => c.ComponentGuid).Concat(wireParameters.Select(p => p.ComponentGuid)).Distinct().Count() != tracked.Length + wireTypes.Length)
    throw new InvalidOperationException("Component and wire parameter GUIDs must be globally unique.");
Console.WriteLine("PASS eight typed wire parameters: every type used by a current port; globally unique GUIDs.");
var embeddedIcons = typeof(FlahaGrowAssemblyInfo).Assembly.GetManifestResourceNames().Where(n => n.StartsWith("FlahaGrow.Icons.", StringComparison.Ordinal)).ToArray();
if (embeddedIcons.Length != 24) throw new Exception("Unexpected icon inventory.");
foreach (var resource in embeddedIcons)
{
    var name = resource["FlahaGrow.Icons.".Length..^4];
    var bitmap = ComponentIcons.ForComponent(name) ?? throw new Exception("Missing icon: " + name);
    if (bitmap.Width != 24 || bitmap.Height != 24) throw new Exception("Wrong icon dimensions: " + name);
    if (name != "FlahaGrow_Icon_logo" && !tracked.Any(c => c.Name == name)) throw new Exception("Unmapped named icon: " + name);
    if (!ReferenceEquals(bitmap, ComponentIcons.ForComponent(name))) throw new Exception("Icon is not cached.");
}
if (ComponentIcons.ForComponent("Unassigned component") is not null) throw new Exception("Unknown component received unrelated artwork.");
foreach (var component in tracked)
{
    var expected = ComponentIcons.ForComponent(component.Name);
    var actual = typeof(FlahaGrowComponent).GetProperty("Icon", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component);
    if (!ReferenceEquals(expected, actual)) throw new Exception("Component icon mapping mismatch: " + component.Name);
}
Console.WriteLine("PASS 23 named component icons and plugin logo: embedded mapping, 24px rendering, shared cache and unmapped fallback.");
foreach (FlahaGrowComponent component in tracked)
{
    var archive = new GH_Archive();
    if (!archive.AppendObject(component, "Component")) throw new InvalidOperationException("Archive write failed: " + component.Name);
    var reloaded = new GH_Archive();
    var copy = (FlahaGrowComponent)Activator.CreateInstance(component.GetType())!;
    if (!reloaded.Deserialize_Xml(archive.Serialize_Xml()) || !reloaded.ExtractObject(copy, "Component")
        || copy.ComponentGuid != component.ComponentGuid || copy.SavedRevision != component.Revision.Version || copy.NeedsRevisionReview)
        throw new InvalidOperationException("Identity/revision archive mismatch: " + component.Name);
    var before = component.Params.Input.Concat(component.Params.Output).Select(p => (p.Name, p.NickName, p.Access, p.Optional, p.GetType()));
    var after = copy.Params.Input.Concat(copy.Params.Output).Select(p => (p.Name, p.NickName, p.Access, p.Optional, p.GetType()));
    if (!before.SequenceEqual(after)) throw new InvalidOperationException("Port archive mismatch: " + component.Name);
}
Console.WriteLine($"PASS all {tracked.Length} component archives: identity, revision and input/output contracts retained.");

var plantCases = new (GH_Component Component, int Inputs, int Outputs)[]
{
    (new SelectHourIndexComponent(), 2, 4),
    (new CustomSpectralProfileComponent(), 5, 3), (new SpectralProfileComponent(), 1, 3), (new PlantLightContextComponent(), 4, 2),
    (new CombinePlantLightComponent(), 1, 2), (new PpfdAtHourComponent(), 2, 3),
    (new AnnualPpfdAtSensorComponent(), 2, 3), (new DliForDayComponent(), 2, 4),
    (new AnnualDliAtSensorComponent(), 2, 3),
    (new AnnualPlotComponent(), 16, 1),
    (new ElectricAnnualSimulationComponent(), 10, 3), (new ReadIlluminanceComponent(), 4, 3),
    (new AnnualResultCacheComponent(), 3, 5), (new LuxToPpfdComponent(), 2, 1),
    (new AnnualDliComponent(), 2, 2),
};
foreach (var (component, inputCount, outputCount) in plantCases)
{
    if (component.Params.Input.Count != inputCount || component.Params.Output.Count != outputCount)
        throw new Exception("Plant-light interface mismatch: " + component.Name);
    var archive = new GH_Archive(); archive.AppendObject(component, "Component");
    var copy = (GH_Component)Activator.CreateInstance(component.GetType(), component.GetType().GetConstructors().Single().GetParameters().Select(p => p.DefaultValue).ToArray())!;
    var restored = new GH_Archive();
    if (!restored.Deserialize_Xml(archive.Serialize_Xml()) || !restored.ExtractObject(copy, "Component")) throw new Exception("Plant-light archive failed.");
    var before = component.Params.Input.Concat(component.Params.Output).Select(p => (p.Name, p.Access, p.Optional, p.GetType()));
    var after = copy.Params.Input.Concat(copy.Params.Output).Select(p => (p.Name, p.Access, p.Optional, p.GetType()));
    if (!before.SequenceEqual(after) || component.ComponentGuid != copy.ComponentGuid) throw new Exception("Plant-light ports changed during archive round trip.");
}
if (new DliForDayComponent().Params.Output[2].Access != GH_ParamAccess.tree
    || new PpfdAtHourComponent().Params.Input[0] is not PlantLightContextParameter)
    throw new Exception("Plant-light typed context/tree contract failed.");
var explicitProfile = new CustomSpectralProfileComponent();
var timing = new SelectHourIndexComponent();
typeof(SelectHourIndexComponent).GetField("selectedIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(timing, (int?)6109);
var timingArchive = new GH_Archive(); timingArchive.AppendObject(timing, "Component");
var timingCopy = new SelectHourIndexComponent(); timingArchive.ExtractObject(timingCopy, "Component");
var timingData = TestData.Create(new() { [0] = true });
SolveUntil(timingCopy, timingData, () => timingData.Outputs.ContainsKey(0));
if ((int)timingData.Outputs[0]! != 6109 || (int)timingData.Outputs[2]! != 254
    || timingData.Outputs.ContainsKey(3)) throw new Exception("Hour/Day contract or missing-weather safeguard failed.");
var librarySelector = new SpectralProfileComponent();
typeof(SpectralProfileComponent).GetField("selectedId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(librarySelector, "CIE_std_illum_D65:1");
var libraryArchive = new GH_Archive(); libraryArchive.AppendObject(librarySelector, "Component");
var libraryCopy = new SpectralProfileComponent();
if (!libraryArchive.ExtractObject(libraryCopy, "Component")) throw new Exception("Library selection restore failed.");
var libraryData = TestData.Create(new() { [0] = true }); // Restored True must not reopen the modal picker.
SolveUntil(libraryCopy, libraryData, () => libraryData.Outputs.ContainsKey(0));
if (Math.Abs(((SpectralProfileGoo)libraryData.Outputs[0]!).Value.Factor - .01801871704609) > 1e-12)
    throw new Exception("Saved library selection emitted a different factor.");
var profileData = TestData.Create(new() { [0] = "Test daylight", [1] = .0185 });
SolveUntil(explicitProfile, profileData, () => profileData.Outputs.ContainsKey(0));
if (((SpectralProfileGoo)profileData.Outputs[0]!).Value.Factor != .0185) throw new Exception("Explicit profile solve failed.");
Console.WriteLine($"PASS {plantCases.Length} plant-light/timing/plot interface archives, saved Hour/Day, weather safeguards, typed ports and hourly integral tree. Live Rhino wiring is not exercised.");

foreach (var (component, inputs, radianceIndex) in new[] { ((GH_Component)new AnnualSimulationComponent(), 12, 7), ((GH_Component)new IesToRadianceComponent(), 11, 10) })
{
    if (component.Params.Input.Count != inputs || component.Params.Input[radianceIndex] is not RadianceParameter || !component.Params.Input[radianceIndex].Optional)
        throw new InvalidOperationException("Verified Radiance environment input mismatch: " + component.Name);
}
Console.WriteLine("PASS annual and IES consumers: appended optional verified Radiance environment inputs.");

var testRoot = Path.Combine(Path.GetTempPath(), "FlahaGrow.SetupSmoke", Guid.NewGuid().ToString("N"));
try
{
    AnnualIntegrationChecks.Run(testRoot, args.Length > 1 ? args[1] : null);
    var pathsComponent = new SimulationPathsSetupComponent();
    var pathsData = TestData.Create(new() { [0] = testRoot, [1] = 3, [2] = Path.Combine(args[0], "src", "Library") });
    SolveUntil(pathsComponent, pathsData, () => pathsData.Outputs.ContainsKey(0));
    if (Directory.Exists(testRoot)) throw new Exception("Paths component wrote to disk.");
    var workspace = new ProjectWorkspaceComponent();
    var data = TestData.Create(new() { [0] = (PathsGoo)pathsData.Outputs[0]!, [1] = "Smoke study", [2] = "baseline", [3] = 0, [4] = true });
    SolveUntil(workspace, data, () => data.Outputs.ContainsKey(0));
    var first = ((ProjectGoo)data.Outputs[0]!).Value.Manifest.ProjectId;
    var originalAnalysis = ((AnalysisGoo)data.Outputs[1]!).Value.Analysis.AnalysisId;
    data.Inputs[4] = false;
    SolveUntil(workspace, data, () => data.Outputs.ContainsKey(0));
    if (((ProjectGoo)data.Outputs[0]!).Value.Manifest.ProjectId != first) throw new Exception("Button release lost project identity.");
    data.Inputs[2] = "alternative"; data.Inputs[4] = true;
    SolveUntil(workspace, data, () => data.Outputs.ContainsKey(1));
    if (((AnalysisGoo)data.Outputs[1]!).Value.Analysis.AnalysisId == originalAnalysis) throw new Exception("New analysis reused old identity.");
    if (((ProjectGoo)data.Outputs[0]!).Value.Manifest.ProjectId != first) throw new Exception("New analysis replaced project identity.");
    Console.WriteLine("PASS direct component solves: read-only paths, initialize, release retention, second analysis.");
    var manifestFile = Path.Combine(testRoot, "flahagrow.project.json");
    var stamp = File.GetLastWriteTimeUtc(manifestFile);
    MeasureWarm(pathsComponent, pathsData);
    MeasureWarm(workspace, data);
    var automatic = new RadianceSetupComponent();
    // A missing explicit fixture is deterministic and must never launch an ambient installation.
    typeof(RadianceSetupComponent).GetField("selectedLocation", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(automatic, Path.Combine(testRoot, "missing-radiance"));
    var automaticData = TestData.Create(new());
    SolveUntil(automatic, automaticData, () => automaticData.Outputs.ContainsKey(0));
    if (((RadianceGoo)automaticData.Outputs[0]!).Value.State != FlahaGrow.Core.Radiance.RadianceState.NotFound) throw new Exception("Automatic initial check failed.");
    data.Inputs[4] = false;
    SolveUntil(workspace, data, () => data.Outputs.ContainsKey(1));
    data.Inputs[2] = "electric"; data.Inputs[3] = 1; data.Inputs[4] = true;
    SolveUntil(workspace, data, () => data.Outputs.ContainsKey(1));
    automaticData.Inputs[0] = (AnalysisGoo)data.Outputs[1]!;
    SolveUntil(automatic, automaticData, () => automaticData.Outputs.ContainsKey(0));
    if (((RadianceGoo)automaticData.Outputs[0]!).Value.Workflow != ((AnalysisGoo)data.Outputs[1]!).Value.AnalysisManifest.Workflow) throw new Exception("Radiance did not inherit analysis workflow.");
    MeasureWarm(automatic, automaticData);
    var savedRadiance = new GH_Archive();
    savedRadiance.AppendObject(automatic, "Component");
    var restoredRadiance = new RadianceSetupComponent();
    if (!savedRadiance.ExtractObject(restoredRadiance, "Component")) throw new Exception("Radiance settings restore failed.");
    var restoredData = TestData.Create(new());
    SolveUntil(restoredRadiance, restoredData, () => restoredData.Outputs.ContainsKey(0));
    if (((RadianceGoo)restoredData.Outputs[0]!).Value.State != FlahaGrow.Core.Radiance.RadianceState.NotFound) throw new Exception("Restored custom selection was lost.");
    Console.WriteLine("PASS automatic Radiance check without a button, explicit selection isolation, and analysis context.");
    if (File.GetLastWriteTimeUtc(manifestFile) != stamp) throw new Exception("Warm solves rewrote the manifest.");
    PlantLightIntegrationChecks.Run(testRoot);
    ValidationIntegrationChecks.Run(testRoot);
    if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1])) ValidationIntegrationChecks.LiveIes(testRoot, args[0], args[1]);
}
finally
{
    var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FlahaGrow.SetupSmoke")) + Path.DirectorySeparatorChar;
    if (!Path.GetFullPath(testRoot).StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new Exception("Cleanup escaped fixture root.");
    if (Directory.Exists(testRoot)) Directory.Delete(testRoot, recursive: true);
}

static void SolveUntil(GH_Component component, TestData data, Func<bool> ready)
{
    var timer = System.Diagnostics.Stopwatch.StartNew();
    do
    {
        data.Outputs.Clear();
        component.GetType().GetMethod("SolveInstance", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(component, new object[] { data.Access });
        if (ready()) return;
        Thread.Sleep(10);
    } while (timer.Elapsed < TimeSpan.FromSeconds(10));
    throw new Exception(component.Name + " did not complete: " + string.Join("; ", data.Outputs.Values));
}

static void MeasureWarm(GH_Component component, TestData data)
{
    var solve = component.GetType().GetMethod("SolveInstance", BindingFlags.NonPublic | BindingFlags.Instance)!;
    var samples = new double[500];
    for (var i = 0; i < samples.Length; i++)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        data.Outputs.Clear();
        solve.Invoke(component, new object[] { data.Access });
        samples[i] = timer.Elapsed.TotalMilliseconds;
    }
    Array.Sort(samples);
    Console.WriteLine($"Warm direct solve {component.Name}: median={samples[250]:F3} ms; p95={samples[475]:F3} ms (500 calls, includes test adapter).");
}

public class TestData : DispatchProxy
{
    public Dictionary<int, object> Inputs { get; private set; } = new();
    public Dictionary<int, object?> Outputs { get; } = new();
    public IGH_DataAccess Access { get; private set; } = null!;
    public static TestData Create(Dictionary<int, object> inputs)
    {
        var access = DispatchProxy.Create<IGH_DataAccess, TestData>();
        var proxy = (TestData)(object)access; proxy.Access = access; proxy.Inputs = inputs; return proxy;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name == "GetData")
        {
            if (!Inputs.TryGetValue((int)args![0]!, out var value)) return false;
            args[1] = value; return true;
        }
        if (method.Name == "GetDataList")
        {
            if (!Inputs.TryGetValue((int)args![0]!, out var value)) return false;
            var list = (System.Collections.IList)args[1]!;
            foreach (var item in (System.Collections.IEnumerable)value) list.Add(item);
            return true;
        }
        if (method.Name == "GetDataTree")
        {
            if (!Inputs.TryGetValue((int)args![0]!, out var value)) return false;
            args[1] = value;
            return true;
        }
        if (method.Name is "SetData" or "SetDataList" or "SetDataTree") { Outputs[(int)args![0]!] = args[1]; return method.ReturnType == typeof(bool) ? true : null; }
        throw new NotSupportedException(method.Name);
    }
}
