namespace FlahaGrow.Core.Annual;

public sealed record AnnualPartReport(string State, string Detail)
{
    public bool Complete => State == "Completed";
}

public static class AnnualPartStatus
{
    public static AnnualPartReport Read(string folder, AnnualRunManifest run, AnnualRunPart part)
    {
        try
        {
            var path = Path.Combine(folder, part.StateFile);
            if (!File.Exists(path)) return new("Prepared", "Not launched.");
            var state = File.ReadAllText(path).Trim();
            var prefix = run.RunId.ToString("N") + " ";
            if (!state.StartsWith(prefix, StringComparison.Ordinal)) return new("Invalid", "State belongs to another run or is incomplete.");
            state = state[prefix.Length..];
            if (state.StartsWith("Failed ", StringComparison.Ordinal)) return new("Failed", state);
            if (state == "Running") return new("Running", "Commands executing.");
            if (state != "CommandsSucceeded") return new("Invalid", "Unknown command state.");
            AnnualMatrix.ValidateIlluminance(Path.Combine(folder, part.ResultFile), run.Hours, part.Sensors);
            return new("Completed", "All commands succeeded and the final matrix passed validation.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException)
        { return new("Invalid", ex.Message); }
    }

    public static void RequireComplete(string folder, AnnualRunManifest run)
    {
        foreach (var part in run.Parts)
        {
            var status = Read(folder, run, part);
            if (!status.Complete) throw new InvalidDataException($"Part {part.Index}: {status.State}. {status.Detail}");
        }
    }
}
