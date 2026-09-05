ghenv.Component.Message = """FlahaGrow 0.1 Beta
Annual Simulation"""

import os, shutil, subprocess


SPLIT_TRIGGER = 10
N_PARTS = 4
_result_path = None




# paths
_root = str(_folder)
os.makedirs(_root, exist_ok=True)

batch_single = os.path.join(_root, "run_annual_single.bat")


def write_bat(path, lines):
    with open(path, "w", newline="\n") as f:
        for ln in lines:
            f.write(ln + "\n")


def copy_to_root(file_name, folder_path):
    full_path = os.path.join(_root, folder_path, file_name)
    root_path = os.path.join(_root, file_name)
    if not os.path.isfile(full_path):
        raise FileNotFoundError(f"{file_name} not found at {full_path}")
    shutil.copy2(full_path, root_path)


def set_n_in_details(details_str, new_n):
    parts = str(details_str).split()
    for i, p in enumerate(parts):
        if p == "-n" and i + 1 < len(parts):
            parts[i+1] = str(int(new_n))
            return " ".join(parts)
    return " ".join(parts + ["-n", str(int(new_n))])


# split 0.pts
def split_pts_file(src_pts, out_dir, n_parts=4):
    with open(src_pts, "r") as f:
        lines = f.readlines()
    total = len(lines)
    if total == 0:
        raise ValueError("0.pts is empty.")

    base = total // n_parts
    rem = total % n_parts

    pts_paths = []
    counts = []
    start = 0
    for i in range(n_parts):
        extra = 1 if i < rem else 0
        end = start + base + extra
        part_path = os.path.join(out_dir, f"0_part{i}.pts")
        with open(part_path, "w", newline="\n") as pf:
            pf.writelines(lines[start:end])
        pts_paths.append(part_path)
        counts.append(end - start)
        start = end
    return pts_paths, counts




# weather copy
if not _weather_file or not os.path.isfile(_weather_file):
    raise ValueError("Set a valid EPW path for _weather_file.")

root_weather = os.path.join(_root, os.path.basename(_weather_file))
shutil.copy2(_weather_file, root_weather)
name_weather = os.path.basename(root_weather)
print("Copied weather:", root_weather)



# 0.pts in root
grid_dir = os.path.join(_root, "model", "grid")
os.makedirs(grid_dir, exist_ok=True)

pts_candidates = [f for f in os.listdir(grid_dir) if f.lower().endswith(".pts")]
if "0.pts" not in [p.lower() for p in pts_candidates] and pts_candidates:
    os.replace(os.path.join(grid_dir, pts_candidates[0]), os.path.join(grid_dir, "0.pts"))



copy_to_root("0.pts", r"model\grid")
copy_to_root("envelope.rad", r"model\scene")
copy_to_root("envelope.mat", r"model\scene")
copy_to_root("envelope.blk", r"model\scene")



# skyglow and groundglow
sky_glow = os.path.join(_root, "skyglow.rad")
sky_glow_text = """#@rfluxmtx u=+Y h=u
void glow ground_glow
0
0
4 1.000 1.000 1.000 0

ground_glow source ground
0
0
4 0 0 -1 180

#@rfluxmtx u=+Y h=r1
void glow sky_glow
0
0
4 1.000 1.000 1.000 0

sky_glow source sky
0
0
4 0 0 1 180
"""
with open(sky_glow, "w", newline="\n") as f:
    f.write(sky_glow_text)




# sky sub division
_m = 4 if (_sky_sub_div == 4) else 1
cpu_total = os.cpu_count() or 12

# sensor count = number of lines in 0.pts
with open(os.path.join(_root, "0.pts"), "r") as f:
    sensor_num = sum(1 for _ in f)

if sensor_num <= 0:
    raise ValueError("0.pts has 0 sensors.")

split_mode = sensor_num > SPLIT_TRIGGER
print("sensor_num:", sensor_num, "split_mode:", split_mode)





# MF presets from _details
details_input = _details
details_str = str(details_input).strip().lower()
try:
    details_int = int(details_input)
except:
    details_int = None

if details_int == 4 or details_str == "high":
    MF_value, cnt_value, ab_value, lw_value = 5, 3625, 0, 1e-3
elif details_int == 5 or details_str == "very high":
    MF_value, cnt_value, ab_value, lw_value = 6, 5221, 0, 1e-3
elif details_int == 3 or details_str == "mid":
    MF_value, cnt_value, ab_value, lw_value = 4, 2321, 0, 1e-3
elif details_int == 2 or details_str == "low":
    MF_value, cnt_value, ab_value, lw_value = 3, 1297, 0, 5e-3
else:
    MF_value, cnt_value, ab_value, lw_value = 2, 577, 0, 1e-2




# rfluxmtx quality string
if _custom_parameter not in (None, ""):
    details_base = str(_custom_parameter).strip()
else:
    if details_int == 1:
        details_base = f"-lw 0.01  -ab 1 -ad 256  -n {cpu_total}"
    elif details_int == 2:
        details_base = f"-lw 0.005 -ab 2 -ad 512  -n {cpu_total}"
    elif details_int == 3:
        details_base = f"-lw 0.002 -ab 2 -ad 1024 -n {cpu_total}"
    elif details_int == 4:
        details_base = f"-lw 0.0015 -ab 3 -ad 1536 -n {cpu_total}"
    elif details_int == 5:
        details_base = f"-lw 0.001 -ab 3 -ad 2048 -n {cpu_total}"
    else:
        details_base = f"-lw 0.002 -ab 2 -ad 1024 -n {cpu_total}"





# per-part max -n
cpu_total = os.cpu_count() or 1

n_chunk = max(1, cpu_total // N_PARTS)

details_single = set_n_in_details(details_base, cpu_total)
details_split  = set_n_in_details(details_base, n_chunk)

print("cpu_total:", cpu_total)
print("n_chunk:", n_chunk)
print("details_split:", details_split)





# build and launch
if _run:
    if not split_mode:
        single_lines = [
            "echo Annual RUN (single -> part0)",
            f"epw2wea {name_weather} Weather_0.wea",
            f"gendaymtx -m {_m} Weather_0.wea > Weather_0.smx",
            "oconv envelope.mat envelope.rad > amodel_0.oct",
            f"rfluxmtx -I+ -y {sensor_num} {details_single} - skyglow.rad -i amodel_0.oct < 0.pts > illum_0.mtx",
            "dctimestep illum_0.mtx Weather_0.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualR_part0.ill",

            "oconv envelope.blk envelope.rad > bmodel_0.oct",
            f"rfluxmtx -I+ -y {sensor_num} {details_single} - skyglow.rad -i bmodel_0.oct < 0.pts > billum_0.mtx",
            f"gendaymtx -m {_m} -d Weather_0.wea > Weatherd_0.smx",
            "dctimestep billum_0.mtx Weatherd_0.smx | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualRd_part0.ill",

            "echo void light solar 0 0 3 1e6 1e6 1e6 > suns_0.rad",
            f"cnt {cnt_value} | rcalc -e MF:{MF_value} -f reinsrc.cal -e Rbin=recno -o \"solar source sun 0 0 4 ${{Dx}} ${{Dy}} ${{Dz}} 0.533\" >> suns_0.rad",
            "oconv -f envelope.blk envelope.rad suns_0.rad > sunCoefficientsDDS_0.oct",
            f"rcontrib -I+ -ab {ab_value} -y {sensor_num} -n {cpu_total} -ad 64 -lw {lw_value} -dc 1 -dt 0 -dj 0 -fa -e MF:{MF_value} -f reinhart.cal -b rbin -bn Nrbins -m solar sunCoefficientsDDS_0.oct < 0.pts > cdsDDS_0.mtx",
            f"gendaymtx -5 0.533 -d -m {MF_value} Weather_0.wea > WeathersunM{MF_value}_0.smx",
            f"dctimestep cdsDDS_0.mtx WeathersunM{MF_value}_0.smx | rmtxop -faf -t -c 47.4 119.9 11.6 - > annualRs_part0.ill",

            "rmtxop annualR_part0.ill + -s -1 annualRd_part0.ill + annualRs_part0.ill > annualRfinal_part0.ill",
            "echo Done single part0"
        ]
        write_bat(batch_single, single_lines)

        # run
        subprocess.Popen(["cmd.exe", "/c", "start", "cmd", "/c", os.path.basename(batch_single)], cwd=_root)

        # output
        _result_path = _root





    else:
        # split pts
        part_pts_paths, part_counts = split_pts_file(os.path.join(_root, "0.pts"), _root, N_PARTS)
        for i in range(N_PARTS):
            rows_i = part_counts[i]
            pts_i  = os.path.basename(part_pts_paths[i])
            bat_i  = os.path.join(_root, f"run_part{i}.bat")



            # unique names per part and avoid collisions
            Weather_wea = f"Weather_{i}.wea"
            Weather_smx = f"Weather_{i}.smx"
            Weatherd_smx = f"Weatherd_{i}.smx"
            Weathersun_smx = f"WeathersunM{MF_value}_{i}.smx"
            amodel = f"amodel_{i}.oct"
            bmodel = f"bmodel_{i}.oct"
            suns = f"suns_{i}.rad"
            sunoct = f"sunCoefficientsDDS_{i}.oct"

            illum = f"illum_part{i}.mtx"
            billum = f"billum_part{i}.mtx"
            cds = f"cdsDDS_part{i}.mtx"

            lines = [
                f"echo Annual RUN (part {i})",
                f"epw2wea {name_weather} {Weather_wea}",

                f"gendaymtx -m {_m} {Weather_wea} > {Weather_smx}",
                f"oconv envelope.mat envelope.rad > {amodel}",
                f"rfluxmtx -I+ -y {rows_i} {details_split} - skyglow.rad -i {amodel} < {pts_i} > {illum}",
                f"dctimestep {illum} {Weather_smx} | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualR_part{i}.ill",

                f"oconv envelope.blk envelope.rad > {bmodel}",
                f"rfluxmtx -I+ -y {rows_i} {details_split} - skyglow.rad -i {bmodel} < {pts_i} > {billum}",
                f"gendaymtx -m {_m} -d {Weather_wea} > {Weatherd_smx}",
                f"dctimestep {billum} {Weatherd_smx} | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualRd_part{i}.ill",

                f"echo void light solar 0 0 3 1e6 1e6 1e6 > {suns}",
                f"cnt {cnt_value} | rcalc -e MF:{MF_value} -f reinsrc.cal -e Rbin=recno -o \"solar source sun 0 0 4 ${{Dx}} ${{Dy}} ${{Dz}} 0.533\" >> {suns}",
                f"oconv -f envelope.blk envelope.rad {suns} > {sunoct}",
                f"rcontrib -I+ -ab {ab_value} -y {rows_i} -n {n_chunk} -ad 64 -lw {lw_value} -dc 1 -dt 0 -dj 0 -fa -e MF:{MF_value} -f reinhart.cal -b rbin -bn Nrbins -m solar {sunoct} < {pts_i} > {cds}",
                f"gendaymtx -5 0.533 -d -m {MF_value} {Weather_wea} > {Weathersun_smx}",
                f"dctimestep {cds} {Weathersun_smx} | rmtxop -fa -t -c 47.4 119.9 11.6 - > annualRs_part{i}.ill",

                f"rmtxop annualR_part{i}.ill + -s -1 annualRd_part{i}.ill + annualRs_part{i}.ill > annualRfinal_part{i}.ill",
                f"echo Done part {i}"
            ]
            write_bat(bat_i, lines)
            subprocess.Popen(["cmd.exe", "/c", "start", "cmd", "/c", os.path.basename(bat_i)], cwd=_root)
        _result_path = _root
    print("RUN complete: parts launched. _result_path set to:", _result_path)
else:
    _result_path = _root