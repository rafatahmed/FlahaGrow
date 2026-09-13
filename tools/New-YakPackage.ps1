[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')]
    [string]$Version = '0.1.0',
    [string]$YakPath = '',
    [switch]$NoRestore
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$stagingDirectory = Join-Path $repoRoot 'artifacts\yak-staging'
$pluginOutput = Join-Path $repoRoot 'src\FlahaGrow.Grasshopper\bin\Release\net7.0-windows\FlahaGrow.gha'
$coreOutput = Join-Path (Split-Path -Parent $pluginOutput) 'FlahaGrow.Core.dll'

if ([string]::IsNullOrWhiteSpace($YakPath)) {
    $yakCommand = Get-Command yak -ErrorAction SilentlyContinue
    if ($yakCommand) {
        $YakPath = $yakCommand.Source
    }
    else {
        $YakPath = Join-Path $env:ProgramFiles 'Rhino 8\System\yak.exe'
    }
}

if (-not (Test-Path -LiteralPath $YakPath)) {
    throw "Yak was not found. Install Rhino 8 or provide -YakPath."
}

$testArguments = @('test', (Join-Path $repoRoot 'tests/FlahaGrow.Core.Tests/FlahaGrow.Core.Tests.csproj'), '--configuration', 'Release', '-m:1', "/p:Version=$Version")
if ($NoRestore) { $testArguments += '--no-restore' }
& dotnet @testArguments
if ($LASTEXITCODE -ne 0) { throw 'Core validation failed. Packaging stopped.' }
& (Join-Path $PSScriptRoot 'Test-SetupComponents.ps1') -NoRestore:$NoRestore -Version $Version
& (Join-Path $PSScriptRoot 'Test-PluginAudit.ps1')
foreach ($output in @($pluginOutput, $coreOutput)) {
    if (-not (Test-Path -LiteralPath $output -PathType Leaf)) { throw "Required package assembly missing: $output" }
}

$expectedStaging = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts\yak-staging'))
if ([IO.Path]::GetFullPath($stagingDirectory) -ne $expectedStaging) { throw 'Unexpected package staging location.' }
foreach ($target in @((Join-Path $repoRoot 'artifacts'), $stagingDirectory)) {
    if (Test-Path -LiteralPath $target) {
        if ((Get-Item -LiteralPath $target -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Staging cannot traverse a junction: $target" }
    }
}

if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingDirectory | Out-Null
Copy-Item -LiteralPath $pluginOutput -Destination $stagingDirectory
Copy-Item -LiteralPath $coreOutput -Destination $stagingDirectory
New-Item -ItemType Directory -Path (Join-Path $stagingDirectory 'PlantLight') | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'src\FlahaGrow.Core\PlantLight\NOTICE.md') -Destination (Join-Path $stagingDirectory 'PlantLight\NOTICE.md')
Copy-Item -LiteralPath (Join-Path $repoRoot 'package\manifest.yml') -Destination $stagingDirectory
$stagedManifest = Join-Path $stagingDirectory 'manifest.yml'
$manifest = Get-Content -LiteralPath $stagedManifest -Raw
$manifest = $manifest -replace '(?m)^version:\s*.*$', "version: $Version"
Set-Content -LiteralPath $stagedManifest -Value $manifest -NoNewline
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'src\Library') -Destination (Join-Path $stagingDirectory 'shared\Library') -Recurse

Push-Location $stagingDirectory
try {
    & $YakPath build --platform win
    if ($LASTEXITCODE -ne 0) {
        throw "yak build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
