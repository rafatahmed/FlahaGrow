ghenv.Component.Message = """FlahaGrow 0.0
Compile Luminaries"""



import os


def flatten_any(x):
    try:
        from ghpythonlib import treehelpers
        return list(treehelpers.flatten(x))
    except:
        pass

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

    try:
        _ = len(x); return list(x)
    except:
        return [x]

def as_str_list(x):
    out = []
    for v in flatten_any(x):
        if v is None: 
            continue
        s = str(v).strip()
        if s:
            out.append(s)
    return out

def norm_folder(path_like):
    if not path_like:
        raise ValueError("No _folder provided.")
    p = os.path.abspath(str(path_like)).strip()
    if not p:
        raise ValueError("Empty _folder path.")
    return p

# main
lum_rad_path, out = None, ""

try:
    if _write:
        lines = as_str_list(_lighting_geometry)
        if not lines:
            raise ValueError("No xform lines in _lighting_geometry.")

        folder = norm_folder(_folder)
        parent_folder = os.path.dirname(folder.rstrip("\\/"))
        luminaire_folder = os.path.join(parent_folder, "Luminaire_files")

        if not os.path.isdir(luminaire_folder):
            raise ValueError("Luminaire_files folder not found:\n{}".format(luminaire_folder))

        fname = "luminaries.rad"
        lum_rad_path = os.path.join(luminaire_folder, fname)

        with open(lum_rad_path, "w", newline="\n") as f:
            f.write("# Auto-generated luminaire placement file\n")
            f.write("# Created by GH Python component: Electric lighting\n\n")
            for l in lines:
                if not l.endswith("\n"):
                    l += "\n"
                f.write(l)

        sample = lines[0] if lines else "(none)"
        out = "Wrote {} line(s) to:\n{}\nExample: {}".format(len(lines), lum_rad_path, sample)
    else:
        out = "Toggle _write to generate luminaries.rad."

except Exception as e:
    out = "ERROR: {}".format(e)