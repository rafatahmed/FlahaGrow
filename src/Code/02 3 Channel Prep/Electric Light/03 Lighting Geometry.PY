ghenv.Component.Message = """FlahaGrow 0.0
Lighting Geometry"""



import os
import Rhino

try:
    from Grasshopper.Kernel.Types import GH_Point, GH_Number, GH_String
except:
    GH_Point = GH_Number = GH_String = None



def flatten_any(x):
    # GH DataTree
    try:
        bc = getattr(x, "BranchCount", None)
        if bc is not None:
            cnt = x.BranchCount if isinstance(bc, int) else x.BranchCount()
            flat = []
            for i in range(cnt):
                flat.extend(list(x.Branch(i)))
            return flat
    except:
        pass
    # list/tuple
    try:
        _ = len(x); return list(x)
    except:
        return [x]

def coerce_point(obj):
    if obj is None: return None
    if isinstance(obj, Rhino.Geometry.Point3d): return obj
    if GH_Point and isinstance(obj, GH_Point):
        try: return obj.Value
        except: pass
    # tuple-like
    try: return Rhino.Geometry.Point3d(float(obj[0]), float(obj[1]), float(obj[2]))
    except: pass
    # object with X/Y/Z
    try: return Rhino.Geometry.Point3d(float(obj.X), float(obj.Y), float(obj.Z))
    except: return None

def coerce_float(obj):
    if obj is None: return None
    if isinstance(obj, (int, float)): return float(obj)
    if GH_Number and isinstance(obj, GH_Number):
        try: return float(obj.Value)
        except: pass
    try: return float(obj)
    except: return None

def coerce_str(obj):
    if obj is None: return None
    if isinstance(obj, str): return obj
    if GH_String and isinstance(obj, GH_String):
        try: return str(obj.Value)
        except: pass
    try: return str(obj)
    except: return None

def ensure_float_list(x, n, default=0.0):
    raw = [r for r in flatten_any(x) if r is not None]
    if len(raw) == 0: return [default]*n
    if len(raw) == 1:
        v = coerce_float(raw[0]); v = default if v is None else v
        return [float(v)]*n
    if len(raw) != n:
        raise ValueError("Rotation list length {} must match number of points {}."
                         .format(len(raw), n))
    out = []
    for r in raw:
        v = coerce_float(r)
        out.append(default if v is None else float(v))
    return out

def ensure_path_list(rad_files_input, n):
    """Return list of N quoted, normalized absolute paths.
       If 1 input, broadcast; if N inputs, use as-is; else error."""
    raw = [coerce_str(s) for s in flatten_any(rad_files_input)]
    raw = [s.strip() for s in raw if s and s.strip() != ""]
    if len(raw) == 0:
        raise ValueError("Provide _rad_files (one path or a list of N paths).")
    paths_src = raw if len(raw) == n else (raw* n if len(raw) == 1 else None)
    if paths_src is None:
        raise ValueError("Number of _rad_files ({}) must be 1 or {} to match points."
                         .format(len(raw), n))
    paths = []
    missing = []
    for r in paths_src:
        p_abs = os.path.abspath(r).replace("\\", "/")
        paths.append('"{}"'.format(p_abs))
        if not os.path.isfile(p_abs):
            missing.append(p_abs)
    return paths, missing

# main
out = ""
_lighting_geometry = []

try:
    # Points
    pts = []
    for item in flatten_any(_points):
        p = coerce_point(item)
        if p is not None: pts.append(p)
    n = len(pts)

    if n == 0:
        out = ("No points detected.\n"
               "• Plug Point(s) into _points (flatten if needed).\n"
               "• Set _rad_files to one path (broadcast) or N paths.")
    else:
        rx = ensure_float_list(_rotationX, n, 0.0)
        ry = ensure_float_list(_rotationY, n, 0.0)
        rz = ensure_float_list(_rotationZ, n, 0.0)


        rad_paths, missing = ensure_path_list(_rad_files, n)

        for i, p in enumerate(pts):
            parts = ["!xform"]
            if abs(rx[i]) > 1e-12: parts.append("-rx {:.6f}".format(rx[i]))
            if abs(ry[i]) > 1e-12: parts.append("-ry {:.6f}".format(ry[i]))
            if abs(rz[i]) > 1e-12: parts.append("-rz {:.6f}".format(rz[i]))
            parts.append("-t {x:.6f} {y:.6f} {z:.6f}".format(x=p.X, y=p.Y, z=p.Z))
            parts.append(rad_paths[i])  # one path per point
            _lighting_geometry.append(" ".join(parts))

        warn = ""
        if missing:
            warn = "WARNING: {} path(s) not found:\n  - {}\n".format(len(missing), "\n  - ".join(missing))

        example = _lighting_geometry[0] if _lighting_geometry else "(none)"
        out = "{}OK: {} line(s). Example → {}".format(warn, len(_lighting_geometry), example)

except Exception as e:
    out = "ERROR: {}".format(e)
