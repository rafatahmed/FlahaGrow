using FlahaGrow.Core.Radiance;
using Xunit;

namespace FlahaGrow.Core.Tests.Radiance;

public sealed class ProcessRunnerTests
{
    private static ProcessCommand Command(string script, int limit = 16384) => new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"WindowsPowerShell\v1.0\powershell.exe"),
        new[] { "-NoProfile", "-NonInteractive", "-Command", script }, Path.GetTempPath(),
        new Dictionary<string, string> { ["FLAHAGROW_TEST_CHILD"] = "child-only" }, TimeSpan.FromSeconds(10), limit);

    [Fact]
    public async Task BothPipesAreDrainedAndCaptureIsBounded()
    {
        var report = await new RadianceProcessRunner().RunAsync(Command("[Console]::Out.Write(('x' * 100000)); [Console]::Error.Write(('y' * 100000))", 128));
        Assert.Equal(ProcessState.Exited, report.State); Assert.Equal(0, report.ExitCode);
        Assert.Equal(128, report.StandardOutput.Length); Assert.Equal(128, report.StandardError.Length); Assert.True(report.OutputTruncated);
    }

    [Fact]
    public async Task ArgumentsAndEnvironmentReachOnlyTheChild()
    {
        var before = Environment.GetEnvironmentVariable("FLAHAGROW_TEST_CHILD");
        var report = await new RadianceProcessRunner().RunAsync(Command("[Console]::Out.Write($env:FLAHAGROW_TEST_CHILD); [Console]::Error.Write('version on stderr'); exit 7"));
        Assert.Equal(7, report.ExitCode); Assert.Equal("child-only", report.StandardOutput); Assert.Equal("version on stderr", report.StandardError);
        Assert.Equal(before, Environment.GetEnvironmentVariable("FLAHAGROW_TEST_CHILD"));
    }

    [Fact]
    public async Task TimeoutTerminatesHungProbe()
    {
        var report = await new RadianceProcessRunner().RunAsync(Command("Start-Sleep -Seconds 30") with { Timeout = TimeSpan.FromMilliseconds(300) });
        Assert.Equal(ProcessState.TimedOut, report.State);
        Assert.NotNull(report.ExitCode);
    }

    [Fact]
    public async Task CallerCanCancelAnActiveProcess()
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));
        var report = await new RadianceProcessRunner().RunAsync(Command("Start-Sleep -Seconds 30"), stop.Token);
        Assert.Equal(ProcessState.Cancelled, report.State);
    }

    [Fact]
    public async Task MissingExecutableIsReported()
    {
        var report = await new RadianceProcessRunner().RunAsync(Command("") with { Executable = @"C:\FlahaGrow-missing\none.exe" });
        Assert.Equal(ProcessState.Failed, report.State); Assert.Null(report.ExitCode); Assert.NotEmpty(report.Diagnostic!);
    }
}
