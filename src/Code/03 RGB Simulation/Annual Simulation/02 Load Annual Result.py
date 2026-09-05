ghenv.Component.Message = """FlahaGrow 0.1 Beta
Load Annual Result"""

import os, io, re, struct, json, time

_result = None
sensors = hours = 0
status = ""

N_PARTS = 4 



# helpers
NUM_LINE = re.compile(r'^[\s\+\-\.0-9eE]+$')

def parse_dims_from_header(path):
    nrows = ncols = ncomp = None
    with io.open(path, "r", encoding="ascii", errors="ignore") as f:
        for line in f:
            s = line.strip()

            # stop if numeric data begins
            if s and NUM_LINE.match(s):
                break

            if s.startswith("NROWS="):
                try:
                    nrows = int(s.split("=",1)[1].strip())
                except:
                    pass

            elif s.startswith("NCOLS="):
                try:
                    ncols = int(s.split("=",1)[1].strip())
                except:
                    pass

            elif s.startswith("NCOMP="):
                try:
                    ncomp = int(s.split("=",1)[1].strip())
                except:
                    pass

    return nrows, ncols, ncomp





# find the result files
def find_part_files(folder):
    parts = {}
    for name in os.listdir(folder):
        low = name.lower()
        if not low.endswith(".ill"):
            continue
        m = re.search(r"_part(\d+)\.ill$", low)
        if not m:
            continue
        try:
            idx = int(m.group(1))
        except:
            continue
        parts[idx] = os.path.join(folder, name)
    return parts





# merge columns
def write_merged_ill(out_path, part_paths_ordered):
    # read header from part0, and dims
    p0 = part_paths_ordered[0]
    nrows, ncols_part, ncomp = parse_dims_from_header(p0)
    if not ncols_part:
        raise ValueError("Could not read NCOLS from part0 header.")
    total_cols = ncols_part * len(part_paths_ordered)


    # capture header lines from part0 up to first blank line
    header_lines = []
    with io.open(p0, "r", encoding="ascii", errors="ignore") as f:
        for line in f:
            if not line.strip():
                break
            header_lines.append(line.rstrip("\n"))


    # patch NCOLS line
    patched = []
    for hl in header_lines:
        if hl.strip().startswith("NCOLS="):
            patched.append("NCOLS={}".format(total_cols))
        else:
            patched.append(hl)


    # open output
    out_dir = os.path.dirname(out_path)
    if out_dir and not os.path.isdir(out_dir):
        os.makedirs(out_dir, exist_ok=True)


    # open all part files and skip headers to data section
    fs = []
    try:
        for p in part_paths_ordered:
            f = io.open(p, "r", encoding="ascii", errors="ignore", buffering=1024*1024)
            for line in f:
                if not line.strip():
                    break
            fs.append(f)

        with io.open(out_path, "w", encoding="ascii", newline="\n", buffering=1024*1024) as out:
            # write header with blank line
            for hl in patched:
                out.write(hl + "\n")
            out.write("\n")


            def next_data_line(fh):
                for line in fh:
                    s = line.strip()
                    if not s:
                        continue
                    if NUM_LINE.match(s):
                        return s
                return None

            rows_written = 0
            while True:
                if nrows is not None and rows_written >= nrows:
                    break

                lines = []
                for fh in fs:
                    s = next_data_line(fh)
                    if s is None:
                        return rows_written, total_cols, ncomp, nrows
                    lines.append(s)

                # concatenate
                out.write(" ".join(lines) + "\n")
                rows_written += 1

            return rows_written, total_cols, ncomp, nrows
    finally:
        for f in fs:
            try: f.close()
            except: pass




# main
if not _result_path or not isinstance(_result_path, str):
    status = "Provide a valid folder path that contains annualRfinal_part0..part3.ill"
else:
    folder = _result_path
    if not os.path.isdir(folder):
        status = "Provide a valid folder path (not a file path)."
    else:
        # target merged .ill
        merged_ill = os.path.join(folder, "annualRfinal.ill")
        raw = os.path.splitext(merged_ill)[0] + ".f32"
        meta = os.path.splitext(merged_ill)[0] + ".meta.json"

        if not _build:
            _result = raw if os.path.isfile(raw) else None
            if _result and os.path.isfile(meta):
                try:
                    m = json.load(open(meta, "r"))
                    sensors, hours = m.get("sensors", 0), m.get("hours", 0)
                    status = "Cache exists"
                except:
                    status = "Cache exists (meta unreadable)"
            else:
                status = "No cache yet — set _build True"
        else:
            try:
                parts = find_part_files(folder)

                missing = [i for i in range(N_PARTS) if i not in parts]
                if missing:
                    raise ValueError("Missing part files: {}. Expected _part0.._part3.ill in folder.".format(missing))

                ordered_paths = [parts[i] for i in range(N_PARTS)]
                rows_written, total_cols, ncomp, nrows_header = write_merged_ill(merged_ill, ordered_paths)
                nrows, ncols, ncomp2 = parse_dims_from_header(merged_ill)


                out = open(raw, "wb")
                rows = 0
                inferred_cols = None


                with io.open(merged_ill, "r", encoding="ascii", errors="ignore") as f:
                    in_data = False
                    for line in f:
                        s = line.strip()
                        if not in_data:
                            if not s:
                                in_data = True
                            continue
                        if not s or not NUM_LINE.match(s):
                            continue
                        parts_line = s.split()
                        if inferred_cols is None:
                            inferred_cols = len(parts_line)
                        for tok in parts_line:
                            try:
                                out.write(struct.pack("<f", float(tok)))
                            except:
                                pass
                        rows += 1
                out.close()


                hours = int(nrows if nrows else rows)
                sensors = int(ncols if ncols else (inferred_cols or 0))


                json.dump({
                    "ill_folder": folder,
                    "ill_path": merged_ill,
                    "raw_path": raw,
                    "sensors": int(sensors),
                    "hours": int(hours),
                    "order": "row-major hours x sensors (each row = hour)",
                    "ncomp": int(ncomp2 or 1),
                    "parts_used": [os.path.basename(p) for p in ordered_paths],
                    "merged_rows_written": int(rows_written),
                    "built": time.strftime("%Y-%m-%d %H:%M:%S")
                }, open(meta, "w"), indent=2)

                _result = raw
                status = "Merged + Cached: {} sensors × {} hours".format(sensors, hours)
                ghenv.Component.Message = "Build Annual Cache\nOK"

            except Exception as e:
                status = "Error: " + repr(e)
                ghenv.Component.Message = "Build Annual Cache\nERROR"