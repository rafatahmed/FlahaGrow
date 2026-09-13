using System.Reflection;
using FlahaGrow.Grasshopper.Components;
using FlahaGrow.Grasshopper.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;

internal static class ValidationIntegrationChecks
{
    internal static void LiveIes(string root, string repository, string bin)
    {
        var sample = Directory.GetFiles(Path.Combine(repository, "src", "Library", "FlahaGrow_Library_Small", "RadIES"), "*.ies").OrderBy(p => p, StringComparer.Ordinal).First();
        var copied = Path.Combine(root, "live-fixture.ies"); File.Copy(sample, copied);
        var component = new IesToRadianceComponent();
        var data = TestData.Create(new() { [0] = copied, [6] = Path.Combine(root, "live-ies"), [8] = true, [9] = bin });
        void Solve() => typeof(IesToRadianceComponent).GetMethod("SolveInstance", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(component, new object[] { data.Access });
        Solve();
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (component.IsConverting && DateTime.UtcNow < deadline) { Thread.Sleep(10); Solve(); }
        if (component.IsConverting || component.RuntimeMessages(GH_RuntimeMessageLevel.Error).Count > 0 || !data.Outputs.ContainsKey(0))
            throw new Exception("Live IES conversion failed: " + string.Join("; ", component.RuntimeMessages(GH_RuntimeMessageLevel.Error)));
        var rad = ((IEnumerable<string>)data.Outputs[0]!).Single();
        var dat = ((IEnumerable<string>)data.Outputs[1]!).ToArray();
        if (!File.Exists(rad) || dat.Any(p => !File.Exists(p))) throw new Exception("Live conversion output is missing.");
        var stamp = File.GetLastWriteTimeUtc(rad);
        for (var i = 0; i < 10; i++) Solve();
        if (component.IsConverting || File.GetLastWriteTimeUtc(rad) != stamp) throw new Exception("Held Run=True restarted IES conversion.");
        if (Directory.GetDirectories(Path.GetDirectoryName(rad)!, ".conversion-*").Length != 0) throw new Exception("Conversion staging was retained.");
        Console.WriteLine("PASS live ies2rad: fresh RAD/DAT files, background completion, staging cleanup and held-True reuse.");
    }

    internal static void Run(string root)
    {
        void Reject(GH_Component component, Dictionary<int, object> inputs)
        {
            var data = TestData.Create(inputs);
            component.GetType().GetMethod("SolveInstance", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(component, new object[] { data.Access });
            if (component.RuntimeMessages(GH_RuntimeMessageLevel.Error).Count == 0 || data.Outputs.ContainsKey(0) && component is not AnnualPlotComponent)
                throw new Exception("Invalid input was not rejected: " + component.Name);
        }
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, -1d })
        {
            Reject(new DliTargetComponent(), new() { [0] = new List<double> { value }, [1] = 10d });
            Reject(new DliTargetComponent(), new() { [0] = new List<double> { 10d }, [1] = value });
            Reject(new AnnualLightingEnergyComponent(), new() { [0] = new List<double> { value }, [1] = 3600d });
            Reject(new AnnualLightingEnergyComponent(), new() { [0] = new List<double> { 100d }, [1] = value });
        }
        Reject(new AnnualLightingEnergyComponent(), new() { [0] = new List<double> { double.MaxValue, double.MaxValue }, [1] = double.MaxValue });
        var energy = TestData.Create(new() { [0] = new List<double> { 1000d, 0d, 2000d }, [1] = 1800d });
        typeof(AnnualLightingEnergyComponent).GetMethod("SolveInstance", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(new AnnualLightingEnergyComponent(), new object[] { energy.Access });
        if ((double)energy.Outputs[0]! != 1.5 || (double)energy.Outputs[1]! != 1.5) throw new Exception("Energy integration regression.");
        var extremeDli = TestData.Create(new() { [0] = Enumerable.Repeat(1e308, 8760).ToList(), [1] = 3600d });
        typeof(AnnualDliComponent).GetMethod("SolveInstance", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(new AnnualDliComponent(), new object[] { extremeDli.Access });
        var average = (double)extremeDli.Outputs[1]!;
        if (!double.IsFinite(average) || Math.Abs(average / 8.64e306 - 1) > 1e-12) throw new Exception("Annual DLI mean overflowed finite daily values.");
        Reject(new SensorMarkerComponent(), new() { [0] = Point3d.Origin, [1] = double.NaN });
        Reject(new AnnualPlotComponent(), new() { [0] = Enumerable.Repeat(1d, 365).ToList(), [14] = new PlotAttributesGoo() });
        var rad = Path.Combine(root, "validation-fixture.rad"); File.WriteAllText(rad, "# fixture");
        Reject(new LightingGeometryComponent(), new() { [0] = new List<Point3d> { Point3d.Origin }, [1] = new List<double> { double.NaN }, [4] = new List<string> { rad } });
        Reject(new LightingGeometryComponent(), new() { [0] = new List<Point3d> { Point3d.Unset }, [4] = new List<string> { rad } });
        Reject(new LightingGeometryComponent(), new() { [0] = new List<Point3d> { Point3d.Origin }, [4] = new List<string> { rad + "\n!unexpected" } });
        var ies = Path.Combine(root, "validation-fixture.ies"); File.WriteAllText(ies, "fixture");
        var idleFolder = Path.Combine(root, "idle-ies");
        Reject(new IesToRadianceComponent(), new() { [0] = ies, [6] = idleFolder, [2] = double.NaN });
        Reject(new IesToRadianceComponent(), new() { [0] = ies, [6] = idleFolder, [5] = 0d });
        if (Directory.Exists(idleFolder)) throw new Exception("Invalid conversion inputs wrote a directory.");
        Console.WriteLine("PASS numeric/geometry validation: nonfinite and overflow rejection, correct energy, invalid plot attributes and write-free invalid IES inputs.");
    }
}
