ghenv.Component.Message = """FlahaGrow 0.1 Beta
PAR Each Sensor"""

import os, mmap, struct, json
import Rhino.Geometry as rg
import scriptcontext as sc
import System
from Grasshopper.Kernel import GH_RuntimeMessageLevel as RML



def msg_error(t):   ghenv.Component.AddRuntimeMessage(RML.Error,   str(t))
def msg_warn(t):    ghenv.Component.AddRuntimeMessage(RML.Warning, str(t))


# Tree helpers
def is_tree(x):
    return hasattr(x, "BranchCount") and hasattr(x, "Branch") and hasattr(x, "Path")

def flat_items(T):
    L = []
    for bi in range(T.BranchCount):
        br = T.Branch(T.Path(bi))
        for it in br:
            L.append(it)
    return L



# Point and factor
def coerce_point(x):
    if isinstance(x, rg.Point3d): return x
    if hasattr(x, "Location"): return x.Location
    if isinstance(x, (list, tuple)) and len(x) == 3:
        try: return rg.Point3d(float(x[0]), float(x[1]), float(x[2]))
        except: return None
    if isinstance(x, System.Guid):
        obj = sc.doc.Objects.FindId(x)
        if obj and hasattr(obj, "Geometry") and isinstance(obj.Geometry, rg.Point):
            return obj.Geometry.Location
    return None



def parse_factor(val):
    default = 0.0185
    if isinstance(val, (int, float)): return float(val)
    if val is None: return default
    s = str(val).strip().lower()
    if s in ("electric","elec","electriconly","electric_light","electriclighting"): return 0.015
    if s in ("sunonly","sun","sunlight"): return 0.0205
    if s in ("skyonly","sky"): return 0.0135
    try: return float(s)
    except: return default



# cache reader
def read_column_from_cache(cache_path, sensor_index):
    """Reads one column from a FlahaGrow .f32 cache."""
    meta_path = os.path.splitext(cache_path)[0] + ".meta.json"
    if not os.path.isfile(meta_path):
        raise FileNotFoundError("Missing metadata JSON: " + meta_path)

    meta = json.load(open(meta_path, "r"))
    sensors = int(meta.get("sensors", 0))
    hours   = int(meta.get("hours",   0))
    if sensors <= 0 or hours <= 0:
        raise ValueError("Invalid metadata dimensions")

    j = int(sensor_index or 0)
    if not (0 <= j < sensors):
        raise IndexError("sensor_index %d out of range (0..%d)" % (j, sensors-1))

    vals = []
    with open(cache_path, "rb") as f:
        mm = mmap.mmap(f.fileno(), 0, access=mmap.ACCESS_READ)
        stride = sensors * 4
        for h in range(hours):
            off = h * stride + j * 4
            vals.append(struct.unpack_from("<f", mm, off)[0])
        mm.close()
    return vals



# Initialize outputs
_PAR, _marker = [], []
_sensor_pt = None

try:
    # Determine cache path
    cp = _result if '_result' in globals() and _result else None
    if not cp:
        raise FileNotFoundError("Provide a valid _result (.f32).")

    cp = "".join(str(cp).splitlines()).strip()
    if not os.path.isfile(cp):
        raise FileNotFoundError("File not found: " + cp)

    idx = int(_sensor_index)
    factor = parse_factor(_conversion_factor)

    lux_vals = read_column_from_cache(cp, idx)
    _PAR = [v * factor for v in lux_vals]



except Exception as e:
    msg_error(e)
    _PAR = []


# Optional: sensor point and hemisphere marker
if _sensor_pts is not None:
    if is_tree(_sensor_pts):
        pts_flat = flat_items(_sensor_pts)
        if len(pts_flat) > _sensor_index:
            _sensor_pt = coerce_point(pts_flat[_sensor_index])
    elif isinstance(_sensor_pts, (list, tuple)) and len(_sensor_pts) > _sensor_index:
        _sensor_pt = coerce_point(_sensor_pts[_sensor_index])



if _mark and (_sensor_pt is not None) and (_marker_size is not None) and (_marker_size > 0):
    r = float(_marker_size) * 0.5
    sphere_brep = rg.Brep.CreateFromSphere(rg.Sphere(_sensor_pt, r))
    up = _marker_up if (_marker_up and _marker_up.IsValid and _marker_up.Length > 0) else rg.Vector3d.ZAxis
    up.Unitize()
    pl = rg.Plane(_sensor_pt, up)
    side = _marker_size * 10.0
    psrf = rg.PlaneSurface(pl, rg.Interval(-side, side), rg.Interval(-side, side))
    pbrep = psrf.ToBrep()
    tol = sc.doc.ModelAbsoluteTolerance if sc.doc else 1e-6
    parts = sphere_brep.Split(pbrep, tol)
    if parts and len(parts) >= 2:
        def score(b):
            c = b.GetBoundingBox(True).Center
            return rg.Vector3d(c - _sensor_pt) * up
        _marker = [max(parts, key=score)]
else:
    _marker = []
