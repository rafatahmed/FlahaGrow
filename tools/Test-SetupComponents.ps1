[CmdletBinding()]
param([switch]$NoRestore, [string]$RadianceBin = '', [string]$Version = '')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'tests\FlahaGrow.SetupSmoke\FlahaGrow.SetupSmoke.csproj'
$arguments = @('build', $project, '--configuration', 'Release', '-m:1')
if ($NoRestore) { $arguments += '--no-restore' }
if ($Version) { $arguments += "/p:Version=$Version" }
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Setup component check build failed.' }
$output = Join-Path $repoRoot 'tests\FlahaGrow.SetupSmoke\bin\Release\net7.0-windows'
# The standalone CLR accepts .dll runtime assets; Rhino itself loads .gha files.
# Adapt only this test output, never the distributed plugin.
Copy-Item -LiteralPath (Join-Path $output 'FlahaGrow.gha') -Destination (Join-Path $output 'FlahaGrow.dll') -Force
$depsPath = Join-Path $output 'FlahaGrow.SetupSmoke.deps.json'
$deps = [IO.File]::ReadAllText($depsPath).Replace('FlahaGrow.gha', 'FlahaGrow.dll')
[IO.File]::WriteAllText($depsPath, $deps)
& dotnet (Join-Path $output 'FlahaGrow.SetupSmoke.dll') $repoRoot $RadianceBin
if ($LASTEXITCODE -ne 0) { throw 'Setup component checks failed.' }
$stamp = [ordered]@{
    SourceFingerprint = & (Join-Path $PSScriptRoot 'Get-PluginAuditFingerprint.ps1') -Repository $repoRoot
    RuntimeHashes = @{}
}
foreach ($name in @('FlahaGrow.dll', 'FlahaGrow.Core.dll', 'FlahaGrow.SetupSmoke.dll')) {
    $stamp.RuntimeHashes[$name] = (Get-FileHash -LiteralPath (Join-Path $output $name) -Algorithm SHA256).Hash
}
$stamp | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'component-validation.json') -Encoding utf8
