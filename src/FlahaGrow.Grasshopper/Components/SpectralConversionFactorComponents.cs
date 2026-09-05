using System.Globalization;
using System.Windows.Forms;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

public class SelectSpectralFactorComponent : GH_Component
{
    protected static readonly Dictionary<string, double> Sources = new(StringComparer.OrdinalIgnoreCase) { ["CIE-D55"] = .017833, ["CIE-D65"] = .018043, ["CIE-D75"] = .018345, ["CIE-HPS-1"] = .011640, ["CIE-HPS-5"] = .016180, ["CIE-LED-BH1"] = .013633, ["CIE-LED-V2"] = .017691 };
    private double _factor = .018043; private string _label = "CIE-D65";
    public SelectSpectralFactorComponent(string name = "Select Spectral Factor", string nick = "Spectral Factor", Guid? id = null) : base(name, nick, "Choose a standard spectrum or open a custom spectral CSV to set the illuminance-to-PPFD factor.", "FlahaGrow", "Spectral") => Id = id ?? new Guid("30fa20be-c063-4d37-9002-46d73774f697");
    private Guid Id { get; }
    public override Guid ComponentGuid => Id;
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddBooleanParameter("Run", "Run", "Open the conversion-factor selection window.", GH_ParamAccess.item, false); p.AddIntegerParameter("Wavelength interval", "nm", "CSV calculation sampling interval in nm.", GH_ParamAccess.item, 1); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("Conversion factor", "Factor", "μmol/m²/s per lux.", GH_ParamAccess.item); p.AddTextParameter("Source", "Source", "Selected standard source or CSV filename.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da) { var run = false; var step = 1; da.GetData(0, ref run); da.GetData(1, ref step); if (run) ShowPicker(Math.Max(1, step)); da.SetData(0, _factor); da.SetData(1, _label); }
    private void ShowPicker(int step)
    {
        using var form = new Form { Text = "Select Illuminance to PPFD Factor", Width = 440, Height = 420, StartPosition = FormStartPosition.CenterScreen };
        var list = new ListBox { Dock = DockStyle.Top, Height = 210 }; foreach (var pair in Sources) list.Items.Add(pair.Key); list.SelectedItem = Sources.ContainsKey(_label) ? _label : "CIE-D65";
        var custom = new TextBox { Dock = DockStyle.Top, Text = _factor.ToString("0.000000", CultureInfo.InvariantCulture) }; var browse = new Button { Dock = DockStyle.Top, Height = 34, Text = "Open spectral CSV…" }; var ok = new Button { Dock = DockStyle.Bottom, Height = 38, Text = "Set Factor and Close", DialogResult = DialogResult.OK };
        browse.Click += (_, _) => { using var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" }; if (dialog.ShowDialog() == DialogResult.OK) { var result = SpectralMath.Compute(dialog.FileName, step); custom.Text = result.Factor.ToString("0.000000", CultureInfo.InvariantCulture); _label = Path.GetFileName(dialog.FileName); list.ClearSelected(); } };
        form.Controls.Add(custom); form.Controls.Add(browse); form.Controls.Add(list); form.Controls.Add(ok); form.AcceptButton = ok;
        if (form.ShowDialog() == DialogResult.OK) { if (list.SelectedItem is string source && Sources.TryGetValue(source, out var selected)) { _factor = selected; _label = source; } else if (double.TryParse(custom.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0) _factor = value; }
    }
}
public sealed class SelectSpectralFactorLegacyComponent : SelectSpectralFactorComponent { public SelectSpectralFactorLegacyComponent() : base("Select Spectral Factor (Legacy)", "Spectral Factor 2", new Guid("36362f09-1294-4d39-8d9e-0185ec44c538")) { } }

public sealed class LoadSpectralDataComponent : GH_Component
{
    private double _factor; private double _par; private double _lux; private string _file = "No file loaded";
    public LoadSpectralDataComponent() : base("Load Spectral Data", "Load Spectral", "Opens a CSV file and calculates its illuminance-to-PPFD conversion factor.", "FlahaGrow", "Spectral") { }
    public override Guid ComponentGuid => new("061e0342-6d6f-4ecb-a207-a0807393de1f");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddBooleanParameter("Load spectral data", "Load", "Open the spectral CSV picker.", GH_ParamAccess.item, false); p.AddIntegerParameter("Wavelength interval", "nm", "Sampling interval in nm.", GH_ParamAccess.item, 1); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("Conversion factor", "Factor", "μmol/m²/s per lux.", GH_ParamAccess.item); p.AddNumberParameter("PAR sum", "PAR", "Integrated photon quantity.", GH_ParamAccess.item); p.AddNumberParameter("Lux sum", "Lux", "Integrated photopic quantity.", GH_ParamAccess.item); p.AddTextParameter("File", "File", "Selected CSV filename.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da) { var load = false; var step = 1; da.GetData(0, ref load); da.GetData(1, ref step); if (load) { using var dialog = new OpenFileDialog { Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*" }; if (dialog.ShowDialog() == DialogResult.OK) { var result = SpectralMath.Compute(dialog.FileName, Math.Max(1, step)); _factor = result.Factor; _par = result.Par; _lux = result.Lux; _file = Path.GetFileName(dialog.FileName); } } da.SetData(0, _factor); da.SetData(1, _par); da.SetData(2, _lux); da.SetData(3, _file); }
}

internal static class SpectralMath
{
    internal static (double Factor, double Par, double Lux) Compute(string path, int step) { var rows = File.ReadAllLines(path).Skip(1).Select(line => line.Split(',')).Where(row => row.Length >= 2).Select(Parse).Where(row => row.Ok).ToDictionary(row => (int)Math.Round(row.Wavelength), row => row.Power); double par = 0, lux = 0, last = 0; for (var nm = 380; nm <= 780; nm += step) { if (rows.TryGetValue(nm, out var value)) last = value; par += nm * 1e-3 / (6.62607015e-34 * 2.99792458e8 * 6.02214076e23) * last; lux += Photopic(nm) * last; } return (lux == 0 ? 0 : par / (lux * 683), par, lux); }
    private static (bool Ok, double Wavelength, double Power) Parse(string[] row) { var wavelengthOk = double.TryParse(row[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var wavelength); var powerOk = double.TryParse(row[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var power); return (wavelengthOk && powerOk, wavelength, power); }
    private static double Photopic(double nm) => 1.056 * Math.Exp(-.5 * Math.Pow((nm - 599.8) / 37.9, 2)) + .362 * Math.Exp(-.5 * Math.Pow((nm - 442) / 16, 2)) - .065 * Math.Exp(-.5 * Math.Pow((nm - 501.1) / 20.4, 2));
}
