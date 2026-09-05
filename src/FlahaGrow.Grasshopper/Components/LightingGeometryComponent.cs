using Grasshopper.Kernel;
using Rhino.Geometry;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Generates Radiance xform placement commands for selected luminaires.</summary>
public sealed class LightingGeometryComponent : GH_Component
{
    public LightingGeometryComponent() : base("Lighting Geometry", "Light Geometry", "Generates xform commands that position Radiance luminaire files at points.", "FlahaGrow", "Electric Light") { }
    public override Guid ComponentGuid => new("2bb0d862-d310-4c90-8836-3760fd9870c5");

    protected override void RegisterInputParams(GH_InputParamManager parameters)
    {
        parameters.AddPointParameter("Points", "Pts", "Luminaire placement points.", GH_ParamAccess.list);
        parameters.AddNumberParameter("X rotation", "Rx", "X-axis rotations in degrees; one value broadcasts.", GH_ParamAccess.list, 0.0);
        parameters.AddNumberParameter("Y rotation", "Ry", "Y-axis rotations in degrees; one value broadcasts.", GH_ParamAccess.list, 0.0);
        parameters.AddNumberParameter("Z rotation", "Rz", "Z-axis rotations in degrees; one value broadcasts.", GH_ParamAccess.list, 0.0);
        parameters.AddTextParameter("Radiance files", "Rad", "One .rad path broadcasts, or provide one per point.", GH_ParamAccess.list);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager parameters)
    {
        parameters.AddTextParameter("Lighting geometry", "xform", "Radiance xform placement lines.", GH_ParamAccess.list);
        parameters.AddTextParameter("Status", "Status", "Validation status.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess dataAccess)
    {
        var points = new List<Point3d>(); var rx = new List<double>(); var ry = new List<double>(); var rz = new List<double>(); var files = new List<string>();
        if (!dataAccess.GetDataList(0, points) || points.Count == 0 || !dataAccess.GetDataList(4, files) || files.Count == 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide placement points and Radiance files."); return; }
        dataAccess.GetDataList(1, rx); dataAccess.GetDataList(2, ry); dataAccess.GetDataList(3, rz);
        try
        {
            var count = points.Count;
            var xs = Broadcast(rx, count); var ys = Broadcast(ry, count); var zs = Broadcast(rz, count); var paths = Broadcast(files, count);
            var lines = points.Select((point, index) => Build(point, xs[index], ys[index], zs[index], paths[index])).ToList();
            dataAccess.SetDataList(0, lines);
            dataAccess.SetData(1, $"OK: {lines.Count} xform line(s).");
        }
        catch (Exception exception) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, exception.Message); }
    }

    private static List<T> Broadcast<T>(IReadOnlyList<T> values, int count)
    {
        if (values.Count == 0) return Enumerable.Repeat(default(T)!, count).ToList();
        if (values.Count == 1) return Enumerable.Repeat(values[0], count).ToList();
        if (values.Count != count) throw new ArgumentException($"List length {values.Count} must be one or match point count {count}.");
        return values.ToList();
    }
    private static string Build(Point3d point, double x, double y, double z, string file)
    {
        var parts = new List<string> { "!xform" };
        if (Math.Abs(x) > 1e-12) parts.Add($"-rx {x:0.######}");
        if (Math.Abs(y) > 1e-12) parts.Add($"-ry {y:0.######}");
        if (Math.Abs(z) > 1e-12) parts.Add($"-rz {z:0.######}");
        parts.Add($"-t {point.X:0.######} {point.Y:0.######} {point.Z:0.######}");
        parts.Add($"\"{Path.GetFullPath(file).Replace('\\', '/')}\"");
        return string.Join(" ", parts);
    }
}
