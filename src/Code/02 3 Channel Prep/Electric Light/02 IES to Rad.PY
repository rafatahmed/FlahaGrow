ghenv.Component.Message = """FlahaGrow 0.0
IES to Rad"""

import os, shlex, subprocess, glob, re


def fail(msg):
    raise ValueError(msg)


def norm_path(p):
    return os.path.abspath(p).replace("\\", "/")


def replace_all_dat_tokens(lines, dat_full):
    out = []
    for ln in lines:
        if ".dat" not in ln.lower():
            out.append(ln)
            continue

        new_ln = ln
        for q in ("'", '"'):
            i = 0
            while True:
                start = new_ln.find(q, i)
                if start == -1:
                    break
                end = new_ln.find(q, start + 1)
                if end == -1:
                    break
                inner = new_ln[start + 1:end]
                if inner.strip().lower().endswith(".dat"):
                    new_ln = new_ln[:start + 1] + dat_full + new_ln[end:]
                    i = start + 1 + len(dat_full)
                else:
                    i = end + 1

        if ".dat" in new_ln.lower():
            tail_nl = "\n" if new_ln.endswith("\n") else ""
            parts = new_ln.strip().split()
            changed = False
            for j, t in enumerate(parts):
                if t.lower().endswith(".dat"):
                    parts[j] = '"%s"' % dat_full
                    changed = True
            if changed:
                new_ln = " ".join(parts) + tail_nl

        out.append(new_ln)
    return out


def atomic_write_lines(path, lines):
    tmp = path + ".tmp"
    with open(tmp, "w", encoding="utf-8", newline="\n") as f:
        f.writelines(lines)
    os.replace(tmp, path)


def as_float(x, default=None):
    try:
        return float(x)
    except:
        return default


# normalized r, g, b
def normalize_rgb(r, g, b):
    coeff_r = 0.265
    coeff_g = 0.67
    coeff_b = 0.065

    wr = r * coeff_r
    wg = g * coeff_g
    wb = b * coeff_b
    total = wr + wg + wb

    if total <= 0:
        return 1.0, 1.0, 1.0, 1.0, 1.0

    nr = wr / total
    ng = wg / total
    nb = wb / total
    scale = 1.0 / total
    return nr, ng, nb, scale, total



# replace the rgb line
def replace_rgb_line(lines, nr, ng, nb):
    out = []
    replaced = False
    rgb_line_pattern = re.compile(
        r'^\s*3\s+[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?\s+[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?\s+[-+]?\d*\.?\d+(?:[eE][-+]?\d+)?\s*$'
    )

    new_rgb_line = "3 {:.12g} {:.12g} {:.12g}\n".format(nr, ng, nb)

    for ln in lines:
        if (not replaced) and rgb_line_pattern.match(ln):
            out.append(new_rgb_line)
            replaced = True
        else:
            out.append(ln)

    return out, replaced




# Grasshopper inputs
_ies_path   = locals().get("_ies_path", None)
_ies_name   = locals().get("_ies_name", None)
_r          = locals().get("_r", 1.0)
_g          = locals().get("_g", 1.0)
_b          = locals().get("_b", 1.0)
_multiplier = locals().get("_multiplier", None)
_folder     = locals().get("_folder", None)
_datfile    = locals().get("_datfile", None)
_run        = bool(locals().get("_run", False))


if not _ies_path or not os.path.isfile(_ies_path):
    fail("Provide a valid IES file path for '_ies_path'.")

_folder = _folder or os.path.join(os.path.expanduser("~"), "radiance_out")
os.makedirs(_folder, exist_ok=True)

# write luminaire files outside _folder
_parent_folder = os.path.dirname(_folder.rstrip("\\/"))
_luminaire_folder = os.path.join(_parent_folder, "Luminaire_files")
os.makedirs(_luminaire_folder, exist_ok=True)

stem = _ies_name.strip() if (_ies_name and isinstance(_ies_name, str)) \
       else os.path.splitext(os.path.basename(_ies_path))[0]


# Read RGB inputs safely
r = as_float(_r, 1.0)
g = as_float(_g, 1.0)
b = as_float(_b, 1.0)


# Compute normalized RGB
normalized_r, normalized_g, normalized_b, rgb_scale, weighted_sum = normalize_rgb(r, g, b)



# Build ies2rad command
cmd = ["ies2rad", "-o", stem, "-t", "default"]

if isinstance(_multiplier, (int, float)) and _multiplier not in (None, 0):
    cmd += ["-m", str(float(_multiplier))]
cmd += [_ies_path]


_cmdline = " ".join(shlex.quote(c) for c in cmd)
_rad_files = []
_dat_files = []



# output parameter
out_lines = []
out_lines.append(_cmdline)
out_lines.append("Luminaire folder: " + _luminaire_folder)
out_lines.append("Normalized RGB values: R={:.12g}, G={:.12g}, B={:.12g}".format(
    normalized_r, normalized_g, normalized_b
))

_log = ""

print(_cmdline)
print("Luminaire folder:", _luminaire_folder)
print("Normalized RGB values: R={:.12g}, G={:.12g}, B={:.12g}".format(
    normalized_r, normalized_g, normalized_b
))



# run
if _run:
    before = set(os.listdir(_luminaire_folder))

    proc = subprocess.run(
        cmd,
        cwd=_luminaire_folder,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True
    )
    _log = (proc.stdout or "") + (("\n" + proc.stderr) if proc.stderr else "")

    if proc.returncode != 0:
        fail("ies2rad failed.\nCommand:\n{}\n\nLog:\n{}".format(_cmdline, _log))

    after = set(os.listdir(_luminaire_folder))
    new = list(after - before)

    _rad_files = [os.path.join(_luminaire_folder, f) for f in new if f.lower().endswith(".rad")]
    _dat_files = [os.path.join(_luminaire_folder, f) for f in new if f.lower().endswith(".dat")]

    if not _rad_files:
        _rad_files = glob.glob(os.path.join(_luminaire_folder, stem + "*.rad"))
    if not _dat_files:
        _dat_files = glob.glob(os.path.join(_luminaire_folder, stem + "*.dat"))



    # Choose dat path
    dat_full = None
    if _datfile and isinstance(_datfile, str) and os.path.isfile(_datfile):
        dat_full = norm_path(_datfile)
    elif _dat_files:
        dat_full = norm_path(_dat_files[0])
    else:
        _log += "\n[info] No .dat provided/found; .rad files left unchanged."



    # Rewrite each .rad file:
    for each_file in _rad_files:
        with open(each_file, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()

        if dat_full:
            lines = replace_all_dat_tokens(lines, dat_full)

        lines, rgb_replaced = replace_rgb_line(lines, normalized_r, normalized_g, normalized_b)

        atomic_write_lines(each_file, lines)

        if rgb_replaced:
            msg = "normalized r g b values added"
            out_lines.append(msg)
            out_lines.append("overwrote: " + each_file)
            print(msg)
            print("overwrote:", each_file)
        else:
            msg = "warning: rgb line was not found in " + each_file
            out_lines.append(msg)
            print(msg)



    # Put info into _log
    _log += "\nLuminaire folder: " + _luminaire_folder
    _log += "\nNormalized RGB values: R={:.12g}, G={:.12g}, B={:.12g}".format(
        normalized_r, normalized_g, normalized_b
    )

else:
    _log = "Waiting for '_run' trigger. Command prepared but not executed.\n" + _cmdline
    _log += "\nLuminaire folder: " + _luminaire_folder
    _log += "\nNormalized RGB values: R={:.12g}, G={:.12g}, B={:.12g}".format(
        normalized_r, normalized_g, normalized_b
    )



# Output for the 'out' parameter
out = out_lines