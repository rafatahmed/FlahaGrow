using System.Reflection;
using FlahaGrow.Core.Operations;
using FlahaGrow.Grasshopper;
using FlahaGrow.Grasshopper.Parameters;
using FlahaGrow.Grasshopper.Components;
using FlahaGrow.Grasshopper.Components.Setup;
using GH_IO.Serialization;
using Grasshopper.Kernel;

var cases = new (GH_Component Component, Guid Id, int Inputs, int Outputs)[]
{
    (new SimulationPathsComponent(), new("71c6a045-9308-4a0c-9f72-cab76ceefa5c"), 2, 5),
    (new WorkingDirectoryComponent(), new("3bc3011e-2b2f-4c14-9344-dcb3554f3722"), 7, 7),
    (new RadianceStatusComponent(), new("f6f1d5d4-9a1a-4de7-a090-6299c94e0060"), 1, 2),
    (new RadianceVersionComponent(), new("272aa83d-9898-460d-8cbd-7f49374153ba"), 2, 1),
    (new ProjectPathsComponent(), new("37c57f57-1be3-4eaa-aa88-12a20f0172ef"), 5, 5),
    (new ProjectWorkspaceComponent(), new("d236c57b-eab1-4329-8e4e-beb2285ba04d"), 7, 7),
    (new ProjectRadianceComponent(), new("59892a1c-7e97-46d5-989c-2283c9476ea4"), 5, 7),
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
Console.WriteLine("Nine component checks passed; exactly three visible Setup components. This does not exercise the Rhino canvas or UI scheduler.");

var tracked = typeof(FlahaGrowAssemblyInfo).Assembly.GetTypes()
    .Where(type => !type.IsAbstract && typeof(GH_Component).IsAssignableFrom(type))
    .Select(type => (GH_Component)Activator.CreateInstance(type, type.GetConstructors().Single().GetParameters()
        .Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : throw new InvalidOperationException($"Component constructor requires an explicit value: {type.FullName}"))
        .ToArray())!)
    .OrderBy(component => component.ComponentGuid)
    .ToArray();
if (tracked.Length == 0 || tracked.Length != ComponentRevisionCatalog.Count || tracked.Any(component => component is not FlahaGrowComponent || !ComponentRevisionCatalog.Contains(component.ComponentGuid)))
    throw new InvalidOperationException("Every concrete FlahaGrow component must inherit revision tracking and have a catalog entry.");
var revisionArchive = new GH_Archive();
var trackedComponent = (FlahaGrowComponent)tracked[0];
if (!revisionArchive.AppendObject(trackedComponent, "Component")) throw new InvalidOperationException("Revision archive write failed.");
var restoredTracked = (FlahaGrowComponent)Activator.CreateInstance(tracked[0].GetType())!;
if (!revisionArchive.ExtractObject(restoredTracked, "Component") || restoredTracked.SavedRevision != trackedComponent.Revision.Version || restoredTracked.NeedsRevisionReview)
    throw new InvalidOperationException("Component revision was not preserved through a Grasshopper archive.");
Console.WriteLine($"PASS component revision ledger: {tracked.Length} components registered; archive revision retained.");

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
    var radiance = new ProjectRadianceComponent();
    var radianceData = TestData.Create(new() { [3] = false });
    MeasureWarm(radiance, radianceData);
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
