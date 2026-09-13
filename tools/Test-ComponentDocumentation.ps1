param([string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$runner = Join-Path $repository "tests/FlahaGrow.SetupSmoke/bin/$Configuration/net7.0-windows/FlahaGrow.SetupSmoke.dll"
if (-not (Test-Path -LiteralPath $runner)) { throw 'Run tools/Test-SetupComponents.ps1 first to prepare the registration runtime.' }
$stampPath = Join-Path (Split-Path -Parent $runner) 'component-validation.json'
if (-not (Test-Path -LiteralPath $stampPath)) { throw 'Missing successful smoke-test stamp. Run tools/Test-SetupComponents.ps1.' }
$stamp = Get-Content -LiteralPath $stampPath -Raw | ConvertFrom-Json
$fingerprint = & (Join-Path $PSScriptRoot 'Get-PluginAuditFingerprint.ps1') -Repository $repository
if ($stamp.SourceFingerprint -ne $fingerprint) { throw 'Source changed since the successful smoke checks. Rebuild with tools/Test-SetupComponents.ps1.' }
foreach ($entry in $stamp.RuntimeHashes.PSObject.Properties) {
    $runtimePath = Join-Path (Split-Path -Parent $runner) $entry.Name
    if ((Get-FileHash -LiteralPath $runtimePath -Algorithm SHA256).Hash -ne $entry.Value) { throw "Validated runtime changed: $($entry.Name)" }
}
$files = @('00-setup', '01-materials', '02-spectral', '03-annual', '04-electric-light', '05-ppfd', '06-dli', '07-energy')

function ConvertTo-Cell([object]$Value) {
    $cell = [string]$Value
    $cell = [regex]::Replace($cell, '[\x00-\x1f]', {
        param($match)
        if ($match.Value -eq "`n" -or $match.Value -eq "`r") { return ' ' }
        return ('\u{0:x4}' -f [int][char]$match.Value)
    })
    return $cell.Replace('|', '\|').Replace('<', '&lt;').Replace('>', '&gt;')
}

$componentCount = 0
$portCount = 0
$referenceIndex = Get-Content -LiteralPath (Join-Path $repository 'docs/components/README.md') -Raw
$toolbarCatalog = Get-Content -LiteralPath (Join-Path $repository 'README.md') -Raw
foreach ($category in $files) {
    $prefix = $category.Substring(0, 2)
    $json = & dotnet $runner --component-catalog $prefix
    if ($LASTEXITCODE -ne 0) { throw "Catalog export failed: $category" }
    $catalog = @((($json -join "`n") | ConvertFrom-Json))
    $categoryRow = [regex]::Match($referenceIndex, '\| \[[^\]]+\]\(' + [regex]::Escape($category) + '\.md\) \| (\d+) \|')
    if (-not $categoryRow.Success -or [int]$categoryRow.Groups[1].Value -ne $catalog.Count) { throw "Index category count mismatch: $category" }
    $path = Join-Path $repository "docs/components/$category.md"
    $markdown = Get-Content -LiteralPath $path -Raw
    $markers = [regex]::Matches($markdown, '<!-- component: ([a-f0-9-]+) -->')
    if ($markers.Count -ne $catalog.Count) { throw "Component count mismatch: $category" }
    foreach ($component in $catalog) {
        if ($component.Exposure -eq 'hidden') { throw "Unexpected hidden component: $($component.Type)" }
        $toolbarRow = '| ' + $component.SubCategory + ' | ' + $component.Name + ' | '
        if (-not $toolbarCatalog.Contains($toolbarRow)) { throw "Missing toolbar entry: $($component.Type)" }
        $marker = "<!-- component: $($component.Guid) -->"
        if (($markers | Where-Object { $_.Value -eq $marker }).Count -ne 1) { throw "Missing/duplicate component: $($component.Type)" }
        $start = $markdown.IndexOf($marker, [StringComparison]::Ordinal)
        $next = $markdown.IndexOf('<a id=', $start, [StringComparison]::Ordinal)
        $section = if ($next -lt 0) { $markdown.Substring($start) } else { $markdown.Substring($start, $next - $start) }
        if (-not $section.Contains($component.Description) -or -not $section.Contains('Revision note: ' + $component.Revision.Change)) {
            throw "Description/revision note mismatch: $($component.Type)"
        }
        foreach ($text in @("``$(ConvertTo-Cell $component.NickName)``", "**$($component.Revision.Version)**")) {
            if (-not $section.Contains($text)) { throw "Metadata mismatch for $($component.Type): $text" }
        }
        $documentDate = [regex]::Match($section, 'Last component update: \*\*([^*]+)\*\*').Groups[1].Value
        if (-not $documentDate -or ([datetimeoffset]$documentDate).UtcDateTime -ne ([datetimeoffset]$component.Revision.UpdatedAt).UtcDateTime) { throw "Revision date mismatch: $($component.Type)" }
        $identity = '<a id="' + $component.Type.ToLowerInvariant() + '"></a>' + "`n`n## " + $component.Name
        if (-not $markdown.Replace("`r`n", "`n").Contains($identity)) { throw "Component name/anchor mismatch: $($component.Type)" }
        $expectedIcon = if ($component.Icon) { 'Icon available: Yes' } else { 'Icon available: No' }
        if (-not $section.Contains($expectedIcon)) { throw "Icon mismatch: $($component.Type)" }
        $split = $section -split '### Outputs', 2
        foreach ($direction in @('Inputs', 'Outputs')) {
            $portSection = if ($direction -eq 'Inputs') { $split[0] } else { ($split[1] -split '### Workflow description', 2)[0] }
            $rows = @($portSection -split "`n" | Where-Object { $_ -match '^\| \d+ \|' })
            $ports = @($component.$direction)
            if ($rows.Count -ne $ports.Count) { throw "Port count mismatch: $($component.Type) $direction" }
            foreach ($port in $ports) {
                $prefixCells = @($port.Index, $port.NickName, $port.Name, $port.ParameterType, $port.Access) | ForEach-Object { ConvertTo-Cell $_ }
                $expected = '| ' + ($prefixCells -join ' | ') + ' | '
                $row = $rows[$port.Index]
                if (-not $row.StartsWith($expected, [StringComparison]::Ordinal)) { throw "Port mismatch: $($component.Type) $direction $($port.Index)" }
                if (-not $row.Contains((ConvertTo-Cell $port.Description))) { throw "Description mismatch: $($component.Type) $($port.Name)" }
                if ($direction -eq 'Inputs') {
                    $required = if ($port.Optional) { 'Optional registration; see workflow' } elseif ($port.Defaults.Count) { 'Required registration; default supplied' } else { 'Required registration' }
                    $default = if ($port.Defaults.Count) { ConvertTo-Cell ($port.Defaults -join '; ') } else { 'None registered' }
                    if (-not $row.StartsWith($expected + $required + ' | ' + $default + ' | ', [StringComparison]::Ordinal)) { throw "Required/default mismatch: $($component.Type) $($port.Name)" }
                }
                $portCount++
            }
        }
        foreach ($heading in @('### Short description', '### Inputs', '### Outputs', '### Workflow description', '### Notes', '### Version + last update')) {
            if (-not $section.Contains($heading)) { throw "Missing $heading for $($component.Type)" }
        }
        $componentCount++
    }
    Write-Output "$category : $($catalog.Count) components checked"
}

$summaryCount = [regex]::Match($referenceIndex, '\*\*(\d+) components\*\*')
if (-not $summaryCount.Success -or [int]$summaryCount.Groups[1].Value -ne $componentCount) { throw 'Reference summary component count mismatch.' }
$toolbarRows = [regex]::Matches($toolbarCatalog, '(?m)^\| 0[0-7] [^|]+ \|')
if ($toolbarRows.Count -ne $componentCount) { throw 'Toolbar catalog contains extra or missing component rows.' }

$documents = @(Get-ChildItem -LiteralPath (Join-Path $repository 'docs/components') -Filter '*.md' | ForEach-Object { $_.FullName })
foreach ($path in $documents) {
    $markdown = Get-Content -LiteralPath $path -Raw
    foreach ($match in [regex]::Matches($markdown, '\[[^\]]+\]\(([^)]+)\)')) {
        $target = $match.Groups[1].Value
        if ($target -match '^(https?:|#)') { continue }
        $relativePath = ($target -split '#', 2)[0]
        $resolved = Join-Path (Split-Path -Parent $path) ([Uri]::UnescapeDataString($relativePath))
        if (-not (Test-Path -LiteralPath $resolved)) { throw "Broken link in ${path}: $target" }
    }
}
Write-Output "PASS: $componentCount components, $portCount input/output ports, eight categories, metadata and local file links. Semantic/scientific notes still require source review."
