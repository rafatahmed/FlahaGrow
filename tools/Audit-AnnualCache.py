"""Read-only legacy annual cache comparison. Prints JSON; never repairs/clamps data."""
import argparse
import json
import math
from pathlib import Path
import struct
from itertools import zip_longest


def rows(path):
    with path.open(encoding="ascii") as stream:
        fields = {}
        for line in stream:
            text = line.strip()
            if not text and "FORMAT" in fields:
                break
            if "=" in text:
                key, value = text.split("=", 1)
                if key in ("FORMAT", "NROWS", "NCOLS", "NCOMP"):
                    if key in fields:
                        raise ValueError(f"Duplicate field in {path}: {key}")
                    fields[key] = value
        if fields.get("FORMAT") != "ascii" or fields.get("NCOMP") != "1":
            raise ValueError(f"Expected scalar ASCII matrix: {path}")
        count = 0
        for line in stream:
            if not line.strip():
                continue
            values = list(map(float, line.split()))
            if len(values) != int(fields["NCOLS"]) or not all(math.isfinite(v) for v in values):
                raise ValueError(f"Malformed row {count} in {path}")
            count += 1
            yield values
        if count != int(fields["NROWS"]):
            raise ValueError(f"Truncated/extra rows: {path}")


def audit(folder):
    meta = json.loads((folder / "annualRfinal.meta.json").read_text())
    sensors, hours = meta["sensors"], meta["hours"]
    paths = sorted(folder.glob("annualRfinal_part*.ill"), key=lambda p: int(p.stem.split("part")[-1]))
    if not paths:
        raise ValueError("No final parts found")
    streams = [rows(path) for path in paths]
    compared = mismatches = negative = count = 0
    worst = None
    with (folder / "annualRfinal.f32").open("rb") as stream:
        for hour, parts in enumerate(zip_longest(*streams)):
            if any(part is None for part in parts):
                raise ValueError("Part lengths differ")
            values = [v for part in parts for v in part]
            if len(values) != sensors:
                raise ValueError("Part widths do not match cache metadata")
            cached = struct.unpack(f"<{sensors}f", stream.read(sensors * 4))
            offset = 0
            for part_index, part in enumerate(parts):
                for local_sensor, value in enumerate(part):
                    sensor = offset + local_sensor
                    expected = struct.unpack("<f", struct.pack("<f", value))[0]
                    compared += 1
                    mismatches += expected != cached[sensor]
                    negative += value < 0
                    if worst is None or value < worst["lux"]:
                        worst = dict(lux=value, hour=hour, sensor=sensor, part=part_index, local_sensor=local_sensor)
                offset += len(part)
            count += 1
        if count != hours or stream.read(1):
            raise ValueError("Cache time dimension or byte length mismatch")
    terms = {}
    part_id = paths[worst["part"]].stem.split("part")[-1]
    for term in ("annualR", "annualRd", "annualRs"):
        for hour, values in enumerate(rows(folder / f"{term}_part{part_id}.ill")):
            if hour == worst["hour"]:
                terms[term] = values[worst["local_sensor"]]
                break
    return dict(folder=str(folder), hours=hours, sensors=sensors, compared=compared,
                float32_mismatches=mismatches, negative_values=negative, minimum=worst,
                minimum_terms=terms, boundary="Structural/cache parity only; not scientific validation. Files unchanged.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("folder", type=Path)
    print(json.dumps(audit(parser.parse_args().folder.resolve()), indent=2))
