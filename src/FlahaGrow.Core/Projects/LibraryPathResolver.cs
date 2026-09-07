namespace FlahaGrow.Core.Projects;

/// <summary>Resolves the asset-root and legacy selector-folder forms without creating files or folders.</summary>
public sealed class LibraryPathResolver
{
    public const string Materials = "RadMaterials";
    public const string Glazing = "RadGlazing";
    public const string Ies = "RadIES";

    private static readonly string[] Sections = { Materials, Glazing, Ies };
    private readonly IProjectPathReader reader;

    public LibraryPathResolver(IProjectPathReader? reader = null) => this.reader = reader ?? new ProjectPathReader();

    /// <summary>Returns the complete FlahaGrow asset root, accepting that root or its containing folder.</summary>
    public string ResolveAssetRoot(string location)
    {
        var root = ProjectLayout.Absolute(location);
        if (IsAssetRoot(root)) return root;
        var child = Path.Combine(root, "FlahaGrow_Library_Small");
        if (IsAssetRoot(child)) return child;
        throw new DirectoryNotFoundException("Library must contain RadMaterials, RadGlazing, and RadIES, directly or under FlahaGrow_Library_Small.");
    }

    /// <summary>Returns one selector folder from a complete asset root, its parent, or a legacy direct folder.</summary>
    public string ResolveSection(string location, string section)
    {
        if (!Sections.Contains(section, StringComparer.Ordinal)) throw new ArgumentException("Unknown library section.", nameof(section));
        var input = ProjectLayout.Absolute(location);
        foreach (var candidate in new[] { Path.Combine(input, section), Path.Combine(input, "FlahaGrow_Library_Small", section) })
            if (reader.DirectoryExists(candidate)) return candidate;
        if (reader.DirectoryExists(input)) return input;
        throw new DirectoryNotFoundException($"{section} folder was not found. Supply that folder, the FlahaGrow library root, or its containing folder.");
    }

    private bool IsAssetRoot(string folder) => Sections.All(section => reader.DirectoryExists(Path.Combine(folder, section)));
}
