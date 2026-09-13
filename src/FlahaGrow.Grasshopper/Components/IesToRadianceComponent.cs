using System.Text.RegularExpressions;
using FlahaGrow.Core.Projects;
using FlahaGrow.Core.Operations;
using FlahaGrow.Grasshopper.Components.Setup;
using FlahaGrow.Core.Radiance;
using FlahaGrow.Grasshopper.Parameters;
using Grasshopper.Kernel;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Runs ies2rad and applies the legacy three-channel RGB normalization.</summary>
public sealed class IesToRadianceComponent : AsyncSetupComponent<IesConversionResult>
{
    private readonly ActionLatch runLatch = new();
    public bool IsConverting => Status == "Working…";

    public IesToRadianceComponent() : base("IES to Radiance", "IES→Rad", "Converts an IES luminaire to Radiance files and applies normalized RGB channels.", "04 Electric Light") { }
    public override Guid ComponentGuid => new("e64e15f4-7cee-48b2-a232-2064d3a9e602");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddTextParameter("IES path", "IES", "IES file path.", GH_ParamAccess.item);
        parameters.AddTextParameter("Luminaire name", "Name", "Output luminaire name. Blank uses the IES filename.", GH_ParamAccess.item); parameters[1].Optional = true;
        parameters.AddNumberParameter("Red", "R", "Red channel multiplier.", GH_ParamAccess.item, 1.0);
        parameters.AddNumberParameter("Green", "G", "Green channel multiplier.", GH_ParamAccess.item, 1.0);
        parameters.AddNumberParameter("Blue", "B", "Blue channel multiplier.", GH_ParamAccess.item, 1.0);
        parameters.AddNumberParameter("Multiplier", "M", "Optional finite positive ies2rad multiplier; leave unwired to use the converter default.", GH_ParamAccess.item); parameters[5].Optional = true;
        parameters.AddTextParameter("Project folder", "Project", "Simulation project folder; Luminaire_files is created inside it.", GH_ParamAccess.item);
        parameters.AddTextParameter("DAT file", "DAT", "Optional replacement data-file path.", GH_ParamAccess.item); parameters[7].Optional = true;
        parameters.AddBooleanParameter("Run", "Run", "Connect a Button. False to True starts background conversion once; timeout is two minutes. Changed inputs or closing the document cancel pending work.", GH_ParamAccess.item, false);
        parameters.AddTextParameter("Radiance bin folder", "Bin", "Optional folder containing ies2rad.exe. Leave empty for automatic detection.", GH_ParamAccess.item); parameters[9].Optional = true;
        parameters.AddParameter(new RadianceParameter(), "Radiance Environment", "Radiance", "Optional checked Radiance Status environment. When connected, it must be ready for electric-light preparation and determines the exact executable environment.", GH_ParamAccess.item); parameters[10].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddTextParameter("Radiance files", "Rad", "Generated .rad paths.", GH_ParamAccess.list);
        parameters.AddTextParameter("Data files", "DAT", "Generated .dat paths.", GH_ParamAccess.list);
        parameters.AddTextParameter("Log", "Log", "Command and conversion log.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        string ies = string.Empty, name = string.Empty, project = string.Empty, dat = string.Empty, radianceBin = string.Empty;
        var radiance = new RadianceGoo();
        double r = 1, g = 1, b = 1, multiplier = 0; var run = false;
        if (!dataAccess.GetData(0, ref ies) || !dataAccess.GetData(6, ref project)) { Invalidate(); return; }
        dataAccess.GetData(1, ref name); dataAccess.GetData(2, ref r); dataAccess.GetData(3, ref g); dataAccess.GetData(4, ref b);
        var hasMultiplier = dataAccess.GetData(5, ref multiplier);
        dataAccess.GetData(7, ref dat); dataAccess.GetData(8, ref run); dataAccess.GetData(9, ref radianceBin); dataAccess.GetData(10, ref radiance);
        try
        {
            var rgb = LuminaireOutput.Normalize(r, g, b);
            if (hasMultiplier && (!double.IsFinite(multiplier) || multiplier <= 0)) throw new ArgumentException("Multiplier must be finite and positive when supplied.");
            ies = Path.GetFullPath(ies);
            if (!File.Exists(ies)) throw new FileNotFoundException("IES file was not found.", ies);
            if (!string.IsNullOrWhiteSpace(dat) && !File.Exists(dat)) throw new FileNotFoundException("DAT override was not found.", dat);
            project = ProjectLayout.Absolute(project);
            if (!radiance.IsValid && Params.Input[10].SourceCount > 0) throw new InvalidOperationException("Connected Radiance environment is unresolved.");
            var verified = radiance.IsValid ? RadianceExecutionEnvironment.Require(radiance.Value, AnalysisWorkflow.ElectricLighting, radianceBin) : null;
            var stem = SanitizeStem(string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(ies) : name, "luminaire");
            var executable = verified is null ? FindIes2Rad(radianceBin) : verified.Executables.GetValueOrDefault("ies2rad");
            if (executable is null)
            {
                Invalidate(); runLatch.Observe(run);
                if (run) throw new FileNotFoundException("ies2rad.exe was not found. Provide the Radiance bin folder.");
                dataAccess.SetData(2, "Waiting for Run and a Radiance installation. No files written."); return;
            }
            var environment = verified is null
                ? RadianceStatusService.ChildEnvironment(new RadianceInstallation(Path.GetDirectoryName(executable)!, Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(executable))!, "lib"), new Dictionary<string, string>(), Array.Empty<string>(), "conversion"))
                : RadianceStatusService.ChildEnvironment(verified);
            var request = new IesConversionRequest(ies, LuminairePathResolver.ResolveFolder(project), stem,
                executable, environment, r, g, b, hasMultiplier ? multiplier : null, dat);
            Update(Key(request), runLatch.Observe(run), token => new IesConversionService().ConvertAsync(request, token), "Waiting for Run.");
            if (Result is { } result)
            {
                dataAccess.SetDataList(0, new[] { result.RadianceFile }); dataAccess.SetDataList(1, result.DataFiles);
                dataAccess.SetData(2, $"{result.Process.StandardOutput}\n{result.Process.StandardError}\nNormalized RGB: {rgb.R:0.######}, {rgb.G:0.######}, {rgb.B:0.######}. Output truncated: {result.Process.OutputTruncated}.");
            }
            else
            {
                dataAccess.SetData(2, Status);
                if (!IsConverting && !Status.StartsWith("Waiting", StringComparison.Ordinal)) AddRuntimeMessage(GH_RuntimeMessageLevel.Error, Status);
            }
        }
        catch (Exception exception) { Invalidate(); AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exception.Message); }
    }

    public override bool Read(GH_IO.Serialization.GH_IReader reader) { Invalidate(); runLatch.Disarm(); return base.Read(reader); }

    private static string? FindIes2Rad(string binFolder)
    {
        var resolved = new RadianceDiscovery().FindBin(RadianceRequest.FromSystem() with
        {
            ExplicitLocation = string.IsNullOrWhiteSpace(binFolder) ? null : binFolder,
            Workflow = AnalysisWorkflow.ElectricLighting
        }, "ies2rad");
        return resolved is null ? null : Path.Combine(resolved, "ies2rad.exe");
    }
    private static string SanitizeStem(string name, string fallback)
    {
        var stem = Regex.Replace(name, @"[^A-Za-z0-9._-]+", "_").Trim('_', '.');
        return string.IsNullOrWhiteSpace(stem) ? fallback : stem;
    }
}
