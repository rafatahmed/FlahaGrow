using System.Globalization;
using System.Security.Cryptography;
using Microsoft.VisualBasic.FileIO;

namespace FlahaGrow.Core.PlantLight;

public enum SpectralBasis { Energy, Photon }
public readonly record struct SpectralSample(double Wavelength, double Value);
public sealed record SpectralCalculation(double Factor, double PhotonIntegral, double PhotopicIntegral,
    double MinimumNm, double MaximumNm, string Warning);

/// <summary>Integrates an explicit energy/photon spectral basis. Relative inputs yield ratios only.</summary>
public static class SpectralCalculator
{
    public const string Method = "cie-vlambda-linear-trapezoid-v1";
    public const double PhotonMultiplier = 1e-3 / (6.62607015e-34 * 299792458 * 6.02214076e23);
    private static readonly double[] PhotopicValues = LoadPhotopic();

    private static double[] LoadPhotopic()
    {
        using var stream = typeof(SpectralCalculator).Assembly.GetManifestResourceStream("FlahaGrow.CIE.Vlambda.csv")
            ?? throw new InvalidDataException("CIE V(lambda) resource missing.");
        using var bytes = new MemoryStream(); stream.CopyTo(bytes);
        var raw = bytes.ToArray();
        if (Convert.ToHexString(SHA256.HashData(raw)) != "EE5D5D17922AE645D4AF52CACF6A50BDB9385749F9D2181CA312EB2B08FEBAC2")
            throw new InvalidDataException("CIE V(lambda) resource checksum mismatch.");
        using var reader = new StringReader(System.Text.Encoding.UTF8.GetString(raw));
        var values = new List<double>();
        while (reader.ReadLine() is { } line)
        {
            var fields = line.Split(',');
            if (int.Parse(fields[0], CultureInfo.InvariantCulture) != 360 + values.Count)
                throw new InvalidDataException("Unexpected CIE wavelength grid.");
            values.Add(double.Parse(fields[1], CultureInfo.InvariantCulture));
        }
        if (values.Count != 471) throw new InvalidDataException("Incomplete CIE V(lambda).");
        return values.ToArray();
    }

    public static double Photopic(double nm)
    {
        if (nm < 360 || nm > 830) return 0;
        var index = (int)Math.Floor(nm) - 360;
        return index == 470 ? PhotopicValues[index]
            : PhotopicValues[index] + (nm - Math.Floor(nm)) * (PhotopicValues[index + 1] - PhotopicValues[index]);
    }

    public static IReadOnlyList<SpectralSample> ReadCsv(string path, SpectralBasis basis = SpectralBasis.Energy)
    {
        using var parser = new TextFieldParser(path) { TextFieldType = FieldType.Delimited, HasFieldsEnclosedInQuotes = true };
        parser.SetDelimiters(",");
        var headers = parser.ReadFields() ?? throw new InvalidDataException("CSV requires a header.");
        var names = headers.Select(h => h.Trim().ToLowerInvariant().Replace('_', ' ')).ToArray();
        var wi = Array.FindIndex(names, h => h == "wavelength nm" || h == "wavelength (nm)");
        var vi = Array.FindIndex(names, h => h == "value" || h.StartsWith("spectral power", StringComparison.Ordinal)
            || h == "spectral photon flux");
        if (wi < 0 || vi < 0 || wi == vi)
            throw new InvalidDataException("Use CSV headers wavelength_nm,value (nm; basis supplied separately). Headerless and multi-profile data require explicit preprocessing.");
        if ((names[vi] == "spectral photon flux" && basis != SpectralBasis.Photon)
            || (names[vi].StartsWith("spectral power", StringComparison.Ordinal) && basis != SpectralBasis.Energy))
            throw new InvalidDataException("CSV header and selected energy/photon basis disagree.");
        var rows = new List<SpectralSample>();
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields is null || fields.Length != headers.Length
                || !double.TryParse(fields[wi], NumberStyles.Float, CultureInfo.InvariantCulture, out var nm)
                || !double.TryParse(fields[vi], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw new InvalidDataException($"Invalid spectral CSV record near line {parser.LineNumber}.");
            rows.Add(new(nm, value));
        }
        Validate(rows); return rows.AsReadOnly();
    }

    private static void Validate(IReadOnlyList<SpectralSample> samples)
    {
        if (samples.Count < 2) throw new InvalidDataException("At least two spectral samples required.");
        double previous = 0;
        foreach (var s in samples)
        {
            if (!double.IsFinite(s.Wavelength) || s.Wavelength <= previous)
                throw new InvalidDataException("Wavelengths must be finite, positive, unique and increasing; values are not rounded.");
            PlantLightMath.NonNegative(s.Value, "Spectral value"); previous = s.Wavelength;
        }
        if (samples[0].Wavelength > 400 || samples[^1].Wavelength < 700)
            throw new InvalidDataException("Spectrum must cover the complete 400–700 nm PAR band.");
    }

    public static double Interpolate(IReadOnlyList<SpectralSample> samples, double nm)
    {
        if (nm < samples[0].Wavelength || nm > samples[^1].Wavelength) return 0;
        var low = 0; var high = samples.Count - 1;
        while (low < high) { var mid = (low + high) / 2; if (samples[mid].Wavelength < nm) low = mid + 1; else high = mid; }
        if (samples[low].Wavelength == nm) return samples[low].Value;
        var a = samples[low - 1]; var b = samples[low];
        return a.Value + (b.Value - a.Value) * (nm - a.Wavelength) / (b.Wavelength - a.Wavelength);
    }

    public static SpectralCalculation Calculate(IReadOnlyList<SpectralSample> samples, SpectralBasis basis,
        bool allowZeroTails = false, int stepNm = 1)
    {
        Validate(samples);
        if (!Enum.IsDefined(typeof(SpectralBasis), basis)) throw new ArgumentException("Unknown spectral basis.");
        if (stepNm < 1 || stepNm > 10) throw new ArgumentOutOfRangeException(nameof(stepNm), "Use 1–10 nm; resampling does not add resolution.");
        var incomplete = samples[0].Wavelength > 360 || samples[^1].Wavelength < 830;
        if (incomplete && !allowZeroTails) throw new InvalidDataException("Incomplete 360–830 nm photopic coverage. Explicitly accept zero unmeasured tails or supply full coverage.");
        double Energy(double nm)
        {
            var value = Interpolate(samples, nm);
            return basis == SpectralBasis.Energy ? value : value / (PhotonMultiplier * nm);
        }
        double Integral(int start, int end, Func<double, double> value)
        {
            double total = 0;
            for (var a = start; a < end; a += stepNm)
            {
                var b = Math.Min(a + stepNm, end);
                total += (value(a) + value(b)) * .5 * (b - a);
            }
            return PlantLightMath.NonNegative(total, "Spectral integral");
        }
        var photons = Integral(400, 700, nm => Energy(nm) * PhotonMultiplier * nm);
        var lux = Integral(360, 830, nm => Energy(nm) * Photopic(nm)) * 683;
        if (!double.IsFinite(lux) || lux <= 0) throw new InvalidDataException("Photopic integral must be finite and positive; a zero spectrum has no conversion factor.");
        var factor = PlantLightMath.NonNegative(photons / lux, "Spectral factor");
        return new(factor, photons, lux, samples[0].Wavelength, samples[^1].Wavelength,
            (incomplete ? "Unmeasured photopic tails assumed zero. " : "")
            + "Not project-validated; relative input integrals are not absolute irradiance or illuminance.");
    }
}
