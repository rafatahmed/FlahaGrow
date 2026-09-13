using System.Reflection;
using System.Globalization;
using System.Windows.Forms;
using GH_IO.Serialization;
using FlahaGrow.Core.Projects;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

public sealed class OpaqueMaterialComponent : FlahaGrowComponent
{
    private string? selected;
    public OpaqueMaterialComponent() : base("Opaque Material", "Opaque Mat", "Selects a Radiance opaque material for any opaque surface.", "FlahaGrow", "01 Materials") { }
    public override Guid ComponentGuid => new("29e2836f-5da0-4e4c-bdac-990365a0471e");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddBooleanParameter("Run", "Run", "Open the material selector.", GH_ParamAccess.item, false);
        parameters.AddTextParameter("RadMaterials folder", "Materials", "Optional RadMaterials folder, FlahaGrow library root, or its containing folder. Leave empty to use the bundled library.", GH_ParamAccess.item);
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
        if (!run) { if (!string.IsNullOrWhiteSpace(selected)) dataAccess.SetData(0, selected); return; }

        try
        {
            folder = new LibraryPathResolver().ResolveSection(string.IsNullOrWhiteSpace(folder)
                ? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "shared", "Library", "FlahaGrow_Library_Small", LibraryPathResolver.Materials)
                : Path.GetFullPath(folder), LibraryPathResolver.Materials);
        }
        catch (Exception exception) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exception.Message); return; }

        if (!string.IsNullOrWhiteSpace(selected = MaterialSelectionDialog.SelectOpaque(folder)))
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
}

internal static class MaterialSelectionDialog
{
    private static readonly string[] OpaqueColumns = { "Material", "R", "G", "B", "Specularity", "Roughness", "VLR" };

    internal static string? SelectOpaque(string folder) => Select(
        "FlahaGrowRadiance Material",
        folder,
        "Select a material!",
        "RGB(0.00, 0.00, 0.00) | VLR: 0.0%",
        OpaqueColumns,
        () => Directory.EnumerateFiles(folder, "*.rad")
            .Select(Parse)
            .OrderBy(row => string.IsNullOrEmpty(row.Name) || char.IsDigit(row.Name[0]))
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .Select(row => new MaterialTableRow(row.Name,
                new[] { DisplayName(row.Name), row.R, row.G, row.B, row.Specularity, row.Roughness, row.Vlr },
                $"RGB({row.R}, {row.G}, {row.B}) | VLR: {row.Vlr}%"))
            .ToList());

    internal static string? SelectGlazing(string folder, Func<IEnumerable<MaterialTableRow>> loadRows) => Select(
        "FlahaGrowRadiance Glazing",
        folder,
        "Select a glazing!",
        "RGB(0.00, 0.00, 0.00) | VLT: 0.0% | VLR: 0.0%",
        new[] { "Glazing", "R", "G", "B", "VLT", "VLR%", "Specularity", "Roughness" },
        () => loadRows().ToList());

    private static string? Select(string title, string folder, string initialName, string initialInfo, IReadOnlyList<string> columns, Func<List<MaterialTableRow>> loadRows)
    {
        using var form = new Form { Text = title, Width = 1500, Height = 850, StartPosition = FormStartPosition.CenterScreen, BackColor = System.Drawing.Color.White };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = System.Drawing.Color.White };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 300)); layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = System.Drawing.Color.White };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300)); top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); top.RowStyles.Add(new RowStyle(SizeType.Percent, 80)); top.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
        var preview = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = System.Drawing.Color.White };
        var name = new Label { Text = initialName, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.BottomLeft, Font = new System.Drawing.Font("Segoe UI", 20, System.Drawing.FontStyle.Bold) };
        var info = new Label { Text = initialInfo, Dock = DockStyle.Fill, TextAlign = System.Drawing.ContentAlignment.TopLeft, Font = new System.Drawing.Font("Segoe UI", 10) };
        top.Controls.Add(preview, 0, 0); top.Controls.Add(name, 1, 0); top.Controls.Add(info, 1, 1);
        var grid = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, MultiSelect = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false, AllowUserToResizeRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BackgroundColor = System.Drawing.Color.White, GridColor = System.Drawing.Color.LightGray, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal };
        grid.ColumnHeadersDefaultCellStyle.Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold); grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing; grid.ColumnHeadersHeight = 40;
        foreach (var column in columns) grid.Columns.Add(column, column);
        if (grid.Columns.Count > 0) grid.Columns[0].Width = 80;
        void Load()
        {
            grid.Rows.Clear(); name.Text = initialName; info.Text = initialInfo; preview.Image?.Dispose(); preview.Image = null;
            foreach (var row in loadRows()) { var index = grid.Rows.Add(row.Cells); grid.Rows[index].Tag = row; }
        }
        void UpdatePreview()
        {
            if (grid.SelectedRows.Count == 0 || grid.SelectedRows[0].Tag is not MaterialTableRow row) return;
            name.Text = DisplayName(row.Modifier); info.Text = row.Info;
            preview.Image?.Dispose(); preview.Image = null;
            var bitmap = Path.Combine(folder, row.Modifier + "_b.bmp");
            if (File.Exists(bitmap)) { using var image = System.Drawing.Image.FromFile(bitmap); preview.Image = new System.Drawing.Bitmap(image); }
        }
        grid.SelectionChanged += (_, _) => UpdatePreview();
        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = new Padding(8, 6, 8, 6) };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80)); buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1)); buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        var select = new Button { Text = "Select", Dock = DockStyle.Fill, MinimumSize = new System.Drawing.Size(120, 28) };
        var reload = new Button { Text = "Reload", Dock = DockStyle.Fill, MinimumSize = new System.Drawing.Size(120, 28) };
        select.Click += (_, _) => { if (grid.SelectedRows.Count > 0) form.DialogResult = DialogResult.OK; };
        reload.Click += (_, _) => Load();
        buttons.Controls.Add(select, 0, 0); buttons.Controls.Add(reload, 2, 0);
        layout.Controls.Add(top, 0, 0); layout.Controls.Add(grid, 0, 1); layout.Controls.Add(buttons, 0, 2); form.Controls.Add(layout); form.AcceptButton = select;
        Load(); var result = form.ShowDialog(); preview.Image?.Dispose();
        return result == DialogResult.OK && grid.SelectedRows.Count > 0 && grid.SelectedRows[0].Tag is MaterialTableRow selected ? selected.Modifier : null;
    }

    internal static string DisplayName(string modifier) => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(modifier.Replace("_", " ").ToLowerInvariant());

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
                for (var index = 0; index < 5; index++) double.TryParse(tokens[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out values[index]);
                break;
            }
        }
        return new MaterialRow(name, values[0].ToString("0.000"), values[1].ToString("0.000"), values[2].ToString("0.000"), values[3].ToString("0.00"), values[4].ToString("0.00"), (0.265 * values[0] + 0.670 * values[1] + 0.065 * values[2]).ToString("0.0"));
    }

    private sealed record MaterialRow(string Name, string R, string G, string B, string Specularity, string Roughness, string Vlr);
}

internal sealed record MaterialTableRow(string Modifier, string[] Cells, string Info);
