using FlahaGrow.Core.Operations;
using Xunit;

namespace FlahaGrow.Core.Tests.Projects;

public sealed class SetupOperationTests
{
    [Fact]
    public void HeldActionFiresOnceAndRearmsOnRelease()
    {
        var latch = new ActionLatch();
        Assert.True(latch.Observe(true)); Assert.False(latch.Observe(true));
        Assert.False(latch.Observe(false)); Assert.True(latch.Observe(true));
    }
    [Fact]
    public void RestoredTrueActionDoesNotRunUntilRearmed()
    {
        var latch = new ActionLatch(); latch.Disarm();
        Assert.False(latch.Observe(true)); Assert.False(latch.Observe(true));
        Assert.False(latch.Observe(false)); Assert.True(latch.Observe(true));
    }
    [Fact]
    public async Task SupersededWorkCannotPublishOldResult()
    {
        var operation = new SetupOperation<string>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var first = operation.Start(async _ => { started.SetResult(); await release.Task; return "old"; });
        await started.Task;
        var revision = operation.Revision;
        var second = operation.Start(_ => Task.FromResult("new"));
        Assert.True(operation.Revision > revision); Assert.False(second.IsCompleted);
        release.SetResult();
        Assert.True((await first).Cancelled);
        Assert.Equal("new", (await second).Value);
        Assert.Same(second, operation.Current);
        operation.Cancel();
    }
    [Fact]
    public async Task FailureIsReportedAndNextOperationCanRecover()
    {
        var operation = new SetupOperation<string>();
        Assert.Equal("fixture failure", (await operation.Start(_ => throw new IOException("fixture failure"))).Error);
        Assert.Equal("ready", (await operation.Start(_ => Task.FromResult("ready"))).Value);
        operation.Cancel();
    }
    [Fact]
    public async Task RemovalCancelsRunningWork()
    {
        var operation = new SetupOperation<string>();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var task = operation.Start(async token => { started.SetResult(); await Task.Delay(Timeout.Infinite, token); return "unreachable"; });
        await started.Task; operation.Cancel();
        Assert.True((await task).Cancelled);
    }
}
