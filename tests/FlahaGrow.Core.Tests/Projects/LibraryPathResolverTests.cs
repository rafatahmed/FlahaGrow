using FlahaGrow.Core.Projects;
using Xunit;

namespace FlahaGrow.Core.Tests.Projects;

public sealed class LibraryPathResolverTests
{
    private readonly MemoryReader reader = new();
    private LibraryPathResolver Resolver => new(reader);

    [Theory]
    [InlineData(LibraryPathResolver.Materials)]
    [InlineData(LibraryPathResolver.Glazing)]
    [InlineData(LibraryPathResolver.Ies)]
    public void SectionAcceptsAssetRoot(string section)
    {
        AddAssetRoot(@"D:\bundle\FlahaGrow_Library_Small");
        Assert.Equal(Path.Combine(@"D:\bundle\FlahaGrow_Library_Small", section), Resolver.ResolveSection(@"D:\bundle\FlahaGrow_Library_Small", section));
    }

    [Fact]
    public void SectionAcceptsAssetRootParent()
    {
        AddAssetRoot(@"D:\bundle\FlahaGrow_Library_Small");
        Assert.Equal(@"D:\bundle\FlahaGrow_Library_Small\RadIES", Resolver.ResolveSection(@"D:\bundle", LibraryPathResolver.Ies));
    }

    [Fact]
    public void SectionPreservesLegacyDirectFolder()
    {
        reader.Directories.Add(@"D:\legacy\custom-materials");
        Assert.Equal(@"D:\legacy\custom-materials", Resolver.ResolveSection(@"D:\legacy\custom-materials", LibraryPathResolver.Materials));
    }

    [Fact]
    public void MissingSectionFailsWithoutSelectingAnUnrelatedFolder()
    {
        Assert.Throws<DirectoryNotFoundException>(() => Resolver.ResolveSection(@"D:\missing", LibraryPathResolver.Glazing));
    }

    [Fact]
    public void IncompleteLibraryCannotBeMistakenForALegacySelectorFolder()
    {
        reader.Directories.Add(@"D:\bundle");
        reader.Directories.Add(@"D:\bundle\RadMaterials");
        Assert.Throws<DirectoryNotFoundException>(() => Resolver.ResolveSection(@"D:\bundle", LibraryPathResolver.Glazing));
    }

    private void AddAssetRoot(string root)
    {
        reader.Directories.Add(root);
        foreach (var section in new[] { LibraryPathResolver.Materials, LibraryPathResolver.Glazing, LibraryPathResolver.Ies })
            reader.Directories.Add(Path.Combine(root, section));
    }

    private sealed class MemoryReader : IProjectPathReader
    {
        public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool DirectoryExists(string path) => Directories.Contains(path);
        public bool FileExists(string path) => false;
        public string ReadManifest(string path) => throw new NotSupportedException();
    }
}
