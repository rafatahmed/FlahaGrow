ghenv.Component.Message = """FlahaGrow 0.1 Beta
Sensor Marker"""

import Rhino.Geometry as rg
import scriptcontext as sc
import System

# input
def coerce_point(x):
    if isinstance(x, rg.Point3d): return x
    if hasattr(x, "Location"):    return x.Location
    if isinstance(x, System.Guid):
        obj = sc.doc.Objects.FindId(x)
        if obj and hasattr(obj, "Geometry") and isinstance(obj.Geometry, rg.Point):
            return obj.Geometry.Location
    return None

_marker = None

C = coerce_point(_point)
if C and _grid_size and _grid_size > 0:
    tol = sc.doc.ModelAbsoluteTolerance
    r   = float(_grid_size) * 0.5

    # sphere
    s_brep = rg.Brep.CreateFromSphere(rg.Sphere(C, r))

    # cutting plane
    up = Up if (Up and Up.IsValid and Up.Length > 0) else rg.Vector3d.ZAxis
    up.Unitize()
    pl = rg.Plane(C, up)

    # finite plane
    side = _grid_size * 10.0
    psrf = rg.PlaneSurface(pl, rg.Interval(-side, side), rg.Interval(-side, side))
    pbrep = psrf.ToBrep()

    # split sphere with plane-brep
    parts = s_brep.Split(pbrep, tol)
    if parts and len(parts) >= 2:
        # the piece
        def score(b):
            c = b.GetBoundingBox(True).Center
            v = c - C
            return rg.Vector3d.Multiply(up, rg.Vector3d(v))
        _marker = max(parts, key=score)
