ghenv.Component.Message = """flahagrow 0.1 Beta
DLI Each Sensor"""

import os, json, mmap, struct
import Rhino.Geometry as rg
import scriptcontext as sc
import System
from Grasshopper.Kernel import GH_RuntimeMessageLevel as RML

_DLI, _marker = [], []
_sensor_pt = None


def is_tree(x):
    return hasattr(x, "BranchCount") and hasattr(x, "Branch") and hasattr(x, "Path")

def flat_items(T):
    L = []
    for bi in range(T.BranchCount):
        br = T.Branch(T.Path(bi))
        for it in br: L.append(it)
    return L

def coerce_point(x):
    if isinstance(x, rg.Point3d): return x
    if hasattr(x, "Location"):    return x.Location
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

def dli_from_par_day(par24):
    """par24: list of 24 PPFD values (µmol·s⁻¹·m⁻²) → DLI (mol·m⁻²·day⁻¹)"""
    s = 0.0
    for v in par24:
        if v is not None:
            s += v * 3600.0 / 1e6
    return s


def meta_path_for(cache_path):
    base, _ = os.path.splitext(cache_path)
    return base + ".meta.json"

def read_meta(meta_path):
    with open(meta_path, "r") as f:
        m = json.load(f)
    S = int(m.get("sensors", 0))
    H = int(m.get("hours", 0))
    C = int(m.get("ncomp", 1))
    if S <= 0 or H <= 0:
        raise ValueError("Invalid dims in meta: sensors=%r hours=%r" % (S, H))
    if C != 1:
        raise ValueError("ncomp=%r not supported (expect 1 component illuminance)" % C)
    return S, H

def open_memmap_f32_le(path):
    f = open(path, "rb")
    mm = mmap.mmap(f.fileno(), 0, access=mmap.ACCESS_READ)
    return f, mm

def read_sensor_col(mm, sensors, hours, s):
    if not (0 <= s < sensors):
        raise IndexError("sensor_index %d out of range [0..%d]" % (s, sensors-1))
    out = []
    stride = sensors * 4
    offset = s * 4
    unpack_from = struct.unpack_from
    for h in range(hours):
        pos = h * stride + offset
        out.append(unpack_from("<f", mm, pos)[0])
    return out


_DLI, _marker, _sensor_pt = [], [], None

def error(msg):
    ghenv.Component.AddRuntimeMessage(RML.Error, str(msg))

try:
    if not (isinstance(_result, str) and os.path.isfile(_result)):
        error("Provide _result as a valid path to the .f32 cache file.")
    elif _sensor_index is None:
        error("Provide _sensor_index (0-based).")
    else:
        cache_path = _result
        meta_path  = meta_path_for(cache_path)
        if not os.path.isfile(meta_path):
            error("Meta file not found beside cache: %s" % meta_path)
        else:
            S, H = read_meta(meta_path)


            f, mm = open_memmap_f32_le(cache_path)
            try:
                sidx = int(_sensor_index)
                lux_vals = read_sensor_col(mm, S, H, sidx)
            finally:
                mm.close(); f.close()


            k = parse_factor(_conversion_factor)
            par_vals = [v * k for v in lux_vals]


            days = min(365, len(par_vals) // 24)
            _DLI = []
            for d in range(days):
                day_slice = par_vals[d*24:(d+1)*24]
                _DLI.append(dli_from_par_day(day_slice))


            if _sensor_pts is not None:
                if is_tree(_sensor_pts):
                    pts_flat = flat_items(_sensor_pts)
                    if len(pts_flat) > sidx:
                        _sensor_pt = coerce_point(pts_flat[sidx])
                elif isinstance(_sensor_pts, (list, tuple)) and len(_sensor_pts) > sidx:
                    _sensor_pt = coerce_point(_sensor_pts[sidx])


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

except Exception as e:
    _DLI, _marker, _sensor_pt = [], [], None
    error("Unhandled error: %s" % e)
