using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Geometry;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>
/// Exact C# port of <c>DLI Hourly.py</c>. Reads one 24-hour day, selected from an
/// annual illuminance cache by hour index, for every sensor.
/// </summary>
public sealed class DliHourlyComponent : FlahaGrowComponent
{
    public DliHourlyComponent() : base("DLI Hourly", "DLI Hourly", "Returns the selected day's total and hourly DLI for every sensor from an annual result cache.", "FlahaGrow", "DLI") { }
    public override Guid ComponentGuid => new("d4f97934-9fd5-4d9c-a6e0-b550d0c9cedf");

    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Result cache", "Result", "Path to the annual .f32 result cache.", GH_ParamAccess.item);
        p.AddIntegerParameter("Hour index", "Hour", "Integer annual hour number used to select its 24-hour day.", GH_ParamAccess.item);
        p.AddGenericParameter("Conversion factor", "Factor", "Number or legacy preset: electric, sunonly, or skyonly. Default 0.0185.", GH_ParamAccess.item);
        p[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddNumberParameter("DLI", "DLI", "Selected-day total DLI per sensor in mol/m²/day.", GH_ParamAccess.list);
        p.AddNumberParameter("Hourly DLI", "Hourly DLI", "One 24-value branch per sensor in mol/m²/hour.", GH_ParamAccess.tree);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string cache = string.Empty;
        var hourIndex = 0;
        object? factorInput = null;
        if (!da.GetData(0, ref cache) || !da.GetData(1, ref hourIndex)) return;
        da.GetData(2, ref factorInput);
        try
        {
            var factor = AnnualCacheData.Factor(factorInput);
            var day = AnnualCacheData.Day24(cache, hourIndex);
            var totals = new List<double>(day.Sensors);
            var hourlyTree = new GH_Structure<GH_Number>();

            for (var sensor = 0; sensor < day.Sensors; sensor++)
            {
                var path = new GH_Path(sensor);
                var total = 0.0;
                for (var item = 0; item < 24; item++)
                {
                    if (item * day.Sensors + sensor < day.Values.Count)
                    {
                        var dli = day.Values[item * day.Sensors + sensor] * factor * 3600.0 / 1_000_000.0;
                        total += dli;
                        hourlyTree.Append(new GH_Number(dli), path);
                    }
                    else
                    {
                        // Python emits None to retain exactly 24 items for partial days.
                        hourlyTree.Append(null!, path);
                    }
                }
                totals.Add(total);
            }

            da.SetDataList(0, totals);
            da.SetDataTree(1, hourlyTree);
        }
        catch (Exception ex)
        {
            da.SetDataList(0, Array.Empty<double>());
            da.SetDataTree(1, new GH_Structure<GH_Number>());
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }
}

/// <summary>
/// Exact C# port of <c>DLI Each Sensor.py</c>. Returns at most 365 daily DLI
/// values for one zero-based sensor and optionally its upper-hemisphere marker.
/// </summary>
public sealed class DliEachSensorComponent : FlahaGrowComponent
{
    public DliEachSensorComponent() : base("DLI Each Sensor", "DLI Sensor", "Returns daily DLI for one annual-cache sensor and optionally an upper-hemisphere marker.", "FlahaGrow", "DLI") { }
    public override Guid ComponentGuid => new("a77d7b17-274a-444b-af3d-063144dcb3fa");

    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddTextParameter("Result cache", "Result", "Path to the annual .f32 result cache.", GH_ParamAccess.item);
        p.AddIntegerParameter("Sensor index", "Sensor", "Zero-based sensor index.", GH_ParamAccess.item);
        p.AddGenericParameter("Conversion factor", "Factor", "Number or legacy preset: electric, sunonly, or skyonly. Default 0.0185.", GH_ParamAccess.item);
        p[2].Optional = true;
        p.AddGenericParameter("Sensor points", "Pts", "Optional point list or tree; flattened order must match the cache.", GH_ParamAccess.tree);
        p[3].Optional = true;
        p.AddBooleanParameter("Mark", "Mark", "Create the selected sensor's upper-hemisphere marker.", GH_ParamAccess.item, false);
        p.AddNumberParameter("Marker size", "Size", "Marker diameter; the marker radius is half this size.", GH_ParamAccess.item);
        p[5].Optional = true;
        p.AddVectorParameter("Marker up", "Up", "Marker orientation; invalid or absent values use world Z.", GH_ParamAccess.item);
        p[6].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager p)
    {
        p.AddNumberParameter("DLI", "DLI", "Up to 365 daily DLI values in mol/m²/day.", GH_ParamAccess.list);
        p.AddBrepParameter("Marker", "Marker", "Optional upper-hemisphere marker.", GH_ParamAccess.list);
        p.AddPointParameter("Sensor point", "Point", "Selected sensor point when supplied.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess da)
    {
        string cache = string.Empty;
        var sensor = 0;
        object? factorInput = null;
        // A Generic Grasshopper parameter supplies IGH_Goo. GetDataTree performs
        // no type conversion, so the requested tree type must be exactly IGH_Goo.
        var points = new GH_Structure<IGH_Goo>();
        var mark = false;
        var size = 0.0;
        var up = Vector3d.ZAxis;
        if (!da.GetData(0, ref cache) || !da.GetData(1, ref sensor)) return;
        da.GetData(2, ref factorInput);
        da.GetDataTree(3, out points);
        da.GetData(4, ref mark);
        da.GetData(5, ref size);
        da.GetData(6, ref up);
        try
        {
            var lux = AnnualCacheData.Sensor(cache, sensor);
            var factor = AnnualCacheData.Factor(factorInput);
            var days = Math.Min(365, lux.Count / 24);
            var dli = new List<double>(days);
            for (var day = 0; day < days; day++)
            {
                var total = 0.0;
                for (var hour = 0; hour < 24; hour++) total += lux[day * 24 + hour] * factor * 3600.0 / 1_000_000.0;
                dli.Add(total);
            }
            da.SetDataList(0, dli);

            var point = PointAt(points, sensor);
            if (point is null) return;
            da.SetData(2, point.Value);
            if (!mark || size <= 0) return;
            var marker = CreateMarker(point.Value, size, up);
            if (marker is not null) da.SetDataList(1, new[] { marker });
        }
        catch (Exception ex)
        {
            da.SetDataList(0, Array.Empty<double>());
            da.SetDataList(1, Array.Empty<Brep>());
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.Message);
        }
    }

    private static Point3d? PointAt(GH_Structure<IGH_Goo> points, int index)
    {
        if (index < 0 || points.DataCount <= index) return null;
        var item = points.AllData(true).ElementAt(index);
        if (item is null) return null;
        if (item.CastTo(out Point3d point) && point.IsValid) return point;
        return null;
    }

    private static Brep? CreateMarker(Point3d point, double size, Vector3d up)
    {
        if (!up.IsValid || up.IsZero) up = Vector3d.ZAxis;
        up.Unitize();
        var sphere = new Sphere(point, size * 0.5).ToBrep();
        var side = size * 10.0;
        var cutter = new PlaneSurface(new Plane(point, up), new Interval(-side, side), new Interval(-side, side)).ToBrep();
        var parts = sphere.Split(cutter, RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoMath.ZeroTolerance);
        return parts is null || parts.Length < 2 ? null : parts.OrderByDescending(part => Vector3d.Multiply(up, part.GetBoundingBox(true).Center - point)).First();
    }
}
