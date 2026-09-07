[CmdletBinding()]
param([switch]$NoRestore)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'tests\FlahaGrow.SetupSmoke\FlahaGrow.SetupSmoke.csproj'
$arguments = @('build', $project, '--configuration', 'Release', '-m:1')
if ($NoRestore) { $arguments += '--no-restore' }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Setup component check build failed.' }
$output = Join-Path $repoRoot 'tests\FlahaGrow.SetupSmoke\bin\Release\net7.0-windows'
# The standalone CLR accepts .dll runtime assets; Rhino itself loads .gha files.
# Adapt only this test output, never the distributed plugin.
Copy-Item -LiteralPath (Join-Path $output 'FlahaGrow.gha') -Destination (Join-Path $output 'FlahaGrow.dll') -Force
$depsPath = Join-Path $output 'FlahaGrow.SetupSmoke.deps.json'
$deps = [IO.File]::ReadAllText($depsPath).Replace('FlahaGrow.gha', 'FlahaGrow.dll')
[IO.File]::WriteAllText($depsPath, $deps)
& dotnet (Join-Path $output 'FlahaGrow.SetupSmoke.dll') $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Setup component checks failed.' }
