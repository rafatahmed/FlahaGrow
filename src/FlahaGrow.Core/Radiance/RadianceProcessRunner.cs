using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace FlahaGrow.Core.Radiance;

public enum ProcessState { Exited, Failed, TimedOut, Cancelled }
public sealed record ProcessCommand(string Executable, IReadOnlyList<string> Arguments, string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment, TimeSpan Timeout, int OutputLimit = 16384)
{
    /// <summary>Optional literal stdin. It is never passed through a command shell.</summary>
    public string? StandardInput { get; init; }
}
public sealed record ProcessReport(ProcessState State, int? ExitCode, string StandardOutput, string StandardError,
    bool OutputTruncated, string? Diagnostic = null);
public interface IRadianceProcessRunner
{
    Task<ProcessReport> RunAsync(ProcessCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Direct process execution with bounded capture and concurrent pipe draining.</summary>
public sealed class RadianceProcessRunner : IRadianceProcessRunner
{
    public async Task<ProcessReport> RunAsync(ProcessCommand command, CancellationToken cancellationToken = default)
    {
        if (!Path.IsPathFullyQualified(command.Executable) || !Path.IsPathFullyQualified(command.WorkingDirectory))
            throw new ArgumentException("Executable and working directory must be absolute paths.");
        if (command.Timeout <= TimeSpan.Zero || command.Timeout > TimeSpan.FromMinutes(5) || command.OutputLimit is < 1 or > 1048576)
            throw new ArgumentException("Invalid process timeout or capture limit.");
        if (cancellationToken.IsCancellationRequested) return new(ProcessState.Cancelled, null, "", "", false);
        var start = new ProcessStartInfo(command.Executable)
        {
            WorkingDirectory = command.WorkingDirectory, UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            RedirectStandardInput = command.StandardInput is not null
        };
        foreach (var argument in command.Arguments) start.ArgumentList.Add(argument);
        foreach (var variable in command.Environment) start.Environment[variable.Key] = variable.Value;
        using var process = new Process { StartInfo = start };
        try { process.Start(); }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException)
        { return new(ProcessState.Failed, null, "", "", false, ex.Message); }

        using var timeout = new CancellationTokenSource(command.Timeout);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        var stdout = DrainAsync(process.StandardOutput, command.OutputLimit, stop.Token);
        var stderr = DrainAsync(process.StandardError, command.OutputLimit, stop.Token);
        var input = command.StandardInput is null ? Task.FromResult<string?>(null)
            : WriteInputAsync(process.StandardInput, command.StandardInput, stop.Token);
        var state = ProcessState.Exited;
        string? diagnostic = null;
        try
        {
            await process.WaitForExitAsync(stop.Token).ConfigureAwait(false);
            await Task.WhenAll(stdout, stderr, input).WaitAsync(stop.Token).ConfigureAwait(false);
            if (stop.IsCancellationRequested) throw new OperationCanceledException(stop.Token);
        }
        catch (OperationCanceledException)
        {
            state = cancellationToken.IsCancellationRequested ? ProcessState.Cancelled : ProcessState.TimedOut;
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or TimeoutException)
            { diagnostic = "Process cleanup: " + ex.Message; }
        }
        var output = await stdout.ConfigureAwait(false);
        var error = await stderr.ConfigureAwait(false);
        var inputError = await input.ConfigureAwait(false);
        if (state == ProcessState.Exited && inputError is not null)
        { state = ProcessState.Failed; diagnostic = inputError; }
        return new(state, process.HasExited ? process.ExitCode : null, output.Text, error.Text, output.Truncated || error.Truncated, diagnostic);
    }

    private static async Task<string?> WriteInputAsync(StreamWriter writer, string input, CancellationToken token)
    {
        try
        {
            await writer.WriteAsync(input.AsMemory(), token).ConfigureAwait(false);
            await writer.FlushAsync().WaitAsync(token).ConfigureAwait(false);
            writer.Close();
            return null;
        }
        catch (OperationCanceledException) { return null; }
        catch (IOException ex) { return "Process input: " + ex.Message; }
    }

    private static async Task<(string Text, bool Truncated)> DrainAsync(StreamReader reader, int limit, CancellationToken token)
    {
        var text = new StringBuilder();
        var buffer = new char[4096];
        var truncated = false;
        try
        {
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), token).ConfigureAwait(false)) > 0)
            {
                var retained = Math.Min(count, limit - text.Length);
                text.Append(buffer, 0, retained);
                truncated |= retained != count;
            }
        }
        catch (OperationCanceledException) { }
        return (text.ToString(), truncated);
    }
}
