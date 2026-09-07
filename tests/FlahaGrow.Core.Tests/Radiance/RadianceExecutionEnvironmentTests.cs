using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Radiance;
using Xunit;

namespace FlahaGrow.Core.Tests.Radiance;

public sealed class RadianceExecutionEnvironmentTests
{
    private static readonly RadianceInstallation Installation = new(@"C:\Radiance\bin", @"C:\Radiance\lib",
        new Dictionary<string, string> { ["ies2rad"] = @"C:\Radiance\bin\ies2rad.exe" }, Array.Empty<string>(), "fixture");

    [Fact]
    public void ReturnsTheSelectedReadyInstallation()
    {
        var status = new RadianceStatus(RadianceState.Ready, Installation, "6.1", null, Array.Empty<string>()) { Workflow = AnalysisWorkflow.ElectricLighting };
        Assert.Same(Installation, RadianceExecutionEnvironment.Require(status, AnalysisWorkflow.ElectricLighting));
    }

    [Fact]
    public void RejectsAnUncheckedEnvironment()
    {
        var status = new RadianceStatus(RadianceState.Failed, Installation, null, null, Array.Empty<string>()) { Workflow = AnalysisWorkflow.ElectricLighting };
        Assert.Throws<InvalidOperationException>(() => RadianceExecutionEnvironment.Require(status, AnalysisWorkflow.ElectricLighting));
    }

    [Fact]
    public void RejectsAnEnvironmentCheckedForAnotherWorkflow()
    {
        var status = new RadianceStatus(RadianceState.Ready, Installation, "6.1", null, Array.Empty<string>()) { Workflow = AnalysisWorkflow.AnnualDaylight };
        Assert.Throws<InvalidOperationException>(() => RadianceExecutionEnvironment.Require(status, AnalysisWorkflow.ElectricLighting));
    }

    [Fact]
    public void UsesTheSelectedInstallationAndItsSeparateCalculationLibrary()
    {
        var selected = Installation with { BinFolder = @"D:\Ladybug\bin", LibraryFolder = @"D:\CustomLibrary", Executables = new Dictionary<string, string> { ["ies2rad"] = @"D:\Ladybug\bin\ies2rad.exe" } };
        var status = new RadianceStatus(RadianceState.Ready, selected, "6.1", null, Array.Empty<string>()) { Workflow = AnalysisWorkflow.ElectricLighting };
        var result = RadianceExecutionEnvironment.Require(status, AnalysisWorkflow.ElectricLighting, @"D:\Ladybug\bin");
        Assert.Equal(@"D:\Ladybug\bin", result.BinFolder);
        Assert.Equal(@"D:\CustomLibrary", result.LibraryFolder);
        Assert.Equal(@"D:\Ladybug\bin\ies2rad.exe", result.Executables["ies2rad"]);
    }

    [Fact]
    public void RejectsABinThatWouldSelectAnotherInstallation()
    {
        var status = new RadianceStatus(RadianceState.Ready, Installation, "6.1", null, Array.Empty<string>()) { Workflow = AnalysisWorkflow.ElectricLighting };
        Assert.Throws<InvalidOperationException>(() => RadianceExecutionEnvironment.Require(status, AnalysisWorkflow.ElectricLighting, @"C:\AnotherRadiance\bin"));
    }
}
