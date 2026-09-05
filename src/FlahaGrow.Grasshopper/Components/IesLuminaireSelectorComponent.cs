using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Selects an LM-63 IES luminaire from the bundled or supplied IES library.</summary>
public sealed class IesLuminaireSelectorComponent : GH_Component
{
    public IesLuminaireSelectorComponent() : base("Select IES Luminaire", "IES Select", "Selects an IES grow-light luminaire.", "FlahaGrow", "Electric Light") { }
    public override Guid ComponentGuid => new("492e14e7-163e-4c2a-a6d8-c44184da664d");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddBooleanParameter("Run", "Run", "Open the IES luminaire selector.", GH_ParamAccess.item, false);
        parameters.AddTextParameter("RadIES folder", "IES", "Optional IES library folder. Leave empty to use the bundled library.", GH_ParamAccess.item);
        parameters[1].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddTextParameter("IES path", "IES", "Selected IES file path.", GH_ParamAccess.item);
        parameters.AddTextParameter("Luminaire name", "Name", "Selected luminaire identifier.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var run = false;
        var folder = string.Empty;
        dataAccess.GetData(0, ref run);
        dataAccess.GetData(1, ref folder);
        if (!run) return;
        folder = string.IsNullOrWhiteSpace(folder)
            ? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "shared", "Library", "FlahaGrow_Library_Small", "RadIES")
            : Path.GetFullPath(folder);
        if (!Directory.Exists(folder)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "RadIES folder was not found."); return; }

        var entries = Directory.EnumerateFiles(folder).Where(path => string.Equals(Path.GetExtension(path), ".ies", StringComparison.OrdinalIgnoreCase)).Select(Parse).OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase).ToList();
        using var form = new Form { Text = "FlahaGrow IES Selector", Width = 1000, Height = 650, StartPosition = FormStartPosition.CenterScreen };
        using var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
        foreach (var column in new[] { "Name", "Lumens", "CCT", "CRI", "Vertical range", "Horizontal range" }) grid.Columns.Add(column, column);
        foreach (var entry in entries)
        {
            var row = grid.Rows.Add(entry.Name, entry.Lumens, entry.Cct, entry.Cri, entry.VerticalRange, entry.HorizontalRange);
            grid.Rows[row].Tag = entry;
        }
        var select = new Button { Text = "Select", Dock = DockStyle.Bottom, Height = 36, DialogResult = DialogResult.OK };
        form.Controls.Add(grid); form.Controls.Add(select); form.AcceptButton = select;
        if (form.ShowDialog() == DialogResult.OK && grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is IesEntry selected)
        {
            dataAccess.SetData(0, selected.Path);
            dataAccess.SetData(1, selected.Name);
        }
    }

    private static IesEntry Parse(string path)
    {
        var text = File.ReadAllText(path);
        var name = First(text, @"(?im)^\s*\[(?:LUMINAIRE|LUMCAT|LABEL|TEST)\]\s*(.+)$") ?? Path.GetFileNameWithoutExtension(path);
        var cct = First(text, @"(?i)\b(?:CCT\D*|)(\d{4,5})\s*K\b") ?? "—";
        var cri = First(text, @"(?i)\b(?:CRI|Ra)\D*(\d{2,3})\b") ?? "—";
        var tilt = Regex.Match(text, @"(?im)^\s*TILT\s*=.*$(?<data>[\s\S]*)");
        var numeric = tilt.Success ? Regex.Matches(tilt.Groups["data"].Value, @"[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?").Select(match => double.TryParse(match.Value, out var value) ? value : 0).ToList() : new List<double>();
        var lumens = numeric.Count > 1 ? Math.Round(numeric[0] * numeric[1]).ToString() : "—";
        return new IesEntry(path, name.Trim(), lumens, cct, cri, "See IES", "See IES");
    }

    private static string? First(string text, string pattern) => Regex.Match(text, pattern).Success ? Regex.Match(text, pattern).Groups[1].Value.Trim() : null;
    private sealed record IesEntry(string Path, string Name, string Lumens, string Cct, string Cri, string VerticalRange, string HorizontalRange);
}
