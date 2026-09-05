using System.Diagnostics;
using System.Text.RegularExpressions;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Runs ies2rad and applies the legacy three-channel RGB normalization.</summary>
public sealed class IesToRadianceComponent : GH_Component
{
    public IesToRadianceComponent() : base("IES to Radiance", "IES→Rad", "Converts an IES luminaire to Radiance files and applies normalized RGB channels.", "FlahaGrow", "Electric Light") { }
    public override Guid ComponentGuid => new("e64e15f4-7cee-48b2-a232-2064d3a9e602");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddTextParameter("IES path", "IES", "IES file path.", GH_ParamAccess.item);
        parameters.AddTextParameter("Luminaire name", "Name", "Output luminaire name. Blank uses the IES filename.", GH_ParamAccess.item); parameters[1].Optional = true;
        parameters.AddNumberParameter("Red", "R", "Red channel multiplier.", GH_ParamAccess.item, 1.0);
        parameters.AddNumberParameter("Green", "G", "Green channel multiplier.", GH_ParamAccess.item, 1.0);
        parameters.AddNumberParameter("Blue", "B", "Blue channel multiplier.", GH_ParamAccess.item, 1.0);
        parameters.AddNumberParameter("Multiplier", "M", "Optional ies2rad multiplier.", GH_ParamAccess.item); parameters[5].Optional = true;
        parameters.AddTextParameter("Project folder", "Project", "Simulation project folder; Luminaire_files is created beside it.", GH_ParamAccess.item);
        parameters.AddTextParameter("DAT file", "DAT", "Optional replacement data-file path.", GH_ParamAccess.item); parameters[7].Optional = true;
        parameters.AddBooleanParameter("Run", "Run", "Run ies2rad and rewrite generated .rad files.", GH_ParamAccess.item, false);
        parameters.AddTextParameter("Radiance bin folder", "Bin", "Optional folder containing ies2rad.exe. Leave empty for automatic detection.", GH_ParamAccess.item); parameters[9].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddTextParameter("Radiance files", "Rad", "Generated .rad paths.", GH_ParamAccess.list);
        parameters.AddTextParameter("Data files", "DAT", "Generated .dat paths.", GH_ParamAccess.list);
        parameters.AddTextParameter("Log", "Log", "Command and conversion log.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        string ies = string.Empty, name = string.Empty, project = string.Empty, dat = string.Empty, radianceBin = string.Empty;
        double r = 1, g = 1, b = 1, multiplier = 0; var run = false;
        if (!dataAccess.GetData(0, ref ies) || !dataAccess.GetData(6, ref project)) return;
        dataAccess.GetData(1, ref name); dataAccess.GetData(2, ref r); dataAccess.GetData(3, ref g); dataAccess.GetData(4, ref b); dataAccess.GetData(5, ref multiplier); dataAccess.GetData(7, ref dat); dataAccess.GetData(8, ref run); dataAccess.GetData(9, ref radianceBin);
        if (!File.Exists(ies)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "IES path was not found."); return; }
        name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(ies) : name.Trim();
        var outputStem = SanitizeStem(name, Path.GetFileNameWithoutExtension(ies));
        var (nr, ng, nb) = Normalize(r, g, b);
        project = Path.GetFullPath(project);
        var outputFolder = Path.Combine(project, "Luminaire_files");
        Directory.CreateDirectory(outputFolder);
        var command = $"ies2rad -o {outputStem} -t default{(multiplier != 0 ? $" -m {multiplier}" : string.Empty)} {ies}";
        if (!run) { dataAccess.SetData(2, $"Waiting for Run. {command}"); return; }
        try
        {
            var existingFiles = Directory.EnumerateFiles(outputFolder).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var executable = FindIes2Rad(radianceBin);
            if (executable is null) throw new FileNotFoundException("ies2rad.exe was not found. Provide the Radiance bin folder.");
            var start = new ProcessStartInfo(executable) { WorkingDirectory = outputFolder, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add("-o"); start.ArgumentList.Add(outputStem); start.ArgumentList.Add("-t"); start.ArgumentList.Add("default");
            if (multiplier != 0) { start.ArgumentList.Add("-m"); start.ArgumentList.Add(multiplier.ToString(System.Globalization.CultureInfo.InvariantCulture)); }
            start.ArgumentList.Add(ies);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start ies2rad.");
            var stdout = process.StandardOutput.ReadToEnd(); var stderr = process.StandardError.ReadToEnd(); process.WaitForExit();
            if (process.ExitCode != 0) throw new InvalidOperationException($"ies2rad failed: {stderr}");
            var generatedFiles = Directory.EnumerateFiles(outputFolder).Where(path => !existingFiles.Contains(path)).ToList();
            var radFiles = generatedFiles.Where(path => string.Equals(Path.GetExtension(path), ".rad", StringComparison.OrdinalIgnoreCase)).ToList();
            var datFiles = generatedFiles.Where(path => string.Equals(Path.GetExtension(path), ".dat", StringComparison.OrdinalIgnoreCase)).ToList();
            if (radFiles.Count == 0) radFiles = Directory.EnumerateFiles(outputFolder, "*.rad").OrderByDescending(File.GetLastWriteTimeUtc).Take(1).ToList();
            if (datFiles.Count == 0) datFiles = Directory.EnumerateFiles(outputFolder, "*.dat").OrderByDescending(File.GetLastWriteTimeUtc).Take(1).ToList();
            var datPath = File.Exists(dat) ? Path.GetFullPath(dat).Replace('\\', '/') : datFiles.FirstOrDefault()?.Replace('\\', '/');
            foreach (var radFile in radFiles) RewriteRad(radFile, nr, ng, nb, datPath);
            dataAccess.SetDataList(0, radFiles); dataAccess.SetDataList(1, datFiles);
            dataAccess.SetData(2, $"{command}\n{stdout}\nNormalized RGB: {nr:0.######}, {ng:0.######}, {nb:0.######}");
        }
        catch (Exception exception) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exception.Message); }
    }

    private static (double R, double G, double B) Normalize(double r, double g, double b)
    {
        var total = r * .265 + g * .67 + b * .065;
        return total <= 0 ? (1, 1, 1) : (r * .265 / total, g * .67 / total, b * .065 / total);
    }
    private static string? FindIes2Rad(string binFolder)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(binFolder)) candidates.Add(Path.Combine(binFolder, "ies2rad.exe"));
        candidates.AddRange((Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Select(folder => Path.Combine(folder.Trim(), "ies2rad.exe")));
        candidates.Add(@"C:\Program Files\ladybug_tools\radiance\bin\ies2rad.exe");
        candidates.Add(@"C:\Radiance\bin\ies2rad.exe");
        return candidates.FirstOrDefault(File.Exists);
    }
    private static string SanitizeStem(string name, string fallback)
    {
        var stem = Regex.Replace(name, @"[^A-Za-z0-9._-]+", "_").Trim('_', '.');
        return string.IsNullOrWhiteSpace(stem) ? fallback : stem;
    }
    private static void RewriteRad(string path, double r, double g, double b, string? datPath)
    {
        var rgb = new Regex(@"^\s*3\s+[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?\s+[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?\s+[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?\s*$");
        var lines = File.ReadAllLines(path).Select(line => rgb.IsMatch(line) ? $"3 {r:0.############} {g:0.############} {b:0.############}" : line).ToList();
        if (!string.IsNullOrWhiteSpace(datPath)) lines = lines.Select(line => Regex.Replace(line, @"(?i)\S+\.dat", $"\"{datPath}\"")).ToList();
        File.WriteAllLines(path, lines);
    }
}
