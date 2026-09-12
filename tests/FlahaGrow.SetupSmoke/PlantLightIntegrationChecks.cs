using System.Reflection;
using FlahaGrow.Core.Annual;
using FlahaGrow.Grasshopper.Components;
using FlahaGrow.Grasshopper.Parameters;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

internal static class PlantLightIntegrationChecks
{
    public static void Run(string testRoot)
    {
        var root = Path.Combine(testRoot, "plant-light");
        var folder = AnnualRun.Create(root, root, 1, Enumerable.Range(0, 13).Select(i => $"{i} 0 0 0 0 1").ToArray(), new());
        var run = AnnualRun.Read(folder);
        ElectricAnnualMatrix.WriteRun(folder, run, Enumerable.Range(1, 13).Select(i => i * 1000d).ToArray(), Enumerable.Repeat(1d, 8760).ToArray());
        foreach (var part in run.Parts) File.WriteAllText(Path.Combine(folder, part.StateFile), run.RunId.ToString("N") + " CommandsSucceeded");
        var cache = Solve(new AnnualResultCacheComponent(), new() { [0] = folder, [1] = true });
        var profile = Solve(new SpectralProfileComponent(), new() { [0] = "Fixture assumption", [1] = .0185 });
        var context = Solve(new PlantLightContextComponent(), new() { [0] = cache.Outputs[0]!, [1] = profile.Outputs[0]! });
        if (context.Outputs[0] is not PlantLightContextGoo) throw new Exception("No typed context.");
        Dictionary<int, object> Inputs(int index) => new() { [0] = context.Outputs[0]!, [1] = index };
        var hour = Solve(new PpfdAtHourComponent(), Inputs(23));
        var sensor = Solve(new AnnualPpfdAtSensorComponent(), Inputs(12));
        var day = Solve(new DliForDayComponent(), Inputs(0));
        var annual = Solve(new AnnualDliAtSensorComponent(), Inputs(12));
        var hourlyValues = ((IEnumerable<double>)hour.Outputs[0]!).ToArray();
        var sensorValues = ((IEnumerable<double>)sensor.Outputs[0]!).ToArray();
        var dayValues = ((IEnumerable<double>)day.Outputs[0]!).ToArray();
        var annualValues = ((IEnumerable<double>)annual.Outputs[0]!).ToArray();
        if (hourlyValues.Length != 13 || sensorValues.Length != 8760 || dayValues.Length != 13 || annualValues.Length != 365)
            throw new Exception("Plant-light reader shape mismatch.");
        var tree = (GH_Structure<GH_Number>)day.Outputs[2]!;
        if (tree.PathCount != 13 || tree.Branches.Any(b => b.Count != 24)) throw new Exception("Photon-integral tree shape mismatch.");
        if (Math.Abs(dayValues[12] - 240.5 * .0864) > 1e-9 || Math.Abs(dayValues[12] - annualValues[0]) > 1e-9
            || Math.Abs(tree.Branches[12].Sum(v => v.Value) - dayValues[12]) > 1e-9)
            throw new Exception("Independent component PPFD/DLI branches disagree.");
        var bad = Solve(new DliForDayComponent(), Inputs(365), expectError: true);
        if (bad.Outputs.ContainsKey(0)) throw new Exception("Invalid day emitted data.");
        var both = Solve(new SpectralProfileComponent(), new() { [0] = "Ambiguous", [1] = .0185, [2] = "unused.csv" }, expectError: true);
        if (both.Outputs.ContainsKey(0)) throw new Exception("Ambiguous spectral inputs emitted a profile.");
        var csv = Path.Combine(root, "spectrum.csv");
        File.WriteAllText(csv, "wavelength_nm,value\n360,1\n830,1\n");
        var imported = Solve(new SpectralProfileComponent(), new() { [0] = "Energy reference", [2] = csv });
        var legacy = Solve(new LoadSpectralDataComponent(), new() { [2] = csv });
        if (Math.Abs((double)legacy.Outputs[0]! - ((SpectralProfileGoo)imported.Outputs[0]!).Value.Factor) > 1e-12)
            throw new Exception("Legacy/new spectral calculation diverged.");
        var expectedLux = (double)legacy.Outputs[1]! / (double)legacy.Outputs[0]!;
        if (Math.Abs((double)legacy.Outputs[2]! - expectedLux) > 1e-7) throw new Exception("Integrated legacy lux omitted photopic scale.");
        Console.WriteLine("PASS plant-light pipeline: 13-sensor Load Result, explicit/CSV profiles, typed context, four independent readers, daily tree, invalid day, ambiguous profile rejection and shared legacy spectral math.");
    }

    private static TestData Solve(GH_Component component, Dictionary<int, object> inputs, bool expectError = false)
    {
        var data = TestData.Create(inputs);
        component.GetType().GetMethod("SolveInstance", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(component, new object[] { data.Access });
        var errors = component.RuntimeMessages(GH_RuntimeMessageLevel.Error);
        if (expectError ? errors.Count == 0 : errors.Count != 0)
            throw new Exception(component.Name + ": unexpected solve status: " + string.Join("; ", errors));
        return data;
    }
}
