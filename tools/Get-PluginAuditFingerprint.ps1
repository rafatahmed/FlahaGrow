param([string]$Repository = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
$files = @(foreach ($directory in @('src/FlahaGrow.Core', 'src/FlahaGrow.Grasshopper', 'tests/FlahaGrow.SetupSmoke')) {
    Get-ChildItem -LiteralPath (Join-Path $Repository $directory) -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' -and $_.Extension -in '.cs', '.csproj', '.props', '.targets' }
})
$files += @(Get-ChildItem -LiteralPath (Join-Path $Repository 'docs/Icon') -Filter '*.png')
foreach ($relative in @('Directory.Build.props', 'Directory.Build.targets', 'global.json', 'FlahaGrow.sln',
    'docs/research/plant-light/profile-audit.json', 'docs/research/plant-light/data/cie/CIE_sle_photopic.csv',
    'docs/research/plant-light/data/cie/CIE_sle_photopic.csv_metadata.json')) {
    $path = Join-Path $Repository $relative
    if (Test-Path -LiteralPath $path) { $files += Get-Item -LiteralPath $path }
}
$records = @($files | Sort-Object FullName -Unique | ForEach-Object {
    [IO.Path]::GetRelativePath($Repository, $_.FullName).Replace('\', '/') + ':' + (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
})
[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes(($records -join "`n"))))
