using System.Windows.Forms;
using FlahaGrow.Core.Operations;
using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Radiance;
using FlahaGrow.Grasshopper.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components.Setup;

/// <summary>Automatically checks the engine once per configuration; overrides live in the component menu.</summary>
public sealed class RadianceSetupComponent : AsyncSetupComponent<RadianceStatus>
{
    private readonly ActionLatch refreshLatch = new();
    private readonly RadianceStatusService service = new();
    private string? selectedLocation;
    private string? libraryOverride;
    private AnalysisWorkflow standaloneWorkflow = AnalysisWorkflow.AnnualDaylight;
    private bool analysisControlsWorkflow;
    private bool menuRefresh;

    public RadianceSetupComponent() : base("Radiance Status", "Radiance", "Automatically locates Radiance and checks its version and requirements. Standalone Radiance is preferred; Ladybug Tools is supported. Right-click for advanced overrides.") { }
    public override Guid ComponentGuid => new("9cd39fc4-7c35-4aee-a247-1c8b980c4b17");

    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddParameter(new AnalysisParameter(), "Analysis", "Analysis", "Optional Working Directory analysis. Its workflow determines required Radiance tools. Leave empty to check independently.", GH_ParamAccess.item); p[0].Optional = true;
        p.AddBooleanParameter("Refresh", "Refresh", "Optional Button to repeat discovery and the version check. The initial check is automatic.", GH_ParamAccess.item, false);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddParameter(new RadianceParameter(), "Radiance Environment", "Radiance", "Selected installation, workflow, and check result.", GH_ParamAccess.item);
        p.AddBooleanParameter("Ready", "Ready", "Required files, version probe, and small workflow execution check passed. Full simulation has not been tested.", GH_ParamAccess.item);
        p.AddTextParameter("Version", "Version", "Version reported by the selected installation.", GH_ParamAccess.item);
        p.AddTextParameter("Bin folder", "Bin", "Automatically resolved executable folder.", GH_ParamAccess.item);
        p.AddTextParameter("Library folder", "Lib", "Resolved calculation-library folder.", GH_ParamAccess.item);
        p.AddTextParameter("Installations", "Found", "Discovered installations and missing requirements; right-click to choose an alternative.", GH_ParamAccess.list);
        p.AddTextParameter("Status", "Status", "Selection source, workflow, and diagnostics.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        var analysis = new AnalysisGoo(); var refresh = false;
        da.GetData(0, ref analysis); da.GetData(1, ref refresh);
        var force = refreshLatch.Observe(refresh) || menuRefresh; menuRefresh = false;
        analysisControlsWorkflow = analysis.IsValid;
        if (!analysis.IsValid && Params.Input[0].SourceCount > 0)
        {
            Invalidate(); da.SetData(1, false); da.SetData(6, "Waiting for the connected analysis. Initialize or open it in Working Directory."); return;
        }
        var workflow = analysis.IsValid ? analysis.Value.AnalysisManifest.Workflow : standaloneWorkflow;
        var root = analysis.IsValid ? analysis.Value.Project.Root.Path : null;
        var customLocation = selectedLocation;
        var customLibrary = libraryOverride;
        var key = Key(new { root, workflow, customLocation, customLibrary });
        Update(key, force || Changed(key), async token =>
        {
            string? Resolve(string? path) => path is null ? null : ProjectLayout.Absolute(path, root);
            var request = RadianceRequest.FromSystem() with
            {
                Workflow = workflow, ExplicitLocation = Resolve(customLocation), LibraryOverride = Resolve(customLibrary)
            };
            return await service.CheckAsync(request, refresh: force, cancellationToken: token).ConfigureAwait(false);
        }, "Detecting Radiance.");
        da.SetData(1, Result?.Ready ?? false);
        if (Result is not { } result) { da.SetData(6, Status); return; }
        da.SetData(0, new RadianceGoo(result)); da.SetData(2, result.Version);
        da.SetData(3, result.Installation?.BinFolder); da.SetData(4, result.Installation?.LibraryFolder);
        da.SetDataList(5, result.Candidates.Select(candidate => $"{candidate.Source}: {candidate.BinFolder}" +
            (candidate.Missing.Count == 0 ? " (required files present)" : $" (missing {string.Join(", ", candidate.Missing)})")));
        var selection = result.Installation is null ? "No installation selected" : $"{result.Installation.Source}: {result.Installation.BinFolder}";
        var workflowName = workflow == AnalysisWorkflow.AnnualDaylight ? "Annual daylight" : "Electric-light preparation";
        da.SetData(6, $"{result.State} — {workflowName}. {selection}.\n{string.Join("\n", result.Diagnostics)}");
        Message = result.State.ToString();
    }

    protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
    {
        base.AppendAdditionalComponentMenuItems(menu);
        Menu_AppendSeparator(menu);
        Menu_AppendItem(menu, "Automatic detection (prefer standalone)", (_, _) =>
            Change("Use automatic Radiance", () => { selectedLocation = null; libraryOverride = null; }), true, selectedLocation is null && libraryOverride is null);
        Menu_AppendItem(menu, "Refresh Radiance check", (_, _) => { menuRefresh = true; ExpireSolution(true); });
        foreach (var candidate in Result?.Candidates ?? Array.Empty<RadianceInstallation>())
        {
            var location = candidate.BinFolder;
            Menu_AppendItem(menu, $"Use {candidate.Source}: {location}", (_, _) =>
                Change("Select Radiance installation", () => { selectedLocation = location; libraryOverride = null; }), true,
                string.Equals(selectedLocation, location, StringComparison.OrdinalIgnoreCase));
        }
        Menu_AppendItem(menu, "Choose custom Radiance folder…", (_, _) => Browse(false));
        Menu_AppendItem(menu, "Choose custom calculation library…", (_, _) => Browse(true));
        Menu_AppendSeparator(menu);
        Menu_AppendItem(menu, "Independent check: annual daylight", (_, _) => Change("Set check workflow", () => standaloneWorkflow = AnalysisWorkflow.AnnualDaylight), !analysisControlsWorkflow, standaloneWorkflow == AnalysisWorkflow.AnnualDaylight);
        Menu_AppendItem(menu, "Independent check: electric-light preparation", (_, _) => Change("Set check workflow", () => standaloneWorkflow = AnalysisWorkflow.ElectricLighting), !analysisControlsWorkflow, standaloneWorkflow == AnalysisWorkflow.ElectricLighting);
    }

    private void Browse(bool library)
    {
        using var dialog = new FolderBrowserDialog { Description = library ? "Select the Radiance calculation library" : "Select the Radiance installation or bin folder", ShowNewFolderButton = false };
        if (dialog.ShowDialog() != DialogResult.OK) return;
        Change("Choose Radiance folder", () => { if (library) libraryOverride = dialog.SelectedPath; else { selectedLocation = dialog.SelectedPath; libraryOverride = null; } });
    }

    private void Change(string description, Action update)
    { RecordUndoEvent(description); update(); menuRefresh = true; ExpireSolution(true); }

    public override bool Write(GH_IWriter writer)
    {
        if (selectedLocation is not null) writer.SetString("SelectedLocation", selectedLocation);
        if (libraryOverride is not null) writer.SetString("LibraryOverride", libraryOverride);
        writer.SetInt32("IndependentWorkflow", (int)standaloneWorkflow);
        return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
        Invalidate(); refreshLatch.Disarm(); menuRefresh = false;
        selectedLocation = reader.ItemExists("SelectedLocation") ? reader.GetString("SelectedLocation") : null;
        libraryOverride = reader.ItemExists("LibraryOverride") ? reader.GetString("LibraryOverride") : null;
        standaloneWorkflow = reader.ItemExists("IndependentWorkflow") ? (AnalysisWorkflow)reader.GetInt32("IndependentWorkflow") : AnalysisWorkflow.AnnualDaylight;
        return base.Read(reader);
    }
}
