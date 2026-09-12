using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Radiance;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

namespace FlahaGrow.Grasshopper.Parameters;

/// <summary>Transient typed connections. Component configuration rehydrates contexts after reopening.</summary>
public abstract class SetupGoo<T> : GH_Goo<T> where T : class
{
    protected SetupGoo() { }
    protected SetupGoo(T value) : base(value) { }
    public override bool IsValid => Value is not null;
    public override string TypeDescription => TypeName + " supplied by FlahaGrow Setup.";
    public override string ToString() => Value is null ? "Unresolved " + TypeName : TypeName;
    public override object ScriptVariable() => Value;
    public override bool CastFrom(object source)
    {
        if (source is T value) { Value = value; return true; }
        if (source is SetupGoo<T> goo) { Value = goo.Value; return true; }
        return false;
    }
}
public sealed class PathsGoo : SetupGoo<ResolvedPaths>
{
    public PathsGoo() { } public PathsGoo(ResolvedPaths value) : base(value) { }
    public override string TypeName => "Resolved Paths";
    public override IGH_Goo Duplicate() => Value is null ? new PathsGoo() : new PathsGoo(Value);
}
public sealed class ProjectGoo : SetupGoo<ProjectContext>
{
    public ProjectGoo() { } public ProjectGoo(ProjectContext value) : base(value) { }
    public override string TypeName => "Project Context";
    public override IGH_Goo Duplicate() => Value is null ? new ProjectGoo() : new ProjectGoo(Value);
}
public sealed class AnalysisGoo : SetupGoo<WorkspaceContext>
{
    public AnalysisGoo() { } public AnalysisGoo(WorkspaceContext value) : base(value) { }
    public override string TypeName => "Analysis Context";
    public override IGH_Goo Duplicate() => Value is null ? new AnalysisGoo() : new AnalysisGoo(Value);
}
public sealed class RadianceGoo : SetupGoo<RadianceStatus>
{
    public RadianceGoo() { } public RadianceGoo(RadianceStatus value) : base(value) { }
    public override string TypeName => "Radiance Environment";
    public override IGH_Goo Duplicate() => Value is null ? new RadianceGoo() : new RadianceGoo(Value);
}

public sealed class PathsParameter : GH_Param<PathsGoo>
{
    public PathsParameter() : base("Resolved Paths", "Paths", "Resolved project locations.", "FlahaGrow", "00 Setup", GH_ParamAccess.item) { }
    public override Guid ComponentGuid => new("ac523fea-16c1-4501-b8e3-eb39a575215c");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
}
public sealed class ProjectParameter : GH_Param<ProjectGoo>
{
    public ProjectParameter() : base("Project Context", "Project", "Opened FlahaGrow project.", "FlahaGrow", "00 Setup", GH_ParamAccess.item) { }
    public override Guid ComponentGuid => new("8be5c7f1-d559-4a84-ac4b-94ea047a7868");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
}
public sealed class AnalysisParameter : GH_Param<AnalysisGoo>
{
    public AnalysisParameter() : base("Analysis Context", "Analysis", "Opened FlahaGrow analysis.", "FlahaGrow", "00 Setup", GH_ParamAccess.item) { }
    public override Guid ComponentGuid => new("8cce8328-7691-4c36-81e6-d968260841b8");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
}
public sealed class RadianceParameter : GH_Param<RadianceGoo>
{
    public RadianceParameter() : base("Radiance Environment", "Radiance", "Checked Radiance installation.", "FlahaGrow", "00 Setup", GH_ParamAccess.item) { }
    public override Guid ComponentGuid => new("bda63116-0a69-489c-ab37-24d1f6b61155");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
}
