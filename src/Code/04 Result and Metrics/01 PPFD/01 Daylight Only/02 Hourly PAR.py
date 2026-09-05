ghenv.Component.Message = """FlahaGrow 0.1 Beta
Hourly PAR"""


import os, json, mmap, struct
import Rhino.Geometry as rg

_PAR = []


def is_tree(x):
    return hasattr(x, "BranchCount") and hasattr(x, "Branch") and hasattr(x, "Path")

def to_float(x):
    try: return float(x)
    except:
        try: return float(str(x))
        except: return None

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

def lux_to_par(lux, factor):
    return lux * factor 

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
        raise ValueError("ncomp=%r not supported (expect illuminance single component)" % C)
    return S, H

def open_memmap_f32_le(path):
    f = open(path, "rb")
    mm = mmap.mmap(f.fileno(), 0, access=mmap.ACCESS_READ)
    return f, mm

def read_hour_row(mm, sensors, hours, h):
    if not (0 <= h < hours):
        raise IndexError("hour_index %d out of range [0..%d]" % (h, hours-1))

    start = h * sensors * 4
    end   = start + sensors * 4
    bs = mm[start:end]
    return list(struct.unpack("<%df" % sensors, bs))


_PAR = []
if isinstance(_result, str) and os.path.isfile(_result) and _hour_index is not None:
    try:
        cache_path = _result
        meta_path  = meta_path_for(cache_path)
        if not os.path.isfile(meta_path):
            raise IOError("Meta file not found: %s" % meta_path)

        S, H = read_meta(meta_path)


        f, mm = open_memmap_f32_le(cache_path)
        try:
            h = int(_hour_index)
            lux_row = read_hour_row(mm, S, H, h)
        finally:
            mm.close(); f.close()


        k = parse_factor(_conversion_factor)
        _PAR = [lux_to_par(v, k) for v in lux_row]

    except Exception as e:
        _PAR = []

else:
    _PAR = []
