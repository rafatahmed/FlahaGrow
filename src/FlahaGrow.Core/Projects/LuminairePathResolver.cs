namespace FlahaGrow.Core.Projects;

/// <summary>Defines the one project-local location for generated luminaire assets.</summary>
public static class LuminairePathResolver
{
    public const string FolderName = "Luminaire_files";

    public static string ResolveFolder(string projectRoot) => Path.Combine(ProjectLayout.Absolute(projectRoot), FolderName);
}
