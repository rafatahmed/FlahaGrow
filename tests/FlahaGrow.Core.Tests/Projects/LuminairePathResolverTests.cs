using FlahaGrow.Core.Projects;
using Xunit;

namespace FlahaGrow.Core.Tests.Projects;

public sealed class LuminairePathResolverTests
{
    [Fact]
    public void ResolvesLuminaireFolderInsideProjectRoot() =>
        Assert.Equal(@"D:\study\Luminaire_files", LuminairePathResolver.ResolveFolder(@"D:\study"));

    [Fact]
    public void RejectsRelativeProjectRoots() =>
        Assert.Throws<ArgumentException>(() => LuminairePathResolver.ResolveFolder("study"));
}
