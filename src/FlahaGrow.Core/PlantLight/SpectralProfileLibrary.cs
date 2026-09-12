using System.Text.Json;

namespace FlahaGrow.Core.PlantLight;

/// <summary>Versioned, pre-integrated research references; not matched fixture measurements.</summary>
public static class SpectralProfileLibrary
{
    public const string Revision = "research-library-2026-09-12-v1";
    public static IReadOnlyList<LibraryProfile> Profiles { get; } = Load();
    public static LibraryProfile Get(string id) => Profiles.SingleOrDefault(p => p.Id == id)
        ?? throw new ArgumentException("Unknown spectral library profile; select a current row.");

    private static IReadOnlyList<LibraryProfile> Load()
    {
        using var stream = typeof(SpectralProfileLibrary).Assembly.GetManifestResourceStream("FlahaGrow.PlantLight.Library.json")
            ?? throw new InvalidDataException("Missing spectral library.");
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;
        var files = root.GetProperty("files").EnumerateArray().ToDictionary(f => f.GetProperty("file").GetString()!);
        var result = new List<LibraryProfile>();
        foreach (var row in root.GetProperty("profiles").EnumerateArray())
        {
            var id = row.GetProperty("id").GetString()!;
            if (id.StartsWith("CIE_illum_HPs:", StringComparison.Ordinal)) continue;
            var source = files[row.GetProperty("data_file").GetString()!];
            var research = id.StartsWith("park-runkle", StringComparison.Ordinal);
            var warning = "Reference assumption, not a matched commercial fixture or project validation. Spectrum must represent received light at the sensor.";
            if (row.GetProperty("photopic_coverage").GetString() == "truncated")
                warning += $" Unmeasured photopic tails assumed zero; source coverage {row.GetProperty("source_range_nm")[0].GetDouble():G}–{row.GetProperty("source_range_nm")[1].GetDouble():G} nm.";
            if (source.TryGetProperty("sha256_matches", out var match) && !match.GetBoolean())
                warning += " Publisher SHA256 metadata exception; published MD5 matches. Local source SHA256 retained.";
            var provenance = $"{Revision}; {id}; https://doi.org/{row.GetProperty("doi").GetString()}; source SHA256 {source.GetProperty("sha256").GetString()}; basis {row.GetProperty("basis").GetString()}; PAR 400–700 nm; photopic 360–830 nm; 1 nm trapezoid; "
                + (research ? "Park & Runkle (2018), CC BY 4.0" : "CIE, CC BY-SA 4.0");
            var profile = new SpectralProfile(row.GetProperty("label").GetString()!, row.GetProperty("factor_umol_m2_s_per_lux").GetDouble(), root.GetProperty("method").GetString()!, provenance, warning);
            if (profile.Factor <= 0) throw new InvalidDataException("Library factor must be positive.");
            result.Add(new(id, research ? "Horticultural research" : id.Contains("LED") ? "LED reference" : "Daylight reference", profile));
        }
        if (result.Count != 18 || result.Select(p => p.Id).Distinct().Count() != result.Count)
            throw new InvalidDataException("Unexpected spectral library inventory.");
        return result.AsReadOnly();
    }
}

public sealed record LibraryProfile(string Id, string Category, SpectralProfile Profile);
