using System.Globalization;
using System.Security.Cryptography;
using System.Windows.Forms;
using GH_IO.Serialization;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.PlantLight;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

public class SelectSpectralFactorComponent : FlahaGrowComponent
{
    protected static readonly Dictionary<string, double> Sources = new(StringComparer.OrdinalIgnoreCase) { ["CIE-D55"] = .017833, ["CIE-D65"] = .018043, ["CIE-D75"] = .018345, ["CIE-HPS-1"] = .011640, ["CIE-HPS-5"] = .016180, ["CIE-LED-BH1"] = .013633, ["CIE-LED-V2"] = .017691 };
    private double _factor = .018043; private string _label = "CIE-D65"; private string? _sourcePath; private string? _sourceHash;
    private readonly ActionLatch pickerLatch = new();
    private string? loadedKey;
    public SelectSpectralFactorComponent(string name = "Select Spectral Factor", string nick = "Spectral Factor", Guid? id = null) : base(name, nick, "Choose a standard spectrum or open a custom spectral CSV to set the illuminance-to-PPFD factor.", "FlahaGrow", "02 Spectral") => Id = id ?? new Guid("30fa20be-c063-4d37-9002-46d73774f697");
    private Guid Id { get; }
    public override Guid ComponentGuid => Id;
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddBooleanParameter("Run", "Run", "Open the conversion-factor selection window once on a false→true edge.", GH_ParamAccess.item, false); p.AddIntegerParameter("Wavelength interval", "nm", "CSV calculation sampling interval in nm.", GH_ParamAccess.item, 1); p.AddTextParameter("Custom spectral CSV", "CSV", "Optional CSV path. When supplied, loads this custom spectrum without opening a dialog.", GH_ParamAccess.item); p[2].Optional = true; }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("Conversion factor", "Factor", "μmol/m²/s per lux.", GH_ParamAccess.item); p.AddTextParameter("Source", "Source", "Selected standard source or CSV filename.", GH_ParamAccess.item); p.AddTextParameter("CSV path", "CSV", "Loaded custom CSV path; blank for standard sources.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da) { try { var run = false; var step = 1; var csv = string.Empty; da.GetData(0, ref run); da.GetData(1, ref step); da.GetData(2, ref csv); step = Math.Max(1, step); if (!string.IsNullOrWhiteSpace(csv)) LoadCsv(csv, step); else if (pickerLatch.Observe(run)) ShowPicker(step); else pickerLatch.Observe(false); WarnIfSourceChanged(); da.SetData(0, _factor); da.SetData(1, _label); da.SetData(2, _sourcePath ?? string.Empty); } catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); } }
    private void LoadCsv(string path, int step)
    {
        path = Path.GetFullPath(path); if (!File.Exists(path)) throw new FileNotFoundException("Custom spectral CSV was not found.", path);
        var hash = SpectralMath.Hash(path); var key = path + "|" + hash + "|" + step.ToString(CultureInfo.InvariantCulture);
        if (key == loadedKey) return;
        var result = SpectralMath.Compute(path, step); _factor = result.Factor; _label = Path.GetFileName(path); _sourcePath = path; _sourceHash = hash; loadedKey = key;
    }
    private void WarnIfSourceChanged()
    {
        PlantLightMath.NonNegative(_factor, "Saved spectral factor");
        if (string.IsNullOrWhiteSpace(_sourcePath) || string.IsNullOrWhiteSpace(_sourceHash)) return;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Legacy CSV path assumes an energy spectrum and zero unmeasured photopic tails. Use Spectral Profile for explicit basis/coverage. Saved results are retained until CSV reload.");
        if (!File.Exists(_sourcePath) || !string.Equals(SpectralMath.Hash(_sourcePath), _sourceHash, StringComparison.OrdinalIgnoreCase))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The saved spectral CSV is missing or changed. The saved factor is retained; reload the CSV to update it.");
    }
    private void ShowPicker(int step)
    {
        using var form = new Form { Text = "Select Illuminance to PPFD Factor", Width = 440, Height = 420, StartPosition = FormStartPosition.CenterScreen };
        var list = new ListBox { Dock = DockStyle.Top, Height = 210 }; foreach (var pair in Sources) list.Items.Add(pair.Key); list.SelectedItem = Sources.ContainsKey(_label) ? _label : "CIE-D65";
        var custom = new TextBox { Dock = DockStyle.Top, Text = _factor.ToString("G17", CultureInfo.InvariantCulture) }; var browse = new Button { Dock = DockStyle.Top, Height = 34, Text = "Open spectral CSV…" }; var ok = new Button { Dock = DockStyle.Bottom, Height = 38, Text = "Set Factor and Close", DialogResult = DialogResult.OK };
        browse.Click += (_, _) => { using var dialog = new OpenFileDialog { Title = "Select custom spectral CSV", Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", CheckFileExists = true }; if (dialog.ShowDialog() == DialogResult.OK) { LoadCsv(dialog.FileName, step); custom.Text = _factor.ToString("G17", CultureInfo.InvariantCulture); list.ClearSelected(); } };
        var updating = false;
        custom.TextChanged += (_, _) => { if (!updating) list.ClearSelected(); };
        list.SelectedIndexChanged += (_, _) => { if (list.SelectedItem is string key) { updating = true; custom.Text = Sources[key].ToString("G17", CultureInfo.InvariantCulture); updating = false; } };
        form.Controls.Add(custom); form.Controls.Add(browse); form.Controls.Add(list); form.Controls.Add(ok); form.AcceptButton = ok;
        if (form.ShowDialog() == DialogResult.OK) { if (list.SelectedItem is string source && Sources.TryGetValue(source, out var selected)) { _factor = selected; _label = source; _sourcePath = null; _sourceHash = null; } else if (double.TryParse(custom.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) && value >= 0) { if (value != _factor) { _label = "Custom numeric assumption"; _sourcePath = null; _sourceHash = null; } _factor = value; } else throw new ArgumentException("Custom factor must be finite and non-negative."); }
    }
    public override bool Write(GH_IWriter writer)
    {
        writer.SetDouble("Factor", _factor); writer.SetString("Label", _label);
        if (!string.IsNullOrWhiteSpace(_sourcePath)) writer.SetString("SourcePath", _sourcePath);
        if (!string.IsNullOrWhiteSpace(_sourceHash)) writer.SetString("SourceHash", _sourceHash);
        return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
        if (reader.ItemExists("Factor")) _factor = reader.GetDouble("Factor");
        if (reader.ItemExists("Label")) _label = reader.GetString("Label");
        _sourcePath = reader.ItemExists("SourcePath") ? reader.GetString("SourcePath") : null;
        _sourceHash = reader.ItemExists("SourceHash") ? reader.GetString("SourceHash") : null;
        loadedKey = null; pickerLatch.Disarm(); return base.Read(reader);
    }
}
public sealed class SelectSpectralFactorLegacyComponent : SelectSpectralFactorComponent { public SelectSpectralFactorLegacyComponent() : base("Select Spectral Factor (Legacy)", "Spectral Factor 2", new Guid("36362f09-1294-4d39-8d9e-0185ec44c538")) { } }

public sealed class LoadSpectralDataComponent : FlahaGrowComponent
{
    private double _factor; private double _par; private double _lux; private string _file = "No file loaded"; private string? _sourcePath; private string? _sourceHash;
    private readonly ActionLatch loadLatch = new();
    private string? loadedKey;
    private SpectralDataForm? dataForm;
    public LoadSpectralDataComponent() : base("Load Spectral Data", "Load Spectral", "Opens a CSV file and calculates its illuminance-to-PPFD conversion factor.", "FlahaGrow", "02 Spectral") { }
    public override Guid ComponentGuid => new("061e0342-6d6f-4ecb-a207-a0807393de1f");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddBooleanParameter("Load spectral data", "Load", "Open the spectral CSV picker once on a false→true edge.", GH_ParamAccess.item, false); p.AddIntegerParameter("Wavelength interval", "nm", "Sampling interval in nm.", GH_ParamAccess.item, 1); p.AddTextParameter("Custom spectral CSV", "CSV", "Optional CSV path. Loads the file without opening a dialog.", GH_ParamAccess.item); p[2].Optional = true; }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("Conversion factor", "Factor", "μmol/m²/s per lux.", GH_ParamAccess.item); p.AddNumberParameter("PAR sum", "PAR", "Integrated photon quantity.", GH_ParamAccess.item); p.AddNumberParameter("Lux sum", "Lux", "Integrated photopic quantity.", GH_ParamAccess.item); p.AddTextParameter("File", "File", "Selected CSV filename.", GH_ParamAccess.item); p.AddTextParameter("CSV path", "CSV", "Loaded custom CSV path.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da) { try { var load = false; var step = 1; var csv = string.Empty; da.GetData(0, ref load); da.GetData(1, ref step); da.GetData(2, ref csv); step = Math.Max(1, step); if (!string.IsNullOrWhiteSpace(csv)) LoadCsv(csv, step); else if (loadLatch.Observe(load)) OpenDataWindow(step); else loadLatch.Observe(false); WarnIfSourceChanged(); da.SetData(0, _factor); da.SetData(1, _par); da.SetData(2, _lux); da.SetData(3, _file); da.SetData(4, _sourcePath ?? string.Empty); } catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); } }
    private void OpenDataWindow(int step)
    {
        if (dataForm is { IsDisposed: false }) { dataForm.Activate(); return; }
        dataForm = new SpectralDataForm(step, _sourcePath, result =>
        {
            _factor = result.Result.Factor; _par = result.Result.Par; _lux = result.Result.Lux; _file = Path.GetFileName(result.Path); _sourcePath = result.Path; _sourceHash = SpectralMath.Hash(result.Path); loadedKey = null;
            var document = OnPingDocument(); if (document is not null) document.ScheduleSolution(1, _ => ExpireSolution(false));
        });
        dataForm.Show();
    }
    private void LoadCsv(string path, int step) { path = Path.GetFullPath(path); if (!File.Exists(path)) throw new FileNotFoundException("Custom spectral CSV was not found.", path); var hash = SpectralMath.Hash(path); var key = path + "|" + hash + "|" + step.ToString(CultureInfo.InvariantCulture); if (key == loadedKey) return; var result = SpectralMath.Compute(path, step); _factor = result.Factor; _par = result.Par; _lux = result.Lux; _file = Path.GetFileName(path); _sourcePath = path; _sourceHash = hash; loadedKey = key; }
    private void WarnIfSourceChanged()
    {
        if (string.IsNullOrWhiteSpace(_sourcePath) || string.IsNullOrWhiteSpace(_sourceHash)) return;
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Legacy CSV path assumes an energy spectrum and zero unmeasured photopic tails. Use Spectral Profile for explicit basis/coverage. Saved results are retained until CSV reload.");
        if (!File.Exists(_sourcePath) || !string.Equals(SpectralMath.Hash(_sourcePath), _sourceHash, StringComparison.OrdinalIgnoreCase))
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The saved spectral CSV is missing or changed. The saved result is retained; reload the CSV to update it.");
    }
    public override bool Write(GH_IWriter writer)
    {
        writer.SetDouble("Factor", _factor); writer.SetDouble("Par", _par); writer.SetDouble("Lux", _lux); writer.SetString("File", _file);
        if (!string.IsNullOrWhiteSpace(_sourcePath)) writer.SetString("SourcePath", _sourcePath);
        if (!string.IsNullOrWhiteSpace(_sourceHash)) writer.SetString("SourceHash", _sourceHash);
        return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
        if (reader.ItemExists("Factor")) _factor = reader.GetDouble("Factor");
        if (reader.ItemExists("Par")) _par = reader.GetDouble("Par");
        if (reader.ItemExists("Lux")) _lux = reader.GetDouble("Lux");
        if (reader.ItemExists("File")) _file = reader.GetString("File");
        _sourcePath = reader.ItemExists("SourcePath") ? reader.GetString("SourcePath") : null;
        _sourceHash = reader.ItemExists("SourceHash") ? reader.GetString("SourceHash") : null;
        loadedKey = null; loadLatch.Disarm(); return base.Read(reader);
    }
}

internal sealed class SpectralDataForm : Form
{
    private readonly int step; private readonly Action<(string Path, SpectralResult Result)> publish;
    private readonly DataGridView grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly Label file = new() { AutoSize = true, Text = "Calculated: (No file loaded)" };
    private readonly Label sums = new() { AutoSize = true, Text = "Sum of Calculated PAR: 0.000000\nSum of Calculated Lx: 0.000000" };
    private readonly TextBox factor = new() { ReadOnly = true, Width = 220 };
    private string? path; private SpectralResult? result;
    public SpectralDataForm(int step, string? initialPath, Action<(string Path, SpectralResult Result)> publish)
    {
        this.step = step; this.publish = publish; Text = "Spectral Data"; Width = 1180; Height = 620; MinimumSize = new System.Drawing.Size(900, 620);
        var top = new Panel { Dock = DockStyle.Top, Height = 250, Padding = new Padding(10) };
        var heading = new Label { Text = "SPECTRAL DATA", Font = new System.Drawing.Font("Segoe UI", 14, System.Drawing.FontStyle.Bold), AutoSize = true, Location = new System.Drawing.Point(40, 10) };
        var load = new Button { Text = "Load Spectral Data", Dock = DockStyle.Top, Height = 30, Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold) }; load.Click += (_, _) => Browse();
        var calculation = new Label { Text = "Conversion Factor Calculation", AutoSize = true, Location = new System.Drawing.Point(10, 60) };
        factor.Location = new System.Drawing.Point(10, 85); factor.Font = new System.Drawing.Font("Segoe UI", 11, System.Drawing.FontStyle.Bold);
        var update = new Button { Text = "Update Conversion Factor", Location = new System.Drawing.Point(240, 85), Size = new System.Drawing.Size(220, 30) }; update.Click += (_, _) => RefreshValues();
        file.Location = new System.Drawing.Point(10, 125); sums.Location = new System.Drawing.Point(10, 145);
        var actions = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 35, ColumnCount = 2 }; actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        var save = new Button { Text = "Save as CSV File", Dock = DockStyle.Fill }; save.Click += (_, _) => SaveCalculated();
        var setClose = new Button { Text = "Set conversion factor and close", Dock = DockStyle.Fill }; setClose.Click += (_, _) => { if (result is not null) { if (path is not null) publish((path, result)); } Close(); };
        actions.Controls.Add(save, 0, 0); actions.Controls.Add(setClose, 1, 0); top.Controls.AddRange(new Control[] { actions, load, heading, calculation, factor, update, file, sums }); Controls.Add(grid); Controls.Add(top);
        result = SpectralMath.Empty(step); RefreshValues();
        if (!string.IsNullOrWhiteSpace(initialPath) && File.Exists(initialPath)) LoadCsvFile(initialPath);
    }
    private void Browse() { using var dialog = new OpenFileDialog { Title = "Select spectral CSV (wavelength + spectral power)", Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*", CheckFileExists = true }; if (dialog.ShowDialog() == DialogResult.OK) LoadCsvFile(dialog.FileName); }
    private void LoadCsvFile(string selected) { try { path = Path.GetFullPath(selected); result = SpectralMath.Compute(path, step); RefreshValues(); } catch (Exception ex) { MessageBox.Show("Failed to read CSV:\n" + ex.Message, "Spectral Data", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
    private void RefreshValues()
    {
        if (result is null) return; factor.Text = result.Factor.ToString("0.000000000", CultureInfo.InvariantCulture); file.Text = path is null ? "Calculated: (No file loaded)" : "Calculated: " + Path.GetFileName(path); sums.Text = $"Sum of Calculated PAR: {result.Par:0.######}\nSum of Calculated Lx: {result.Lux:0.######}";
        grid.DataSource = result.Rows.Select(row => new { Wavelength_nm = row.Wavelength, Equal_PAR = row.EqualPar, PAR_Spectral = row.ParSpectral, OPN1 = row.Opn1, Spectral_Power = row.Power, Calculated_PAR = row.CalculatedPar, Calculated_Lx = row.CalculatedLux }).ToList();
    }
    private void SaveCalculated()
    {
        if (path is null || result is null) { MessageBox.Show("Please load a spectral CSV first.", "Spectral Data"); return; }
        var output = Path.Combine(Path.GetDirectoryName(path)!, Path.GetFileNameWithoutExtension(path) + "_calculated.csv");
        using var writer = new StreamWriter(output, false, System.Text.Encoding.UTF8); writer.WriteLine("Wavelength (nm),Equal PAR,PAR Spectral,OPN1,Spectral Power (W/m2/nm1),Calculated PAR,Calculated Lx");
        foreach (var row in result.Rows) writer.WriteLine(string.Join(",", row.Wavelength.ToString(CultureInfo.InvariantCulture), row.EqualPar.ToString("G17", CultureInfo.InvariantCulture), row.ParSpectral.ToString("G17", CultureInfo.InvariantCulture), row.Opn1.ToString("G17", CultureInfo.InvariantCulture), row.Power.ToString("G17", CultureInfo.InvariantCulture), row.CalculatedPar.ToString("G17", CultureInfo.InvariantCulture), row.CalculatedLux.ToString("G17", CultureInfo.InvariantCulture)));
        writer.WriteLine(); writer.WriteLine("Calculated file," + Path.GetFileName(path)); writer.WriteLine("Sum of Calculated PAR," + result.Par.ToString("G17", CultureInfo.InvariantCulture)); writer.WriteLine("Sum of Calculated Lx," + result.Lux.ToString("G17", CultureInfo.InvariantCulture)); writer.WriteLine("Conversion Factor (PPFD/Lux)," + result.Factor.ToString("G17", CultureInfo.InvariantCulture)); MessageBox.Show("Saved:\n" + output, "Spectral Data");
    }
}

internal sealed record SpectralRow(int Wavelength, double EqualPar, double ParSpectral, double Opn1, double Power, double CalculatedPar, double CalculatedLux);
internal sealed record SpectralResult(double Factor, double Par, double Lux, IReadOnlyList<SpectralRow> Rows);

internal static class SpectralMath
{
    private const int MinimumWavelength = 380, MaximumWavelength = 780;
    internal static string Hash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
    internal static SpectralResult Compute(string path, int step)
    {
        var raw = SpectralCalculator.ReadCsv(path);
        var result = SpectralCalculator.Calculate(raw, SpectralBasis.Energy, allowZeroTails: true, stepNm: step);
        var rows = new List<SpectralRow>();
        for (var nm = MinimumWavelength; nm <= MaximumWavelength; nm += step)
        {
            var power = SpectralCalculator.Interpolate(raw, nm);
            var equalPar = nm is >= 400 and <= 700 ? 1d : 0d;
            var parSpectral = nm * equalPar * SpectralCalculator.PhotonMultiplier;
            var photopic = SpectralCalculator.Photopic(nm);
            rows.Add(new(nm, equalPar, parSpectral, photopic, power, parSpectral * power, 683 * photopic * power));
        }
        return new(result.Factor, result.PhotonIntegral, result.PhotopicIntegral, rows);
    }
    internal static SpectralResult Empty(int step)
    {
        if (step < 1 || step > 10) throw new ArgumentOutOfRangeException(nameof(step), "Use 1–10 nm.");
        var rows = new List<SpectralRow>();
        for (var nm = MinimumWavelength; nm <= MaximumWavelength; nm += step)
        {
            var equalPar = nm is >= 400 and <= 700 ? 1d : 0d;
            rows.Add(new(nm, equalPar, nm * equalPar * SpectralCalculator.PhotonMultiplier, SpectralCalculator.Photopic(nm), 0, 0, 0));
        }
        return new(0, 0, 0, rows);
    }
}
