using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Radiance;
using Xunit;

namespace FlahaGrow.Core.Tests.Radiance;

public sealed class RadianceTests
{
    private readonly Files files = new();
    private static RadianceRequest Request => new() { ExplicitLocation = @"C:\Radiance" };
    private RadianceDiscovery Discovery => new(files);

    private sealed class PassingCheck : IRadianceWorkflowCheck
    {
        public int Calls;
        public bool Passed = true;
        public Func<RadianceInstallation, bool>? Resolve;
        public Task<WorkflowCheck> CheckAsync(RadianceInstallation installation, AnalysisWorkflow workflow)
        { Calls++; return Task.FromResult(new WorkflowCheck(Resolve?.Invoke(installation) ?? Passed, "Fixture execution result")); }
    }

    [Fact]
    public async Task ExecutionFailureFallsBackOnlyForAutomaticSelection()
    {
        files.Install(@"C:\First"); files.Install(@"C:\Second");
        var check = new PassingCheck { Resolve = installation => installation.BinFolder.Contains("Second") };
        var service = new RadianceStatusService(Discovery, new Runner(), check);
        var request = new RadianceRequest { StandaloneLocations = new[] { @"C:\First", @"C:\Second" } };
        var automatic = await service.CheckAsync(request);
        Assert.True(automatic.Ready); Assert.Equal(@"C:\Second\bin", automatic.Installation!.BinFolder);
        Assert.Contains(automatic.Diagnostics, line => line.Contains("Readiness check failed"));
        Assert.False((await service.CheckAsync(request with { ExplicitLocation = @"C:\First" })).Ready);
    }

    [Fact]
    public async Task ElectricCheckRejectsMissingSceneAndCleansFixture()
    {
        files.Install(@"C:\Radiance"); string? folder = null;
        var runner = new Runner { Resolve = command => { folder = command.WorkingDirectory; return new(ProcessState.Exited, 0, "", "", false); } };
        var result = await new RadianceWorkflowCheck(runner).CheckAsync(Discovery.Discover(Request with { Workflow = AnalysisWorkflow.ElectricLighting }).Selected!, AnalysisWorkflow.ElectricLighting);
        Assert.False(result.Passed); Assert.Contains("no scene", result.Detail);
        Assert.NotNull(folder); Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public async Task WorkflowTimeoutDoesNotReportReady()
    {
        files.Install(@"C:\Radiance");
        var runner = new Runner { Report = new(ProcessState.TimedOut, null, "", "", false) };
        var result = await new RadianceWorkflowCheck(runner).CheckAsync(Discovery.Discover(Request).Selected!, AnalysisWorkflow.AnnualDaylight);
        Assert.False(result.Passed); Assert.Contains("TimedOut", result.Detail);
    }

    [Fact]
    public void DuplicatePathsDoNotExhaustDiscoveryBudget()
    {
        files.Install(@"C:\Last");
        var result = Discovery.Discover(new() { SearchPaths = Enumerable.Repeat(@"C:\duplicate", 200).Append(@"C:\Last").ToArray() });
        Assert.NotNull(result.Selected);
        Assert.DoesNotContain(result.Diagnostics, message => message.Contains("limit"));
        Assert.True(files.DirectoryReads <= 4);
    }

    [Fact]
    public async Task ExecutionFailurePreventsReadinessAndSuccessfulExecutionIsCached()
    {
        files.Install(@"C:\Radiance"); var runner = new Runner(); var execution = new PassingCheck { Passed = false };
        var service = new RadianceStatusService(Discovery, runner, execution);
        Assert.False((await service.CheckAsync(Request)).Ready);
        execution.Passed = true;
        Assert.True((await service.CheckAsync(Request)).Ready);
        Assert.True((await service.CheckAsync(Request)).Ready);
        Assert.Equal(2, execution.Calls);
        await service.CheckAsync(Request, refresh: true);
        Assert.Equal(3, execution.Calls);
    }

    [Theory]
    [InlineData("0 0 1", true)]
    [InlineData("0 0 0", false)]
    [InlineData("NaN 0 1", false)]
    [InlineData("version", false)]
    public async Task AnnualExecutionRequiresFiniteUnitVector(string output, bool expected)
    {
        files.Install(@"C:\Radiance");
        var runner = new Runner { Resolve = command =>
        {
            Assert.Equal(@"C:\Radiance\lib\reinsrc.cal", command.Arguments[3]);
            Assert.True(File.Exists(command.Arguments.Last()));
            return new(ProcessState.Exited, 0, output, "", false);
        } };
        var check = await new RadianceWorkflowCheck(runner).CheckAsync(Discovery.Discover(Request).Selected!, AnalysisWorkflow.AnnualDaylight);
        Assert.Equal(expected, check.Passed);
    }

    [Fact]
    public void RootAndBinNormalizeToSameInstallation()
    {
        files.Install(@"C:\Radiance");
        Assert.Equal(Discovery.Discover(Request).Selected!.Fingerprint,
            Discovery.Discover(Request with { ExplicitLocation = @"C:\Radiance\bin" }).Selected!.Fingerprint);
    }

    [Fact]
    public void InvalidExplicitLocationDoesNotFallBack()
    {
        files.Install(@"C:\Radiance");
        var result = Discovery.Discover(Request with { ExplicitLocation = @"D:\missing", SearchPaths = new[] { @"C:\Radiance\bin" } });
        Assert.Null(result.Selected); Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void CandidatesAreDeduplicatedAndKeepSearchOrder()
    {
        files.Install(@"C:\First"); files.Install(@"C:\Second");
        var result = Discovery.Discover(new() { SearchPaths = new[] { @"C:\First\bin", @"c:\first\bin", @"C:\Second\bin" } });
        Assert.Equal(2, result.Candidates.Count); Assert.Equal(@"C:\First\bin", result.Selected!.BinFolder);
    }

    [Fact]
    public void ProjectOverrideWinsOverPathButExplicitWinsOverProject()
    {
        files.Install(@"C:\First"); files.Install(@"C:\Second");
        var request = new RadianceRequest { ProjectRadianceLocation = @"C:\First", SearchPaths = new[] { @"C:\Second" } };
        Assert.Equal(@"C:\First\bin", Discovery.Discover(request).Selected!.BinFolder);
        Assert.Equal(@"C:\Second\bin", Discovery.Discover(request with { ExplicitLocation = @"C:\Second" }).Selected!.BinFolder);
    }

    [Fact]
    public void MissingToolsAreNotBorrowedFromAnotherInstallation()
    {
        files.Install(@"C:\First"); files.Install(@"C:\Second");
        files.Entries.Remove(@"C:\First\bin\rmtxop.exe");
        var result = Discovery.Discover(new() { SearchPaths = new[] { @"C:\First", @"C:\Second" } });
        Assert.Equal(@"C:\Second\bin", result.Selected!.BinFolder);
        Assert.Contains("rmtxop.exe", result.Candidates[0].Missing);
        Assert.DoesNotContain("rmtxop", result.Candidates[0].Executables.Keys);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public void StandaloneIsPreferredToLadybugAndPath()
    {
        files.Install(@"C:\Standalone"); files.Install(@"C:\Ladybug"); files.Install(@"C:\Path");
        var result = Discovery.Discover(new() { StandaloneLocations = new[] { @"C:\Standalone" },
            LadybugLocations = new[] { @"C:\Ladybug" }, SearchPaths = new[] { @"C:\Path" } });
        Assert.Equal("Standalone", result.Selected!.Source);
        Assert.Equal(@"C:\Standalone\bin", result.Selected.BinFolder);
        files.Entries.Remove(@"C:\Standalone\bin\oconv.exe");
        result = Discovery.Discover(new() { StandaloneLocations = new[] { @"C:\Standalone" }, LadybugLocations = new[] { @"C:\Ladybug" } });
        Assert.Equal("Ladybug Tools", result.Selected!.Source);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    public async Task FailedAutomaticProbeFallsBackButExplicitSelectionDoesNot()
    {
        files.Install(@"C:\First"); files.Install(@"C:\Second");
        var runner = new Runner { Resolve = command => command.Executable.Contains("First")
            ? new(ProcessState.Failed, null, "", "", false) : new(ProcessState.Exited, 0, "version", "", false) };
        var service = new RadianceStatusService(Discovery, runner, new PassingCheck());
        var request = new RadianceRequest { StandaloneLocations = new[] { @"C:\First" }, LadybugLocations = new[] { @"C:\Second" } };
        var status = await service.CheckAsync(request);
        Assert.True(status.Ready); Assert.Equal("Ladybug Tools", status.Installation!.Source);
        Assert.NotEmpty(status.Diagnostics);
        Assert.False((await service.CheckAsync(request with { ExplicitLocation = @"C:\First" })).Ready);
        Assert.Equal(3, runner.Calls);
    }

    [Fact]
    public async Task AutomaticVersionAttemptsAreBounded()
    {
        var locations = Enumerable.Range(0, 8).Select(i => @"C:\Install" + i).ToArray();
        foreach (var location in locations) files.Install(location);
        var runner = new Runner { Report = new(ProcessState.TimedOut, null, "", "", false) };
        var status = await new RadianceStatusService(Discovery, runner, new PassingCheck()).CheckAsync(new() { StandaloneLocations = locations });
        Assert.False(status.Ready); Assert.Equal(RadianceStatusService.MaximumVersionProbes, runner.Calls);
        Assert.Contains(status.Diagnostics, message => message.Contains("limit"));
    }

    [Fact]
    public void CapabilityListsDifferByWorkflow()
    {
        files.Install(@"C:\Radiance"); files.Entries.Remove(@"C:\Radiance\bin\ies2rad.exe");
        Assert.Empty(Discovery.Discover(Request).Selected!.Missing);
        Assert.Contains("ies2rad.exe", Discovery.Discover(Request with { Workflow = AnalysisWorkflow.ElectricLighting }).Selected!.Missing);
    }

    [Fact]
    public void CalculationFilesAndCustomLibraryAreVerified()
    {
        files.Install(@"C:\Radiance");
        files.Entries.Remove(@"C:\Radiance\lib\reinhart.cal");
        Assert.Contains("reinhart.cal", Discovery.Discover(Request).Selected!.Missing);
        files.Entries[@"D:\custom\reinhart.cal"] = "1"; files.Entries[@"D:\custom\reinsrc.cal"] = "1";
        Assert.Empty(Discovery.Discover(Request with { LibraryOverride = @"D:\custom" }).Selected!.Missing);
    }

    [Fact]
    public void DiscoveryHasABoundedSearch()
    {
        var result = Discovery.Discover(new() { SearchPaths = Enumerable.Range(0, 1000).Select(i => @"C:\Candidate" + i).ToArray() });
        Assert.Contains(result.Diagnostics, message => message.Contains("limit"));
        Assert.True(files.DirectoryReads <= 2 * RadianceDiscovery.MaximumCandidates);
    }

    [Fact]
    public async Task IncompleteInstallationDoesNotLaunchVersionProbe()
    {
        files.Install(@"C:\Radiance"); files.Entries.Remove(@"C:\Radiance\bin\oconv.exe");
        var runner = new Runner();
        var result = await new RadianceStatusService(Discovery, runner, new PassingCheck()).CheckAsync(Request);
        Assert.Equal(RadianceState.Incomplete, result.State); Assert.Equal(0, runner.Calls);
    }

    [Fact]
    public async Task StderrVersionIsAcceptedAndWarmChecksReuseProbe()
    {
        files.Install(@"C:\Radiance"); var runner = new Runner();
        var service = new RadianceStatusService(Discovery, runner, new PassingCheck());
        var result = await service.CheckAsync(Request);
        Assert.True(result.Ready); Assert.Equal("RADIANCE fixture", result.Version);
        await service.CheckAsync(Request); Assert.Equal(1, runner.Calls);
        await service.CheckAsync(Request, refresh: true); Assert.Equal(2, runner.Calls);
        files.Entries[@"C:\Radiance\bin\rcontrib.exe"] = "changed";
        await service.CheckAsync(Request); Assert.Equal(3, runner.Calls);
        files.Entries.Remove(@"C:\Radiance\lib\reinhart.cal");
        Assert.False((await service.CheckAsync(Request)).Ready); Assert.Equal(3, runner.Calls);
    }

    [Theory]
    [InlineData(ProcessState.Exited, 1, RadianceState.Failed)]
    [InlineData(ProcessState.TimedOut, 0, RadianceState.TimedOut)]
    [InlineData(ProcessState.Failed, 0, RadianceState.Failed)]
    public async Task FailedProbeNeverReportsReady(ProcessState state, int exit, RadianceState expected)
    {
        files.Install(@"C:\Radiance");
        var runner = new Runner { Report = new(state, exit, "", "error", false) };
        var service = new RadianceStatusService(Discovery, runner, new PassingCheck());
        Assert.Equal(expected, (await service.CheckAsync(Request)).State);
        await service.CheckAsync(Request); Assert.Equal(2, runner.Calls);
    }

    [Fact]
    public async Task ConcurrentRequestsShareOneProbeAndCancellationDoesNotCancelOtherWaiters()
    {
        files.Install(@"C:\Radiance"); var runner = new Runner { Pending = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var service = new RadianceStatusService(Discovery, runner, new PassingCheck());
        using var cancel = new CancellationTokenSource();
        var first = service.CheckAsync(Request, cancellationToken: cancel.Token);
        await runner.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = service.CheckAsync(Request);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        runner.Pending.SetResult(runner.Report);
        Assert.True((await second).Ready); Assert.Equal(1, runner.Calls);
    }

    [Fact]
    public async Task EmptySuccessfulProcessOutputDoesNotEstablishReadiness()
    {
        files.Install(@"C:\Radiance");
        var runner = new Runner { Report = new(ProcessState.Exited, 0, "", "", false) };
        Assert.False((await new RadianceStatusService(Discovery, runner, new PassingCheck()).CheckAsync(Request)).Ready);
    }

    [Fact]
    public async Task CacheEvictsCompletedChecksInsteadOfGrowingIndefinitely()
    {
        var runner = new Runner(); var service = new RadianceStatusService(Discovery, runner, new PassingCheck());
        for (var i = 0; i < 33; i++)
        {
            var path = @"C:\Install" + i; files.Install(path);
            await service.CheckAsync(Request with { ExplicitLocation = path });
        }
        Assert.Equal(33, runner.Calls);
        await service.CheckAsync(Request with { ExplicitLocation = @"C:\Install0" });
        Assert.Equal(34, runner.Calls);
    }

    [Fact]
    public void ChildEnvironmentDoesNotChangeHostVariables()
    {
        files.Install(@"C:\Radiance");
        var path = Environment.GetEnvironmentVariable("PATH"); var ray = Environment.GetEnvironmentVariable("RAYPATH");
        var result = RadianceStatusService.ChildEnvironment(Discovery.Discover(Request).Selected!);
        Assert.StartsWith(@"C:\Radiance\bin;", result["PATH"]);
        Assert.Equal(@".;C:\Radiance\lib", result["RAYPATH"]);
        Assert.Equal(path, Environment.GetEnvironmentVariable("PATH")); Assert.Equal(ray, Environment.GetEnvironmentVariable("RAYPATH"));
    }

    [Fact]
    public void ConsumerLookupDoesNotFallbackFromExplicitMissingOrIncompleteLocation()
    {
        files.Install(@"C:\Available");
        var request = new RadianceRequest { SearchPaths = new[] { @"C:\Available" }, ExplicitLocation = @"C:\Missing" };
        Assert.Null(Discovery.FindBin(request, "rfluxmtx", "rmtxop"));
        files.Install(@"C:\Incomplete");
        files.Entries.Remove(@"C:\Incomplete\bin\rmtxop.exe");
        Assert.Null(Discovery.FindBin(request with { ExplicitLocation = @"C:\Incomplete" }, "rfluxmtx", "rmtxop"));
    }

    [Fact]
    public void ConsumerLookupRequiresToolsFromOneInstallationAndAcceptsRootOrBin()
    {
        files.Install(@"C:\First"); files.Install(@"C:\Second");
        files.Entries.Remove(@"C:\First\bin\rmtxop.exe");
        files.Entries.Remove(@"C:\Second\bin\rfluxmtx.exe");
        var request = new RadianceRequest { SearchPaths = new[] { @"C:\First", @"C:\Second" } };
        Assert.Null(Discovery.FindBin(request, "rfluxmtx", "rmtxop"));
        files.Entries[@"C:\Second\bin\rfluxmtx.exe"] = "1";
        Assert.Equal(@"C:\Second\bin", Discovery.FindBin(request, "rfluxmtx", "rmtxop"));
        Assert.Equal(@"C:\Second\bin", Discovery.FindBin(request with { ExplicitLocation = @"C:\Second\bin" }, "rfluxmtx", "rmtxop"));
    }

    private sealed class Files : IRadianceFiles
    {
        public readonly HashSet<string> Directories = new(StringComparer.OrdinalIgnoreCase);
        public readonly Dictionary<string, string> Entries = new(StringComparer.OrdinalIgnoreCase);
        public int DirectoryReads;
        public bool DirectoryExists(string path) { Interlocked.Increment(ref DirectoryReads); return Directories.Contains(path); }
        public string? Fingerprint(string path) => Entries.GetValueOrDefault(path);
        public void Install(string root)
        {
            Directories.Add(root); Directories.Add(Path.Combine(root, "bin"));
            foreach (var tool in new[] { "rcontrib", "gendaymtx", "oconv", "rfluxmtx", "dctimestep", "rmtxop", "cnt", "rcalc", "ies2rad", "xform" })
                Entries[Path.Combine(root, "bin", tool + ".exe")] = "1";
            foreach (var file in new[] { "reinsrc.cal", "reinhart.cal", "source.cal", "lamp.tab" }) Entries[Path.Combine(root, "lib", file)] = "1";
        }
    }

    private sealed class Runner : IRadianceProcessRunner
    {
        public int Calls;
        public Func<ProcessCommand, ProcessReport>? Resolve;
        public ProcessReport Report = new(ProcessState.Exited, 0, "", "RADIANCE fixture", false);
        public TaskCompletionSource<ProcessReport>? Pending;
        public TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<ProcessReport> RunAsync(ProcessCommand command, CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref Calls); Started.TrySetResult(); return Pending?.Task ?? Task.FromResult(Resolve?.Invoke(command) ?? Report); }
    }
}
