using System.Reflection;
using System.Globalization;
using System.Windows.Forms;
using GH_IO.Serialization;
using FlahaGrow.Core.Projects;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Selects a Radiance glazing modifier from the bundled or supplied glazing library.</summary>
public sealed class GlazingMaterialComponent : FlahaGrowComponent
{
    private string? selected;
    public GlazingMaterialComponent()
        : base("Glazing Material", "Glazing Mat", "Selects a Radiance glazing modifier and reports its visual properties.", "FlahaGrow", "01 Materials")
    {
    }

    public override Guid ComponentGuid => new("53402415-6620-4ecd-bfa3-593e7a148f29");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddBooleanParameter("Run", "Run", "Open the glazing selector.", GH_ParamAccess.item, false);
        parameters.AddTextParameter("RadGlazing folder", "Glazing", "Optional RadGlazing folder, FlahaGrow library root, or its containing folder. Leave empty to use the bundled library.", GH_ParamAccess.item);
        parameters[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters) =>
        parameters.AddTextParameter("Modifier", "Modifier", "Selected Radiance glazing modifier.", GH_ParamAccess.item);

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var run = false;
        var folder = string.Empty;
        dataAccess.GetData(0, ref run);
        dataAccess.GetData(1, ref folder);
        if (!run) { if (!string.IsNullOrWhiteSpace(selected)) dataAccess.SetData(0, selected); return; }

        try
        {
            folder = new LibraryPathResolver().ResolveSection(string.IsNullOrWhiteSpace(folder)
                ? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "shared", "Library", "FlahaGrow_Library_Small", LibraryPathResolver.Glazing)
                : Path.GetFullPath(folder), LibraryPathResolver.Glazing);
        }
        catch (Exception exception) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exception.Message); return; }

        if (!string.IsNullOrWhiteSpace(selected = MaterialSelectionDialog.SelectGlazing(folder, () =>
            Directory.EnumerateFiles(folder, "*.rad")
                .Select(Parse)
                .OrderBy(row => string.IsNullOrEmpty(row.Name) || char.IsDigit(row.Name[0]))
                .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .Select(row => new MaterialTableRow(row.Name,
                    new[] { MaterialSelectionDialog.DisplayName(row.Name), row.R, row.G, row.B, row.Vlt, row.Vlr, row.Specularity, row.Roughness },
                    $"RGB({row.R}, {row.G}, {row.B}) | VLT: {row.Vlt}% | VLR: {row.Vlr}%")))))
        {
            dataAccess.SetData(0, selected);
        }
    }
    public override bool Write(GH_IWriter writer)
    {
        if (!string.IsNullOrWhiteSpace(selected)) writer.SetString("Selected", selected);
        return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
        selected = reader.ItemExists("Selected") ? reader.GetString("Selected") : null;
        return base.Read(reader);
    }

    private static GlazingRow Parse(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var type = string.Empty;
        var values = new List<double>();
        foreach (var line in File.ReadLines(path).Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith("#")))
        {
            var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 3 && tokens[0] == "void") { type = tokens[1].ToLowerInvariant(); name = tokens[2]; }
            foreach (var token in tokens) if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)) values.Add(value);
        }
        var r = values.Count > 0 ? values[0] : 0;
        var g = values.Count > 1 ? values[1] : 0;
        var b = values.Count > 2 ? values[2] : 0;
        var index = type == "glass" && values.Count > 3 && values[3] is >= 1.2 and <= 2.2 ? values[3] : 1.52;
        var specularity = type == "trans" && values.Count > 3 ? Math.Clamp(values[3], 0, 1) : 0;
        var roughness = type == "trans" && values.Count > 4 ? Math.Clamp(values[4], 0, 1) : 0;
        return new GlazingRow(name, r.ToString("0.000"), g.ToString("0.000"), b.ToString("0.000"), (0.265 * r + 0.670 * g + 0.065 * b).ToString("0.0"), (100 * Math.Pow((index - 1) / (index + 1), 2)).ToString("0.0"), specularity.ToString("0.00"), roughness.ToString("0.00"));
    }

    private sealed record GlazingRow(string Name, string R, string G, string B, string Vlt, string Vlr, string Specularity, string Roughness);
}
