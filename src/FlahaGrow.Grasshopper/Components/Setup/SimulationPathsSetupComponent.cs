using System.Reflection;
using System.Windows.Forms;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.Projects;
using FlahaGrow.Grasshopper.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components.Setup;

public sealed class SimulationPathsSetupComponent : AsyncSetupComponent<PathResolution>
{
    private readonly ActionLatch refreshLatch = new();
    private Guid draftId = Guid.NewGuid();
    private string? pinnedRoot;
    private PathSource pinnedSource;
    private Guid? pinnedId;
    private bool reset;
    public SimulationPathsSetupComponent() : base("Simulation Paths", "Paths", "Resolves and remembers project and material-library locations without creating files. Right-click to reset the automatic location.") { }
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public override Guid ComponentGuid => new("71ce89f2-1439-4730-915f-07436692926c");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Project location", "Location", "Optional project root or project manifest path.", GH_ParamAccess.item); p[0].Optional = true;
        p.AddIntegerParameter("Location mode", "Mode", "0 Auto, 1 Project-relative, 2 System, 3 Custom.", GH_ParamAccess.item, 0);
        p.AddTextParameter("Library location", "Library", "Optional asset root or parent folder.", GH_ParamAccess.item); p[2].Optional = true;
        p.AddBooleanParameter("Refresh", "Refresh", "Connect a Button to recheck files. Does not create folders.", GH_ParamAccess.item, false);
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddParameter(new PathsParameter(), "Resolved Paths", "Paths", "Connect to Working Directory.", GH_ParamAccess.item);
        p.AddTextParameter("Project folder", "Folder", "Resolved project location; write access has not been tested.", GH_ParamAccess.item);
        p.AddTextParameter("Library folder", "Library", "Resolved asset root.", GH_ParamAccess.item);
        p.AddTextParameter("Status", "Status", "Resolution source and diagnostics.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string location = "", library = ""; var mode = 0; var refresh = false;
        da.GetData(0, ref location); da.GetData(1, ref mode); da.GetData(2, ref library);
        da.GetData(3, ref refresh);
        var statusIndex = 3;
        var usePin = mode == 0 && string.IsNullOrWhiteSpace(location) && pinnedRoot is not null;
        var request = new PathRequest
        {
            Mode = (LocationMode)mode, ProjectLocation = usePin ? pinnedRoot : Optional(location),
            DefinitionPath = usePin ? null : Optional(OnPingDocument()?.FilePath),
            DocumentsFolder = Optional(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)), DraftId = draftId,
            LibraryLocation = Optional(library), RadianceLocation = null,
            ResolveRadianceLocation = false,
            BundledLibraryLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "shared", "Library", "FlahaGrow_Library_Small")
        };
        var expectedId = usePin ? pinnedId : null;
        var key = Key(request);
        var start = refreshLatch.Observe(refresh) || reset || Changed(key); reset = false;
        Update(key, start, _ =>
        {
            var resolution = new ProjectPathResolver().Resolve(request);
            if (expectedId is not null && resolution.Paths?.Manifest?.ProjectId != expectedId)
                return Task.FromResult(new PathResolution(null, new[] { "The remembered project is missing or its identity changed. Select the existing project explicitly." }));
            return Task.FromResult(resolution);
        }, "Resolving paths…");
        if (Result?.Paths is { } paths)
        {
            if (mode == 0 && string.IsNullOrWhiteSpace(location))
            {
                if (pinnedRoot is null) { pinnedRoot = paths.Project.Path; pinnedSource = paths.Project.Source; }
                pinnedId = paths.Manifest?.ProjectId ?? pinnedId;
                paths = paths with { Project = paths.Project with { Source = pinnedSource } };
            }
            da.SetData(0, new PathsGoo(paths)); da.SetData(1, paths.Project.Path);
            da.SetData(2, paths.Library?.Path);
            da.SetData(statusIndex, $"{paths.Project.Source}: {paths.Project.Path}. Location resolved; write access unverified.");
        }
        else
        {
            da.SetData(statusIndex, Result is null ? Status : string.Join("\n", Result.Errors));
            if (Result is not null) SetRevisionMessage("Not ready");
        }
    }
    private static string? Optional(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;
    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalComponentMenuItems(menu);
        Menu_AppendItem(menu, "Reset automatic project location", (_, _) => { RecordUndoEvent("Reset project location"); pinnedRoot = null; pinnedId = null; reset = true; ExpireSolution(true); });
    }
    public override bool Write(GH_IWriter writer)
    {
        writer.SetGuid("DraftId", draftId);
        if (pinnedRoot is not null) { writer.SetString("PinnedRoot", pinnedRoot); writer.SetInt32("PinnedSource", (int)pinnedSource); }
        if (pinnedId is not null) writer.SetGuid("PinnedId", pinnedId.Value);
        return base.Write(writer);
    }
    public override bool Read(GH_IReader reader)
    {
        Invalidate(); refreshLatch.Disarm(); pinnedRoot = null; pinnedId = null;
        if (reader.ItemExists("DraftId")) draftId = reader.GetGuid("DraftId");
        if (reader.ItemExists("PinnedRoot")) { pinnedRoot = reader.GetString("PinnedRoot"); pinnedSource = (PathSource)reader.GetInt32("PinnedSource"); }
        if (reader.ItemExists("PinnedId")) pinnedId = reader.GetGuid("PinnedId");
        return base.Read(reader);
    }
}
