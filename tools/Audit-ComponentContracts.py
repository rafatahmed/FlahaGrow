"""Read-only source comparison; writes isolated probes under ignored artifacts/.

Runs extracted pure Python functions and extracted C# SpectralMath/Detail code.
This is not a Rhino component, GUI, or scientific reference validation.
"""
import ast
import csv
import json
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "artifacts" / "component-contract-audit"
OUT.mkdir(parents=True, exist_ok=True)
scripts = sorted(p for p in (ROOT / "src/Code").rglob("*") if p.suffix.lower() == ".py")
trees = {p: ast.parse(p.read_text(encoding="utf-8-sig")) for p in scripts}
selection = next(p for p in scripts if p.name == "CF_ILL_PPFD_Spectral Data Selection.py")
names = {"H", "C", "NA", "OPN1_1NM"}
functions = {"opn1_at_nm", "to_float", "best_header_match", "compute_cf_from_csv"}
nodes = [n for n in trees[selection].body
         if (isinstance(n, ast.FunctionDef) and n.name in functions)
         or (isinstance(n, ast.Assign) and any(isinstance(t, ast.Name) and t.id in names for t in n.targets))]
scope = {"csv": csv, "WL_MIN": 380, "WL_MAX": 780, "WL_LIST": list(range(380, 781))}
exec(compile(ast.Module(body=nodes, type_ignores=[]), str(selection), "exec"), scope)
legacy = {}
for label, wavelength in [("555nm", 555), ("750nm", 750)]:
    path = OUT / (label + ".csv")
    with path.open("w", newline="", encoding="utf-8") as stream:
        writer = csv.writer(stream)
        writer.writerow(["Wavelength (nm)", "Spectral Power"])
        writer.writerows((nm, int(nm == wavelength)) for nm in range(380, 781))
    legacy[label] = scope["compute_cf_from_csv"](str(path))[2]
components = ROOT / "src/FlahaGrow.Grasshopper/Components"
spectral = (components / "SpectralConversionFactorComponents.cs").read_text(encoding="utf-8")
spectral = spectral[spectral.index("internal static class SpectralMath"):]
annual = (components / "AnnualSimulationComponent.cs").read_text(encoding="utf-8")
detail = next(line.strip() for line in annual.splitlines() if "private static string Detail(" in line)
detail = detail.replace("private static", "internal static", 1)
program = '''using System.Globalization;
using System.Text.Json;
var output = new Dictionary<string, object>();
foreach (var label in new[] { "555nm", "750nm" })
    output[label] = SpectralMath.Compute(Path.Combine(args[0], label + ".csv"), 1).Factor;
output["numericDetail4"] = Quality.Detail("4", 8);
output["namedDetailHigh"] = Quality.Detail("high", 8);
Console.WriteLine(JsonSerializer.Serialize(output));
'''
(OUT / "Program.cs").write_text(program + spectral + "\ninternal static class Quality {\n" + detail + "\n}\n", encoding="utf-8")
(OUT / "Probe.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework></PropertyGroup></Project>', encoding="utf-8")
result = subprocess.run(["dotnet", "run", "--project", str(OUT / "Probe.csproj"), "--configuration", "Release", "--", str(OUT)], cwd=ROOT, text=True, capture_output=True)
if result.returncode:
    raise SystemExit(result.stdout + result.stderr)
compiled = json.loads(next(line for line in result.stdout.splitlines() if line.startswith('{')))
assert legacy["750nm"] == 0 and compiled["750nm"] > 0
assert compiled["numericDetail4"] == compiled["namedDetailHigh"], "Numeric/named annual quality regression"
report = {"python_scripts_parsed": len(trees), "legacy_factors": legacy, "compiled_source_probe": compiled,
          "boundary": "Extracted pure source code; not Rhino-host or end-to-end validation."}
(OUT / "results.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, indent=2))
