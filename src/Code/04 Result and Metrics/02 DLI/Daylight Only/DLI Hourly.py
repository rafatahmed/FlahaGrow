ghenv.Component.Message = """flahagrow 0.1 Beta
DLI Hourly"""

import os, json, mmap, struct, System
from Grasshopper import DataTree
from Grasshopper.Kernel.Data import GH_Path
from Grasshopper.Kernel import GH_RuntimeMessageLevel as RML

_DLI = []
_hourly_DLI = DataTree[System.Object]()


def error(msg):
    ghenv.Component.AddRuntimeMessage(RML.Error, str(msg))

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
        raise ValueError("Invalid dims in meta: sensors=%r hours=%r" % (S,H))
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

def hourly_dli_from_lux(lux24, k):
    hourly = []
    total = 0.0
    for v in lux24:
        fv = float(v)
        ppfd = fv * k
        mol  = ppfd * 3600.0 / 1e6
        hourly.append(mol)
        total += mol
    return hourly, total



try:
    if not (isinstance(_result, str) and os.path.isfile(_result)):
        error("Provide _result as a valid path to the .f32 cache file.")
    elif _hour_index is None:
        error("Provide _hour_index (integer hour number).")
    else:
        cache_path = _result
        meta_path  = meta_path_for(cache_path)
        if not os.path.isfile(meta_path):
            error("Meta file not found beside cache: %s" % meta_path)
        else:
            S, H = read_meta(meta_path)
            k = parse_factor(_conversion_factor)
            h = max(0, int(_hour_index))

            f, mm = open_memmap_f32_le(cache_path)
            try:
                for sidx in range(S):
                    path = GH_Path(sidx)
                    lux_vals = read_sensor_col(mm, S, H, sidx)

                    L = len(lux_vals)
                    if L == 0:
                        _DLI.append(None)
                        continue


                    days_available = max(1, L // 24) if L >= 24 else 1
                    day_idx = min(h // 24, days_available - 1)
                    start = day_idx * 24
                    end   = min(start + 24, L)
                    lux24 = lux_vals[start:end]


                    hourly, total = hourly_dli_from_lux(lux24, k)


                    if len(hourly) < 24:
                        hourly += [None] * (24 - len(hourly))


                    for val in hourly:
                        _hourly_DLI.Add(val, path)
                    _DLI.append(total)

            finally:
                mm.close(); f.close()

except Exception as e:
    _DLI = []
    _hourly_DLI = DataTree[System.Object]()
    error("Unhandled error: %s" % e)
