using System.Globalization;
using System.Text.RegularExpressions;

namespace FlahaGrow.Core.Radiance;

/// <summary>Edits generated Radiance primitives without confusing geometry arguments with emitter RGB.</summary>
public static class LuminaireOutput
{
    public static (double R, double G, double B) Normalize(double r, double g, double b)
    {
        if (new[] { r, g, b }.Any(v => !double.IsFinite(v) || v < 0))
            throw new ArgumentException("RGB values must be finite and nonnegative.");
        var scale = Math.Max(r, Math.Max(g, b));
        if (scale == 0) throw new ArgumentException("At least one RGB channel must be positive.");
        r = r / scale * .265; g = g / scale * .67; b = b / scale * .065;
        var total = r + g + b;
        return (r / total, g / total, b / total);
    }

    public static string Rewrite(string text, double r, double g, double b, string? datOverride = null, string? dataDirectory = null)
    {
        if (new[] { r, g, b }.Any(v => !double.IsFinite(v) || v < 0)) throw new ArgumentException("Invalid emitter RGB.");
        var tokens = Regex.Matches(text, "\"[^\"\r\n]*\"|#[^\r\n]*|(?m:^[ \\t]*![^\r\n]*)|[^\\s\"#]+")
            .Cast<Match>().Where(m => !m.Value.TrimStart().StartsWith('#') && !m.Value.TrimStart().StartsWith('!')).ToArray();
        var edits = new List<(int Start, int Length, string Value)>();
        var index = 0;
        var emitters = 0;
        while (index < tokens.Length)
        {
            if (tokens.Length - index < 3) throw new InvalidDataException("Truncated Radiance primitive header.");
            var type = tokens[index + 1].Value;
            index += 3;
            for (var group = 0; group < 3; group++)
            {
                if (index >= tokens.Length || !int.TryParse(tokens[index++].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var count)
                    || count < 0 || count > tokens.Length - index)
                    throw new InvalidDataException("Invalid Radiance primitive argument count.");
                if (group == 0)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var token = tokens[index + i];
                        var value = token.Value.Trim('"');
                        if (!value.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)) continue;
                        var replacement = datOverride ?? (dataDirectory is null ? null : Path.Combine(dataDirectory, Path.GetFileName(value)));
                        if (replacement is null) continue;
                        if (replacement.Any(c => c == '"' || char.IsControl(c))) throw new ArgumentException("Invalid DAT path.");
                        edits.Add((token.Index, token.Length, "\"" + replacement.Replace('\\', '/') + "\""));
                    }
                }
                if (group == 2 && type is "light" or "illum")
                {
                    if (count != 3) throw new InvalidDataException("Emitter must have three real RGB arguments.");
                    var rgb = new[] { r, g, b };
                    for (var i = 0; i < 3; i++) edits.Add((tokens[index + i].Index, tokens[index + i].Length, rgb[i].ToString("G17", CultureInfo.InvariantCulture)));
                    emitters++;
                }
                index += count;
            }
        }
        if (emitters == 0) throw new InvalidDataException("Generated Radiance output contains no light or illum emitter.");
        foreach (var edit in edits.OrderByDescending(e => e.Start)) text = text.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Value);
        return text;
    }
}
