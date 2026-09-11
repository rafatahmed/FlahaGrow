using FlahaGrow.Core.Operations;
using FlahaGrow.Core.Projects;
using FlahaGrow.Grasshopper.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components.Setup;

public sealed class ProjectWorkspaceComponent : AsyncSetupComponent<WorkspaceContext>
{
    private readonly ActionLatch initializeLatch = new();
    private readonly ActionLatch refreshLatch = new();
    private readonly WorkspaceService service = new();
    private Guid? rememberedId;
    private string? rememberedRoot;
    public ProjectWorkspaceComponent() : base("Working Directory", "Workspace", "Opens or explicitly initializes a project and named analysis. Connect a Button to Initialize.") { }
    public override Guid ComponentGuid => new("d236c57b-eab1-4329-8e4e-beb2285ba04d");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddParameter(new PathsParameter(), "Resolved Paths", "Paths", "From Simulation Paths.", GH_ParamAccess.item);
        p.AddTextParameter("Project name", "Name", "Display name when creating a project.", GH_ParamAccess.item, "Greenhouse Study");
        p.AddTextParameter("Analysis name", "Analysis", "Named analysis folder.", GH_ParamAccess.item, "baseline");
        p.AddIntegerParameter("Workflow", "Workflow", "0 Annual daylight, 1 Electric-light preparation.", GH_ParamAccess.item, 0);
        p.AddBooleanParameter("Initialize", "Initialize", "Button: create/open workspace. Held True fires once.", GH_ParamAccess.item, false);
        p.AddBooleanParameter("Adopt existing", "Adopt", "Explicitly allow initialization of nonempty folders without manifests; files are preserved.", GH_ParamAccess.item, false);
        p.AddBooleanParameter("Refresh", "Refresh", "Button: reread an existing workspace without creating files.", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddParameter(new ProjectParameter(), "Project Context", "Project", "Opened project identity and configuration.", GH_ParamAccess.item);
        p.AddParameter(new AnalysisParameter(), "Analysis Context", "Analysis", "Opened named analysis.", GH_ParamAccess.item);
        p.AddTextParameter("Project folder", "Folder", "Project root.", GH_ParamAccess.item);
        p.AddTextParameter("Input folder", "Inputs", "Study input folders; not a Honeybee ModelToRad root.", GH_ParamAccess.item);
        p.AddTextParameter("Runs folder", "Runs", "Container for future isolated simulation runs.", GH_ParamAccess.item);
        p.AddTextParameter("Library folder", "Library", "Resolved shared assets.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Workspace operation status.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var goo = new PathsGoo(); string name = "Greenhouse Study", analysis = "baseline"; var workflow = 0; var initialize = false; var adopt = false; var refresh = false;
        da.GetData(1, ref name); da.GetData(2, ref analysis); da.GetData(3, ref workflow); da.GetData(4, ref initialize); da.GetData(5, ref adopt); da.GetData(6, ref refresh);
        var create = initializeLatch.Observe(initialize); var reread = refreshLatch.Observe(refresh);
        if (!da.GetData(0, ref goo) || !goo.IsValid) { Invalidate(); da.SetData(6, "Connect resolved project paths."); return; }
        var paths = goo.Value;
        var expectedId = string.Equals(rememberedRoot, paths.Project.Path, StringComparison.OrdinalIgnoreCase) ? rememberedId : null;
        var request = new WorkspaceRequest(paths, name, analysis, (AnalysisWorkflow)workflow, adopt);
        var key = Key(request);
        Update(key, create || reread || Changed(key), token =>
        {
            if (expectedId is not null && !create)
            {
                var manifestPath = Path.Combine(paths.Project.Path, ProjectManifest.FileName);
                if (!File.Exists(manifestPath))
                    throw new InvalidDataException($"Remembered workspace manifest is missing at '{manifestPath}'. Press Initialize to create a workspace; enable Adopt to preserve this legacy study.");
                var current = ProjectManifestCodec.Read(new ProjectPathReader().ReadManifest(manifestPath));
                if (current.ProjectId != expectedId) throw new InvalidDataException("Project identity changed; reconnect the intended project.");
            }
            var result = create ? service.Initialize(request, token) : service.Open(paths, analysis, token);
            return Task.FromResult(result);
        }, "Open an existing workspace or press Initialize.");
        if (Result is { } workspace)
        {
            rememberedRoot = workspace.Project.Root.Path; rememberedId = workspace.Project.Manifest.ProjectId;
            da.SetData(0, new ProjectGoo(workspace.Project)); da.SetData(1, new AnalysisGoo(workspace));
            da.SetData(2, workspace.Project.Root.Path); da.SetData(3, workspace.InputsFolder); da.SetData(4, workspace.RunsFolder); da.SetData(5, paths.Library?.Path);
            da.SetData(6, $"Opened {workspace.Project.Manifest.Name} / {workspace.Analysis.Name}. No simulation has been launched.");
        }
        else da.SetData(6, Status);
    }
    public override bool Write(GH_IWriter writer)
    {
        if (rememberedRoot is not null && rememberedId is not null) { writer.SetString("Root", rememberedRoot); writer.SetGuid("ProjectId", rememberedId.Value); }
        return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
        Invalidate(); initializeLatch.Disarm(); refreshLatch.Disarm(); rememberedRoot = null; rememberedId = null;
        if (reader.ItemExists("Root") && reader.ItemExists("ProjectId")) { rememberedRoot = reader.GetString("Root"); rememberedId = reader.GetGuid("ProjectId"); }
        return base.Read(reader);
    }
}
