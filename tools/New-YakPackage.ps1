[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')]
    [string]$Version = '0.1.0',
    [string]$YakPath = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$pluginProject = Join-Path $repoRoot 'src\FlahaGrow.Grasshopper\FlahaGrow.Grasshopper.csproj'
$stagingDirectory = Join-Path $repoRoot 'artifacts\yak-staging'
$pluginOutput = Join-Path $repoRoot 'src\FlahaGrow.Grasshopper\bin\Release\net7.0-windows\FlahaGrow.gha'

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

dotnet build $pluginProject --configuration Release "/p:Version=$Version"

if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingDirectory | Out-Null
Copy-Item -LiteralPath $pluginOutput -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'package\manifest.yml') -Destination $stagingDirectory
$stagedManifest = Join-Path $stagingDirectory 'manifest.yml'
$manifest = Get-Content -LiteralPath $stagedManifest -Raw
$manifest = $manifest -replace '(?m)^version:\s*.*$', "version: $Version"
Set-Content -LiteralPath $stagedManifest -Value $manifest -NoNewline
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'src\Library') -Destination (Join-Path $stagingDirectory 'shared\Library') -Recurse

Push-Location $stagingDirectory
try {
    & $YakPath build
    if ($LASTEXITCODE -ne 0) {
        throw "yak build failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}
