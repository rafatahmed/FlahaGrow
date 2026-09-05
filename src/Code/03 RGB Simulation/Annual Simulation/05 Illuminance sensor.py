ghenv.Component.Message = """FlahaGrow 0.1 Beta
Illuminance"""


import os, json
from array import array



result = []
nrows = ncols = 0
meta = {}
status = ""

def meta_path_for(cache_path):
    base, ext = os.path.splitext(cache_path)
    return base + ".meta.json"

def read_meta(path):
    with open(path, "r") as f:
        m = json.load(f)
    sensors = int(m.get("sensors", 0))
    hours   = int(m.get("hours", 0))
    ncomp   = int(m.get("ncomp", 1))
    return m, sensors, hours, ncomp

def load_f32(cache_path, expected_count):
    a = array('f')
    with open(cache_path, "rb") as f:
        a.fromfile(f, os.path.getsize(cache_path) // 4)
    if expected_count is not None and len(a) != expected_count:
        return a, "Size mismatch: got %d floats, expected %d" % (len(a), expected_count)
    return a, ""



if _run:
    if not _result or not isinstance(_result, str) or not os.path.isfile(_result):
        status = "Provide _result = path to .f32 cache"
    else:
        meta_path = meta_path_for(_result)
        if not os.path.isfile(meta_path):
            status = "Meta not found beside cache: %s" % meta_path
        else:
            try:
                meta, sensors, hours, ncomp = read_meta(meta_path)
                if sensors <= 0 or hours <= 0:
                    raise ValueError("Invalid dims in meta: sensors=%s, hours=%s" % (sensors, hours))
                if ncomp != 1:
                    raise ValueError("ncomp=%d not supported (expect illuminance single-component)" % ncomp)
                nrows, ncols = hours, sensors

                # load data
                expected = sensors * hours
                data, warn = load_f32(_result, expected)
                if not data:
                    raise ValueError("Failed to read cache")
                # choose mode
                mode = (_mode or "sensor").strip().lower() if isinstance(_mode, str) else "sensor"
                if mode not in ("sensor", "hour"):
                    mode = "sensor"

                idx = int(_index) if _index is not None else -1
                if mode == "sensor":
                    if idx < 0 or idx >= sensors:
                        raise IndexError("Sensor index out of range [0..%d]" % (sensors-1))
                    result = list(data[idx::sensors])[:hours]
                    status = "OK: sensor %d → %d hour values%s" % (idx, len(result), (" | " + warn) if warn else "")
                else:
                    if idx < 0 or idx >= hours:
                        raise IndexError("Hour index out of range [0..%d]" % (hours-1))
                    start = idx * sensors
                    end   = start + sensors
                    result = list(data[start:end])
                    if len(result) != sensors:
                        raise ValueError("Row width mismatch at hour %d" % idx)
                    status = "OK: hour %d → %d sensor values%s" % (idx, len(result), (" | " + warn) if warn else "")

            except Exception as e:
                status = "Error: %r" % e
