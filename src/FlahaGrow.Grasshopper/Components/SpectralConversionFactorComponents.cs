using System.Globalization;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Selects the legacy standard illuminance-to-PPFD factors.</summary>
public sealed class SelectSpectralFactorComponent : GH_Component
{
    private static readonly Dictionary<string, double> Sources = new(StringComparer.OrdinalIgnoreCase)
    {
        ["CIE-D55"] = .017833, ["CIE-D65"] = .018043, ["CIE-D75"] = .018345,
        ["CIE-HPS-1"] = .011640, ["CIE-HPS-5"] = .016180, ["CIE-LED-BH1"] = .013633, ["CIE-LED-V2"] = .017691
    };
    public SelectSpectralFactorComponent() : base("Select Spectral Factor", "Spectral Factor", "Selects a standard or custom illuminance-to-PPFD conversion factor.", "FlahaGrow", "Spectral") { }
    public override Guid ComponentGuid => new("30fa20be-c063-4d37-9002-46d73774f697");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddTextParameter("Source", "Source", "CIE-D55, CIE-D65, CIE-D75, CIE-HPS-1, CIE-HPS-5, CIE-LED-BH1, CIE-LED-V2, or Custom.", GH_ParamAccess.item, "CIE-D65"); p.AddNumberParameter("Custom factor", "Custom", "Used only when Source is Custom; units are μmol/m²/s per lux.", GH_ParamAccess.item, .018043); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("Conversion factor", "Factor", "μmol/m²/s per lux.", GH_ParamAccess.item); p.AddTextParameter("Selected source", "Label", "Selected source label.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da) { string source = "CIE-D65"; var custom = .018043; da.GetData(0, ref source); da.GetData(1, ref custom); if (source.Equals("Custom", StringComparison.OrdinalIgnoreCase)) { if (custom < 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Custom factor must be non-negative."); return; } da.SetData(0, custom); da.SetData(1, "Custom"); return; } if (!Sources.TryGetValue(source.Trim(), out var factor)) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unknown source. Use a listed CIE source or Custom."); return; } da.SetData(0, factor); da.SetData(1, source.Trim()); }
}

/// <summary>Calculates an illuminance-to-PPFD factor from a wavelength/spectral-power CSV.</summary>
public class SpectralCsvFactorComponent : GH_Component
{
    public SpectralCsvFactorComponent(string name = "Load Spectral Data", string nick = "Spectral CSV", Guid? id = null) : base(name, nick, "Calculates an illuminance-to-PPFD factor from spectral power data between 380 and 780 nm.", "FlahaGrow", "Spectral") => Id = id ?? new Guid("061e0342-6d6f-4ecb-a207-a0807393de1f");
    private Guid Id { get; }
    public override Guid ComponentGuid => Id;
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddTextParameter("Spectral CSV", "CSV", "CSV containing wavelength and spectral-power columns.", GH_ParamAccess.item); p.AddIntegerParameter("Wavelength interval", "nm", "Sampling interval in nm.", GH_ParamAccess.item, 1); }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("Conversion factor", "Factor", "μmol/m²/s per lux.", GH_ParamAccess.item); p.AddNumberParameter("PAR sum", "PAR", "Integrated photon quantity before lux normalization.", GH_ParamAccess.item); p.AddNumberParameter("Lux sum", "Lux", "Integrated photopic quantity before 683 scaling.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string csv = string.Empty; var step = 1; if (!da.GetData(0, ref csv)) return; da.GetData(1, ref step); if (!File.Exists(csv) || step <= 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide a valid CSV and positive wavelength interval."); return; }
        try { var values = ReadCsv(csv); double par = 0, lux = 0; double last = 0; for (var nm = 380; nm <= 780; nm += step) { if (values.TryGetValue(nm, out var value)) last = value; var photons = nm * 1e-3 / (6.62607015e-34 * 2.99792458e8 * 6.02214076e23); par += photons * last; lux += Photopic(nm) * last; } var factor = lux == 0 ? 0 : par / (lux * 683); da.SetData(0, factor); da.SetData(1, par); da.SetData(2, lux); }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private static Dictionary<int, double> ReadCsv(string path) { var rows = File.ReadAllLines(path).Where(line => !string.IsNullOrWhiteSpace(line)).ToList(); if (rows.Count < 2) throw new InvalidDataException("CSV requires a header and data rows."); var result = new Dictionary<int, double>(); foreach (var line in rows.Skip(1)) { var fields = line.Split(','); if (fields.Length < 2) continue; if (double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var wavelength) && double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var power)) result[(int)Math.Round(wavelength)] = power; } return result; }
    private static double Photopic(double nm) => 1.056 * Math.Exp(-.5 * Math.Pow((nm - 599.8) / 37.9, 2)) + .362 * Math.Exp(-.5 * Math.Pow((nm - 442.0) / 16.0, 2)) - .065 * Math.Exp(-.5 * Math.Pow((nm - 501.1) / 20.4, 2));
}

public sealed class SelectSpectralFactorLegacyComponent : SpectralCsvFactorComponent { public SelectSpectralFactorLegacyComponent() : base("Select Spectral Factor (Legacy)", "Spectral Factor 2", new Guid("36362f09-1294-4d39-8d9e-0185ec44c538")) { } }
