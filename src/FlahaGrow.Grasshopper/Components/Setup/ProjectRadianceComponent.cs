using FlahaGrow.Core.Operations;
using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Radiance;
using FlahaGrow.Grasshopper.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components.Setup;

public sealed record RadianceCheckResult(RadianceStatus Status, RadianceDiscoveryResult Discovery);
public sealed class ProjectRadianceComponent : AsyncSetupComponent<RadianceCheckResult>
{
    private readonly ActionLatch checkLatch = new();
    private readonly RadianceStatusService service = new();
    public ProjectRadianceComponent() : base("Radiance Status (Project)", "Project Radiance", "Checks a consistent Radiance installation, required workflow tools, and version on explicit Check.") { }
    public override Guid ComponentGuid => new("59892a1c-7e97-46d5-989c-2283c9476ea4");
    public override GH_Exposure Exposure => GH_Exposure.hidden;
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddParameter(new ProjectParameter(), "Project Context", "Project", "Optional project configuration.", GH_ParamAccess.item); p[0].Optional = true;
        p.AddTextParameter("Radiance location", "Location", "Optional explicit installation/bin override.", GH_ParamAccess.item); p[1].Optional = true;
        p.AddIntegerParameter("Workflow", "Workflow", "0 Annual daylight, 1 Electric-light preparation.", GH_ParamAccess.item, 0);
        p.AddBooleanParameter("Check", "Check", "Button: check files and run version probe. Releasing preserves results.", GH_ParamAccess.item, false);
        p.AddTextParameter("Calculation library", "Lib", "Optional explicit Radiance calculation library.", GH_ParamAccess.item); p[4].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddParameter(new RadianceParameter(), "Radiance Environment", "Radiance", "Checked installation descriptor.", GH_ParamAccess.item);
        p.AddBooleanParameter("Ready", "Ready", "Listed requirements and version check passed; not simulation validation.", GH_ParamAccess.item);
        p.AddTextParameter("Version", "Version", "Version text.", GH_ParamAccess.item);
        p.AddTextParameter("Bin folder", "Bin", "Selected executable folder.", GH_ParamAccess.item);
        p.AddTextParameter("Library folder", "Lib", "Selected calculation library.", GH_ParamAccess.item);
        p.AddTextParameter("Candidates", "Candidates", "Located installation folders in selection order.", GH_ParamAccess.list);
        p.AddTextParameter("Status", "Status", "Readiness diagnostics.", GH_ParamAccess.item);
    }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var project = new ProjectGoo(); string location = "", library = ""; var workflow = 0; var check = false;
        da.GetData(0, ref project); da.GetData(1, ref location); da.GetData(2, ref workflow); da.GetData(3, ref check); da.GetData(4, ref library);
        var fire = checkLatch.Observe(check);
        var key = Key(new { Project = project.Value, location, library, workflow });
        Update(key, fire, async token =>
        {
            var root = project.IsValid ? project.Value.Root.Path : null;
            string? Resolve(string? path) => string.IsNullOrWhiteSpace(path) ? null : ProjectLayout.Absolute(path, root);
            var request = RadianceRequest.FromSystem() with
            {
                ExplicitLocation = Resolve(location), ProjectRadianceLocation = Resolve(project.Value?.Manifest.RadianceLocation),
                LibraryOverride = Resolve(library), Workflow = (AnalysisWorkflow)workflow
            };
            var status = await service.CheckAsync(request, refresh: true, cancellationToken: token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return new(status, new RadianceDiscovery().Discover(request));
        }, "Not checked. Press Check.");
        da.SetData(1, Result?.Status.Ready ?? false);
        if (Result is { } result)
        {
            da.SetData(0, new RadianceGoo(result.Status)); da.SetData(2, result.Status.Version);
            da.SetData(3, result.Status.Installation?.BinFolder); da.SetData(4, result.Status.Installation?.LibraryFolder);
            da.SetDataList(5, result.Discovery.Candidates.Select(candidate => candidate.BinFolder));
            da.SetData(6, result.Status.State + ": " + string.Join("; ", result.Status.Diagnostics));
            Message = result.Status.State.ToString();
        }
        else da.SetData(6, Status);
    }
    public override bool Read(GH_IReader reader) { Invalidate(); checkLatch.Disarm(); return base.Read(reader); }
}
