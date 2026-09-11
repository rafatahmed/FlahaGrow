[CmdletBinding()]
param(
    [string]$RadianceBin = 'C:\Program Files\ladybug_tools\radiance\bin',
    [switch]$Keep
)

$ErrorActionPreference = 'Stop'
$RadianceBin = [IO.Path]::GetFullPath($RadianceBin)
foreach ($tool in 'oconv','rtrace') {
    if (-not (Test-Path -LiteralPath (Join-Path $RadianceBin "$tool.exe"))) {
        throw "Radiance executable was not found: $(Join-Path $RadianceBin "$tool.exe")"
    }
}

$root = Join-Path ([IO.Path]::GetTempPath()) ('FlahaGrow-electric-reference-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$succeeded = $false
try {
    Set-Content -LiteralPath (Join-Path $root 'scene.rad') -Encoding ascii -Value @'
void plastic ground
0
0
5 .2 .2 .2 0 0
ground polygon floor
0
0
12 -10 -10 0 10 -10 0 10 10 0 -10 10 0
void light lamp
0
0
3 1000 1000 1000
lamp sphere source
0
0
4 0 0 4 .1
'@
    Set-Content -LiteralPath (Join-Path $root '0.pts') -Encoding ascii -Value '0 0 1 0 0 1'
    Push-Location -LiteralPath $root
    try {
        # oconv writes binary stdout. cmd redirects that stream byte-for-byte; PowerShell 5 text redirection does not.
        Set-Content -LiteralPath (Join-Path $root 'compile.bat') -Encoding ascii -Value ('@echo off' + [Environment]::NewLine + '"' + (Join-Path $RadianceBin 'oconv.exe') + '" scene.rad > electric.oct' + [Environment]::NewLine + '"' + (Join-Path $RadianceBin 'rtrace.exe') + '" -I+ -h -ab 1 -ad 128 -lw .01 electric.oct < 0.pts > rgb.txt')
        & (Join-Path $env:SystemRoot 'System32\cmd.exe') /d /c 'compile.bat'
        if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath (Join-Path $root 'electric.oct'))) { throw 'oconv did not create the electric octree.' }
        $rgb = Get-Content -LiteralPath (Join-Path $root 'rgb.txt') -Raw
    }
    finally { Pop-Location }
    Write-Verbose ("rtrace RGB: " + ($rgb -join ' '))
    $values = @($rgb -split '\s+' | Where-Object { $_ } | ForEach-Object { [double]::Parse($_, [Globalization.CultureInfo]::InvariantCulture) })
    if ($values.Count -ne 3 -or @($values | Where-Object { $_ -ne $_ -or [double]::IsInfinity($_) -or $_ -lt 0 }).Count -ne 0) { throw 'Electric reference returned invalid RGB output.' }
    $lux = 47.4 * $values[0] + 119.9 * $values[1] + 11.6 * $values[2]
    if ($lux -ne $lux -or [double]::IsInfinity($lux) -or $lux -le 0) { throw "Electric reference returned non-positive illuminance: $lux lux" }
    [pscustomobject]@{ Fixture = 'one upward sensor below a point luminaire'; Sensors = 1; FullOutputLux = $lux; Folder = $(if ($Keep) { $root } else { '<removed after successful validation>' }) } | Format-List
    $succeeded = $true
}
finally {
    if ($succeeded -and -not $Keep -and (Test-Path -LiteralPath $root)) { Remove-Item -LiteralPath $root -Recurse -Force }
}
