[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')]
    [string]$Version = '0.1.0'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$pluginProject = Join-Path $repoRoot 'src\FlahaGrow.Grasshopper\FlahaGrow.Grasshopper.csproj'
$stagingDirectory = Join-Path $repoRoot 'artifacts\yak-staging'
$pluginOutput = Join-Path $repoRoot 'src\FlahaGrow.Grasshopper\bin\Release\net7.0-windows\FlahaGrow.gha'

dotnet build $pluginProject --configuration Release "/p:Version=$Version"

if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}

New-Item -ItemType Directory -Path $stagingDirectory | Out-Null
Copy-Item -LiteralPath $pluginOutput -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'package\manifest.yml') -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $stagingDirectory
Copy-Item -LiteralPath (Join-Path $repoRoot 'src\Library') -Destination (Join-Path $stagingDirectory 'shared\Library') -Recurse

Push-Location $stagingDirectory
try {
    yak build
}
finally {
    Pop-Location
}
