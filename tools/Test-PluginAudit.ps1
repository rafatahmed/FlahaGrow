[CmdletBinding()]
param([string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$runner = Join-Path $repository "tests/FlahaGrow.SetupSmoke/bin/$Configuration/net7.0-windows/FlahaGrow.SetupSmoke.dll"
if (-not (Test-Path -LiteralPath $runner)) { throw 'Run Test-SetupComponents.ps1 first to prepare the current registration runtime.' }

& (Join-Path $PSScriptRoot 'Test-ComponentDocumentation.ps1') -Configuration $Configuration
$json = & dotnet $runner --component-catalog
if ($LASTEXITCODE -ne 0) { throw 'Component registration export failed.' }
$catalog = @(($json -join "`n") | ConvertFrom-Json)
$issues = [Collections.Generic.List[string]]::new()
if (@($catalog | Where-Object Exposure -eq 'hidden').Count) { $issues.Add('Hidden executable component registered.') }
foreach ($key in @('Guid', 'Name', 'Type')) {
    if (@($catalog | Group-Object -Property $key | Where-Object Count -gt 1).Count) { $issues.Add("Duplicate component $key.") }
}

# Scientific constants, GUIDs, format contracts and documented defaults are not
# machine-specific paths. Exclude generated code; it contains build-machine paths.
$sourceFiles = @(foreach ($directory in @('src/FlahaGrow.Core', 'src/FlahaGrow.Grasshopper')) {
    Get-ChildItem -LiteralPath (Join-Path $repository $directory) -Filter '*.cs' -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
})
foreach ($file in $sourceFiles) {
    if ([IO.File]::ReadAllText($file.FullName) -match '(?i)(?<![a-z0-9])[a-z]:[\\/]|/(?:Users|home)/') {
        $issues.Add("Machine-specific absolute path in $($file.Name).")
    }
}
foreach ($file in Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.ps1') {
    if ([IO.File]::ReadAllText($file.FullName) -match '(?i)(?<![a-z0-9])[a-z]:[\\/]|/(?:Users|home)/') {
        $issues.Add("Machine-specific absolute path in tool $($file.Name).")
    }
    $tokens = $null; $parseErrors = $null
    $null = [Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$parseErrors)
    if ($parseErrors.Count) { $issues.Add("PowerShell syntax errors in $($file.Name).") }
}
$allowedIcons = @($catalog | Where-Object Icon | ForEach-Object Name) + @('FlahaGrow_Icon_logo')
foreach ($file in Get-ChildItem -LiteralPath (Join-Path $repository 'docs/Icon') -Filter '*.png') {
    if ($file.BaseName -notin $allowedIcons) { $issues.Add("Orphaned component icon: $($file.Name).") }
}
$factor = @($catalog | Where-Object Type -eq 'LuxToPpfdComponent')[0].Inputs[1]
if ($factor.Optional -or $factor.Defaults.Count) { $issues.Add('Numeric PPFD conversion has an implicit spectral factor.') }

$historical = @(foreach ($directory in @('docs/archive')) {
    $path = Join-Path $repository $directory
    if (Test-Path -LiteralPath $path) {
        [pscustomobject]@{ Path = $directory; Files = @(Get-ChildItem -LiteralPath $path -Recurse -File).Count }
    }
})
$report = [ordered]@{
    GeneratedUtc = [DateTimeOffset]::UtcNow.ToString('o')
    ComponentCount = $catalog.Count
    PortCount = ($catalog | ForEach-Object { $_.Inputs.Count + $_.Outputs.Count } | Measure-Object -Sum).Sum
    ProductionSourceFilesChecked = $sourceFiles.Count
    PluginInventoryPassed = $issues.Count -eq 0
    Issues = @($issues.ToArray())
    HistoricalDirectoriesRemaining = $historical
    PreservedReferenceSource = 'src/Code is intentionally retained by user instruction; it is outside the compiled projects and this production-code scan.'
    RepositoryCleanupComplete = $issues.Count -eq 0 -and $historical.Count -eq 0
    Scope = 'Registration, documentation, source-path portability, tool syntax, icon inventory and explicit numeric factor. Run Core and Setup smoke suites separately; Rhino and physical validation are separate acceptance checks.'
}
$artifacts = Join-Path $repository 'artifacts'
$null = New-Item -ItemType Directory -Path $artifacts -Force
$reportPath = Join-Path $artifacts 'plugin-audit.json'
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
Write-Output "Audit report: $reportPath"
if ($issues.Count) { throw ($issues -join "`n") }
Write-Output "PASS plugin inventory: $($catalog.Count) components; no duplicate/hidden executable registrations, orphaned icons, fixed machine paths or implicit PPFD factor."
if ($historical.Count) { throw 'Approved archive cleanup remains incomplete: docs/archive still exists. See plugin-audit.json.' }
