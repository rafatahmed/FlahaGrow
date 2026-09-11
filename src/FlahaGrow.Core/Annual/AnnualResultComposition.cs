using System.Globalization;

namespace FlahaGrow.Core.Annual;

/// <summary>Combines two completed annual illuminance runs only when their declared sensor/time contracts agree.</summary>
public static class AnnualResultComposition
{
    /// <summary>Creates a new manifest-owned run from completed daylight and electric runs.</summary>
    public static string ComposeRuns(string daylightFolder, string electricFolder)
    {
        daylightFolder = Path.GetFullPath(daylightFolder); electricFolder = Path.GetFullPath(electricFolder);
        var daylight = AnnualRun.Read(daylightFolder); var electric = AnnualRun.Read(electricFolder);
        AnnualPartStatus.RequireComplete(daylightFolder, daylight); AnnualPartStatus.RequireComplete(electricFolder, electric);
        if (daylight.Hours != electric.Hours || daylight.Sensors != electric.Sensors || !string.Equals(daylight.SensorHash, electric.SensorHash, StringComparison.Ordinal))
            throw new InvalidDataException("Daylight and electric runs must have identical hours, sensor count, and sensor ordering.");
        if (electric.Parts.Length != 1) throw new InvalidDataException("Electric annual run must contain one full-sensor result part.");
        var pointsPath = Path.Combine(daylightFolder, "0.pts");
        if (!File.Exists(pointsPath)) throw new InvalidDataException("Daylight run is missing its snapshotted 0.pts sensor file.");
        var points = File.ReadLines(pointsPath).Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
        if (points.Length != daylight.Sensors) throw new InvalidDataException("Daylight 0.pts snapshot does not match its manifest sensor count.");
        var inputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["daylightManifest"] = AnnualRun.HashFile(Path.Combine(daylightFolder, AnnualRun.ManifestName)),
            ["electricManifest"] = AnnualRun.HashFile(Path.Combine(electricFolder, AnnualRun.ManifestName))
        };
        foreach (var path in AnnualRun.RequireResults(daylightFolder, daylight)) inputs["daylight:" + Path.GetFileName(path)] = AnnualRun.HashFile(path);
        foreach (var path in AnnualRun.RequireResults(electricFolder, electric)) inputs["electric:" + Path.GetFileName(path)] = AnnualRun.HashFile(path);
        var root = AnnualRun.Create(Path.Combine(daylight.SourceRoot, "runs"), daylight.SourceRoot, daylight.Sky, points, inputs, daylight.ProjectId, daylight.AnalysisId, daylight.Hours);
        var combined = AnnualRun.Read(root);
        var daylightReaders = AnnualRun.RequireResults(daylightFolder, daylight).Select((path, index) => AnnualMatrix.Rows(path, daylight.Hours, daylight.Parts[index].Sensors).GetEnumerator()).ToArray();
        using var electricReader = AnnualMatrix.Rows(AnnualRun.RequireResults(electricFolder, electric)[0], electric.Hours, electric.Sensors).GetEnumerator();
        var writers = combined.Parts.Select(part => new StreamWriter(Path.Combine(root, part.ResultFile), false, System.Text.Encoding.ASCII)).ToArray();
        try
        {
            foreach (var writer in writers) writer.Write($"#?RADIANCE\nSOFTWARE= FlahaGrow AnnualResultComposition\nNROWS={combined.Hours}\nNCOLS={combined.Parts[Array.IndexOf(writers, writer)].Sensors}\nNCOMP=1\nFORMAT=ascii\n\n");
            for (var hour = 0; hour < combined.Hours; hour++)
            {
                if (!electricReader.MoveNext()) throw new InvalidDataException("Electric annual matrix is truncated.");
                for (var partIndex = 0; partIndex < writers.Length; partIndex++)
                {
                    if (!daylightReaders[partIndex].MoveNext()) throw new InvalidDataException("Daylight annual matrix is truncated.");
                    var part = combined.Parts[partIndex]; var row = daylightReaders[partIndex].Current;
                    for (var sensor = 0; sensor < part.Sensors; sensor++)
                    {
                        var value = row[sensor] + electricReader.Current[part.SensorStart + sensor];
                        if (!float.IsFinite(value) || value < 0) throw new InvalidDataException($"Combined illuminance is invalid at hour index {hour}, sensor index {part.SensorStart + sensor}.");
                        if (sensor > 0) writers[partIndex].Write(' ');
                        writers[partIndex].Write(value.ToString("G9", CultureInfo.InvariantCulture));
                    }
                    writers[partIndex].Write('\n');
                }
            }
            if (electricReader.MoveNext() || daylightReaders.Any(reader => reader.MoveNext())) throw new InvalidDataException("Source annual matrix contains extra rows.");
        }
        finally { foreach (var writer in writers) writer.Dispose(); foreach (var reader in daylightReaders) reader.Dispose(); }
        foreach (var part in combined.Parts) File.WriteAllText(Path.Combine(root, part.StateFile), combined.RunId.ToString("N") + " CommandsSucceeded");
        AnnualPartStatus.RequireComplete(root, combined);
        return root;
    }

    public static void Add(string daylightPath, string electricPath, string outputPath, int hours, int sensors)
    {
        if (hours <= 0 || sensors <= 0) throw new ArgumentOutOfRangeException(nameof(hours));
        using var daylight = AnnualMatrix.Rows(daylightPath, hours, sensors).GetEnumerator();
        using var electric = AnnualMatrix.Rows(electricPath, hours, sensors).GetEnumerator();
        using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.ASCII);
        writer.Write("#?RADIANCE\nSOFTWARE= FlahaGrow AnnualResultComposition\n");
        writer.Write($"NROWS={hours}\nNCOLS={sensors}\nNCOMP=1\nFORMAT=ascii\n\n");
        for (var hour = 0; hour < hours; hour++)
        {
            if (!daylight.MoveNext() || !electric.MoveNext()) throw new InvalidDataException("Source annual matrix is truncated.");
            for (var sensor = 0; sensor < sensors; sensor++)
            {
                var value = daylight.Current[sensor] + electric.Current[sensor];
                if (!float.IsFinite(value) || value < 0) throw new InvalidDataException($"Combined illuminance is invalid at hour index {hour}, sensor index {sensor}.");
                if (sensor > 0) writer.Write(' ');
                writer.Write(value.ToString("G9", CultureInfo.InvariantCulture));
            }
            writer.Write('\n');
        }
        if (daylight.MoveNext() || electric.MoveNext()) throw new InvalidDataException("Source annual matrix contains extra rows.");
    }
}
