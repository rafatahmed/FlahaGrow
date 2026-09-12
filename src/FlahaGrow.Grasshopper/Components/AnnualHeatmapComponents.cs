using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Classified annual viewer and PNG exporter for hourly, daily, or monthly series.</summary>
public abstract class AnnualHeatmapComponent : FlahaGrowComponent
{
    private readonly string defaultTitle;
    private static readonly HashSet<Form> OpenForms = new();
    protected AnnualHeatmapComponent(string name, string nick, string description, string title, Guid guid) : base(name, nick, description, "FlahaGrow", "03 Annual") { defaultTitle = title; Id = guid; }
    private Guid Id { get; }
    public override Guid ComponentGuid => Id;
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddNumberParameter("Annual results", "Data", "Annual values: 8,760 hourly, 365 daily, or 12 monthly values.", GH_ParamAccess.list);
        p.AddNumberParameter("Range 1", "R1", "First inclusive threshold.", GH_ParamAccess.item, 0);
        p.AddNumberParameter("Range 2", "R2", "Second inclusive threshold.", GH_ParamAccess.item, 10);
        p.AddNumberParameter("Range 3", "R3", "Third inclusive threshold.", GH_ParamAccess.item, 20);
        p.AddNumberParameter("Range 4", "R4", "Fourth inclusive threshold.", GH_ParamAccess.item, 50);
        p.AddIntegerParameter("Grid mode", "Grid", "0 plain, 1 inset, 2 grid, 3 dark grid.", GH_ParamAccess.item, 0);
        p.AddColourParameter("Grid color", "Grid color", "Grid color for mode 2.", GH_ParamAccess.item, Color.LightGray); p[6].Optional = true;
        p.AddTextParameter("Range 1 name", "Name 1", "Optional legend label.", GH_ParamAccess.item, "Imperceptible");
        p.AddTextParameter("Range 2 name", "Name 2", "Optional legend label.", GH_ParamAccess.item, "Perceptible");
        p.AddTextParameter("Range 3 name", "Name 3", "Optional legend label.", GH_ParamAccess.item, "Disturbing");
        p.AddTextParameter("Range 4 name", "Name 4", "Optional legend label.", GH_ParamAccess.item, "Intolerable");
        p.AddTextParameter("Range 5 name", "Name 5", "Optional legend label.", GH_ParamAccess.item, "Excessive");
        p.AddTextParameter("Graph title", "Title", "Optional heatmap title.", GH_ParamAccess.item, defaultTitle); p[12].Optional = true;
        p.AddBooleanParameter("Run", "Run", "Set True to open the annual heatmap and PNG exporter.", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p) => p.AddTextParameter("Status", "Status", "Heatmap status.", GH_ParamAccess.item);
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var data = new List<double>(); var ranges = new[] { 0.0, 10.0, 20.0, 50.0 }; var gridMode = 0; var gridColor = Color.LightGray; var names = new[] { "Imperceptible", "Perceptible", "Disturbing", "Intolerable", "Excessive" }; var title = defaultTitle; var run = false;
        if (!da.GetDataList(0, data)) return;
        for (var i = 0; i < 4; i++) da.GetData(i + 1, ref ranges[i]);
        da.GetData(5, ref gridMode); da.GetData(6, ref gridColor);
        for (var i = 0; i < names.Length; i++) da.GetData(i + 7, ref names[i]);
        da.GetData(12, ref title); da.GetData(13, ref run);
        if (!run) { da.SetData(0, "Set Run True to open the annual heatmap."); return; }
        try
        {
            if (data.Count is not (8760 or 365 or 12)) throw new InvalidDataException($"Annual results contain {data.Count} values. Supply exactly 8,760 hourly, 365 daily, or 12 monthly values.");
            if (ranges.Any(double.IsNaN) || ranges.Any(double.IsInfinity) || ranges[0] > ranges[1] || ranges[1] > ranges[2] || ranges[2] > ranges[3]) throw new InvalidDataException("Ranges must be ascending (R1 ≤ R2 ≤ R3 ≤ R4).");
            gridMode = Math.Clamp(gridMode, 0, 3); title = string.IsNullOrWhiteSpace(title) ? defaultTitle : title.Trim();
            var form = new AnnualHeatmapForm(data, ranges, gridMode, gridColor, names, title); OpenForms.Add(form); form.FormClosed += (_, _) => OpenForms.Remove(form); form.Show();
            da.SetData(0, $"Opened {title}: {AnnualHeatmapForm.ResolutionName(data.Count)} annual values.");
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}

public sealed class AnnualPlotComponent : AnnualHeatmapComponent
{
    public AnnualPlotComponent() : base("Annual Plot", "Annual Plot", "Displays 8,760 hourly, 365 daily, or 12 monthly annual illuminance values and exports PNG.", "Annual Illuminance", new Guid("5747b67c-4aec-4117-83a2-5e30a7308920")) { }
}

public sealed class AnnualPpfdPlotComponent : AnnualHeatmapComponent
{
    public AnnualPpfdPlotComponent() : base("Annual Plot PPFD for Sensor", "PPFD Plot", "Displays 8,760 hourly, 365 daily, or 12 monthly annual PPFD/DLI values and exports PNG.", "Annual PPFD", new Guid("ce9c1e5d-c2ce-4c29-9c7d-277d19d25e42")) { }
}

internal sealed class AnnualHeatmapForm : Form
{
    private readonly IReadOnlyList<double> data, ranges;
    private readonly int mode;
    private readonly Color gridColor;
    private readonly string[] names;
    private readonly int columns, rows, cellWidth, cellHeight;
    private readonly string resolution;
    private const int MarginLeft = 30, MarginTop = 40, TitleHeight = 25, MarginRight = 150, MarginBottom = 180;
    private readonly Color[] colors;
    private readonly Label hover = new() { AutoSize = true };
    internal AnnualHeatmapForm(IReadOnlyList<double> data, IReadOnlyList<double> ranges, int mode, Color gridColor, string[] names, string title)
    {
        this.data = data; this.ranges = ranges; this.mode = mode; this.gridColor = mode == 3 ? Color.FromArgb(99, 99, 99) : gridColor; this.names = names;
        (resolution, columns, rows, cellWidth, cellHeight) = data.Count switch
        {
            8760 => ("hourly", 365, 24, 4, 20),
            365 => ("daily", 365, 1, 4, 40),
            12 => ("monthly", 12, 1, 80, 40),
            _ => throw new ArgumentOutOfRangeException(nameof(data), "Unsupported annual result count.")
        };
        colors = new[] { mode == 3 ? Color.FromArgb(128, 128, 128) : Color.White, Color.FromArgb(249, 235, 171), Color.FromArgb(240, 190, 57), Color.FromArgb(228, 104, 40), Color.FromArgb(215, 14, 23) };
        Text = title; StartPosition = FormStartPosition.CenterScreen; FormBorderStyle = FormBorderStyle.Sizable; DoubleBuffered = true; BackColor = mode == 3 ? Color.Gray : Color.White;
        ClientSize = new Size(MarginLeft + columns * cellWidth + MarginRight, MarginTop + TitleHeight + rows * cellHeight + MarginBottom);
        Paint += (_, e) => DrawHeatmap(e.Graphics); MouseMove += OnMouseMove;
        var export = new Button { Text = "Export PNG", Height = 30, Dock = DockStyle.Bottom }; export.Click += (_, _) => ExportPng(); Controls.Add(export);
        hover.Location = new Point(120, MarginTop + TitleHeight + rows * cellHeight + 75); Controls.Add(hover);
    }
    internal static string ResolutionName(int count) => count switch { 8760 => "8,760 hourly", 365 => "365 daily", 12 => "12 monthly", _ => "unsupported" };
    private int Classify(double value) => value <= ranges[0] ? 0 : value <= ranges[1] ? 1 : value <= ranges[2] ? 2 : value <= ranges[3] ? 3 : 4;
    private void DrawHeatmap(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.Default; g.PixelOffsetMode = PixelOffsetMode.HighSpeed;
        var inset = mode == 1 ? .5f : 0f; var grid = mode is 2 or 3;
        using var pen = new Pen(gridColor);
        for (var row = 0; row < rows; row++) for (var column = 0; column < columns; column++)
        {
            var x = MarginLeft + column * cellWidth; var y = MarginTop + TitleHeight + row * cellHeight;
            using var brush = new SolidBrush(colors[Classify(data[column * rows + row])]);
            g.FillRectangle(brush, x + inset, y + inset, cellWidth - 2 * inset, cellHeight - 2 * inset);
            if (grid) g.DrawRectangle(pen, x, y, cellWidth, cellHeight);
        }
        var monthStarts = new[] { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 }; var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        if (resolution is "hourly" or "daily")
            for (var i = 0; i < monthStarts.Length; i++) { var x = MarginLeft + monthStarts[i] * cellWidth; g.DrawLine(Pens.Gray, x, MarginTop + TitleHeight, x, MarginTop + TitleHeight + rows * cellHeight); g.DrawString(monthNames[i], Font, Brushes.Black, x + 4, MarginTop + TitleHeight + rows * cellHeight + 5); }
        else
            for (var i = 0; i < monthNames.Length; i++) g.DrawString(monthNames[i], Font, Brushes.Black, MarginLeft + i * cellWidth + 5, MarginTop + TitleHeight + rows * cellHeight + 5);
        if (resolution == "hourly") foreach (var hour in new[] { 0, 6, 12, 18, 24 }) g.DrawString($"{hour:00}:00", Font, Brushes.Black, MarginLeft + columns * cellWidth + 5, MarginTop + TitleHeight + hour * cellHeight - 8);
        else g.DrawString(resolution == "daily" ? "Daily" : "Monthly", Font, Brushes.Black, MarginLeft + columns * cellWidth + 5, MarginTop + TitleHeight + cellHeight / 2 - 8);
        g.DrawString(Text, new Font(Font.FontFamily, 10), Brushes.Black, MarginLeft, 10); g.DrawString(resolution == "monthly" ? "Month" : "Day of Year", Font, Brushes.Black, MarginLeft + Math.Max(0, columns * cellWidth - 130), MarginTop + TitleHeight + rows * cellHeight + 55);
        var counts = new int[5]; foreach (var value in data) counts[Classify(value)]++; var xLegend = MarginLeft;
        for (var i = 0; i < 5; i++) { using var brush = new SolidBrush(colors[i]); var label = $"{Math.Round(counts[i] * 100d / data.Count)}% {names[i]}"; g.FillRectangle(brush, xLegend, MarginTop + TitleHeight + rows * cellHeight + 85, 20, 15); g.DrawString(label, Font, Brushes.Black, xLegend + 25, MarginTop + TitleHeight + rows * cellHeight + 85); xLegend += 25 + (int)g.MeasureString(label, Font).Width + 35; }
    }
    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        var column = (e.X - MarginLeft) / cellWidth; var row = (e.Y - MarginTop - TitleHeight) / cellHeight;
        if (column is < 0 or >= 365 || row is < 0 or >= 24 || column >= columns || row >= rows) return;
        var value = data[column * rows + row];
        hover.Text = resolution switch { "hourly" => $"Day {column + 1}, Hour {row}: {value:0.##}", "daily" => $"Day {column + 1}: {value:0.##}", _ => $"Month {column + 1}: {value:0.##}" };
    }
    private void ExportPng()
    {
        using var dialog = new SaveFileDialog { Filter = "PNG Image|*.png", FileName = Text };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        using var bitmap = new Bitmap(ClientSize.Width, ClientSize.Height); using var graphics = Graphics.FromImage(bitmap); graphics.Clear(BackColor); DrawHeatmap(graphics); bitmap.Save(dialog.FileName);
        MessageBox.Show($"Saved successfully to:\n{dialog.FileName}");
    }
}
