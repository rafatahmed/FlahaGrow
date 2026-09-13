using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Grasshopper.Kernel;
using GH_IO.Serialization;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.PlantLight;
using FlahaGrow.Grasshopper.Parameters;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Classified annual viewer and PNG exporter for hourly, daily, or monthly series.</summary>
public sealed class AnnualPlotComponent : FlahaGrowComponent
{
    private const string defaultTitle = "Annual results — connect Plot Attributes";
    private readonly ActionLatch openLatch = new();
    private static readonly HashSet<Form> OpenForms = new();
    public AnnualPlotComponent() : base("Annual Plot", "Annual Plot", "Displays annual illuminance, PPFD or DLI with matching Plot Attributes and exports PNG.", "FlahaGrow", "03 Annual") { }
    public override Guid ComponentGuid => new("5747b67c-4aec-4117-83a2-5e30a7308920");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddNumberParameter("Annual results", "Data", "Annual values: 8,760 hourly, 365 daily, or 12 monthly values.", GH_ParamAccess.list);
        p.AddNumberParameter("Range 1", "R1", "First inclusive threshold.", GH_ParamAccess.item, 0);
        p.AddNumberParameter("Range 2", "R2", "Second inclusive threshold.", GH_ParamAccess.item, 10);
        p.AddNumberParameter("Range 3", "R3", "Third inclusive threshold.", GH_ParamAccess.item, 20);
        p.AddNumberParameter("Range 4", "R4", "Fourth inclusive threshold.", GH_ParamAccess.item, 50);
        p.AddIntegerParameter("Grid mode", "Grid", "0 plain, 1 inset, 2 grid, 3 dark grid.", GH_ParamAccess.item, 0);
        p.AddColourParameter("Grid color", "Grid color", "Grid color for mode 2.", GH_ParamAccess.item, Color.LightGray); p[6].Optional = true;
        p.AddTextParameter("Range 1 name", "Name 1", "Optional user label, not a crop-quality claim. Exact numeric interval is always shown.", GH_ParamAccess.item, "Bin 1");
        p.AddTextParameter("Range 2 name", "Name 2", "Optional user label; exact numeric interval is always shown.", GH_ParamAccess.item, "Bin 2");
        p.AddTextParameter("Range 3 name", "Name 3", "Optional user label; exact numeric interval is always shown.", GH_ParamAccess.item, "Bin 3");
        p.AddTextParameter("Range 4 name", "Name 4", "Optional user label; exact numeric interval is always shown.", GH_ParamAccess.item, "Bin 4");
        p.AddTextParameter("Range 5 name", "Name 5", "Optional user label; exact numeric interval is always shown.", GH_ParamAccess.item, "Bin 5");
        p.AddTextParameter("Graph title", "Title", "Optional heatmap title.", GH_ParamAccess.item, defaultTitle); p[12].Optional = true;
        p.AddBooleanParameter("Run", "Run", "Connect a Button: False → True opens a snapshot. Changes do not update an already open plot; click again to open the current data.", GH_ParamAccess.item, false);
        p.AddParameter(new PlotAttributesParameter(), "Plot Attributes", "Plot", "Connect the same reader's Plot output as the values connected to Data. Automatically supplies title, units and ranges; mismatched data is rejected.", GH_ParamAccess.item); p[14].Optional = true;
        p.AddBooleanParameter("Use manual ranges", "Manual", "False: automatic min-to-max ranges with Plot attributes. True: use R1–R4. Legacy numeric-only plots retain their existing ranges.", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p) => p.AddTextParameter("Status", "Status", "Heatmap status.", GH_ParamAccess.item);
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var data = new List<double>(); var ranges = new[] { 0.0, 10.0, 20.0, 50.0 }; var gridMode = 0; var gridColor = Color.LightGray; var names = new[] { "Bin 1", "Bin 2", "Bin 3", "Bin 4", "Bin 5" }; var title = defaultTitle; var run = false;
        if (!da.GetDataList(0, data)) return;
        for (var i = 0; i < 4; i++) da.GetData(i + 1, ref ranges[i]);
        da.GetData(5, ref gridMode); da.GetData(6, ref gridColor);
        for (var i = 0; i < names.Length; i++) da.GetData(i + 7, ref names[i]);
        da.GetData(12, ref title); da.GetData(13, ref run);
        var open = openLatch.Observe(run);
        try
        {
            var attributes = new PlotAttributesGoo(); bool manual = false; da.GetData(15, ref manual);
            var supplied = da.GetData(14, ref attributes);
            if ((supplied || Params.Input[14].SourceCount > 0) && !attributes.IsValid)
                throw new ArgumentException("Connected Plot Attributes are invalid or unresolved; provide the matching reader output.");
            if (attributes.IsValid)
            {
                var a = attributes.Value; a.RequireMatching(data);
                if (a.Shape is not ("annual-hourly" or "annual-daily" or "annual-monthly")) throw new ArgumentException("Annual Plot requires an annual series, not a selected-hour/day sensor grid. Use Annual PPFD/DLI at Sensor.");
                if (!manual) ranges = a.AutomaticRanges;
                if (title == defaultTitle || string.IsNullOrWhiteSpace(title)) title = a.Title;
            }
            AnnualPlotData.Validate(data, ranges);
            if (!open)
            {
                da.SetData(0, attributes.IsValid
                    ? $"Ready: {title}; {(manual ? "manual" : "automatic")} ranges [{string.Join(", ", ranges.Select(v => v.ToString("G6", System.Globalization.CultureInfo.InvariantCulture)))}]. Click Button → Run. {attributes.Value.Provenance}"
                    : "Values validated. Connect matching Plot Attributes for automatic title/units/ranges, or set legacy Title and R1–R4. Click Button → Run.");
                return;
            }
            gridMode = Math.Clamp(gridMode, 0, 3); title = string.IsNullOrWhiteSpace(title) ? defaultTitle : title.Trim();
            var form = new AnnualHeatmapForm(data, ranges, gridMode, gridColor, names, title); OpenForms.Add(form); form.FormClosed += (_, _) => OpenForms.Remove(form); form.Show();
            da.SetData(0, $"Opened {title}: {AnnualHeatmapForm.ResolutionName(data.Count)} annual values.");
        }
        catch (Exception ex) { da.SetData(0, ex.Message); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    public override bool Read(GH_IReader reader) { openLatch.Disarm(); return base.Read(reader); }
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
    private int Classify(double value) => AnnualPlotData.Classify(value, ranges);
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
        using var titleFont = new Font(Font.FontFamily, 10);
        g.DrawString(Text, titleFont, Brushes.Black, MarginLeft, 10); g.DrawString(resolution == "monthly" ? "Month" : "Day of Year", Font, Brushes.Black, MarginLeft + Math.Max(0, columns * cellWidth - 130), MarginTop + TitleHeight + rows * cellHeight + 55);
        var counts = new int[5]; foreach (var value in data) counts[Classify(value)]++; var xLegend = MarginLeft;
        for (var i = 0; i < 5; i++)
        {
            using var brush = new SolidBrush(colors[i]);
            var label = $"{counts[i]}/{data.Count} ({counts[i] * 100d / data.Count:0.##}%) {AnnualPlotData.IntervalLabel(i, ranges)}";
            var y = MarginTop + TitleHeight + rows * cellHeight + 85 + i * 17;
            g.FillRectangle(brush, xLegend, y, 20, 15); g.DrawString(label, Font, Brushes.Black, xLegend + 25, y);
            g.DrawString("User label: " + names[i], Font, Brushes.Black, xLegend + 430, y);
        }
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
