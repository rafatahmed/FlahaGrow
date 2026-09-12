"""Read-only research audit; not plugin implementation. Python 3.10+ stdlib.

Run from any directory. Emits JSON to stdout; never rewrites source datasets.
Linear interpolation to a 1 nm grid, trapezoidal integration with exact band
endpoints. Missing spectral tails are explicitly zero-assumed, not measured.
"""
import csv
import hashlib
import json
import math
from bisect import bisect_left
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parent
H, C, NA = 6.62607015e-34, 299792458.0, 6.02214076e23
PHOTON = 1e-3 / (H * C * NA)
METHOD = "research-linear-1nm-trapezoid-v1"


def validate(rows):
    if len(rows) < 2:
        raise ValueError("At least two spectral samples required")
    previous = -math.inf
    for row in rows:
        if not all(math.isfinite(x) for x in row):
            raise ValueError("Nonfinite sample")
        if row[0] <= previous or row[0] <= 0 or any(x < 0 for x in row[1:]):
            raise ValueError("Unordered, duplicate, nonpositive wavelength or negative power")
        previous = row[0]
    return rows


def interpolate(xs, ys, x):
    if x < xs[0] or x > xs[-1]:
        return 0.0
    i = bisect_left(xs, x)
    if xs[i] == x:
        return ys[i]
    return ys[i-1] + (ys[i] - ys[i-1]) * (x-xs[i-1]) / (xs[i]-xs[i-1])


def integral(values, first, last):
    return math.fsum((values[n] + values[n+1]) * 0.5 for n in range(first, last))


def calculate(rows, col, basis, vl):
    validate(rows)
    xs = [r[0] for r in rows]
    if xs[0] > 400 or xs[-1] < 700:
        raise ValueError("Incomplete PAR coverage")
    ys = [r[col] for r in rows]
    if basis not in ("relative_energy", "photon_irradiance"):
        raise ValueError("Unknown spectral basis")
    sample = {n: interpolate(xs, ys, n) for n in range(360, 831)}
    # Photon-input spectra are interpolated in their original photon basis.
    energy = {n: sample[n] / (PHOTON*n) if basis == "photon_irradiance"
              else sample[n] for n in sample}
    photons = {n: energy[n] * PHOTON*n for n in energy}
    ppfd = integral(photons, 400, 700)
    lux = 683 * integral({n: energy[n]*vl[n] for n in energy}, 360, 830)
    if lux <= 0 or not math.isfinite(lux):
        raise ValueError("Nonpositive or nonfinite photopic denominator")
    return {
        "factor_umol_m2_s_per_lux": ppfd/lux,
        "source_range_nm": [xs[0], xs[-1]],
        "source_max_step_nm": max(b-a for a, b in zip(xs, xs[1:])),
        "outside_source_range": "zero assumed; not measured",
        "photopic_coverage": "complete" if xs[0] <= 360 and xs[-1] >= 830 else "truncated",
        "source_ppfd_umol_m2_s": ppfd if basis == "photon_irradiance" else None,
        "source_illuminance_lux": lux if basis == "photon_irradiance" else None,
    }


def source_hash(path):
    raw = path.read_bytes()
    return {"file": path.relative_to(ROOT).as_posix(),
            "md5": hashlib.md5(raw).hexdigest(), "sha256": hashlib.sha256(raw).hexdigest()}


def horticulture():
    """Extract only literal numeric cells; reject formulas instead of trusting caches."""
    path = ROOT / "data/horticulture/park-runkle-2018-s1.xlsx"
    ns = {"s": "http://schemas.openxmlformats.org/spreadsheetml/2006/main"}
    with ZipFile(path) as z:
        strings = ["".join(si.itertext()) for si in
                   ET.fromstring(z.read("xl/sharedStrings.xml")).findall("s:si", ns)]
        sheet = ET.fromstring(z.read("xl/worksheets/sheet1.xml"))
        cells = {}
        for cell in sheet.findall(".//s:c", ns):
            if cell.find("s:f", ns) is not None:
                raise ValueError("Unexpected formula in research source")
            value = cell.find("s:v", ns)
            if value is not None:
                cells[cell.attrib["r"]] = (strings[int(value.text)] if cell.get("t") == "s"
                                           else float(value.text))
    names = [cells[c+"3"] for c in "BCDEFG"]
    rows = [[cells[c+str(n)] for c in "ABCDEFG"] for n in range(4, 404)]
    if len(rows) != 400 or rows[0][0] != 400 or rows[-1][0] != 799:
        raise ValueError("Unexpected S1 source layout")
    return path, names, validate(rows)


def self_tests(vl):
    assert interpolate([400, 405], [0, 10], 402) == 4
    assert integral({n: 1 for n in range(400, 701)}, 400, 700) == 300
    assert math.isclose(100*86400/1e6, 8.64)
    assert math.isclose(160*18*3600/1e6, 10.368)
    a = calculate([[360, 1], [830, 1]], 1, "relative_energy", vl)
    b = calculate([[360, 17], [830, 17]], 1, "relative_energy", vl)
    assert math.isclose(a["factor_umol_m2_s_per_lux"], b["factor_umol_m2_s_per_lux"])
    cases = [[[400, 1], [400, 2]], [[400, -1], [700, 1]],
             [[400, math.nan], [700, 1]], [[401, 1], [700, 1]],
             [[360, 0], [830, 0]], [[700, 1], [400, 1]]]
    for rows in cases:
        try:
            calculate(rows, 1, "relative_energy", vl)
        except ValueError:
            pass
        else:
            raise AssertionError("Invalid spectrum accepted")
    return "passed: interpolation, band endpoints, scale invariance, DLI and six invalid-input cases"


def main():
    profiles, files = [], []
    cie = ROOT / "data/cie"
    vl = {int(r[0]): float(r[1]) for r in csv.reader((cie / "CIE_sle_photopic.csv").open())}
    for path in sorted(cie.glob("*.csv")):
        metadata_path = Path(str(path) + "_metadata.json")
        meta = json.loads(metadata_path.read_text(encoding="utf-8-sig"))
        record = source_hash(path)
        checksums = {x["hashMethod"]: x["checksum"] for x in meta["checksums"]}
        record.update({"doi": meta["identifier"]["identifier"],
                       "published_checksums": checksums,
                       "md5_matches": record["md5"] == checksums["md5"],
                       "sha256_matches": record["sha256"] == checksums["sha256"]})
        assert record["md5_matches"], path.name
        files.extend([record, source_hash(metadata_path)])
        if path.name == "CIE_sle_photopic.csv":
            continue
        rows = validate([[float(x) for x in r] for r in csv.reader(path.open())])
        for col, header in enumerate(meta["datatableInfo"]["columnHeaders"][1:], 1):
            result = calculate(rows, col, "relative_energy", vl)
            result.update({"id": path.stem + ":" + str(col), "label": header["title"],
                           "data_file": record["file"], "column_1based": col+1,
                           "basis": "relative_energy", "doi": record["doi"],
                           "status": "reference candidate; not plugin-enabled"})
            profiles.append(result)
    path, names, rows = horticulture()
    record = source_hash(path)
    assert record["md5"] == "994fdf47594930488dd7ff04aed2f63e"
    files.extend([record, source_hash(ROOT / "data/horticulture/figshare-6946136.json")])
    for col, name in enumerate(names, 1):
        result = calculate(rows, col, "photon_irradiance", vl)
        result.update({"id": "park-runkle-2018:" + name, "label": name,
                       "data_file": record["file"], "sheet": "Figure 1",
                       "source_cells": f"{'ABCDEFG'[col]}4:{'ABCDEFG'[col]}403",
                       "basis": "photon_irradiance", "doi": "10.6084/m9.figshare.6946136.v1",
                       "status": "research treatment; incomplete photopic coverage; not a fixture preset"})
        profiles.append(result)
    # Compare published-data factors with the current hardcoded values only.
    # This does not claim to execute the C# importer or validate a Radiance run.
    existing = {"CIE_illum_D55:1": .017833, "CIE_std_illum_D65:1": .018043,
                "CIE_illum_D75:1": .018345, "CIE_illum_HPs:1": .011640,
                "CIE_illum_HPs:5": .016180, "CIE_illum_LEDs_1nm:6": .013633,
                "CIE_illum_LEDs_1nm:9": .017691}
    for p in profiles:
        if p["id"] in existing:
            p["current_hardcoded_factor"] = existing[p["id"]]
            p["hardcoded_minus_research_percent"] = 100*(existing[p["id"]]/p["factor_umol_m2_s_per_lux"]-1)
    output = {"method": METHOD, "par_band_nm": [400, 700],
                      "photopic_integration_nm": [360, 830], "checks": self_tests(vl),
                      "files": files, "profiles": profiles}
    if sys.argv[1:] == ["--check"]:
        expected = json.loads((ROOT / "profile-audit.json").read_text(encoding="utf-8"))
        assert output == expected, "Audit snapshot changed; inspect data, hashes and method"
        print(f"PASS: {len(profiles)} profiles, {len(files)} source files, snapshot and self-tests")
    elif sys.argv[1:]:
        raise SystemExit("Usage: audit_profiles.py [--check]")
    else:
        print(json.dumps(output, indent=2, allow_nan=False))


if __name__ == "__main__":
    main()
