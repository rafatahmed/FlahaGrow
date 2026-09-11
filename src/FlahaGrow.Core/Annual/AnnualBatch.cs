namespace FlahaGrow.Core.Annual;

/// <summary>Turns generated commands into a one-shot, fail-fast Windows batch. Pipeline sides run separately.</summary>
public static class AnnualBatch
{
    public static IReadOnlyList<string> Build(IEnumerable<string> commands, Guid runId, int part, bool keepIntermediates = false)
    {
        var state = $"annual_state_part{part}.txt";
        var log = $"annual_progress_part{part}.log";
        var errors = $"annual_errors_part{part}.log";
        var id = runId.ToString("N");
        var lines = new List<string>
        {
            "@echo off", "setlocal EnableExtensions DisableDelayedExpansion", "set \"ERRORLEVEL=\"", "cd /d \"%~dp0\"",
            "if errorlevel 1 exit /b 1",
            $"mkdir part{part}.execution-lock 2>nul", "if errorlevel 1 exit /b 1",
            $"echo {id} Running> {state}", "if errorlevel 1 exit /b 1"
        };
        var step = 0;
        void Add(string command)
        {
            lines.Add($"set \"fg_step={++step}\"");
            lines.Add($"echo Step {step} >> {errors}");
            lines.Add("if not \"%errorlevel%\"==\"0\" goto fg_failed");
            lines.Add(command + $" 2>> {errors}");
            lines.Add("if not \"%errorlevel%\"==\"0\" goto fg_failed");
        }
        foreach (var command in commands)
        {
            if (string.IsNullOrWhiteSpace(command) || command.StartsWith("@echo", StringComparison.OrdinalIgnoreCase)
                || command.StartsWith("setlocal", StringComparison.OrdinalIgnoreCase)) continue;
            if (command.StartsWith("set ", StringComparison.OrdinalIgnoreCase)) { lines.Add(command); continue; }
            var pipeline = command.IndexOf(" | ", StringComparison.Ordinal);
            if (pipeline < 0) Add(command);
            else
            {
                var intermediate = $"pipeline_part{part}_step{step}.tmp";
                Add(command[..pipeline] + " > " + intermediate);
                Add(command[(pipeline + 3)..] + " < " + intermediate);
                // The consumer has succeeded at this point. Do not retain a large pipe
                // staging file for every completed annual run. Preserve it on producer
                // or consumer failure because the cleanup line is never reached then.
                lines.Add($"if exist \"{intermediate}\" del /q \"{intermediate}\" >nul 2>nul");
                lines.Add("cmd /d /c exit /b 0");
            }
        }
        if (!keepIntermediates)
        {
            // These are reproducible from the run snapshot. Keep the final and source-term
            // .ill matrices for validation/diagnosis, but remove the largest coefficient,
            // sky, octree and weather intermediates only after every command succeeded.
            var generated = new[]
            {
                $"Weather_{part}.smx", $"Weatherd_{part}.smx", $"WeathersunM*_{part}.smx",
                $"amodel_{part}.oct", $"bmodel_{part}.oct", $"sunCoefficientsDDS_{part}.oct", $"suns_{part}.rad",
                $"illum_part{part}.mtx", $"billum_part{part}.mtx", $"cdsDDS_part{part}.mtx"
            };
            foreach (var file in generated) lines.Add($"if exist \"{file}\" del /q \"{file}\" >nul 2>nul");
            lines.Add("cmd /d /c exit /b 0");
        }
        lines.Add($"echo {id} CommandsSucceeded> {state}.tmp");
        lines.Add("if errorlevel 1 goto fg_failed");
        lines.Add($"move /y {state}.tmp {state} >nul");
        lines.Add("if errorlevel 1 goto fg_failed");
        lines.Add($"echo [Part {part}] Commands succeeded; matrix validation pending>> {log}");
        lines.Add("exit /b 0");
        lines.Add(":fg_failed");
        lines.Add("set \"fg_exit=%errorlevel%\"");
        lines.Add($"echo {id} Failed step=%fg_step% exit=%fg_exit% > {state}");
        lines.Add($"echo [Part {part}] Failed step=%fg_step% exit=%fg_exit%. See {errors}.>> {log}");
        lines.Add("exit /b %fg_exit%");
        return lines;
    }
}
