using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper;

public sealed class FlahaGrowAssemblyInfo : GH_AssemblyInfo
{
    public override string Name => "FlahaGrow";
    public override string Description => "Annual greenhouse-lighting simulation components for Grasshopper.";
    public override Guid Id => new("ec22dc49-8acb-4b45-9b8a-8d8f24e9bc76");
    public override string AuthorName => "Rafat A. Al Khashan";
    public override string AuthorContact => "https://github.com/rafatahmed/FlahaGrow";
}
