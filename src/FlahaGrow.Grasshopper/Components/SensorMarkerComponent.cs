using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace FlahaGrow.Grasshopper.Components;

/// <summary>Creates the legacy upper-hemisphere marker at a selected annual sensor point.</summary>
public sealed class SensorMarkerComponent : GH_Component
{
    public SensorMarkerComponent() : base("Sensor Marker", "Marker", "Creates an upper-hemisphere marker at a sensor point, scaled from the sensor-grid size.", "FlahaGrow", "Annual") { }
    public override Guid ComponentGuid => new("e0c7494d-bf04-4bd1-a9ed-9184fd2b9b53");
    protected override void RegisterInputParams(GH_InputParamManager p)
    {
        p.AddPointParameter("Sensor point", "Point", "Selected sensor point.", GH_ParamAccess.item);
        p.AddNumberParameter("Grid size", "Size", "Sensor-grid spacing; marker radius is half this value.", GH_ParamAccess.item);
        p.AddVectorParameter("Up", "Up", "Marker orientation. Defaults to world Z.", GH_ParamAccess.item, Vector3d.ZAxis);
        p[2].Optional = true;
    }
    protected override void RegisterOutputParams(GH_OutputParamManager p) => p.AddBrepParameter("Marker", "Marker", "Upper-hemisphere sensor marker.", GH_ParamAccess.item);
    protected override void SolveInstance(IGH_DataAccess da)
    {
        var point = Point3d.Unset; var gridSize = 0.0; var up = Vector3d.ZAxis;
        if (!da.GetData(0, ref point) || !da.GetData(1, ref gridSize)) return;
        da.GetData(2, ref up);
        if (!point.IsValid || gridSize <= 0) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Provide a valid sensor point and a grid size greater than zero."); return; }
        if (!up.IsValid || up.IsZero) up = Vector3d.ZAxis;
        up.Unitize();
        var sphere = new Sphere(point, gridSize * 0.5).ToBrep();
        var side = gridSize * 10.0;
        var cutter = new PlaneSurface(new Plane(point, up), new Interval(-side, side), new Interval(-side, side)).ToBrep();
        var parts = sphere.Split(cutter, RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? RhinoMath.ZeroTolerance);
        if (parts is null || parts.Length < 2) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not split the marker sphere into an upper hemisphere."); return; }
        var marker = parts.OrderByDescending(part => Vector3d.Multiply(up, part.GetBoundingBox(true).Center - point)).First();
        da.SetData(0, marker);
    }
}
