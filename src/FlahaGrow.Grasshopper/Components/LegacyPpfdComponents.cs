using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Legacy-compatible cache-native point-in-time PPFD reader.</summary>
public sealed class HourlyParComponent : GH_Component
{
    public HourlyParComponent() : base("Hourly PAR", "Hourly PAR", "Reads one annual-cache hour and converts every sensor value from lux to PPFD.", "FlahaGrow", "PPFD") { }
    public override Guid ComponentGuid => new("3bb97076-25e8-4623-84fa-1245717b5a58");
    protected override void RegisterInputParams(GH_InputParamManager p) { p.AddTextParameter("Result cache", "F32", "Annual .f32 cache.", GH_ParamAccess.item); p.AddIntegerParameter("Hour index", "Hour", "0-based annual hour index.", GH_ParamAccess.item); p.AddGenericParameter("Conversion factor", "Factor", "Number or legacy preset: electric, sunonly, or skyonly.", GH_ParamAccess.item); p[2].Optional = true; }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("PPFD", "PAR", "Per-sensor PPFD at the selected hour in μmol/m²/s.", GH_ParamAccess.list); p.AddTextParameter("Status", "Status", "Read and conversion status.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string cache = string.Empty; var hour = 0; object? factorInput = null; if (!da.GetData(0, ref cache) || !da.GetData(1, ref hour)) return; da.GetData(2, ref factorInput);
        try { var factor = AnnualCacheData.Factor(factorInput); var ppfd = AnnualCacheData.Hour(cache, hour).Select(value => value * factor).ToList(); da.SetDataList(0, ppfd); da.SetData(1, $"OK: hour {hour} → {ppfd.Count} PPFD values; factor {factor:0.####}."); }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
}

/// <summary>Legacy-compatible cache-native annual PPFD reader with optional sensor marker.</summary>
public sealed class ParEachSensorComponent : GH_Component
{
    public ParEachSensorComponent() : base("PAR Each Sensor", "Sensor PAR", "Reads one sensor's annual cache column, converts it to PPFD, and can return its marker.", "FlahaGrow", "PPFD") { }
    public override Guid ComponentGuid => new("c1296cd8-151c-46e0-a5a0-0d2e8f54d9f6");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Result cache", "F32", "Annual .f32 cache.", GH_ParamAccess.item);
        p.AddIntegerParameter("Sensor index", "Sensor", "0-based sensor index.", GH_ParamAccess.item);
        p.AddGenericParameter("Conversion factor", "Factor", "Number or legacy preset: electric, sunonly, or skyonly.", GH_ParamAccess.item); p[2].Optional = true;
        p.AddPointParameter("Sensor points", "Pts", "Optional sensor point list; its ordering must match the cache.", GH_ParamAccess.list); p[3].Optional = true;
        p.AddBooleanParameter("Mark", "Mark", "Create a hemisphere marker at the selected sensor.", GH_ParamAccess.item, false);
        p.AddNumberParameter("Marker size", "Size", "Grid size used to scale the marker.", GH_ParamAccess.item); p[5].Optional = true;
        p.AddVectorParameter("Marker up", "Up", "Marker orientation.", GH_ParamAccess.item, Vector3d.ZAxis); p[6].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p) { p.AddNumberParameter("PPFD", "PAR", "8,760 PPFD values in μmol/m²/s.", GH_ParamAccess.list); p.AddPointParameter("Sensor point", "Point", "Selected sensor point when supplied.", GH_ParamAccess.item); p.AddBrepParameter("Marker", "Marker", "Optional upper-hemisphere marker.", GH_ParamAccess.list); p.AddTextParameter("Status", "Status", "Read and conversion status.", GH_ParamAccess.item); }
    protected override void SolveInstance(IGH_DataAccess da)
    {
        string cache = string.Empty; var sensor = 0; object? factorInput = null; var points = new List<Point3d>(); var mark = false; var size = 0.0; var up = Vector3d.ZAxis;
        if (!da.GetData(0, ref cache) || !da.GetData(1, ref sensor)) return; da.GetData(2, ref factorInput); da.GetDataList(3, points); da.GetData(4, ref mark); da.GetData(5, ref size); da.GetData(6, ref up);
        try
        {
            var factor = AnnualCacheData.Factor(factorInput); var ppfd = AnnualCacheData.Sensor(cache, sensor).Select(value => value * factor).ToList(); da.SetDataList(0, ppfd);
            if (points.Count > sensor) { var point = points[sensor]; da.SetData(1, point); if (mark && size > 0) { var marker = CreateMarker(point, size, up); if (marker is not null) da.SetDataList(2, new[] { marker }); } }
            da.SetData(3, $"OK: sensor {sensor} → {ppfd.Count} PPFD values; factor {factor:0.####}.");
        }
        catch (Exception ex) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message); }
    }
    private static Brep? CreateMarker(Point3d point, double size, Vector3d up)
    {
        if (!up.IsValid || up.IsZero) up = Vector3d.ZAxis; up.Unitize(); var sphere = new Sphere(point, size * .5).ToBrep(); var side = size * 10; var cutter = new PlaneSurface(new Plane(point, up), new Interval(-side, side), new Interval(-side, side)).ToBrep();
        var parts = sphere.Split(cutter, RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoMath.ZeroTolerance); return parts is null || parts.Length < 2 ? null : parts.OrderByDescending(part => Vector3d.Multiply(up, part.GetBoundingBox(true).Center - point)).First();
    }
}
