using System.Reflection;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

public abstract class OpaqueMaterialSelectorComponent : GH_Component
{
    protected OpaqueMaterialSelectorComponent(string name, string nickname, string description, Guid id)
        : base(name, nickname, description, "FlahaGrow", "Materials") => ComponentId = id;

    private Guid ComponentId { get; }
    public override Guid ComponentGuid => ComponentId;

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddBooleanParameter("Run", "Run", "Open the material selector.", GH_ParamAccess.item, false);
        parameters.AddTextParameter("RadMaterials folder", "Materials", "Optional RadMaterials folder. Leave empty to use the bundled library.", GH_ParamAccess.item);
        parameters[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters) =>
        parameters.AddTextParameter("Modifier", "Modifier", "Selected Radiance material modifier.", GH_ParamAccess.item);

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var run = false;
        var folder = string.Empty;
        dataAccess.GetData(0, ref run);
        dataAccess.GetData(1, ref folder);
        if (!run)
        {
            return;
        }

        folder = string.IsNullOrWhiteSpace(folder)
            ? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "shared", "Library", "FlahaGrow_Library_Small", "RadMaterials")
            : Path.GetFullPath(folder);
        if (!Directory.Exists(folder))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "RadMaterials folder was not found.");
            return;
        }

        var selected = MaterialSelectionDialog.Select(folder);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            dataAccess.SetData(0, selected);
        }
    }
}

public sealed class FacadeMaterialComponent : OpaqueMaterialSelectorComponent
{
    public FacadeMaterialComponent() : base("Facade Material", "Facade Mat", "Selects a Radiance opaque material for the façade.", new Guid("29e2836f-5da0-4e4c-bdac-990365a0471e")) { }
}

public sealed class FrameMaterialComponent : OpaqueMaterialSelectorComponent
{
    public FrameMaterialComponent() : base("Frame Material", "Frame Mat", "Selects a Radiance opaque material for the frame.", new Guid("97544a63-5ca5-4255-bd41-8b8ea8d0a2ef")) { }
}

public sealed class GroundMaterialComponent : OpaqueMaterialSelectorComponent
{
    public GroundMaterialComponent() : base("Ground Material", "Ground Mat", "Selects a Radiance opaque material for the ground.", new Guid("747f6a35-6fbc-4231-a042-75fb7a18f7b4")) { }
}

public sealed class ConcreteMaterialComponent : OpaqueMaterialSelectorComponent
{
    public ConcreteMaterialComponent() : base("Concrete Material", "Concrete Mat", "Selects a Radiance opaque material for concrete.", new Guid("2befd6cd-dde7-4e45-9f0d-2ff0089c065d")) { }
}

internal static class MaterialSelectionDialog
{
    public static string? Select(string folder)
    {
        var rows = Directory.EnumerateFiles(folder, "*.rad")
            .Select(path => Parse(path))
            .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        using var form = new Form { Text = "FlahaGrow Radiance Material", Width = 900, Height = 600, StartPosition = FormStartPosition.CenterScreen };
        using var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        foreach (var title in new[] { "Material", "R", "G", "B", "Specularity", "Roughness", "VLR" }) grid.Columns.Add(title, title);
        foreach (var row in rows)
        {
            var index = grid.Rows.Add(row.Name, row.R, row.G, row.B, row.Specularity, row.Roughness, row.Vlr);
            grid.Rows[index].Tag = row.Name;
        }
        var select = new Button { Text = "Select", Dock = DockStyle.Bottom, Height = 36, DialogResult = DialogResult.OK };
        form.Controls.Add(grid);
        form.Controls.Add(select);
        form.AcceptButton = select;
        return form.ShowDialog() == DialogResult.OK && grid.SelectedRows.Count > 0 ? grid.SelectedRows[0].Tag as string : null;
    }

    private static MaterialRow Parse(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var values = new double[5];
        foreach (var line in File.ReadLines(path).Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith("#")))
        {
            var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 3 && tokens[0] == "void") name = tokens[2];
            if (tokens.Length >= 6 && tokens[0] == "5")
            {
                for (var index = 0; index < 5; index++) double.TryParse(tokens[index + 1], out values[index]);
                break;
            }
        }
        return new MaterialRow(name, values[0].ToString("0.000"), values[1].ToString("0.000"), values[2].ToString("0.000"), values[3].ToString("0.00"), values[4].ToString("0.00"), (0.265 * values[0] + 0.670 * values[1] + 0.065 * values[2]).ToString("0.0"));
    }

    private sealed record MaterialRow(string Name, string R, string G, string B, string Specularity, string Roughness, string Vlr);
}
